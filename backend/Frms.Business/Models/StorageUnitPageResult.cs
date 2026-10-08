namespace Frms.Business.Models;

public sealed record StorageUnitPageResult(
    IReadOnlyList<StorageUnitListItem> Items,
    int TotalItems);