namespace Clinica.Api.DTOs
{
    public class DoencaSugeridaDto
    {
        public Guid DoencaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeSintomas { get; set; }
        public int MaiorPrioridade { get; set; }
    }
}