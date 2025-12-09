using Agendamentos.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agendamentos.Api.Domain.Context
{
    public class HospitalAgendamentosContext : DbContext
    {
        public HospitalAgendamentosContext(DbContextOptions<HospitalAgendamentosContext> options) : base(options)
        {
        }

        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Paciente>()
                .HasMany(p => p.Agendamentos)
                .WithOne(a => a.Paciente)
                .HasForeignKey(a => a.PacienteId);
        }
    }   
}