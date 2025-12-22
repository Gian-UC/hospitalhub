using Agendamentos.Api.DTOs;
using Agendamentos.Api.Domain.Context;
using Agendamentos.Api.Domain.Entities;
using Agendamentos.Api.Messaging.Events;
using Agendamentos.Api.Messaging.Producer;
using Agendamentos.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Agendamentos.Api.Services.Implementations
{
    public class AgendamentoService : IAgendamentoService
    {
        private readonly HospitalAgendamentosContext _context;
        private readonly IAgendamentoConfirmadoProducer _producer;
        private readonly IDistributedCache? _cache;

        private const string ListarCacheKey = "agendamentos:list:v1";
        private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

        public AgendamentoService(
            HospitalAgendamentosContext context,
            IAgendamentoConfirmadoProducer producer,
            IDistributedCache? cache = null)
        {
            _context = context;
            _producer = producer;
            _cache = cache;
        }

        public async Task<AgendamentoResponseDto> CriarAsync(AgendamentoCreateDto dto)
        {
            var paciente = await _context.Pacientes.FindAsync(dto.PacienteId);
            if (paciente == null)
                throw new KeyNotFoundException("Paciente não encontrado.");

            var conflito = await _context.Agendamentos
                .AnyAsync(a => a.PacienteId == dto.PacienteId && a.DataHora == dto.DataHora);

            if (conflito)
                throw new InvalidOperationException("Já existe um agendamento para este paciente no mesmo horário.");   

            var ag = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = dto.PacienteId,
                DataHora = dto.DataHora,
                Tipo = (TipoAgendamento)dto.Tipo,
                Descricao = dto.Descricao,
                Confirmado = false,
                DataCriacao = DateTime.UtcNow,
                Emergencial = dto.Emergencial
            };

            _context.Agendamentos.Add(ag);
            await _context.SaveChangesAsync();

            if (_cache != null)
                await _cache.RemoveAsync(ListarCacheKey);

            return await MapToResponseDto(ag);
        }

        public async Task<IEnumerable<AgendamentoResponseDto>> ListarAsync()
        {
            if (_cache != null)
            {
                var cached = await _cache.GetStringAsync(ListarCacheKey);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    var fromCache = JsonSerializer.Deserialize<List<AgendamentoResponseDto>>(cached, CacheJsonOptions);
                    if (fromCache != null)
                        return fromCache;
                }
            }

            var lista = await _context.Agendamentos
                .Include(a => a.Paciente)
                .ToListAsync();

            var retorno = new List<AgendamentoResponseDto>();

            foreach (var ag in lista)
                retorno.Add(await MapToResponseDto(ag));

            if (_cache != null)
            {
                var payload = JsonSerializer.Serialize(retorno, CacheJsonOptions);
                await _cache.SetStringAsync(
                    ListarCacheKey,
                    payload,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                    });
            }

            return retorno;
        }

        public async Task<AgendamentoResponseDto?> BuscarPorIdAsync(Guid id)
        {
            var ag = await _context.Agendamentos
                .Include(a => a.Paciente)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ag == null) return null;

            return await MapToResponseDto(ag);
        }

        public async Task<bool> ConfirmarAsync(Guid id)
        {
            var ag = await _context.Agendamentos
                .Include(a => a.Paciente)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ag == null)
                return false;

            ag.Confirmado = true;
            await _context.SaveChangesAsync();

            if (_cache != null)
                await _cache.RemoveAsync(ListarCacheKey);

            var evt = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = ag.Id,
                PacienteId = ag.PacienteId,
                DataHora = ag.DataHora,
                Tipo = (int)ag.Tipo,
                Emergencial = ag.Emergencial
            };

            _producer.Publicar(evt);

            return true;
        }

        public async Task<bool> CancelarAsync(Guid id)
        {
            var ag = await _context.Agendamentos.FindAsync(id);
            if (ag == null)
                return false;

            _context.Agendamentos.Remove(ag);
            await _context.SaveChangesAsync();

            if (_cache != null)
                await _cache.RemoveAsync(ListarCacheKey);

            return true;
        }

        private Task<AgendamentoResponseDto> MapToResponseDto(Agendamento ag)
        {
            return Task.FromResult(new AgendamentoResponseDto
            {
                Id = ag.Id,
                Paciente = new PacienteDto
                {
                    Id = ag.Paciente!.Id,
                    Nome = ag.Paciente.Nome,
                    Documento = ag.Paciente.Documento,
                    DataNascimento = ag.Paciente.DataNascimento,
                    Telefone = ag.Paciente.Telefone,
                    Email = ag.Paciente.Email
                },
                DataHora = ag.DataHora,
                Tipo = (int)ag.Tipo,
                Descricao = ag.Descricao,
                Confirmado = ag.Confirmado,
                DataCriacao = ag.DataCriacao
            });
        }
    }
}
