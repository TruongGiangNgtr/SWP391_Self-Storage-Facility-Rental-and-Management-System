-- MWP-01 persisted Manager assignment. Executed only by Pg013 after Pg012.
-- Does NOT change Reservation capacity holds or StorageUnit occupancy state.
CREATE TABLE public."HandoverUnitAssignment" (
    "AssignmentId" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ReservationId" uuid NOT NULL REFERENCES public."Reservation"("ReservationId") ON DELETE RESTRICT,
    "StorageUnitId" uuid NOT NULL REFERENCES public."StorageUnit"("StorageUnitId") ON DELETE RESTRICT,
    "AssignedByEmployeeId" uuid NOT NULL REFERENCES public."Employee"("EmployeeId") ON DELETE RESTRICT,
    "AssignedAt" timestamptz(3) NOT NULL DEFAULT clock_timestamp(),
    "Status" varchar(12) NOT NULL DEFAULT 'ACTIVE',
    "ClosedAt" timestamptz(3),
    CONSTRAINT "PK_HandoverUnitAssignment" PRIMARY KEY ("AssignmentId"),
    CONSTRAINT "CK_HandoverUnitAssignment_Status" CHECK ("Status" IN ('ACTIVE','REPLACED','CONSUMED','RELEASED')),
    CONSTRAINT "CK_HandoverUnitAssignment_ClosedAt" CHECK (("Status"='ACTIVE') = ("ClosedAt" IS NULL))
);
CREATE UNIQUE INDEX "UX_HandoverUnitAssignment_ActiveReservation"
    ON public."HandoverUnitAssignment"("ReservationId") WHERE "Status"='ACTIVE';
CREATE UNIQUE INDEX "UX_HandoverUnitAssignment_ActiveUnit"
    ON public."HandoverUnitAssignment"("StorageUnitId") WHERE "Status"='ACTIVE';
CREATE INDEX "IX_HandoverUnitAssignment_History"
    ON public."HandoverUnitAssignment"("ReservationId", "AssignedAt" DESC);

CREATE OR REPLACE FUNCTION public.frms_handover_assignment_history_guard()
RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
    IF TG_OP='DELETE' THEN
        RAISE EXCEPTION USING ERRCODE='P1140', MESSAGE='HANDOVER_ASSIGNMENT_IMMUTABLE';
    END IF;
    IF OLD."Status"<>'ACTIVE' OR NEW."Status" NOT IN ('REPLACED','RELEASED','CONSUMED')
       OR NEW."ClosedAt" IS NULL
       OR NEW."AssignmentId" IS DISTINCT FROM OLD."AssignmentId"
       OR NEW."ReservationId" IS DISTINCT FROM OLD."ReservationId"
       OR NEW."StorageUnitId" IS DISTINCT FROM OLD."StorageUnitId"
       OR NEW."AssignedByEmployeeId" IS DISTINCT FROM OLD."AssignedByEmployeeId"
       OR NEW."AssignedAt" IS DISTINCT FROM OLD."AssignedAt" THEN
        RAISE EXCEPTION USING ERRCODE='P1140', MESSAGE='HANDOVER_ASSIGNMENT_IMMUTABLE';
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_HandoverUnitAssignment_LifecycleGuard"
    BEFORE UPDATE OR DELETE ON public."HandoverUnitAssignment"
    FOR EACH ROW EXECUTE FUNCTION public.frms_handover_assignment_history_guard();

-- Manager identity is resolved from authenticated UserAccount ID, never client-supplied Employee ID.
CREATE OR REPLACE FUNCTION public."usp_AssignHandoverUnit"(
    p_reservation_id uuid, p_storage_unit_id uuid, p_user_account_id uuid)
RETURNS uuid LANGUAGE plpgsql AS $frms$
DECLARE
    v_res public."Reservation"%ROWTYPE;
    v_unit public."StorageUnit"%ROWTYPE;
    v_employee_id uuid;
    v_previous public."HandoverUnitAssignment"%ROWTYPE;
    v_new_id uuid := gen_random_uuid();
