-- FRMS EMAIL_SERVICE / Pg014. Add event-to-NotificationLog hooks without new entities.
-- EMAIL EVENT PRODUCTION GATE (default OFF):
-- ALTER ROLE <app_db_role> IN DATABASE <frms_db> SET frms.email_events_enabled = 'on';
-- Restart application connections after changing role/database settings.
-- Disable: ALTER ROLE ... RESET frms.email_events_enabled;
-- Notification delivery is a SEPARATE application flag, OFF by default.
-- No old data is replayed or backfilled by this migration.
-- All contents are generic transactional notices; no personal details or credentials.

CREATE OR REPLACE FUNCTION public.frms_email_events_enabled()
RETURNS boolean LANGUAGE sql STABLE AS $frms$
    SELECT coalesce(current_setting('frms.email_events_enabled', true) = 'on', false);
$frms$;

CREATE OR REPLACE FUNCTION public.frms_email_queue_customer_event(
    p_customer_id uuid, p_type varchar, p_content varchar)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_user_account_id uuid;
BEGIN
    IF NOT public.frms_email_events_enabled() OR p_customer_id IS NULL THEN RETURN; END IF;
    SELECT c."UserAccountId" INTO v_user_account_id
    FROM public."Customer" c
    JOIN public."UserAccount" u ON u."UserAccountId" = c."UserAccountId"
    WHERE c."CustomerId" = p_customer_id AND u."Status" = 'ACTIVE';
    IF FOUND THEN
        -- Queue and business event commit or roll back in the same transaction.
        -- No provider call or plaintext credential is made here.
        PERFORM public."usp_QueueNotification"(v_user_account_id, p_type, p_content);
    END IF;
END $frms$;

CREATE OR REPLACE FUNCTION public.frms_email_notify_reservation()
RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
    IF OLD."Status" IS DISTINCT FROM NEW."Status" THEN
        IF NEW."Status" = 'CONFIRMED' THEN
            PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'RESERVATION_CONFIRMED',
                'Your reservation is confirmed. See your account for details.');
        ELSIF NEW."Status" = 'CANCELLED' THEN
            PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'RESERVATION_CANCELLED',
                'Your reservation has been cancelled. See your account for details.');
        END IF;
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Email_Reservation_Status"
AFTER UPDATE OF "Status" ON public."Reservation"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_reservation();

-- Invoice EntityId is polymorphic: DEPOSIT -> Reservation; RENTAL_FEE -> Contract.
CREATE OR REPLACE FUNCTION public.frms_email_notify_invoice()
RETURNS trigger LANGUAGE plpgsql AS $frms$
DECLARE v_customer_id uuid;
BEGIN
    IF NEW."InvoiceType" = 'DEPOSIT' THEN
        SELECT r."CustomerId" INTO v_customer_id FROM public."Reservation" r
        WHERE r."ReservationId" = NEW."EntityId";
    ELSIF NEW."InvoiceType" = 'RENTAL_FEE' THEN
        SELECT c."CustomerId" INTO v_customer_id FROM public."Contract" c
        WHERE c."ContractId" = NEW."EntityId";
    ELSE RETURN NEW;
    END IF;

    IF TG_OP = 'INSERT' THEN
        IF NEW."Status" IN ('UNPAID','OVERDUE') THEN
            PERFORM public.frms_email_queue_customer_event(v_customer_id,
                CASE WHEN NEW."InvoiceType" = 'DEPOSIT' THEN 'DEPOSIT_INVOICE_CREATED'
                     ELSE 'RENTAL_INVOICE_CREATED' END,
                'A new invoice is available in your account. Review the due date and amount there.');
        END IF;
    ELSIF OLD."Status" IS DISTINCT FROM NEW."Status" THEN
        IF NEW."Status" = 'PAID' THEN
            -- Authoritative invoice transition is used, not browser redirects.
            PERFORM public.frms_email_queue_customer_event(v_customer_id, 'PAYMENT_CONFIRMED',
                'A payment has been confirmed. See your account for the invoice receipt.');
        ELSIF NEW."Status" = 'OVERDUE' THEN
            PERFORM public.frms_email_queue_customer_event(v_customer_id, 'INVOICE_OVERDUE',
                'An invoice is overdue. Review your account for any applicable late fee.');
        ELSIF NEW."Status" = 'CANCELLED' THEN
            PERFORM public.frms_email_queue_customer_event(v_customer_id, 'INVOICE_CANCELLED',
                'An invoice has been cancelled. Review your account for details.');
        END IF;
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Email_Invoice_Insert"
AFTER INSERT ON public."Invoice"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_invoice();
CREATE TRIGGER "trg_Email_Invoice_Status"
AFTER UPDATE OF "Status" ON public."Invoice"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_invoice();

