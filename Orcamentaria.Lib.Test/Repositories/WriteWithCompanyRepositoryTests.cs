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
    public class WriteWithCompanyRepositoryTests<T, C>
        where T : class
        where C : DbContext
    {
        private readonly BaseFixture<T> _fixture;
        protected readonly C _dbContext;
        protected readonly DbContextOptions<DbContext> _options;
        protected readonly UserAuthContext _userAuthContext;

        public WriteWithCompanyRepositoryTests(BaseFixture<T> fixture)
        {
            _fixture = fixture;
            _userAuthContext = _fixture.CreateUserAuthContext();

            _options = new DbContextOptionsBuilder<DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

            _dbContext = (C)Activator.CreateInstance(typeof(C), _options);
        }

        #region InsertAsync
        [Fact]
        public async Task InsertAsync_WhenValidBody_ReturnsEntity()
        {
            var entityInsert = _fixture.CreateEntity(0);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            var result = await mockRepository.Object.InsertAsync(entityInsert);

            result.Should().NotBeNull();

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "companyid"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "createdat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "createdby"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedby"));
        }

        #endregion

        #region UpdateAsync
        [Fact]
        public async Task UpdateAsync_WhenValidBody_ReturnsEntity()
        {
            var entityUpdate = _fixture.CreateEntity(1);

            await _fixture.SeedInMemoryDatabase(_dbContext, 1);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            var result = await mockRepository.Object.UpdateAsync(1, entityUpdate);

            result.Should().NotBeNull();

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "companyid"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "createdat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "createdby"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Once(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedby"));

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));
        }

        [Fact]
        public async Task UpdateAsync_WhenIdNotFound_ThrowsNotFoundException()
        {
            var entityUpdate = _fixture.CreateEntity(1);

            await _fixture.SeedInMemoryDatabase(_dbContext, 2);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.UpdateAsync(1, entityUpdate);

            var exception = await act.Should().ThrowAsync<NotFoundException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.NotFound);

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "companyid"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "createdat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "createdby"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedby"));

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));
        }

        [Fact]
        public async Task UpdateAsync_WhenCompanyMismatch_ThrowsNotFoundException()
        {
            var userAuthContext = _fixture.CreateUserAuthContext();
            userAuthContext.CompanyId = 99999;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, userAuthContext) { CallBase = true };

            var entityUpdate = _fixture.CreateEntity(1);

            await _fixture.SeedInMemoryDatabase(_dbContext, 2);

            Func<Task> act = async () => await mockRepository.Object.UpdateAsync(1, entityUpdate);

            var exception = await act.Should().ThrowAsync<NotFoundException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.NotFound);

            mockRepository.Protected()
               .Verify(
               "HasFieldMapWrapper",
               Times.Never(),
               ItExpr.Is<string>(x => x.ToLower() == "companyid"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "createdat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "createdby"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedby"));

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == userAuthContext.CompanyId));
        }

        [Fact]
        public async Task UpdateAsync_WhenAnyExceptionIsThrownInApplyFiltersWithCompanyIdWrapper_ThrowsUnexpectedException()
        {
            var entityUpdate = _fixture.CreateEntity(1);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            mockRepository.Protected()
                .Setup<IQueryable<T>>(
                    "ApplyFiltersWithCompanyIdWrapper",
                    ItExpr.IsAny<IQueryable<T>>(),
                    ItExpr.IsAny<IEnumerable<FilterParam>>(),
                    ItExpr.IsAny<long>())
                .Throws(new Exception());

            await _fixture.SeedInMemoryDatabase(_dbContext, 2);

            Func<Task> act = async () => await mockRepository.Object.UpdateAsync(1, entityUpdate);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
               .Verify(
               "HasFieldMapWrapper",
               Times.Never(),
               ItExpr.Is<string>(x => x.ToLower() == "companyid"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "createdat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "createdby"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedat"));

            mockRepository.Protected()
                .Verify(
                "HasFieldMapWrapper",
                Times.Never(),
                ItExpr.Is<string>(x => x.ToLower() == "updatedby"));

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
