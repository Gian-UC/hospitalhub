using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cirurgico.Api.Domain.Entities
{
    public enum CirurgiaStatus
{
    Pendente = 0,
    Agendada = 1,
    EmAndamento = 2,
    Concluida = 3,
    Cancelada = 4
}
}