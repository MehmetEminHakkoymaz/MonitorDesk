namespace MonitorDesk.Services;

internal record MonitorAlias(string Id, int PhysicalIndex, string Name);
internal static class MonitorAliases
{
    internal static List<MonitorAlias>? Validate(List<MonitorAlias>? aliases) => aliases?
        .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Id) && a.PhysicalIndex >= 0 &&
            !string.IsNullOrWhiteSpace(a.Name) && a.Name.Trim().Length <= 64 && !a.Name.Any(char.IsControl))
        .Select(a => a with { Name = a.Name.Trim() }).DistinctBy(a => (a.Id, a.PhysicalIndex)).ToList();
    internal static string Name(Display display, List<MonitorAlias>? aliases)
        => aliases?.FirstOrDefault(a => a.Id == display.Id && a.PhysicalIndex == display.PhysicalIndex)?.Name ?? display.Name;
}
