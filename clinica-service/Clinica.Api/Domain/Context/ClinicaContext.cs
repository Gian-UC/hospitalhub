using Clinica.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Api.Domain.Context
{
    public class ClinicaContext : DbContext
    {
        public ClinicaContext(DbContextOptions<ClinicaContext> options) : base(options)
        {
        }

        public DbSet<Consulta> Consultas => Set<Consulta>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Consulta>()
                .Property(c => c.Status)
                .HasConversion<int>();

            base.OnModelCreating(modelBuilder);
        }
    }
}