namespace Clinica.Api.DTOs
{
    public class ConsultaResponseDto
    {
        public Guid Id { get; set; }
        public Guid AgendamentoId { get; set; }
        public Guid PacienteId { get; set; }
        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }
        public int Status { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}