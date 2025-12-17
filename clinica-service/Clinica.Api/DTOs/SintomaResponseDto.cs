using Clinica.Api.Domain.Enums;

namespace Clinica.Api.DTOs
{
    public class SintomaResponseDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public PrioridadeSintoma Prioridade { get; set; }
        public Guid DoencaId { get; set; }
    }
}