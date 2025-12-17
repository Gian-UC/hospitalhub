using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clinica.Api.DTOs
{
    public class ConsultaCreateDto
    {
        public Guid AgendamentoId { get; set; }
        public Guid PacienteId { get; set; }
        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }
    }
}