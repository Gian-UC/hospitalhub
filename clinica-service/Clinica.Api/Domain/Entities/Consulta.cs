using Clinica.Api.Domain.Enums;

namespace Clinica.Api.Domain.Entities
{
    public class Consulta 
    {
        public Guid Id { get; set; }
        public Guid AgendamentoId { get; set; }
        public Guid PacienteId { get; set; }

        public DateTime DataHora { get; set; }

        public int Tipo { get; set; }
        public StatusConsulta Status { get; set; } = StatusConsulta.Pendente;

        public DateTime CriadoEm { get; set; }
        public ICollection<ConsultaSintoma> Sintomas { get; set; } = new List<ConsultaSintoma>();
    }
}