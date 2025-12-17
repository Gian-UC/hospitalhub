using Clinica.Api.Domain.Enums;

namespace Clinica.Api.Domain.Entities
{
    public class Sintoma
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public PrioridadeSintoma Prioridade { get; set; }

        public Guid DoencaId { get; set; }
        public Doenca Doenca { get; set; } = null!;
    }
}