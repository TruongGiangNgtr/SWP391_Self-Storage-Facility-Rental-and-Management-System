USE frms;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CompleteHandover
    @ReservationId UNIQUEIDENTIFIER,
    @VisitId UNIQUEIDENTIFIER,
    @StorageUnitId UNIQUEIDENTIFIER,
    @EmployeeId UNIQUEIDENTIFIER,
    @DiscountId UNIQUEIDENTIFIER = NULL,
    @NowUtc DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @NowUtc IS NULL
        SET @NowUtc = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
            @CustomerId UNIQUEIDENTIFIER,
            @FacilityId UNIQUEIDENTIFIER,
            @UnitTypeId UNIQUEIDENTIFIER,
            @ReservationPolicyId UNIQUEIDENTIFIER,
            @StartMonth DATE,
            @EndMonth DATE,
            @ReservationStatus VARCHAR(50);

        SELECT
            @CustomerId = CustomerId,
            @FacilityId = FacilityId,
            @UnitTypeId = UnitTypeId,
            @ReservationPolicyId = PolicyId,
            @StartMonth = StartMonth,
            @EndMonth = EndMonth,
            @ReservationStatus = Status
        FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
        WHERE ReservationId = @ReservationId;

        IF @CustomerId IS NULL
            THROW 51101, 'RESOURCE_NOT_FOUND', 1;

        DECLARE @ExistingContractId UNIQUEIDENTIFIER;

        SELECT
            @ExistingContractId = ContractId
        FROM dbo.Contract
        WHERE ReservationId = @ReservationId;

        -- Idempotent retry after a successful handover.
        IF @ExistingContractId IS NOT NULL
        BEGIN
            COMMIT TRANSACTION;

            SELECT
                c.ContractId,
                c.StorageUnitId,
                c.Status AS ContractStatus,
                'ALREADY_COMPLETED' AS Result
            FROM dbo.Contract c
            WHERE c.ContractId = @ExistingContractId;

            RETURN;
        END;

        IF @ReservationStatus <> 'CONFIRMED'
            THROW 51107, 'RESERVATION_INVALID_STATUS', 1;

        DECLARE
            @VisitEntityId UNIQUEIDENTIFIER,
            @VisitType VARCHAR(50),
            @VisitStatus VARCHAR(50);

        SELECT
            @VisitEntityId = EntityId,
            @VisitType = VisitType,
            @VisitStatus = Status
        FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
        WHERE VisitId = @VisitId;

        IF @VisitEntityId IS NULL
            THROW 51101, 'RESOURCE_NOT_FOUND', 1;

        IF @VisitEntityId <> @ReservationId
           OR @VisitType <> 'RESERVATION'
           OR @VisitStatus <> 'CHECKED_IN'
            THROW 51112, 'VISIT_INVALID_STATUS', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Employee e
            JOIN dbo.UserAccount ua
              ON ua.UserAccountId = e.UserAccountId
            JOIN dbo.UserRole ur
              ON ur.RoleId = ua.RoleId
            WHERE e.EmployeeId = @EmployeeId
              AND e.FacilityId = @FacilityId
              AND ua.Status = 'ACTIVE'
              AND ur.RoleName = 'FACILITY_STAFF'
        )
            THROW 51114, 'FORBIDDEN', 1;

        DECLARE
            @UnitFacilityId UNIQUEIDENTIFIER,
            @UnitUnitTypeId UNIQUEIDENTIFIER,
            @UnitStatus VARCHAR(50);

        SELECT
            @UnitFacilityId = FacilityId,
            @UnitUnitTypeId = UnitTypeId,
            @UnitStatus = Status
        FROM dbo.StorageUnit WITH (UPDLOCK, HOLDLOCK)
        WHERE StorageUnitId = @StorageUnitId;

        IF @UnitFacilityId IS NULL
            THROW 51101, 'RESOURCE_NOT_FOUND', 1;

        IF @UnitFacilityId <> @FacilityId
           OR @UnitUnitTypeId <> @UnitTypeId
            THROW 51117, 'UNIT_FACILITY_TYPE_MISMATCH', 1;

        IF @UnitStatus <> 'AVAILABLE'
            THROW 51117, 'UNIT_NOT_AVAILABLE', 1;

        IF @DiscountId IS NOT NULL
        BEGIN
            DECLARE
                @DiscountCustomerId UNIQUEIDENTIFIER,
                @DiscountStatus VARCHAR(50),
                @DiscountEffectiveFrom DATETIME2(3),
                @DiscountEffectiveTo DATETIME2(3);

            SELECT
                @DiscountCustomerId = CustomerId,
                @DiscountStatus = Status,
                @DiscountEffectiveFrom = EffectiveFrom,
                @DiscountEffectiveTo = EffectiveTo
            FROM dbo.Discount
            WHERE DiscountId = @DiscountId;

            IF @DiscountCustomerId IS NULL
                THROW 51118, 'DISCOUNT_NOT_VALID', 1;

            IF @DiscountCustomerId <> @CustomerId
                THROW 51120, 'DISCOUNT_NOT_OWNED_BY_CUSTOMER', 1;

            IF @DiscountStatus <> 'ACTIVE'
               OR @DiscountEffectiveFrom > @NowUtc
               OR (
                    @DiscountEffectiveTo IS NOT NULL
                    AND @DiscountEffectiveTo < @NowUtc
                  )
                THROW 51118, 'DISCOUNT_NOT_VALID', 1;
        END;

        DECLARE @ContractId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO dbo.Contract
        (
            ContractId,
            ReservationId,
            CustomerId,
            FacilityId,
            StorageUnitId,
            PolicyId,
            DiscountId,
            StartMonth,
            EndMonth,
            Status
        )
        VALUES
        (
            @ContractId,
            @ReservationId,
            @CustomerId,
            @FacilityId,
            @StorageUnitId,
            @ReservationPolicyId,
            @DiscountId,
            @StartMonth,
            @EndMonth,
            'ACTIVE'
        );

        -- V10: no first-month Invoice and no first-month Payment.

        UPDATE dbo.StorageUnit
        SET Status = 'IN_USE'
        WHERE StorageUnitId = @StorageUnitId
          AND Status = 'AVAILABLE';

        IF @@ROWCOUNT <> 1
            THROW 51117, 'UNIT_NOT_AVAILABLE', 1;

        UPDATE dbo.Visit
        SET
            Status = 'CHECKED_OUT',
            EmployeeId = @EmployeeId
        WHERE VisitId = @VisitId
          AND Status = 'CHECKED_IN';

        IF @@ROWCOUNT <> 1
            THROW 51112, 'VISIT_INVALID_STATUS', 1;

        UPDATE dbo.Reservation
        SET Status = 'COMPLETED'
        WHERE ReservationId = @ReservationId
          AND Status = 'CONFIRMED';

        IF @@ROWCOUNT <> 1
            THROW 51107, 'RESERVATION_INVALID_STATUS', 1;

        COMMIT TRANSACTION;

        SELECT
            @ContractId AS ContractId,
            @StorageUnitId AS StorageUnitId,
            'ACTIVE' AS ContractStatus,
            'COMPLETED' AS Result;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO