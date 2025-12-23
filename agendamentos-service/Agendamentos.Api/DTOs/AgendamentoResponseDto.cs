using System;

namespace Agendamentos.Api.DTOs
{
    public class AgendamentoResponseDto
    {
        public Guid Id { get; set; }
        public PacienteDto Paciente { get; set; } = new PacienteDto();
        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public bool Confirmado { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}