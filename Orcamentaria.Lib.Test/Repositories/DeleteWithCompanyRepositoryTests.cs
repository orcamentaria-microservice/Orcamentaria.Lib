using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Exceptions;
using Orcamentaria.Lib.Infrastructure.Contexts;
using Orcamentaria.Lib.Infrastructure.Repositories;
using Orcamentaria.Lib.Test.Fixtures;
using Xunit;

namespace Orcamentaria.Lib.Test.Repositories
{
    public class DeleteWithCompanyRepositoryTests<T, C>
        where T : class
        where C : DbContext
    {
        private readonly BaseFixture<T> _fixture;
        protected readonly C _dbContext;
        protected readonly DbContextOptions<DbContext> _options;
        protected readonly UserAuthContext _userAuthContext;

        public DeleteWithCompanyRepositoryTests(BaseFixture<T> fixture)
        {
            _fixture = fixture;
            _userAuthContext = _fixture.CreateUserAuthContext();

            _options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

            _dbContext = (C)Activator.CreateInstance(typeof(C), _options);
        }

        #region DeleteAsync
        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenIdFound_ReturnData(long id)
        {
            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, id);
            _dbContext.ChangeTracker.Clear();

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            var result = await mockRepository.Object.DeleteAsync(id);

            result.Should().NotBeNull();

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenIdNotFound_ThrowNotFoundException(long id)
        {
            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, 999);
            _dbContext.ChangeTracker.Clear();

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.DeleteAsync(id);

            var exception = await act.Should().ThrowAsync<NotFoundException>();

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenCompanyMismatch_ThrowsNotFoundException(long id)
        {
            var userAuthContext = _fixture.CreateUserAuthContext();
            userAuthContext.CompanyId = 99999;

            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, id);
            _dbContext.ChangeTracker.Clear();

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.DeleteAsync(id);

            var exception = await act.Should().ThrowAsync<NotFoundException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.NotFound);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == userAuthContext.CompanyId));
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenAnyExceptionIsThrownInApplyFiltersWithCompanyIdWrapper_ThrowsUnexpectedException(long id)
        {
            await _fixture.SeedInMemoryDatabaseWithIds(_dbContext, id);
            _dbContext.ChangeTracker.Clear();

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            mockRepository.Protected()
                .Setup<IQueryable<T>>(
                    "ApplyFiltersWithCompanyIdWrapper",
                    ItExpr.IsAny<IQueryable<T>>(),
                    ItExpr.IsAny<IEnumerable<FilterParam>>(),
                    ItExpr.IsAny<long>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.DeleteAsync(id);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));
        }
        #endregion
    }
}
