/*
Development/demo seed, explicitly requested by the owner on 2026-10-11.
Resource values reused from the FRMS Demo facilities in FRMS_EPS01_Dev.
Only AVAILABLE units without an ACTIVE Contract in that source were selected.
Not production pricing/configuration. No migration, account/password, employee,
contract, reservation, invoice, payment, policy update or history is included.
Run while connected to FRMS. To target a NEW disposable test database, change
only the @ExpectedDatabase literal. Never run this on production.
Rerun: insert missing IDs, refuse conflicting IDs/names/unit codes, never UPDATE.
Facility ContactInfo/Description are intentionally not copied.
*/
SET XACT_ABORT ON;
DECLARE @ExpectedDatabase sysname=N'FRMS';
IF DB_NAME() <> @ExpectedDatabase
    THROW 51983, 'Wrong database for development seed.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Facility',N'U') IS NULL
       OR OBJECT_ID(N'dbo.UnitType',N'U') IS NULL
       OR OBJECT_ID(N'dbo.StorageUnit',N'U') IS NULL
        THROW 51984, 'Import the FRMS schema before applying demo seed.', 1;

    DECLARE @Facility TABLE (
        FacilityId uniqueidentifier PRIMARY KEY, Name nvarchar(150),
        Address nvarchar(1000), Status varchar(50));
    INSERT @Facility VALUES
    ('F1000000-0000-0000-0000-000000000001',N'FRMS Demo Facility - District 1',N'123 Nguyễn Huệ, Quận 1, TP.HCM','ACTIVE'),
    ('F1000000-0000-0000-0000-000000000002',N'FRMS Demo Facility - Thủ Đức',N'456 Võ Văn Ngân, TP. Thủ Đức, TP.HCM','INACTIVE');

    DECLARE @UnitType TABLE (
        UnitTypeId uniqueidentifier PRIMARY KEY, Name nvarchar(150),
        Mode varchar(50), Size nvarchar(150), RentalPrice decimal(18,2),
        Description nvarchar(1000));
    INSERT @UnitType VALUES
    ('B1000000-0000-0000-0000-000000000001',N'Small Private','PRIVATE',N'2 m²',500000.00,N'Boxes, documents, small household items.'),
    ('B1000000-0000-0000-0000-000000000002',N'Medium Private','PRIVATE',N'5 m²',900000.00,N'Furniture and medium household storage.'),
    ('B1000000-0000-0000-0000-000000000003',N'Shared Public','PUBLIC',N'3 m² equivalent',350000.00,N'Shared storage area handled by facility staff.');

    DECLARE @StorageUnit TABLE (
        StorageUnitId uniqueidentifier PRIMARY KEY, FacilityId uniqueidentifier,
        UnitTypeId uniqueidentifier, UnitCode varchar(50),
        LocationInfo nvarchar(1000), Status varchar(50));
    INSERT @StorageUnit VALUES
    ('71000000-0000-0000-0000-000000000002','F1000000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000001','A-102',N'Tầng 1 - Dãy A','AVAILABLE'),
    ('71000000-0000-0000-0000-000000000003','F1000000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000002','B-201',N'Tầng 2 - Dãy B','AVAILABLE'),
    ('71000000-0000-0000-0000-000000000005','F1000000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000003','P-001',N'Khu Public - Zone P','AVAILABLE'),
    ('71000000-0000-0000-0000-000000000006','F1000000-0000-0000-0000-000000000002','B1000000-0000-0000-0000-000000000001','A-101',N'Tầng 1 - Dãy A','AVAILABLE');

    IF EXISTS (
        SELECT 1 FROM dbo.Facility f WITH (UPDLOCK,HOLDLOCK)
        JOIN @Facility s ON s.FacilityId=f.FacilityId
        WHERE EXISTS (SELECT f.Name,f.Address,f.Status
                      EXCEPT SELECT s.Name,s.Address,s.Status))
       OR EXISTS (
        SELECT 1 FROM dbo.Facility f WITH (UPDLOCK,HOLDLOCK)
        JOIN @Facility s ON s.Name=f.Name WHERE s.FacilityId<>f.FacilityId)
        THROW 51985, 'Facility seed conflicts with existing data; no overwrite.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.UnitType t WITH (UPDLOCK,HOLDLOCK)
        JOIN @UnitType s ON s.UnitTypeId=t.UnitTypeId
        WHERE EXISTS (SELECT t.Name,t.Mode,t.Size,t.RentalPrice,t.Description
                      EXCEPT SELECT s.Name,s.Mode,s.Size,s.RentalPrice,s.Description))
       OR EXISTS (
        SELECT 1 FROM dbo.UnitType t WITH (UPDLOCK,HOLDLOCK)
        JOIN @UnitType s ON s.Name=t.Name WHERE s.UnitTypeId<>t.UnitTypeId)
        THROW 51986, 'UnitType seed conflicts with existing data; no overwrite.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.StorageUnit u WITH (UPDLOCK,HOLDLOCK)
        JOIN @StorageUnit s ON s.StorageUnitId=u.StorageUnitId
        WHERE EXISTS (SELECT u.FacilityId,u.UnitTypeId,u.UnitCode,u.LocationInfo,u.Status
                      EXCEPT SELECT s.FacilityId,s.UnitTypeId,s.UnitCode,s.LocationInfo,s.Status))
       OR EXISTS (
        SELECT 1 FROM dbo.StorageUnit u WITH (UPDLOCK,HOLDLOCK)
        JOIN @StorageUnit s ON s.FacilityId=u.FacilityId AND s.UnitCode=u.UnitCode
        WHERE s.StorageUnitId<>u.StorageUnitId)
        THROW 51987, 'StorageUnit seed conflicts with existing data; no overwrite.', 1;

    INSERT dbo.Facility(FacilityId,Name,Address,Status)
    SELECT s.FacilityId,s.Name,s.Address,s.Status FROM @Facility s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Facility f WITH (UPDLOCK,HOLDLOCK)
                      WHERE f.FacilityId=s.FacilityId);
    DECLARE @AddedFacilities int=@@ROWCOUNT;

    INSERT dbo.UnitType(UnitTypeId,Name,Mode,Size,RentalPrice,Description)
    SELECT s.UnitTypeId,s.Name,s.Mode,s.Size,s.RentalPrice,s.Description FROM @UnitType s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.UnitType t WITH (UPDLOCK,HOLDLOCK)
                      WHERE t.UnitTypeId=s.UnitTypeId);
    DECLARE @AddedTypes int=@@ROWCOUNT;

    INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
    SELECT s.StorageUnitId,s.FacilityId,s.UnitTypeId,s.UnitCode,s.LocationInfo,s.Status
    FROM @StorageUnit s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.StorageUnit u WITH (UPDLOCK,HOLDLOCK)
                      WHERE u.StorageUnitId=s.StorageUnitId);
    DECLARE @AddedUnits int=@@ROWCOUNT;

    COMMIT TRANSACTION;
    SELECT @AddedFacilities AS AddedFacilities,@AddedTypes AS AddedUnitTypes,
           @AddedUnits AS AddedStorageUnits;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

