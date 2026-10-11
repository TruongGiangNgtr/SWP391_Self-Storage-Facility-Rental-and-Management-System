
USE [FRMS];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Tables TABLE
(
    ObjectId INT PRIMARY KEY,
    FullName NVARCHAR(517)
);

INSERT INTO @Tables (ObjectId, FullName)
SELECT
    t.object_id,
    QUOTENAME(s.name) + '.' + QUOTENAME(t.name)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND NOT (
      s.name = 'dbo'
      AND t.name = '__EFMigrationsHistory'
  );

-- Không tự thay đổi trạng thái constraint/trigger
-- vốn đã bị disable hoặc chưa trusted.
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id IN (SELECT ObjectId FROM @Tables)
      AND (is_disabled = 1 OR is_not_trusted = 1)
)
OR EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id IN (SELECT ObjectId FROM @Tables)
      AND (is_disabled = 1 OR is_not_trusted = 1)
)
OR EXISTS (
    SELECT 1 FROM sys.triggers
    WHERE parent_id IN (SELECT ObjectId FROM @Tables)
      AND is_disabled = 1
)
BEGIN
    THROW 50001,
        'Existing disabled/untrusted constraints or triggers. Review before cleanup.',
        1;
END;

DECLARE @Phase INT = 1;
DECLARE @TableName NVARCHAR(517);
DECLARE @Sql NVARCHAR(MAX);

BEGIN TRY
    BEGIN TRANSACTION;

    WHILE @Phase <= 4
    BEGIN
        DECLARE CleanupCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT FullName FROM @Tables ORDER BY FullName;

        OPEN CleanupCursor;
        FETCH NEXT FROM CleanupCursor INTO @TableName;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @Sql =
                CASE @Phase
                    WHEN 1 THEN
                        N'ALTER TABLE ' + @TableName +
                        N' NOCHECK CONSTRAINT ALL; ' +
                        N'DISABLE TRIGGER ALL ON ' + @TableName + N';'
                    WHEN 2 THEN
                        N'DELETE FROM ' + @TableName + N';'
                    WHEN 3 THEN
                        N'ALTER TABLE ' + @TableName +
                        N' WITH CHECK CHECK CONSTRAINT ALL;'
                    WHEN 4 THEN
                        N'ENABLE TRIGGER ALL ON ' + @TableName + N';'
                END;

            EXEC sys.sp_executesql @Sql;

            FETCH NEXT FROM CleanupCursor INTO @TableName;
        END;

        CLOSE CleanupCursor;
        DEALLOCATE CleanupCursor;

        SET @Phase += 1;
    END;

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: FRMS data cleaned. Schema preserved.';
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'CleanupCursor') >= 0
        CLOSE CleanupCursor;

    IF CURSOR_STATUS('local', 'CleanupCursor') > -3
        DEALLOCATE CleanupCursor;

    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
