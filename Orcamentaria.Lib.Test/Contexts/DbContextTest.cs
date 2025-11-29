using Microsoft.EntityFrameworkCore;

namespace Orcamentaria.Lib.Test.Contexts
{
    public abstract class DbContextTest : DbContext
    {
        public bool IsDbUpdateExceptionSaveChangesAsync { get; set; } = false;
        public bool IsExceptionSaveChangesAsync { get; set; } = false;
        public DbContextTest(DbContextOptions<DbContextTest> opts) : base(opts) { }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (IsDbUpdateExceptionSaveChangesAsync)
                throw new DbUpdateException();

            if (IsExceptionSaveChangesAsync)
                throw new Exception();

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
