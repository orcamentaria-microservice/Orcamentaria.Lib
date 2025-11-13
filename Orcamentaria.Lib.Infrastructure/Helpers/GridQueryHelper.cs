using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using System.Globalization;
using System.Linq.Expressions;

namespace Orcamentaria.Lib.Infrastructure.Helpers
{
    public static class GridQueryHelper
    {
        public static bool HasFieldMap<TEntity>(string field)
            => typeof(TEntity).GetProperty(field) is not null;

        public static IQueryable<TEntity> ApplyFiltersWithCompanyId<TEntity>(
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

            return ApplyFilters(query, filters);
        }

        public static IQueryable<TEntity> ApplyFiltersWithoutCompanyId<TEntity>(
            IQueryable<TEntity> query,
            IEnumerable<FilterParam>? filters)
            where TEntity : class
        => ApplyFilters(query, filters);

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

        private static IQueryable<TEntity> ApplyFilters<TEntity>(IQueryable<TEntity> query,
            IEnumerable<FilterParam>? filters)
        {
            if (filters == null) return query;

            foreach (var f in filters)
            {
                if (string.IsNullOrWhiteSpace(f.Field) || f.Value == null)
                    continue;

                var parameter = Expression.Parameter(typeof(TEntity), "e");
                var property = Expression.PropertyOrField(parameter, f.Field);
                var valueType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

                Expression? body = null;

                switch (f.Operator.ToLowerInvariant())
                {
                    case "eq":
                        {
                            var convertedValue = ConvertHelper.ConvertTo(f.Value, valueType);
                            var constant = Expression.Constant(convertedValue, property.Type);
                            body = Expression.Equal(property,
                                   property.Type == constant.Type ? constant : Expression.Convert(constant, property.Type));
                            break;
                        }

                    case "ne":
                        {
                            var convertedValue = ConvertHelper.ConvertTo(f.Value, valueType);
                            var constant = Expression.Constant(convertedValue, property.Type);
                            body = Expression.NotEqual(property,
                                   property.Type == constant.Type ? constant : Expression.Convert(constant, property.Type));
                            break;
                        }

                    case "gt":
                        {
                            var convertedValue = ConvertHelper.ConvertTo(f.Value, valueType);
                            body = Expression.GreaterThan(property, Expression.Constant(convertedValue, property.Type));
                            break;
                        }

                    case "lt":
                        {
                            var convertedValue = ConvertHelper.ConvertTo(f.Value, valueType);
                            body = Expression.LessThan(property, Expression.Constant(convertedValue, property.Type));
                            break;
                        }

                    case "ge":
                        {
                            var convertedValue = ConvertHelper.ConvertTo(f.Value, valueType);
                            body = Expression.GreaterThanOrEqual(property, Expression.Constant(convertedValue, property.Type));
                            break;
                        }

                    case "le":
                        {
                            var convertedValue = ConvertHelper.ConvertTo(f.Value, valueType);
                            body = Expression.LessThanOrEqual(property, Expression.Constant(convertedValue, property.Type));
                            break;
                        }

                    case "in":
                        {
                            var items = ConvertHelper.ToArrayFor(valueType, f.Value);
                            if (items.Length == 0)
                                continue;

                            var typedArray = Array.CreateInstance(valueType, items.Length);
                            for (int i = 0; i < items.Length; i++)
                            {
                                var v = items[i];
                                if (v is not null && v.GetType() != valueType)
                                    v = Convert.ChangeType(v, valueType, CultureInfo.InvariantCulture);
                                typedArray.SetValue(v, i);
                            }

                            var contains = typeof(Enumerable)
                                .GetMethods()
                                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                                .MakeGenericMethod(valueType);

                            Expression left = property;
                            if (Nullable.GetUnderlyingType(property.Type) == valueType && property.Type != valueType)
                                left = Expression.Convert(property, valueType);

                            var right = Expression.Constant(typedArray, typedArray.GetType());

                            body = Expression.Call(contains, right, left);
                            break;
                        }

                    case "ni":
                        {
                            var items = ConvertHelper.ToArrayFor(valueType, f.Value);
                            if (items.Length == 0)
                                continue;

                            var typedArray = Array.CreateInstance(valueType, items.Length);
                            for (int i = 0; i < items.Length; i++)
                            {
                                var v = items[i];
                                if (v is not null && v.GetType() != valueType)
                                    v = Convert.ChangeType(v, valueType, CultureInfo.InvariantCulture);
                                typedArray.SetValue(v, i);
                            }

                            var contains = typeof(Enumerable)
                                .GetMethods()
                                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                                .MakeGenericMethod(valueType);

                            Expression left = property;
                            if (Nullable.GetUnderlyingType(property.Type) == valueType && property.Type != valueType)
                                left = Expression.Convert(property, valueType);

                            var right = Expression.Constant(typedArray, typedArray.GetType());

                            body = Expression.Not(Expression.Call(contains, right, left));
                            break;
                        }

                    default:
                        throw new InfoException("Operação do filtro é inválido.", Lib.Domain.Enums.ErrorCodeEnum.ValidationFailed);
                }

                var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
    }
}