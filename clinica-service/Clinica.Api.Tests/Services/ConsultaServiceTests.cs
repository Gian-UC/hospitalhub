using Clinica.Api.Domain.Context;
using Clinica.Api.Domain.Entities;
using Clinica.Api.Domain.Enums;
using Clinica.Api.Messaging.Events;
using Clinica.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinica.Api.Tests.Services
{
    public class ConsultaServiceTests
    {
        private readonly ClinicaContext _context;
        private readonly ConsultaService _service;

        public ConsultaServiceTests()
        {
            var options = new DbContextOptionsBuilder<ClinicaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ClinicaContext(options);
            _service = new ConsultaService(_context);
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodasConsultas()
        {
            // Arrange
            var consulta1 = new Consulta
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 1,
                Status = StatusConsulta.Pendente,
                CriadoEm = DateTime.UtcNow
            };

            var consulta2 = new Consulta
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(2),
                Tipo = 1,
                Status = StatusConsulta.Finalizada,
                CriadoEm = DateTime.UtcNow
            };

            _context.Consultas.AddRange(consulta1, consulta2);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ListarAsync();

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.Count());
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarConsulta_QuandoExiste()
        {
            // Arrange
            var consulta = new Consulta
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 1,
                Status = StatusConsulta.Pendente,
                CriadoEm = DateTime.UtcNow
            };

            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.BuscarPorIdAsync(consulta.Id);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(consulta.Id, resultado.Id);
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
        public async Task RegistrarConsultaAsync_DeveAdicionarConsultaComStatusPendente()
        {
            // Arrange
            var consulta = new Consulta
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 1
            };

            // Act
            var resultado = await _service.RegistrarConsultaAsync(consulta);

            // Assert
            Assert.NotEqual(Guid.Empty, resultado.Id);
            Assert.Equal(StatusConsulta.Pendente, resultado.Status);
            Assert.True(resultado.CriadoEm > DateTime.MinValue);
        }

        [Fact]
        public async Task RegistrarConsultaPorAgendamentoAsync_DeveAdicionarConsulta_QuandoDadosValidos()
        {
            // Arrange
            var evento = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 1,
                Emergencial = false
            };

            // Act
            await _service.RegistrarConsultaPorAgendamentoAsync(evento);

            // Assert
            var consultas = await _context.Consultas.ToListAsync();
            Assert.Single(consultas);
            Assert.Equal(evento.AgendamentoId, consultas[0].AgendamentoId);
            Assert.Equal(evento.PacienteId, consultas[0].PacienteId);
            Assert.Equal(StatusConsulta.Pendente, consultas[0].Status);
        }

        [Fact]
        public async Task RegistrarConsultaPorAgendamentoAsync_DeveLancarExcecao_QuandoHaConflito()
        {
            // Arrange
            var dataHora = DateTime.UtcNow.AddDays(2);
            var consultaExistente = new Consulta
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 1,
                Status = StatusConsulta.Pendente,
                CriadoEm = DateTime.UtcNow
            };
            _context.Consultas.Add(consultaExistente);
            await _context.SaveChangesAsync();

            var evento = new AgendamentoConfirmadoEvent
            {
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = dataHora,
                Tipo = 1,
                Emergencial = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RegistrarConsultaPorAgendamentoAsync(evento));
        }

        [Fact]
        public async Task VincularSintomasAsync_DeveVincularSintomasAConsulta()
        {
            // Arrange
            var doenca = new Doenca { Id = Guid.NewGuid(), Nome = "Gripe", Descricao = "Infecção viral" };
            _context.Doencas.Add(doenca);
            
            var sintoma1 = new Sintoma { Id = Guid.NewGuid(), Nome = "Febre", DoencaId = doenca.Id };
            var sintoma2 = new Sintoma { Id = Guid.NewGuid(), Nome = "Tosse", DoencaId = doenca.Id };
            _context.Sintomas.AddRange(sintoma1, sintoma2);

            var consulta = new Consulta
            {
                Id = Guid.NewGuid(),
                AgendamentoId = Guid.NewGuid(),
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 1,
                Status = StatusConsulta.Pendente,
                CriadoEm = DateTime.UtcNow
            };
            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();

            var sintomaIds = new[] { sintoma1.Id, sintoma2.Id };

            // Act
            await _service.VincularSintomasAsync(consulta.Id, sintomaIds);

            // Assert
            var consultaComSintomas = await _context.Consultas
                .Include(c => c.Sintomas)
                .FirstAsync(c => c.Id == consulta.Id);

            Assert.NotNull(consultaComSintomas.Sintomas);
            Assert.Equal(2, consultaComSintomas.Sintomas.Count);
        }

        [Fact]
        public async Task VincularSintomasAsync_DeveLancarExcecao_QuandoConsultaNaoExiste()
        {
            // Arrange
            var sintomaIds = new[] { Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.VincularSintomasAsync(Guid.NewGuid(), sintomaIds));
        }
    }
}
