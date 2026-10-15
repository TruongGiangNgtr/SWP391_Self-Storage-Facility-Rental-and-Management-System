-- Source: restored usp_ExpirePendingReservations. Strict DueDate < Now;
-- do not derive deadlines from a newer Policy or allocate a StorageUnit.
CREATE OR REPLACE FUNCTION public."usp_ExpirePendingReservations"(p_now_utc timestamptz DEFAULT clock_timestamp())
RETURNS TABLE("ExpiredReservationCount" integer)
LANGUAGE plpgsql AS $frms$
DECLARE
    v_id uuid;
    v_reservation public."Reservation"%ROWTYPE;
    v_invoice public."Invoice"%ROWTYPE;
    v_count integer := 0;
BEGIN
    PERFORM pg_advisory_xact_lock(70439102);
    p_now_utc := coalesce(p_now_utc,clock_timestamp())::timestamptz(3);
    FOR v_id IN
        SELECT r."ReservationId" FROM public."Reservation" r
        JOIN public."Invoice" i ON i."EntityId"=r."ReservationId" AND i."InvoiceType"='DEPOSIT'
        WHERE r."Status"='PENDING_DEPOSIT' AND i."Status"='UNPAID' AND i."DueDate"<p_now_utc
        ORDER BY r."ReservationId"
    LOOP
        -- Parent then Invoice; re-check after locks so a payment winner is not
        -- cancelled from a stale selection. UUID ordering avoids batch cycles.
        SELECT * INTO v_reservation FROM public."Reservation" WHERE "ReservationId"=v_id FOR UPDATE;
        SELECT * INTO v_invoice FROM public."Invoice" WHERE "EntityId"=v_id AND "InvoiceType"='DEPOSIT' FOR UPDATE;
        IF v_reservation."Status"='PENDING_DEPOSIT' AND v_invoice."Status"='UNPAID' AND v_invoice."DueDate"<p_now_utc THEN
            UPDATE public."Invoice" AS target SET "Status"='CANCELLED' WHERE target."InvoiceId"=v_invoice."InvoiceId";
            UPDATE public."Reservation" AS target SET "Status"='CANCELLED' WHERE target."ReservationId"=v_id;
            v_count := v_count+1;
        END IF;
    END LOOP;
    RETURN QUERY SELECT v_count;
END;
$frms$;