BEGIN
    PERFORM pg_advisory_xact_lock(70439102);
    SELECT * INTO v_res FROM public."Reservation" r
        WHERE r."ReservationId"=p_reservation_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1101', MESSAGE='RESOURCE_NOT_FOUND'; END IF;
    SELECT e."EmployeeId" INTO v_employee_id
      FROM public."Employee" e
      JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId"
      JOIN public."UserRole" ur ON ur."RoleId"=a."RoleId"
      WHERE a."UserAccountId"=p_user_account_id AND a."Status"='ACTIVE'
        AND ur."RoleName"='FACILITY_MANAGER' AND e."FacilityId"=v_res."FacilityId"
      FOR SHARE OF e,a,ur;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1114', MESSAGE='FORBIDDEN'; END IF;
    IF v_res."Status"<>'CONFIRMED' OR EXISTS(
         SELECT 1 FROM public."Contract" c WHERE c."ReservationId"=p_reservation_id)
       THEN RAISE EXCEPTION USING ERRCODE='P1107', MESSAGE='RESERVATION_INVALID_STATUS'; END IF;
    IF NOT EXISTS (SELECT 1 FROM public."Visit" v WHERE v."EntityId"=p_reservation_id
          AND v."VisitType"='RESERVATION' AND v."Status"='CHECKED_IN') THEN
        RAISE EXCEPTION USING ERRCODE='P1112', MESSAGE='VISIT_INVALID_STATUS';
    END IF;
    SELECT * INTO v_unit FROM public."StorageUnit" u
       WHERE u."StorageUnitId"=p_storage_unit_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1101', MESSAGE='RESOURCE_NOT_FOUND'; END IF;
    IF v_unit."FacilityId"<>v_res."FacilityId" OR v_unit."UnitTypeId"<>v_res."UnitTypeId" THEN
        RAISE EXCEPTION USING ERRCODE='P1117', MESSAGE='UNIT_FACILITY_TYPE_MISMATCH';
    END IF;
    IF v_unit."Status"<>'AVAILABLE' THEN
        RAISE EXCEPTION USING ERRCODE='P1117', MESSAGE='UNIT_NOT_AVAILABLE';
    END IF;
    SELECT * INTO v_previous FROM public."HandoverUnitAssignment" a
       WHERE a."ReservationId"=p_reservation_id AND a."Status"='ACTIVE' FOR UPDATE;
    IF FOUND AND v_previous."StorageUnitId"=p_storage_unit_id THEN
        RETURN v_previous."AssignmentId";
    END IF;
    IF EXISTS (SELECT 1 FROM public."HandoverUnitAssignment" a
        WHERE a."StorageUnitId"=p_storage_unit_id AND a."Status"='ACTIVE'
          AND a."ReservationId"<>p_reservation_id) THEN
        RAISE EXCEPTION USING ERRCODE='P1141', MESSAGE='HANDOVER_UNIT_ALREADY_ASSIGNED';
    END IF;
    IF v_previous."AssignmentId" IS NOT NULL THEN
       UPDATE public."HandoverUnitAssignment" a SET "Status"='REPLACED',"ClosedAt"=clock_timestamp()
         WHERE a."AssignmentId"=v_previous."AssignmentId" AND a."Status"='ACTIVE';
    END IF;
    INSERT INTO public."HandoverUnitAssignment"
      ("AssignmentId","ReservationId","StorageUnitId","AssignedByEmployeeId")
      VALUES(v_new_id,p_reservation_id,p_storage_unit_id,v_employee_id);
    RETURN v_new_id;
END $frms$;

CREATE OR REPLACE FUNCTION public."usp_ReleaseHandoverUnit"(
    p_reservation_id uuid, p_user_account_id uuid)
RETURNS void LANGUAGE plpgsql AS $frms$
DECLARE v_res public."Reservation"%ROWTYPE;
BEGIN
    PERFORM pg_advisory_xact_lock(70439102);
    SELECT * INTO v_res FROM public."Reservation" r WHERE r."ReservationId"=p_reservation_id FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1101', MESSAGE='RESOURCE_NOT_FOUND'; END IF;
    PERFORM 1 FROM public."Employee" e
       JOIN public."UserAccount" a ON a."UserAccountId"=e."UserAccountId"
       JOIN public."UserRole" ur ON ur."RoleId"=a."RoleId"
       WHERE a."UserAccountId"=p_user_account_id AND a."Status"='ACTIVE'
         AND ur."RoleName"='FACILITY_MANAGER' AND e."FacilityId"=v_res."FacilityId"
       FOR SHARE OF e,a,ur;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE='P1114', MESSAGE='FORBIDDEN'; END IF;
    IF v_res."Status" <> 'CONFIRMED' THEN
        RAISE EXCEPTION USING ERRCODE='P1107', MESSAGE='RESERVATION_INVALID_STATUS';
    END IF;
    UPDATE public."HandoverUnitAssignment" a
       SET "Status"='RELEASED', "ClosedAt"=clock_timestamp()
       WHERE a."ReservationId"=p_reservation_id AND a."Status"='ACTIVE';
