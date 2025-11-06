namespace Orcamentaria.Lib.Domain.Models
{
    public class GridParams
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortField { get; init; }
        public bool? SortDesc { get; init; } = false;
        public List<FilterParam>? Filters { get; init; } = new();
    }
}
