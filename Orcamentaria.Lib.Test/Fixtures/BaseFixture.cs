using Bogus;
using Microsoft.EntityFrameworkCore;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Infrastructure.Contexts;
using Xunit;

namespace Orcamentaria.Lib.Test.Fixtures
{
    [CollectionDefinition(nameof(BaseCollection))]
    public class BaseCollection : ICollectionFixture<BaseFixture<Base>> { }

    public class BaseFixture<T> where T : class
    {
        public Faker Faker = new Faker();

        public UserAuthContext CreateUserAuthContext()
            => new UserAuthContext()
            {
                Email = Faker.Internet.Email(),
                CompanyId = 1,
                UserId = Faker.Random.Long(1, 9999),
                BearerToken = "Bearer JWT",
            };

        public GridParams CreateGridParamsWithOneFilter(
            FilterParam filter,
            int? page = 1,
            int? pageSize = 10,
            bool? sortDesc = false,
            string? sortField = "id")
            => CreateGridParams(new List<FilterParam>() { filter }, page ?? 1, pageSize ?? 10, sortDesc ?? false, sortField ?? "id");

        public GridParams CreateGridParamsWithManyFilter(
            IEnumerable<FilterParam> filters,
            int? page = 1,
            int? pageSize = 10,
            bool? sortDesc = false,
            string? sortField = "id")
            => CreateGridParams(filters, page ?? 1, pageSize ?? 10, sortDesc ?? false, sortField ?? "id");

        public GridParams CreateGridParamsWithWithoutFilter(
            int? page = 1,
            int? pageSize = 10,
            bool? sortDesc = false,
            string? sortField = "id")
            => CreateGridParams(new List<FilterParam>(), page ?? 1, pageSize ?? 10, sortDesc ?? false, sortField ?? "id");

        private GridParams CreateGridParams(
            IEnumerable<FilterParam> filters,
            int page,
            int pageSize,
            bool sortDesc,
            string sortField)
            => new GridParams
            {
                Page = page,
                PageSize = pageSize,
                SortDesc = sortDesc,
                SortField = sortField,
                Filters = filters.ToList()
            };

        public bool GridParamsAreEquivalent(GridParams actual, GridParams expected)
        {
            if (actual == null || expected == null)
                return false;

            if (actual.Page != expected.Page) return false;
            if (actual.PageSize != expected.PageSize) return false;

            if (actual.Filters == null && expected.Filters == null)
                return true;

            if (actual.Filters == null || expected.Filters == null)
                return false;

            if (actual.Filters.Count != expected.Filters.Count)
                return false;

            for (int i = 0; i < actual.Filters.Count; i++)
            {
                var a = actual.Filters[i];
                var e = expected.Filters[i];

                if (!string.Equals(a.Field, e.Field, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.Equals(a.Operator, e.Operator, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!Equals(a.Value, e.Value))
                    return false;
            }

            return true;
        }

        public virtual extern T CreateEntity(long id);

        public async Task SeedInMemoryDatabase(DbContext dbContext, params long[] ids)
        {
            foreach (var id in ids)
                dbContext.Set<T>().Add(CreateEntity(id));

            await dbContext.SaveChangesAsync();
        }
    }

    public class Base { }
}
