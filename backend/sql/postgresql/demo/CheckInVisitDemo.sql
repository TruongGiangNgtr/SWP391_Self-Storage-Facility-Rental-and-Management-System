-- DEMO ONLY behavior, installed by Pg011 PostgreSQL migration.
-- Production API remains bound to usp_CheckInVisit; the demo route is gated by
-- Development + Demo:AllowOutOfDateVisitCheckIn. Prefer isolated demo databases.
-- Port of SQL Server usp_CheckInVisitDemo (20261011171000_CreateDemoVisitCheckInProcedure).
-- The *only* relaxed validation is VisitDate = current Asia/Ho_Chi_Minh date.
-- It preserves Staff/Facility authorization, lifecycle, row locks and audit triggers.
-- Requires existing Pg001-Pg009 schema; Pg010 may also be present.

CREATE OR REPLACE FUNCTION public."usp_CheckInVisitDemo"(
    p_visit_id uuid,
    p_employee_id uuid,
    p_now_utc timestamptz DEFAULT NULL)
RETURNS void
LANGUAGE plpgsql
AS $frms_demo$
DECLARE
    v_route public."Visit"%ROWTYPE;
    v_visit public."Visit"%ROWTYPE;
    v_facility uuid;
    v_status varchar;
BEGIN
    PERFORM pg_advisory_xact_lock(70439102);

    SELECT * INTO v_route
    FROM public."Visit" v WHERE v."VisitId" = p_visit_id;
    IF NOT FOUND OR v_route."VisitType" NOT IN ('RESERVATION', 'ACCESS', 'RETURN') THEN
        RAISE EXCEPTION USING ERRCODE = 'P1301', MESSAGE = 'VISIT_ENTITY_MISMATCH';
    END IF;

    IF v_route."VisitType" = 'RESERVATION' THEN
        SELECT r."FacilityId", r."Status" INTO v_facility, v_status
        FROM public."Reservation" r
        WHERE r."ReservationId" = v_route."EntityId" FOR UPDATE;
    ELSE
        SELECT c."FacilityId", c."Status" INTO v_facility, v_status
        FROM public."Contract" c
        WHERE c."ContractId" = v_route."EntityId" FOR UPDATE;
    END IF;
    IF v_facility IS NULL THEN
        RAISE EXCEPTION USING ERRCODE = 'P1301', MESSAGE = 'VISIT_ENTITY_MISMATCH';
    END IF;

    SELECT * INTO v_visit
    FROM public."Visit" v WHERE v."VisitId" = p_visit_id FOR UPDATE;
    IF NOT FOUND OR ROW(v_visit."EntityId", v_visit."VisitType")
        IS DISTINCT FROM ROW(v_route."EntityId", v_route."VisitType") THEN
        RAISE EXCEPTION USING ERRCODE = 'P1301', MESSAGE = 'VISIT_ENTITY_MISMATCH';
    END IF;

    PERFORM 1
    FROM public."Employee" e
    JOIN public."UserAccount" a ON a."UserAccountId" = e."UserAccountId"
    JOIN public."UserRole" r ON r."RoleId" = a."RoleId"
    WHERE e."EmployeeId" = p_employee_id
      AND e."FacilityId" = v_facility
      AND a."Status" = 'ACTIVE'
      AND r."RoleName" = 'FACILITY_STAFF'
    FOR SHARE OF e, a, r;
    IF NOT FOUND THEN
        RAISE EXCEPTION USING ERRCODE = 'P1302', MESSAGE = 'FORBIDDEN';
    END IF;

    -- Same checks as usp_CheckInVisit, except the *date equality* restriction.
    -- VisitDate itself is never changed.
    IF v_visit."Status" <> 'SCHEDULED'
        OR (v_visit."VisitType" = 'RESERVATION' AND v_status <> 'CONFIRMED')
        OR (v_visit."VisitType" IN ('ACCESS', 'RETURN') AND v_status <> 'ACTIVE') THEN
        RAISE EXCEPTION USING ERRCODE = 'P1303', MESSAGE = 'VISIT_INVALID_STATUS';
    END IF;

    UPDATE public."Visit" AS target
    SET "Status" = 'CHECKED_IN', "EmployeeId" = p_employee_id
    WHERE target."VisitId" = p_visit_id AND target."Status" = 'SCHEDULED';
    IF NOT FOUND THEN
        RAISE EXCEPTION USING ERRCODE = 'P1303', MESSAGE = 'VISIT_INVALID_STATUS';
    END IF;
END
$frms_demo$;

COMMENT ON FUNCTION public."usp_CheckInVisitDemo"(uuid, uuid, timestamptz)
IS 'DEMO ONLY; skips scheduled date equality, not lifecycle/auth checks. Never production.';