END $frms$;

-- In the existing usp_CompleteHandover, INSERT Contract occurs only after
-- Reservation/Visit/Unit/Staff validation and inside its atomic transaction.
-- A BEFORE INSERT guard consumes the exact Manager selection in THAT transaction.
-- Any exception rolls back both Contract and consumed assignment.
CREATE OR REPLACE FUNCTION public.frms_require_handover_assignment_on_contract()
RETURNS trigger LANGUAGE plpgsql AS $frms$
DECLARE v_assignment public."HandoverUnitAssignment"%ROWTYPE;
BEGIN
    SELECT * INTO v_assignment FROM public."HandoverUnitAssignment" a
      WHERE a."ReservationId"=NEW."ReservationId" AND a."Status"='ACTIVE' FOR UPDATE;
    IF NOT FOUND THEN
       RAISE EXCEPTION USING ERRCODE='P1142', MESSAGE='HANDOVER_UNIT_ASSIGNMENT_REQUIRED';
    END IF;
    IF v_assignment."StorageUnitId"<>NEW."StorageUnitId" THEN
       RAISE EXCEPTION USING ERRCODE='P1143', MESSAGE='HANDOVER_UNIT_MISMATCH';
    END IF;
    UPDATE public."HandoverUnitAssignment" a
       SET "Status"='CONSUMED', "ClosedAt"=clock_timestamp()
       WHERE a."AssignmentId"=v_assignment."AssignmentId" AND a."Status"='ACTIVE';
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Contract_ConsumeHandoverAssignment"
    BEFORE INSERT ON public."Contract" FOR EACH ROW
    EXECUTE FUNCTION public.frms_require_handover_assignment_on_contract();

-- Both Customer cancel and scheduled no-show transition Reservation to CANCELLED.
CREATE OR REPLACE FUNCTION public.frms_release_assignment_on_reservation_cancel()
RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
    IF OLD."Status" IS DISTINCT FROM NEW."Status" AND NEW."Status"='CANCELLED' THEN
        UPDATE public."HandoverUnitAssignment" a
        SET "Status"='RELEASED', "ClosedAt"=clock_timestamp()
        WHERE a."ReservationId"=NEW."ReservationId" AND a."Status"='ACTIVE';
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_Reservation_ReleaseHandoverAssignment"
    AFTER UPDATE OF "Status" ON public."Reservation" FOR EACH ROW
    EXECUTE FUNCTION public.frms_release_assignment_on_reservation_cancel();

-- Once assigned, Facility/UnitType/status may not be mutated behind Staff's back.
CREATE OR REPLACE FUNCTION public.frms_protect_assigned_storage_unit()
RETURNS trigger LANGUAGE plpgsql AS $frms$
BEGIN
    IF EXISTS (SELECT 1 FROM public."HandoverUnitAssignment" a
               WHERE a."StorageUnitId"=OLD."StorageUnitId" AND a."Status"='ACTIVE')
       AND (OLD."Status" IS DISTINCT FROM NEW."Status"
         OR OLD."FacilityId" IS DISTINCT FROM NEW."FacilityId"
         OR OLD."UnitTypeId" IS DISTINCT FROM NEW."UnitTypeId") THEN
        RAISE EXCEPTION USING ERRCODE='P1144', MESSAGE='HANDOVER_UNIT_HAS_ACTIVE_ASSIGNMENT';
    END IF;
    RETURN NEW;
END $frms$;
CREATE TRIGGER "trg_StorageUnit_ProtectHandoverAssignment"
    BEFORE UPDATE OF "Status", "FacilityId", "UnitTypeId" ON public."StorageUnit"
    FOR EACH ROW EXECUTE FUNCTION public.frms_protect_assigned_storage_unit();
