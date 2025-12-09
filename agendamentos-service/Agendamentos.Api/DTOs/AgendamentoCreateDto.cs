using System;

namespace Agendamentos.Api.DTOs
{
    public class AgendamentoCreateDto
    {
        public Guid PacienteId { get; set; }
        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }
}