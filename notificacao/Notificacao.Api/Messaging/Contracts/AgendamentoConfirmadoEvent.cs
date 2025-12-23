using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Notificacao.Api.Messaging.Contracts
{
    public class AgendamentoConfirmadoEvent
    {
        public Guid Id { get; set; }
        public Guid PacienteId { get; set; }
        public DateTime DataHora { get; set; }
        public int Tipo { get; set; }
        public string? Descricao { get; set; }
        public bool Emergencial { get; set; }
    }
}