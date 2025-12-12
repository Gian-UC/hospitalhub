namespace Cirurgico.Api.Domain.Entities
{
    public class Cirurgia
    {
        public Guid Id { get; set; }
        public Guid AgendamentoId { get; set; }
        public Guid PacienteId { get; set; }

        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }

        public CirurgiaStatus Status { get; set; } = CirurgiaStatus.Agendada;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}