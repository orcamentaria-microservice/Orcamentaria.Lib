using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using System.Linq.Expressions;

namespace Orcamentaria.Lib.Infrastructure.Helpers
{
    public static class GridQuery
    {
        public static bool HasFieldMap<TEntity>(string field) 
            => typeof(TEntity).GetProperty(field) is not null;

        public static IQueryable<TEntity> ApplyFilters<TEntity>(
            IQueryable<TEntity> query,
            IEnumerable<FilterParam>? filters,
            long companyId)
            where TEntity : class
        {
            var prop = typeof(TEntity).GetProperty("CompanyId");
            if (prop != null && prop.PropertyType == typeof(long))
            {
                var p = Expression.Parameter(typeof(TEntity), "e");
                var propertyCompanyId = Expression.Property(p, "CompanyId");
                var valueCompanyId = Expression.Constant(companyId);
                var body = Expression.Equal(propertyCompanyId, valueCompanyId);
                var lambda = Expression.Lambda<Func<TEntity, bool>>(body, p);
                query = query.Where(lambda);
            }

            if (filters == null) return query;

            foreach (var f in filters)
            {
                if (string.IsNullOrWhiteSpace(f.Field) || f.Value == null)
                    continue;

                var parameter = Expression.Parameter(typeof(TEntity), "e");
                var property = Expression.PropertyOrField(parameter, f.Field);
                var valueType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

                object? convertedValue;
                try
                {
                    convertedValue = Convert.ChangeType(f.Value, valueType);
                }
                catch
                {
                    continue;
                }

                Expression? body = null;

                switch (f.Operator.ToLowerInvariant())
                {
                    case "eq":
                        body = Expression.Equal(property, Expression.Constant(convertedValue));
                        break;

                    case "in":
                        if (valueType != typeof(string))
                            continue;

                        var method = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
                        body = Expression.Call(property, method!, Expression.Constant(f.Value.ToString() ?? ""));
                        break;

                    case "gt":
                        body = Expression.GreaterThan(property, Expression.Constant(convertedValue));
                        break;

                    case "lt":
                        body = Expression.LessThan(property, Expression.Constant(convertedValue));
                        break;

                    case "ge":
                        body = Expression.GreaterThanOrEqual(property, Expression.Constant(convertedValue));
                        break;

                    case "le":
                        body = Expression.LessThanOrEqual(property, Expression.Constant(convertedValue));
                        break;

                    default:
                        throw new InfoException("Operação do filtro é inválido.", Lib.Domain.Enums.ErrorCodeEnum.ValidationFailed);
                }

                var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        public static IQueryable<TEntity> ApplySorting<TEntity>(
            IQueryable<TEntity> query,
            string? sortField,
            bool sortDesc)
            where TEntity : class
        {
            if (string.IsNullOrWhiteSpace(sortField))
                return query;

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.PropertyOrField(parameter, sortField);
            var lambda = Expression.Lambda(property, parameter);

            string methodName = sortDesc ? "OrderByDescending" : "OrderBy";

            var types = new Type[] { query.ElementType, property.Type };
            var mce = Expression.Call(typeof(Queryable), methodName, types, query.Expression, lambda);

            return query.Provider.CreateQuery<TEntity>(mce);
        }

        public static (int Page, int PageSize, int Skip) NormalizePaging(int page, int pageSize, int maxPageSize = 100)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, maxPageSize);
            var skip = (page - 1) * pageSize;
            return (page, pageSize, skip);
        }
    }
}