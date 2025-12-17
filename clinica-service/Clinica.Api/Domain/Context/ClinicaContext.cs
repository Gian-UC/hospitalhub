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

        public DbSet<ConsultaSintoma> ConsultaSintomas => Set<ConsultaSintoma>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8mb4");
            modelBuilder.UseCollation("utf8mb4_general_ci");

            modelBuilder.Entity<Consulta>()
                .Property(c => c.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Sintoma>()
                .Property(s => s.Prioridade)
                .HasConversion<int>();

            modelBuilder.Entity<ConsultaSintoma>()
                .HasKey(cs => new { cs.ConsultaId, cs.SintomaId });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Doenca> Doencas => Set<Doenca>();
        public DbSet<Sintoma> Sintomas => Set<Sintoma>();
    }
}