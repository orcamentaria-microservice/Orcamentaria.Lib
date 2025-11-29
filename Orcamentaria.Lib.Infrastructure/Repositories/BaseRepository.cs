using Microsoft.EntityFrameworkCore;
using Orcamentaria.Lib.Domain.Contexts;
using Orcamentaria.Lib.Domain.Exceptions;
using Orcamentaria.Lib.Domain.Models;
using Orcamentaria.Lib.Domain.Models.Responses;
using Orcamentaria.Lib.Domain.Repositories;
using Orcamentaria.Lib.Infrastructure.Helpers;
using System.Linq.Expressions;

namespace Orcamentaria.Lib.Infrastructure.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        private readonly IUserAuthContext _userAuthContext;


        public BaseRepository(
            DbContext context,
            IUserAuthContext userAuthContext)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
            _userAuthContext = userAuthContext;
        }

        public virtual async Task<TEntity?> GetByIdAsync(long id, params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                var query = _dbSet.AsNoTracking().AsQueryable();

                foreach (var include in includes)
                    query = query.Include(include);

                var filters = new List<FilterParam>
                {
                    new FilterParam
                    {
                        Field = "Id",
                        Operator = "eq",
                        Value = id.ToString()
                    }
                };

                query = ApplyFiltersWithCompanyIdWrapper(query, filters, _userAuthContext.CompanyId);

                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new UnexpectedException($"Erro inesperado na busca de dados: {ex.Message}");
            }
        }

        public virtual async Task<(IEnumerable<TEntity?>, ResponsePagination pagination)> GetAsync(
            GridParams gridParams,
            params Expression<Func<TEntity, object>>[] includes)
        {

            try
            {
                var query = _dbSet.AsNoTracking().AsQueryable();

                foreach (var include in includes)
                    query = query.Include(include);

                query = ApplyFiltersWithCompanyIdWrapper(query, gridParams.Filters, _userAuthContext.CompanyId);

                query = ApplySortingWrapper(query, gridParams.SortField, gridParams.SortDesc ?? false);

                var (page, pageSize, skip) = NormalizePagingWrapper(gridParams.Page, gridParams.PageSize);

                try
                {
                    return (
                        await query.Skip(skip).Take(pageSize).ToListAsync(), 
                        new ResponsePagination(page, pageSize, await query.CountAsync()));
                }
                catch (Exception ex)
                {
                    throw new DatabaseException($"Erro na busca de dados: {ex.Message}");
                }
            }
            catch (DefaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UnexpectedException($"Erro inesperado na busca de dados: {ex.Message}");
            }
        }

        public virtual async Task<TEntity> InsertAsync(TEntity entity)
        {
            if (HasFieldMapWrapper("CompanyId"))
                _context.Entry(entity).Property("CompanyId").CurrentValue = _userAuthContext.CompanyId;

            if (HasFieldMapWrapper("CreatedAt"))
                _context.Entry(entity).Property("CreatedAt").CurrentValue = DateTime.Now;

            if (HasFieldMapWrapper("CreatedBy"))
                _context.Entry(entity).Property("CreatedBy").CurrentValue = _userAuthContext.UserId;

            if (HasFieldMapWrapper("UpdatedAt"))
                _context.Entry(entity).Property("UpdatedAt").CurrentValue = DateTime.Now;

            if (HasFieldMapWrapper("UpdatedBy"))
                _context.Entry(entity).Property("UpdatedBy").CurrentValue = _userAuthContext.UserId;

            try
            {
                _dbSet.Add(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException($"Entidade inválida: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Erro inesperado ao atualizar entidade: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntity> UpdateAsync(long id, TEntity entity)
        {
            var existing = await _dbSet.FindAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Entity with id {id} not found.");

            if (HasFieldMapWrapper("CompanyId"))
                _context.Entry(entity).Property("CompanyId").CurrentValue = _context.Entry(existing).Property("CompanyId").OriginalValue;

            if (HasFieldMapWrapper("CreatedAt"))
                _context.Entry(entity).Property("CreatedAt").CurrentValue = _context.Entry(existing).Property("CreatedAt").OriginalValue;

            if (HasFieldMapWrapper("CreatedBy"))
                _context.Entry(entity).Property("CreatedBy").CurrentValue = _context.Entry(existing).Property("CreatedBy").OriginalValue;

            if (HasFieldMapWrapper("UpdatedAt"))
                _context.Entry(entity).Property("UpdatedAt").CurrentValue = DateTime.Now;

            if (HasFieldMapWrapper("UpdatedBy"))
                _context.Entry(entity).Property("UpdatedBy").CurrentValue = _userAuthContext.UserId;

            try
            {
                _context.Entry(existing).CurrentValues.SetValues(entity);
                await _context.SaveChangesAsync();
                return existing;
            }
            catch (DbUpdateException ex)
            {
                throw new DatabaseException($"Entidade inválida: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Erro inesperado ao salvar entidade: {ex.Message}", ex);
            }
        }

        public virtual async Task<TEntity> DeleteAsync(long id)
        {
            var existing = await _dbSet.FindAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Entity with id {id} not found.");

            try
            {
                _dbSet.Remove(existing);
                await _context.SaveChangesAsync();
                return existing;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Erro inesperado ao excluir entidade: {ex.Message}", ex);
            }
        }

        #region wrappers
            protected virtual IQueryable<TEntity> ApplyFiltersWithCompanyIdWrapper(
            IQueryable<TEntity> query,
            IEnumerable<FilterParam>? filters,
            long companyId)
            => GridQueryHelper.ApplyFiltersWithCompanyId(query, filters, companyId);

        protected virtual IQueryable<TEntity> ApplySortingWrapper(
            IQueryable<TEntity> query,
            string? sortField,
            bool sortDesc)
            => GridQueryHelper.ApplySorting(query, sortField, sortDesc);

        protected virtual (int Page, int PageSize, int Skip) NormalizePagingWrapper(
            int page, 
            int pageSize, 
            int maxPageSize = 100)
            => GridQueryHelper.NormalizePaging(page, pageSize, maxPageSize);

        protected virtual bool HasFieldMapWrapper(string field) => GridQueryHelper.HasFieldMap<TEntity>(field);
        #endregion
    }
}
