using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Infrastructure.Contexts;
using Orcamentaria.Lib.Infrastructure.Repositories;
using Orcamentaria.Lib.Test.Fixtures;
using Xunit;

namespace Orcamentaria.Lib.Test.Repositories
{
    public class ReadWithoutCompanyRepositoryTests<T, C>
        where T : class
        where C : DbContext
    {
        private readonly BaseFixture<T> _fixture;
        protected readonly C _dbContext;
        protected readonly DbContextOptions<DbContext> _options;
        protected readonly UserAuthContext _userAuthContext;

        public ReadWithoutCompanyRepositoryTests(BaseFixture<T> fixture)
        {
            _fixture = fixture;
            _userAuthContext = _fixture.CreateUserAuthContext();

            _options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

            _dbContext = (C)Activator.CreateInstance(typeof(C), _options);
        }

        #region GetByIdWithoutCompanyIdAsync
        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task GetByIdWithoutCompanyIdAsync_WhenHaveData_ReturnsData(long id)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var result = await mockRepository.Object.GetByIdWithoutCompanyIdAsync(id);

            result.Should().NotBeNull();

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());
        }

        [Xunit.Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(9)]
        public async Task GetByIdWithoutCompanyIdAsync_WhenNotHaveData_ReturnsNull(long id)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var result = await mockRepository.Object.GetByIdWithoutCompanyIdAsync(id);

            result.Should().BeNull();

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task GetByIdWithoutCompanyIdAsync_WhenAnyExceptionIsThrownInApplyFiltersWithoutCompanyIdWrapper(long id)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            mockRepository.Protected()
            .Setup<IQueryable<T>>(
                "ApplyFiltersWithoutCompanyIdWrapper",
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>())
            .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetByIdWithoutCompanyIdAsync(id);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());
        }
        #endregion

        #region GetWithoutCompanyIdAsync
        [Xunit.Theory]
        [InlineData("id", "gt", 1)]
        [InlineData("id", "ne", 3)]
        [InlineData("id", "in", "1, 3")]
        [InlineData("id", "in", "1, 3, 9")]
        public async Task GetWithoutCompanyIdAsync_WhenHaveData_ReturnsData(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            var result = await mockRepository.Object.GetWithoutCompanyIdAsync(gridParams);

            result.Item1.Should().HaveCount(2);
            result.Item2.TotalItems.Should().Be(2);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());

            mockRepository.Protected()
                .Verify(
                "ApplySortingWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<string?>(),
                ItExpr.IsAny<bool>());

            mockRepository.Protected()
                .Verify(
                "NormalizePagingWrapper",
                Times.Once(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>());
        }

        [Xunit.Theory]
        [InlineData("id", "gt", 6)]
        [InlineData("id", "eq", 21)]
        [InlineData("id", "in", "44")]
        public async Task GetWithoutCompanyIdAsync_WhenNotHaveData_ReturnsEmpty(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            var result = await mockRepository.Object.GetWithoutCompanyIdAsync(gridParams);

            result.Item1.Should().HaveCount(0);
            result.Item2.TotalItems.Should().Be(0);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());

            mockRepository.Protected()
                .Verify(
                "ApplySortingWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<string?>(),
                ItExpr.IsAny<bool>());

            mockRepository.Protected()
                .Verify(
                "NormalizePagingWrapper",
                Times.Once(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>());
        }

        [Xunit.Theory]
        [InlineData("id", "eq", 1)]
        [InlineData("id", "eq", 3)]
        [InlineData("id", "in", "6")]
        public async Task GetWithoutCompanyIdAsync_WhenAnyExceptionIsThrownInApplyFiltersWithoutCompanyIdWrapper(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            mockRepository.Protected()
                .Setup<IQueryable<T>>(
                    "ApplyFiltersWithoutCompanyIdWrapper",
                    ItExpr.IsAny<IQueryable<T>>(),
                    ItExpr.IsAny<IEnumerable<FilterParam>>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetWithoutCompanyIdAsync(gridParams);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());

            mockRepository.Protected()
                .Verify(
                "ApplySortingWrapper",
                Times.Never(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<string?>(),
                ItExpr.IsAny<bool>());

            mockRepository.Protected()
                .Verify(
                "NormalizePagingWrapper",
                Times.Never(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>());
        }

        [Xunit.Theory]
        [InlineData("id", "eq", 1)]
        [InlineData("id", "eq", 3)]
        [InlineData("id", "in", "6")]
        public async Task GetWithoutCompanyIdAsync_WhenAnyExceptionIsThrownInApplySortingWrapper(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            mockRepository.Protected()
                .Setup<IQueryable<T>>(
                    "ApplySortingWrapper",
                    ItExpr.IsAny<IQueryable<T>>(),
                    ItExpr.IsAny<string?>(),
                    ItExpr.IsAny<bool>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetWithoutCompanyIdAsync(gridParams);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());

            mockRepository.Protected()
                .Verify(
                "ApplySortingWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<string?>(),
                ItExpr.IsAny<bool>());

            mockRepository.Protected()
                .Verify(
                "NormalizePagingWrapper",
                Times.Never(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>());
        }

        [Xunit.Theory]
        [InlineData("id", "eq", 1)]
        [InlineData("id", "eq", 3)]
        [InlineData("id", "in", "6")]
        public async Task GetWithoutCompanyIdAsync_WhenAnyExceptionIsThrownInNormalizePagingWrapper(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            mockRepository.Protected()
                .Setup<(int, int, int)>(
                    "NormalizePagingWrapper",
                    ItExpr.IsAny<int>(),
                    ItExpr.IsAny<int>(),
                    ItExpr.IsAny<int>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetWithoutCompanyIdAsync(gridParams);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithoutCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>());

            mockRepository.Protected()
                .Verify(
                "ApplySortingWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<string?>(),
                ItExpr.IsAny<bool>());

            mockRepository.Protected()
                .Verify(
                "NormalizePagingWrapper",
                Times.Once(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<int>());
        }
        #endregion
    }
}
