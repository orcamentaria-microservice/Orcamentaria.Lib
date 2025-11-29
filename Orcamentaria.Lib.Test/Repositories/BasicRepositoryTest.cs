using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Orcamentaria.Lib.Test.Contexts;
using Orcamentaria.Lib.Test.Fixtures;
using Orcamentaria.Lib.Domain.Enums;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Infrastructure.Contexts;
using Orcamentaria.Lib.Infrastructure.Repositories;
using Xunit;

namespace Orcamentaria.EquipmentService.Test.Repositories
{
    public abstract class BasicRepositoryTest<T, C> 
        where T : class
        where C : DbContextTest
    {
        private readonly BaseFixture<T> _fixture;
        private readonly DbContextTest _dbContext;
        private readonly DbContextOptions<DbContextTest> _options;
        private readonly UserAuthContext _userAuthContext;

        public BasicRepositoryTest(BaseFixture<T> fixture)
        {
            _fixture = fixture;
            _userAuthContext = _fixture.CreateUserAuthContext();

            _options = new DbContextOptionsBuilder<DbContextTest>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

            _dbContext = (C)Activator.CreateInstance(typeof(C), _options);
        }

        #region GetByIdAsync
        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task GetByIdAsync_WhenHaveData_ReturnsData(long id)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var result = await mockRepository.Object.GetByIdAsync(id);

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
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(9)]
        public async Task GetByIdAsync_WhenNotHaveData_ReturnsNull(long id)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var result = await mockRepository.Object.GetByIdAsync(id);

