// FRMS Reservation dependency repair: AuditLogId is required but legacy audit
// triggers omit it when writing AuditLog. A DEFAULT NEWID() supplies an ID for
// every INSERT row without altering existing rows or replacing any trigger.
// This is an additive migration; do not amend an already-applied migration.
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.DataAccess.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261011133000_FixAuditLogIdDefault")]
public sealed class FixAuditLogIdDefault : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
                THROW 51270, 'Missing dbo.AuditLog; apply the baseline EF migrations first.', 1;

            -- Do not overwrite an unrelated existing default expression.
            IF EXISTS (
                SELECT 1
                FROM sys.default_constraints d
                JOIN sys.columns c
                    ON c.object_id = d.parent_object_id
                   AND c.column_id = d.parent_column_id
                WHERE d.parent_object_id = OBJECT_ID(N'dbo.AuditLog', N'U')
                  AND c.name = N'AuditLogId'
                  AND UPPER(d.definition) NOT LIKE N'%NEWID%'
                  AND UPPER(d.definition) NOT LIKE N'%NEWSEQUENTIALID%'
            )
                THROW 51271, 'dbo.AuditLog.AuditLogId has a non-GUID-generating default; inspect before applying.', 1;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.default_constraints d
                JOIN sys.columns c
                    ON c.object_id = d.parent_object_id
                   AND c.column_id = d.parent_column_id
                WHERE d.parent_object_id = OBJECT_ID(N'dbo.AuditLog', N'U')
                  AND c.name = N'AuditLogId'
            )
            BEGIN
                ALTER TABLE dbo.AuditLog
                    ADD CONSTRAINT DF_AuditLog_AuditLogId
                    DEFAULT (NEWID()) FOR AuditLogId;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A rollback can reintroduce errors in the still-installed audit triggers.
        // Remove only this specifically named default; never touch rows/triggers.
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.default_constraints d
                JOIN sys.columns c
                    ON c.object_id = d.parent_object_id
                   AND c.column_id = d.parent_column_id
                WHERE d.parent_object_id = OBJECT_ID(N'dbo.AuditLog', N'U')
                  AND c.name = N'AuditLogId'
                  AND d.name = N'DF_AuditLog_AuditLogId'
            )
            BEGIN
                ALTER TABLE dbo.AuditLog
                    DROP CONSTRAINT DF_AuditLog_AuditLogId;
            END;
            """);
    }
}
