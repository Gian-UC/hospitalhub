namespace Clinica.Api.Domain.Entities
{
    public class ConsultaSintoma
    {
        public Guid ConsultaId { get; set; }
        public Consulta Consulta { get; set; } = null!; 
        public Guid SintomaId { get; set; }
        public Sintoma Sintoma { get; set; } = null!; 
    }
}