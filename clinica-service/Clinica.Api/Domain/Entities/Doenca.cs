namespace Clinica.Api.Domain.Entities
{
    public class Doenca
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        
        public ICollection<Sintoma> Sintomas { get; set; } = new List<Sintoma>();
    }
}