CREATE OR REPLACE FUNCTION public.frms_email_notify_visit()
RETURNS trigger LANGUAGE plpgsql AS $frms$
DECLARE v_customer_id uuid; v_event varchar(50); v_content varchar(1000);
BEGIN
    IF NEW."VisitType" = 'RESERVATION' THEN
        SELECT r."CustomerId" INTO v_customer_id FROM public."Reservation" r
        WHERE r."ReservationId" = NEW."EntityId";
    ELSIF NEW."VisitType" IN ('ACCESS','RETURN') THEN
        SELECT c."CustomerId" INTO v_customer_id FROM public."Contract" c
        WHERE c."ContractId" = NEW."EntityId";
    ELSE RETURN NEW;
    END IF;
    IF TG_OP = 'INSERT' THEN
        IF NEW."Status" <> 'SCHEDULED' THEN RETURN NEW; END IF;
        v_event := 'VISIT_SCHEDULED';
        v_content := 'A visit has been scheduled. See your account for the date.';
    ELSIF OLD."Status" IS DISTINCT FROM NEW."Status" THEN
        CASE NEW."Status"
            WHEN 'CHECKED_IN' THEN v_event := 'VISIT_CHECKED_IN';
            WHEN 'CHECKED_OUT' THEN v_event := 'VISIT_CHECKED_OUT';
            WHEN 'CANCELLED' THEN v_event := 'VISIT_CANCELLED';
            ELSE RETURN NEW;
        END CASE;
        v_content := 'A visit status has changed. See your account for details.';
    ELSIF OLD."VisitDate" IS DISTINCT FROM NEW."VisitDate" THEN
        v_event := 'VISIT_RESCHEDULED';
        v_content := 'A visit date has changed. See your account for the updated schedule.';
    ELSE RETURN NEW;
    END IF;
    PERFORM public.frms_email_queue_customer_event(v_customer_id, v_event, v_content);
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Email_Visit_Insert"
AFTER INSERT ON public."Visit"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_visit();
CREATE TRIGGER "trg_Email_Visit_Updates"
AFTER UPDATE OF "Status", "VisitDate" ON public."Visit"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_visit();

CREATE OR REPLACE FUNCTION public.frms_email_notify_contract()
RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
    IF TG_OP = 'INSERT' THEN
        PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'CONTRACT_CREATED',
            'Your rental contract is active. See your account for the contract details.');
    ELSIF OLD."Status" IS DISTINCT FROM NEW."Status" AND
          NEW."Status" IN ('COMPLETED','TERMINATED') THEN
        PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'CONTRACT_CLOSED',
            'Your rental contract status has changed. See your account for details.');
    ELSIF OLD."EndMonth" IS DISTINCT FROM NEW."EndMonth" AND
          NEW."EndMonth" > OLD."EndMonth" THEN
        PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'CONTRACT_RENEWED',
            'Your rental contract has been extended. See your account for the new end month.');
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Email_Contract_Insert"
AFTER INSERT ON public."Contract"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_contract();
CREATE TRIGGER "trg_Email_Contract_Updates"
AFTER UPDATE OF "Status", "EndMonth" ON public."Contract"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_contract();

CREATE OR REPLACE FUNCTION public.frms_email_notify_support_ticket()
RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
    IF TG_OP = 'INSERT' THEN
        PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'SUPPORT_TICKET_CREATED',
            'Your support request has been recorded. Check your account for progress.');
    ELSIF OLD."Status" IS DISTINCT FROM NEW."Status" THEN
        PERFORM public.frms_email_queue_customer_event(NEW."CustomerId", 'SUPPORT_TICKET_STATUS',
            'Your support request status has changed. Check your account for details.');
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Email_SupportTicket_Insert"
AFTER INSERT ON public."SupportTicket"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_support_ticket();
CREATE TRIGGER "trg_Email_SupportTicket_Status"
AFTER UPDATE OF "Status" ON public."SupportTicket"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_support_ticket();

CREATE OR REPLACE FUNCTION public.frms_email_notify_settlement()
RETURNS trigger LANGUAGE plpgsql AS $frms$
DECLARE v_customer_id uuid;
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF NEW."Status" <> 'FINALIZED' THEN RETURN NEW; END IF;
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD."Status" IS NOT DISTINCT FROM NEW."Status" OR
           NEW."Status" <> 'FINALIZED' THEN RETURN NEW; END IF;
    ELSE RETURN NEW;
    END IF;
    SELECT c."CustomerId" INTO v_customer_id FROM public."Contract" c
    WHERE c."ContractId" = NEW."ContractId";
    PERFORM public.frms_email_queue_customer_event(v_customer_id, 'DEPOSIT_SETTLEMENT_FINALIZED',
        'Your deposit settlement is finalized. Review the statement in your account.');
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Email_Settlement_Insert"
AFTER INSERT ON public."DepositSettlement"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_settlement();
CREATE TRIGGER "trg_Email_Settlement_Status"
AFTER UPDATE OF "Status" ON public."DepositSettlement"
FOR EACH ROW EXECUTE FUNCTION public.frms_email_notify_settlement();
