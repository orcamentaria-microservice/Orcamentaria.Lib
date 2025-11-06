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
    public class BasicRepository<TEntity> : IBasicRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        private readonly IUserAuthContext _userAuthContext;


        public BasicRepository(
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

                query = GridQuery.ApplyFilters(query, filters, _userAuthContext.CompanyId);

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

                query = GridQuery.ApplyFilters(query, gridParams.Filters, _userAuthContext.CompanyId);

                query = GridQuery.ApplySorting(query, gridParams.SortField, gridParams.SortDesc ?? false);

                var (page, pageSize, skip) = GridQuery.NormalizePaging(gridParams.Page, gridParams.PageSize);

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
            if (GridQuery.HasFieldMap<TEntity>("CompanyId"))
                _context.Entry(entity).Property("CompanyId").CurrentValue = _userAuthContext.CompanyId;

            if (GridQuery.HasFieldMap<TEntity>("CreatedAt"))
                _context.Entry(entity).Property("CreatedAt").CurrentValue = DateTime.Now;

            if (GridQuery.HasFieldMap<TEntity>("CreatedBy"))
                _context.Entry(entity).Property("CreatedBy").CurrentValue = _userAuthContext.UserId;

            if (GridQuery.HasFieldMap<TEntity>("UpdatedAt"))
                _context.Entry(entity).Property("UpdatedAt").CurrentValue = DateTime.Now;

            if (GridQuery.HasFieldMap<TEntity>("UpdatedBy"))
                _context.Entry(entity).Property("UpdatedBy").CurrentValue = _userAuthContext.UserId;


            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<TEntity> UpdateAsync(long id, TEntity entity)
        {
            var existing = await _dbSet.FindAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Entity with id {id} not found.");

            if (GridQuery.HasFieldMap<TEntity>("CompanyId"))
                _context.Entry(entity).Property("CompanyId").CurrentValue = _context.Entry(existing).Property("CompanyId").OriginalValue;

            if (GridQuery.HasFieldMap<TEntity>("CreatedAt"))
                _context.Entry(entity).Property("CreatedAt").CurrentValue = _context.Entry(existing).Property("CreatedAt").OriginalValue;

            if (GridQuery.HasFieldMap<TEntity>("CreatedBy"))
                _context.Entry(entity).Property("CreatedBy").CurrentValue = _context.Entry(existing).Property("CreatedBy").OriginalValue;

            if (GridQuery.HasFieldMap<TEntity>("UpdatedAt"))
                _context.Entry(entity).Property("UpdatedAt").CurrentValue = DateTime.Now;

            if (GridQuery.HasFieldMap<TEntity>("UpdatedBy"))
                _context.Entry(entity).Property("UpdatedBy").CurrentValue = _userAuthContext.UserId;

            _context.Entry(existing).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return existing;
        }

        public virtual async Task<TEntity> DeleteAsync(long id)
        {
            var existing = await _dbSet.FindAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Entity with id {id} not found.");

            _dbSet.Remove(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
