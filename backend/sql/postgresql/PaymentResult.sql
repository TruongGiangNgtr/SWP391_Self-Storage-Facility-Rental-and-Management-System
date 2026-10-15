-- Port of the frozen InvoicePaymentTimestamp routine. One statement owns
-- Payment + Invoice + safe audit atomically; no provider calls occur here.
CREATE OR REPLACE FUNCTION public."usp_ApplyPaymentResult"(
    p_payment_id uuid, p_status varchar, p_transaction_code varchar,
    p_verified_amount numeric, p_verified_paid_at timestamptz, p_source varchar)
RETURNS TABLE("Outcome" text, "Reason" text)
LANGUAGE plpgsql AS $frms$
DECLARE
    v_payment public."Payment"%ROWTYPE;
    v_invoice public."Invoice"%ROWTYPE;
    v_reason text;
    v_mismatch text;
    v_old text;
    v_new text;
    v_audit_context text;
BEGIN
    IF p_status IS NULL OR p_status NOT IN ('SUCCESS','FAILED')
       OR p_source IS NULL OR p_source NOT IN ('VERIFIED_CALLBACK','DEFINITIVE_PRE_SESSION_FAILURE')
       OR (p_source='DEFINITIVE_PRE_SESSION_FAILURE' AND p_status<>'FAILED')
       OR (p_source='VERIFIED_CALLBACK' AND nullif(btrim(p_transaction_code),'') IS NULL)
       OR (p_transaction_code IS NOT NULL AND nullif(btrim(p_transaction_code),'') IS NULL)
       OR (p_status='SUCCESS' AND (p_verified_amount IS NULL OR p_verified_paid_at IS NULL)) THEN
        RETURN QUERY SELECT 'InvalidResult'::text,NULL::text;
        RETURN;
    END IF;
    -- Function parameter typmods do not enforce timestamp precision. Normalize
    -- before comparison so an exact repeated callback matches its stored value.
    p_verified_paid_at := p_verified_paid_at::timestamptz(3);
    PERFORM pg_advisory_xact_lock(70439101);
    SELECT * INTO v_payment FROM public."Payment" WHERE "PaymentId"=p_payment_id;
    IF NOT FOUND THEN
        RETURN QUERY SELECT 'NotFound'::text,NULL::text; RETURN;
    END IF;
    -- Invoice then Payment, shared with repository attempt creation.
    SELECT * INTO v_invoice FROM public."Invoice" WHERE "InvoiceId"=v_payment."InvoiceId" FOR UPDATE;
    IF NOT FOUND THEN
        RETURN QUERY SELECT 'NotFound'::text,NULL::text; RETURN;
    END IF;
    SELECT * INTO v_payment FROM public."Payment" WHERE "PaymentId"=p_payment_id FOR UPDATE;
    IF NOT FOUND THEN
        RETURN QUERY SELECT 'NotFound'::text,NULL::text; RETURN;
    END IF;
    IF v_payment."Status" IN ('SUCCESS','FAILED') THEN
        IF v_payment."Status"=p_status
           AND v_payment."TransactionCode" IS NOT DISTINCT FROM p_transaction_code
           AND (p_status='FAILED' OR (v_payment."Amount"=p_verified_amount AND v_payment."PaidAt"=p_verified_paid_at)) THEN
            RETURN QUERY SELECT 'Duplicate'::text,'PAYMENT_CALLBACK_DUPLICATE'::text;
        ELSE
            RETURN QUERY SELECT 'TerminalConflict'::text,'PAYMENT_RESULT_TERMINAL_CONFLICT'::text;
        END IF;
        RETURN;
    END IF;
    IF p_transaction_code IS NOT NULL AND EXISTS(
        SELECT 1 FROM public."Payment" WHERE "TransactionCode"=p_transaction_code AND "PaymentId"<>p_payment_id) THEN
        RETURN QUERY SELECT 'ReferenceConflict'::text,'PAYMENT_REFERENCE_CONFLICT'::text; RETURN;
    END IF;
    IF p_source='DEFINITIVE_PRE_SESSION_FAILURE' AND v_payment."PaymentUrl" IS NOT NULL THEN
        RETURN QUERY SELECT 'InvalidResult'::text,NULL::text; RETURN;
    END IF;
    IF p_status='SUCCESS' AND v_payment."Amount"<>p_verified_amount THEN
        v_reason := 'PAYMENT_AMOUNT_MISMATCH';
        v_mismatch := jsonb_build_object('Status',v_payment."Status",'Reason',v_reason,
            'VerifiedAmount',p_verified_amount,'TransactionCode',p_transaction_code)::text;
        IF NOT EXISTS(SELECT 1 FROM public."AuditLog" WHERE "EntityId"=p_payment_id
            AND "EntityType"='PAYMENT' AND "Action"='PAYMENT_RESULT' AND "NewValue"=v_mismatch) THEN
            INSERT INTO public."AuditLog"("AuditLogId","UserAccountId","Action","EntityType","EntityId","OldValue","NewValue","CreatedAt")
            VALUES(gen_random_uuid(),NULL,'PAYMENT_RESULT','PAYMENT',p_payment_id,
                '{"Status":"PENDING"}',v_mismatch,clock_timestamp());
        END IF;
        RETURN QUERY SELECT 'AmountMismatch'::text,v_reason; RETURN;
    END IF;
    v_old := jsonb_build_object('Status',v_payment."Status",'InvoiceStatus',v_invoice."Status")::text;
    v_audit_context := coalesce(current_setting('frms.payment_result',true),'');
    PERFORM set_config('frms.payment_result','1',true);
    UPDATE public."Payment" AS target SET "Status"=p_status,"TransactionCode"=p_transaction_code,
        "PaidAt"=CASE WHEN p_status='SUCCESS' THEN p_verified_paid_at ELSE NULL END WHERE target."PaymentId"=p_payment_id;
    PERFORM set_config('frms.payment_result',v_audit_context,true);
    IF p_status='SUCCESS' THEN
        IF v_invoice."Status" IN ('UNPAID','OVERDUE') THEN
            UPDATE public."Invoice" AS target SET "Status"='PAID',"PaidAt"=p_verified_paid_at WHERE target."InvoiceId"=v_invoice."InvoiceId";
        ELSIF v_invoice."Status"='CANCELLED' THEN
            v_reason := 'LATE_SUCCESS_ON_CANCELLED_INVOICE';
        END IF;
    END IF;
    v_new := jsonb_strip_nulls(jsonb_build_object('Status',p_status,'InvoiceStatus',
        CASE WHEN p_status='SUCCESS' AND v_invoice."Status" IN ('UNPAID','OVERDUE') THEN 'PAID' ELSE v_invoice."Status" END,
        'Reason',v_reason,'Source',p_source))::text;
    INSERT INTO public."AuditLog"("AuditLogId","UserAccountId","Action","EntityType","EntityId","OldValue","NewValue","CreatedAt")
        VALUES(gen_random_uuid(),NULL,'PAYMENT_RESULT','PAYMENT',p_payment_id,v_old,v_new,clock_timestamp());
    RETURN QUERY SELECT 'Applied'::text,v_reason;
EXCEPTION WHEN unique_violation THEN
    -- PL/pgSQL rolls back this block's writes before entering the handler.
    RETURN QUERY SELECT 'ReferenceConflict'::text,'PAYMENT_REFERENCE_CONFLICT'::text;
END;
$frms$;
