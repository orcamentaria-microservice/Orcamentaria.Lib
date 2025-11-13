namespace Orcamentaria.Lib.Domain.Models
{
    public class FilterParam
    {
        public string Field { get; init; } = default!;
        public string Operator { get; init; } = "eq";
        public object? Value { get; init; }
    }
}
