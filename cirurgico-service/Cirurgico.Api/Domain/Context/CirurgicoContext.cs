using Cirurgico.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cirurgico.Api.Domain.Context
{
    public class CirurgicoContext : DbContext
    {
        public CirurgicoContext(DbContextOptions<CirurgicoContext> options)
            : base(options)
        {
        }

        public DbSet<Cirurgia> Cirurgias { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cirurgia>()
                .Property(c => c.Status)
                .HasConversion<int>();

            base.OnModelCreating(modelBuilder);    
        }
    }
}
