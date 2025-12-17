using Clinica.Api.Domain.Enums;

namespace Clinica.Api.DTOs
{
    public class SintomaCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public PrioridadeSintoma Prioridade { get; set; }
        public Guid DoencaId { get; set; }
    }
}