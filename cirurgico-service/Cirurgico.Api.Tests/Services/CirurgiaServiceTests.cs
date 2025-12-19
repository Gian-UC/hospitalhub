using Cirurgico.Api.Domain.Context;
using Cirurgico.Api.Domain.Entities;
using Cirurgico.Api.Messaging.Events;
using Cirurgico.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cirurgico.Api.Tests.Services
{
    public class CirurgiaServiceTests
    {
        private readonly CirurgicoContext _context;
        private readonly CirurgiaService _service;

        public CirurgiaServiceTests()
        {
            var options = new DbContextOptionsBuilder<CirurgicoContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CirurgicoContext(options);
            _service = new CirurgiaService(_context);
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodasCirurgias()
        {
            // Arrange
            var cirurgia1 = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 3,
                Status = CirurgiaStatus.Pendente,
                Emergencial = false,
                CriadoEm = DateTime.UtcNow
            };

            var cirurgia2 = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(2),
                Tipo = 3,
                Status = CirurgiaStatus.Agendada,
                Emergencial = false,
                CriadoEm = DateTime.UtcNow
            };

            _context.Cirurgias.AddRange(cirurgia1, cirurgia2);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ListarAsync();

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.Count());
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarCirurgia_QuandoExiste()
        {
            // Arrange
            var cirurgia = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 3,
                Status = CirurgiaStatus.Pendente,
                Emergencial = false,
                CriadoEm = DateTime.UtcNow
            };

            _context.Cirurgias.Add(cirurgia);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.BuscarPorIdAsync(cirurgia.Id);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(cirurgia.Id, resultado.Id);
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarNull_QuandoNaoExiste()
        {
            // Act
            var resultado = await _service.BuscarPorIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public async Task RegistrarCirurgiaAsync_DeveAdicionarCirurgia()
        {
            // Arrange
            var cirurgia = new Cirurgia
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 3,
                Emergencial = false
            };

            // Act
            var resultado = await _service.RegistrarCirurgiaAsync(cirurgia);

            // Assert
            Assert.NotEqual(Guid.Empty, resultado.Id);
            Assert.True(resultado.CriadoEm > DateTime.MinValue);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveAtualizarStatus_QuandoCirurgiaExiste()
        {
            // Arrange
            var cirurgia = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 3,
                Status = CirurgiaStatus.Pendente,
                Emergencial = false,
                CriadoEm = DateTime.UtcNow
            };

            _context.Cirurgias.Add(cirurgia);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.AtualizarStatusAsync(cirurgia.Id, CirurgiaStatus.EmAndamento);

            // Assert
            Assert.True(resultado);
            var cirurgiaAtualizada = await _context.Cirurgias.FindAsync(cirurgia.Id);
            Assert.Equal(CirurgiaStatus.EmAndamento, cirurgiaAtualizada!.Status);
        }

        [Fact]
        public async Task AtualizarStatusAsync_DeveRetornarFalse_QuandoCirugiaNaoExiste()
        {
            // Act
            var resultado = await _service.AtualizarStatusAsync(Guid.NewGuid(), CirurgiaStatus.Agendada);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task RegistrarCirurgiaPorAgendamentoAsync_DeveAdicionarCirurgia_QuandoDadosValidos()
        {
            // Arrange
            var evento = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 3,
                Emergencial = false
            };

            // Act
            await _service.RegistrarCirurgiaPorAgendamentoAsync(evento);

            // Assert
            var cirurgias = await _context.Cirurgias.ToListAsync();
            Assert.Single(cirurgias);
            Assert.Equal(evento.AgendamentoId, cirurgias[0].AgendamentoId);
            Assert.Equal(evento.PacienteId, cirurgias[0].PacienteId);
            Assert.Equal(CirurgiaStatus.Pendente, cirurgias[0].Status);
        }

        [Fact]
        public async Task RegistrarCirurgiaPorAgendamentoAsync_DeveLancarExcecao_QuandoHaCirurgiaNaoEmergencialNoHorario()
        {
            // Arrange
            var dataHora = DateTime.UtcNow.AddDays(2);
            var cirurgiaExistente = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 3,
                Status = CirurgiaStatus.Agendada,
                Emergencial = false,
                CriadoEm = DateTime.UtcNow
            };
            _context.Cirurgias.Add(cirurgiaExistente);
            await _context.SaveChangesAsync();

            var evento = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 3,
                Emergencial = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RegistrarCirurgiaPorAgendamentoAsync(evento));
        }

        [Fact]
        public async Task RegistrarCirurgiaPorAgendamentoAsync_DeveLancarExcecao_QuandoHaCirurgiaEmergencialNoHorario()
        {
            // Arrange
            var dataHora = DateTime.UtcNow.AddDays(3);
            var cirurgiaEmergencialExistente = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 3,
                Status = CirurgiaStatus.Agendada,
                Emergencial = true,
                CriadoEm = DateTime.UtcNow
            };
            _context.Cirurgias.Add(cirurgiaEmergencialExistente);
            await _context.SaveChangesAsync();

            var evento = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 3,
                Emergencial = true
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RegistrarCirurgiaPorAgendamentoAsync(evento));
        }

        [Fact]
        public async Task RegistrarCirurgiaPorAgendamentoAsync_DevePermitirCirurgiaEmergencial_QuandoApenasCirurgiaNormalExiste()
        {
            // Arrange
            var dataHora = DateTime.UtcNow.AddDays(4);
            var cirurgiaNormalExistente = new Cirurgia
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 3,
                Status = CirurgiaStatus.Agendada,
                Emergencial = false,
                CriadoEm = DateTime.UtcNow
            };
            _context.Cirurgias.Add(cirurgiaNormalExistente);
            await _context.SaveChangesAsync();

            var evento = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 3,
                Emergencial = true
            };

            // Act
            await _service.RegistrarCirurgiaPorAgendamentoAsync(evento);

            // Assert
            var cirurgias = await _context.Cirurgias.Where(c => c.DataHora == dataHora).ToListAsync();
            Assert.Equal(2, cirurgias.Count);
            Assert.Contains(cirurgias, c => c.Emergencial);
        }
    }
}
