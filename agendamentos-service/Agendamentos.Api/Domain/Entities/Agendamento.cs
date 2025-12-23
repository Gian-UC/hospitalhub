namespace Agendamentos.Api.Domain.Entities
{
    public class Agendamento
    {
        public Guid Id { get; set; }
        public Guid PacienteId { get; set; }
        public DateTime DataHora { get; set; }
        public TipoAgendamento Tipo { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public bool Emergencial { get; set; }
        public bool Confirmado { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public Paciente? Paciente { get; set; }
    }
}