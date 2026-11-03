-- DBT-RES005: PostgreSQL-only rollback-safe fixture; run on a DISPOSABLE
-- migrated test database with psql -v ON_ERROR_STOP=1 -f <this file>.
-- No data is retained. Do NOT execute this against production.
BEGIN;
SET LOCAL frms.email_events_enabled = 'off';
DO $test$
DECLARE
 v_account uuid:=gen_random_uuid();
 v_customer uuid:=gen_random_uuid();
 v_facility uuid:=gen_random_uuid();
 v_type uuid:=gen_random_uuid();
 v_unpaid_res uuid:=gen_random_uuid();
 v_unpaid_invoice uuid:=gen_random_uuid();
 v_paid_res uuid:=gen_random_uuid();
 v_paid_invoice uuid:=gen_random_uuid();
 v_role uuid;
 v_policy uuid;
 v_paid_at timestamptz(3):=clock_timestamp();
 v_status varchar;
 v_res_status varchar;
 v_paid_at_after timestamptz;
 v_payment uuid:=gen_random_uuid();
 v_payment_status varchar;
 v_outcome text;
 v_reason text;
BEGIN
 SELECT "RoleId" INTO v_role FROM public."UserRole" WHERE "RoleName"='CUSTOMER';
 SELECT "PolicyId" INTO v_policy FROM public."Policy" ORDER BY "Version" DESC LIMIT 1;
 IF v_role IS NULL OR v_policy IS NULL THEN
  RAISE EXCEPTION 'Test requires migrated master roles and a policy';
 END IF;

 INSERT INTO public."UserAccount"("UserAccountId","RoleId","Email","PhoneNumber","PasswordHash","Status")
 VALUES(v_account,v_role,v_account::text||'@example.invalid',substr(replace(v_account::text,'-',''),1,20),'fixture-not-a-password','ACTIVE');
 INSERT INTO public."Customer"("CustomerId","UserAccountId","FullName")
 VALUES(v_customer,v_account,'RES005 transaction test');
 INSERT INTO public."Facility"("FacilityId","Name","Address","Status")
 VALUES(v_facility,'RES005 fixture '||v_facility::text,'Disposable only','ACTIVE');
 INSERT INTO public."UnitType"("UnitTypeId","Name","Mode","Size","RentalPrice")
 VALUES(v_type,'RES005 fixture '||v_type::text,'PUBLIC','test',12500.00);

 INSERT INTO public."Reservation"("ReservationId","CustomerId","FacilityId","UnitTypeId","PolicyId","StartMonth","EndMonth","LockedRentalPrice","DepositAmount","Status")
 VALUES(v_unpaid_res,v_customer,v_facility,v_type,v_policy,DATE '2027-01-01',DATE '2027-01-01',12500.00,12500.00,'PENDING_DEPOSIT');
 INSERT INTO public."Invoice"("InvoiceId","EntityId","InvoiceType","BillingMonth","BaseAmount","DiscountAmount","AmountDue","DueDate","Status")
 VALUES(v_unpaid_invoice,v_unpaid_res,'DEPOSIT',NULL,12500.00,0,12500.00,clock_timestamp() + INTERVAL '1 day','UNPAID');

 -- Existing payOS payment attempt, initiated BEFORE the Reservation cancellation.
 INSERT INTO public."Payment"("PaymentId","InvoiceId","IdempotencyKey","Amount","PaymentMethod","Status")
 VALUES(v_payment,v_unpaid_invoice,gen_random_uuid(),12500.00,'PAYOS','PENDING');

 -- Scenario 1: unpaid Deposit invoice must be cancelled together with Reservation.
 PERFORM public."usp_CancelReservation"(v_account,v_customer,v_unpaid_res);
 SELECT "Status" INTO v_status FROM public."Invoice" WHERE "InvoiceId"=v_unpaid_invoice;
 SELECT "Status" INTO v_res_status FROM public."Reservation" WHERE "ReservationId"=v_unpaid_res;
 IF v_status <> 'CANCELLED' OR v_res_status <> 'CANCELLED' THEN
  RAISE EXCEPTION 'Failed to cancel unpaid Deposit: Reservation=%, Invoice=%',v_res_status,v_status;
 END IF;

 -- Scenario 2: cancellation retry is idempotent and never reopens the invoice.
 PERFORM public."usp_CancelReservation"(v_account,v_customer,v_unpaid_res);
 SELECT "Status" INTO v_status FROM public."Invoice" WHERE "InvoiceId"=v_unpaid_invoice;
 IF v_status <> 'CANCELLED' THEN RAISE EXCEPTION 'Retry changed cancelled Deposit invoice'; END IF;

 -- Scenario 3: a verified late-success callback records Payment SUCCESS but
 -- never reopens the cancelled Invoice (the existing financial contract).
 SELECT "Outcome","Reason" INTO v_outcome,v_reason
 FROM public."usp_ApplyPaymentResult"(v_payment,'SUCCESS','late-'||v_payment::text,
    12500.00,clock_timestamp(),'VERIFIED_CALLBACK');
 SELECT "Status" INTO v_status FROM public."Invoice" WHERE "InvoiceId"=v_unpaid_invoice;
 SELECT "Status" INTO v_payment_status FROM public."Payment" WHERE "PaymentId"=v_payment;
 IF v_outcome <> 'Applied' OR v_reason <> 'LATE_SUCCESS_ON_CANCELLED_INVOICE'
    OR v_status <> 'CANCELLED' OR v_payment_status <> 'SUCCESS' THEN
  RAISE EXCEPTION 'Late payment mismatch: outcome=%, reason=%, invoice=%, payment=%',
   v_outcome,v_reason,v_status,v_payment_status;
 END IF;

 -- Scenario 4: an already-PAID Deposit and its PaidAt timestamp are immutable.
 INSERT INTO public."Reservation"("ReservationId","CustomerId","FacilityId","UnitTypeId","PolicyId","StartMonth","EndMonth","LockedRentalPrice","DepositAmount","Status")
 VALUES(v_paid_res,v_customer,v_facility,v_type,v_policy,DATE '2027-02-01',DATE '2027-02-01',12500.00,12500.00,'CONFIRMED');
 INSERT INTO public."Invoice"("InvoiceId","EntityId","InvoiceType","BillingMonth","BaseAmount","DiscountAmount","AmountDue","DueDate","Status","PaidAt")
 VALUES(v_paid_invoice,v_paid_res,'DEPOSIT',NULL,12500.00,0,12500.00,clock_timestamp(), 'PAID',v_paid_at);
 PERFORM public."usp_CancelReservation"(v_account,v_customer,v_paid_res);
 SELECT "Status","PaidAt" INTO v_status,v_paid_at_after FROM public."Invoice" WHERE "InvoiceId"=v_paid_invoice;
 IF v_status <> 'PAID' OR v_paid_at_after IS DISTINCT FROM v_paid_at THEN
  RAISE EXCEPTION 'Cancellation modified PAID Deposit or its financial timestamp';
 END IF;

 -- Scenario 5: not-owner must fail and perform no writes.
 BEGIN
  PERFORM public."usp_CancelReservation"(v_account,gen_random_uuid(),v_paid_res);
  RAISE EXCEPTION 'Expected RESOURCE_NOT_FOUND for wrong customer';
 EXCEPTION WHEN SQLSTATE 'P1201' THEN NULL;
 END;

 RAISE NOTICE 'DBT-RES005: 5 deterministic scenarios passed; entire fixture will roll back';
END $test$;
ROLLBACK;