            result.Should().BeNull();

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
        public async Task GetByIdAsync_WhenCompanyMismatch_ReturnsNull(long id)
        {
            var userAuthContext = _fixture.CreateUserAuthContext();
            userAuthContext.CompanyId = 99999;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var result = await mockRepository.Object.GetByIdAsync(id);

            result.Should().BeNull();

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
        public async Task GetByIdAsync_WhenAnyExceptionIsThrown(long id)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            mockRepository.Protected()
            .Setup<IQueryable<T>>(
                "ApplyFiltersWithCompanyIdWrapper",
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.IsAny<long>())
            .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetByIdAsync(id);

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

        #region GetAsync
        [Xunit.Theory]
        [InlineData("id", "gt", 1)]
        [InlineData("id", "ne", 3)]
        [InlineData("id", "in", "1, 3")]
        [InlineData("id", "in", "1, 3, 9")]
        public async Task GetAsync_WhenHaveData_ReturnsData(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            var result = await mockRepository.Object.GetAsync(gridParams);

            result.Item1.Should().HaveCount(2);
            result.Item2.TotalItems.Should().Be(2);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));

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
        [InlineData("companyId", "eq", 21)]
        [InlineData("id", "in", "44")]
        public async Task GetAsync_WhenNotHaveData_ReturnsEmpty(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            var result = await mockRepository.Object.GetAsync(gridParams);

            result.Item1.Should().HaveCount(0);
            result.Item2.TotalItems.Should().Be(0);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));

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
        public async Task GetAsync_WhenCompanyMismatch_ReturnsEmpty(string field, string op, object value)
        {
            var userAuthContext = _fixture.CreateUserAuthContext();
            userAuthContext.CompanyId = 99999;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            var result = await mockRepository.Object.GetAsync(gridParams);

            result.Item1.Should().HaveCount(0);
            result.Item2.TotalItems.Should().Be(0);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == userAuthContext.CompanyId));

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
        public async Task GetAsync_WhenAnyExceptionIsThrownInApplyFiltersWithCompanyIdWrapper(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            mockRepository.Protected()
                .Setup<IQueryable<T>>(
                    "ApplyFiltersWithCompanyIdWrapper",
                    ItExpr.IsAny<IQueryable<T>>(),
                    ItExpr.IsAny<IEnumerable<FilterParam>>(),
                    ItExpr.IsAny<long>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetAsync(gridParams);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));

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
        public async Task GetAsync_WhenAnyExceptionIsThrownInApplySortingWrapper(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            mockRepository.Protected()
                .Setup<IQueryable<T>>(
                    "ApplySortingWrapper",
                    ItExpr.IsAny<IQueryable<T>>(),
                    ItExpr.IsAny<string?>(),
                    ItExpr.IsAny<bool>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetAsync(gridParams);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));

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
        public async Task GetAsync_WhenAnyExceptionIsThrownInNormalizePagingWrapper(string field, string op, object value)
        {
            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            await _fixture.SeedInMemoryDatabase(_dbContext, 1, 3, 6);

            var gridParams = _fixture.CreateGridParamsWithOneFilter(new FilterParam { Field = field, Operator = op, Value = value });

            mockRepository.Protected()
                .Setup<(int, int, int)>(
                    "NormalizePagingWrapper",
                    ItExpr.IsAny<int>(),
                    ItExpr.IsAny<int>(),
                    ItExpr.IsAny<int>())
                .Throws(new Exception());

            Func<Task> act = async () => await mockRepository.Object.GetAsync(gridParams);

            var exception = await act.Should().ThrowAsync<UnexpectedException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.InternalError);

            mockRepository.Protected()
                .Verify(
                "ApplyFiltersWithCompanyIdWrapper",
                Times.Once(),
                ItExpr.IsAny<IQueryable<T>>(),
                ItExpr.IsAny<IEnumerable<FilterParam>>(),
                ItExpr.Is<long>(x => x == _userAuthContext.CompanyId));

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

        [Fact]
        public async Task InsertAsync_WhenDbUpdateExceptionIsThrows()
        {
            var entityInsert = _fixture.CreateEntity(0);

            _dbContext.IsDbUpdateExceptionSaveChangesAsync = true;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.InsertAsync(entityInsert);

            var exception = await act.Should().ThrowAsync<DatabaseException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.DatabaseError);

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

        [Fact]
        public async Task InsertAsync_WhenExceptionIsThrows()
        {
            var entityInsert = _fixture.CreateEntity(0);

            _dbContext.IsExceptionSaveChangesAsync = true;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.InsertAsync(entityInsert);

            var exception = await act.Should().ThrowAsync<DatabaseException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.DatabaseError);

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
        }

        [Fact]
        public async Task UpdateAsync_WhenIdNotFound_ThrowsKeyNotFoundException()
        {
            var entityUpdate = _fixture.CreateEntity(1);

            await _fixture.SeedInMemoryDatabase(_dbContext, 2);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.UpdateAsync(1, entityUpdate);

            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();

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
        }

        [Fact]
        public async Task UpdateAsync_WhenDbUpdateExceptionIsThrows()
        {
            var entityUpdate = _fixture.CreateEntity(1);

            await _fixture.SeedInMemoryDatabase(_dbContext, 1);

            _dbContext.IsDbUpdateExceptionSaveChangesAsync = true;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.UpdateAsync(1, entityUpdate);

            var exception = await act.Should().ThrowAsync<DatabaseException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.DatabaseError);

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

        [Fact]
        public async Task UpdateAsync_WhenExceptionIsThrows()
        {
            var entityUpdate = _fixture.CreateEntity(1);

            await _fixture.SeedInMemoryDatabase(_dbContext, 1);

            _dbContext.IsExceptionSaveChangesAsync = true;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.UpdateAsync(1, entityUpdate);

            var exception = await act.Should().ThrowAsync<DatabaseException>();
            exception.Which.ErrorCode.Should().Be((int)ErrorCodeEnum.DatabaseError);

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

        #region DeleteAsync
        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenIdFound(long id)
        {
            await _fixture.SeedInMemoryDatabase(_dbContext, id);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            var result = await mockRepository.Object.DeleteAsync(id);

            result.Should().NotBeNull();
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenIdNotFound(long id)
        {
            await _fixture.SeedInMemoryDatabase(_dbContext, 999);

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.DeleteAsync(id);

            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public async Task DeleteAsync_WhenExceptionIsThrows(long id)
        {
            await _fixture.SeedInMemoryDatabase(_dbContext, id);

            _dbContext.IsExceptionSaveChangesAsync = true;

            var mockRepository = new Mock<BaseRepository<T>>(_dbContext, _userAuthContext) { CallBase = true };

            Func<Task> act = async () => await mockRepository.Object.DeleteAsync(id);

            var exception = await act.Should().ThrowAsync<DatabaseException>();
        }
        #endregion
    }
}
