namespace Notificacao.Api.Services.Pacientes
{
    public class PacienteDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = "";
        public string Documento { get; set; } = "";
        public DateTime DataNascimento { get; set; }
        public string Telefone { get; set; } = "";
        public string Email { get; set; } = "";
    }
}