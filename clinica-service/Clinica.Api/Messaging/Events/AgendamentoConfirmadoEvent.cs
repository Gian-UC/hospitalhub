namespace Clinica.Api.Messaging.Events
{
    public class AgendamentoConfirmadoEvent
    {
        public Guid AgendamentoId { get; set; }
        public Guid PacienteId { get; set; }
        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }
        public bool Emergencial { get; set; }
    }
}
