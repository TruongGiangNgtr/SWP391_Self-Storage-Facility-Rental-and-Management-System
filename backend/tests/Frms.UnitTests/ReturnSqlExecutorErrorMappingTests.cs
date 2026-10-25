using Frms.DataAccess.Repositories.Implementations;

namespace Frms.UnitTests;

[TestFixture]
public sealed class ReturnSqlExecutorErrorMappingTests
{
    // PostgreSQL RAISE pairs from backend/sql/postgresql/Inspections.sql and Returns.sql.
    [TestCase("P1112", "VISIT_INVALID_STATUS")]
    [TestCase("P1114", "STAFF_FACILITY_OR_ROLE_INVALID")]
    [TestCase("P1120", "CONTRACT_NOT_ACTIVE")]
    [TestCase("P1124", "STORAGE_UNIT_NOT_IN_USE")]
    [TestCase("P1125", "INSPECTION_ALREADY_CLAIMED")]
    [TestCase("P1126", "INSPECTION_INVALID_STATUS")]
    [TestCase("P1127", "DAMAGE_AMOUNT_REQUIRED")]
    [TestCase("P1128", "EXTRA_FEE_TYPE_NOT_ACTIVE")]
    [TestCase("P1129", "EXTRA_FEE_AMOUNT_INVALID")]
    [TestCase("P1130", "EVIDENCE_FILE_REQUIRED")]
    [TestCase("P1212", "DAMAGE_TYPE_NOT_ACTIVE")]
    [TestCase("P1280", "VALIDATION_ERROR")]
    [TestCase("P1281", "RESOURCE_NOT_FOUND")]
    [TestCase("P1282", "RETURN_ALREADY_FINALIZED")]
    [TestCase("P1283", "FORBIDDEN")]
    [TestCase("P1284", "UNIT_INVALID_STATUS")]
    [TestCase("P1285", "INSPECTION_INVALID_STATUS")]
    [TestCase("P1286", "VISIT_ENTITY_MISMATCH")]
    [TestCase("P1287", "DAMAGE_DECISION_PENDING")]
    [TestCase("P1288", "VALIDATION_ERROR")]
    public void DatabaseBusinessError_IsMappedBySqlStateAndExactMessage(
        string sqlState, string messageText)
    {
        Assert.That(ReturnSqlExecutor.ResolveBusinessCode(sqlState, messageText),
            Is.EqualTo(messageText));
    }

    [TestCase("23505", "DAMAGE_TYPE_NOT_ACTIVE")]
    [TestCase("P9999", "DAMAGE_TYPE_NOT_ACTIVE")]
    [TestCase("P1212", "DAMAGE_TYPE_INACTIVE")]
    [TestCase("P1128", "EXTRA_FEE_TYPE_INACTIVE")]
    [TestCase("P1212", "EXTRA_FEE_TYPE_NOT_ACTIVE")]
    [TestCase("P1128", "DAMAGE_TYPE_NOT_ACTIVE")]
    [TestCase("P1287", "damage_decision_pending")]
    [TestCase("P1288", "Unrecognized database exception")]
    public void UnexpectedSqlStateOrMessage_IsNotConvertedToBusinessError(
        string sqlState, string messageText)
    {
        Assert.That(ReturnSqlExecutor.ResolveBusinessCode(sqlState, messageText),
            Is.Null);
    }
}
