using Agendamentos.Api.Domain.Context;
using Agendamentos.Api.Domain.Entities;
using Agendamentos.Api.DTOs;
using Agendamentos.Api.Messaging.Events;
using Agendamentos.Api.Messaging.Producer;
using Agendamentos.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Agendamentos.Api.Tests.Services
{
    public class AgendamentoServiceTests
    {
        private readonly Mock<IAgendamentoConfirmadoProducer> _mockProducer;
        private readonly HospitalAgendamentosContext _context;
        private readonly AgendamentoService _service;

        public AgendamentoServiceTests()
        {
            var options = new DbContextOptionsBuilder<HospitalAgendamentosContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new HospitalAgendamentosContext(options);
            _mockProducer = new Mock<IAgendamentoConfirmadoProducer>();
            _service = new AgendamentoService(_context, _mockProducer.Object);
        }

        [Fact]
        public async Task CriarAsync_DeveRetornarAgendamento_QuandoDadosValidos()
        {
            // Arrange
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "João Silva",
                Documento = "12345678900",
                DataNascimento = new DateTime(1990, 1, 1),
                Telefone = "11999999999",
                Email = "joao@teste.com"
            };
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            var dto = new AgendamentoCreateDto
            {
                PacienteId = paciente.Id,
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 0,
                Descricao = "Consulta de rotina",
                Emergencial = false
            };

            // Act
            var resultado = await _service.CriarAsync(dto);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(paciente.Id, resultado.Paciente.Id);
            Assert.Equal(dto.DataHora, resultado.DataHora);
            Assert.False(resultado.Confirmado);
        }

        [Fact]
        public async Task CriarAsync_DeveLancarExcecao_QuandoPacienteNaoExiste()
        {
            // Arrange
            var dto = new AgendamentoCreateDto
            {
                PacienteId = Guid.NewGuid(),
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = 0,
                Descricao = "Consulta"
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CriarAsync(dto));
        }

        [Fact]
        public async Task CriarAsync_DeveLancarExcecao_QuandoHaConflitoDeHorario()
        {
            // Arrange
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Maria Santos",
                Documento = "98765432100",
                DataNascimento = new DateTime(1985, 5, 15),
                Telefone = "11988888888",
                Email = "maria@teste.com"
            };
            _context.Pacientes.Add(paciente);

            var dataHora = DateTime.UtcNow.AddDays(2);
            var agendamentoExistente = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = paciente.Id,
                DataHora = dataHora,
                Tipo = TipoAgendamento.Consulta,
                Descricao = "Primeira consulta",
                Confirmado = false,
                DataCriacao = DateTime.UtcNow
            };
            _context.Agendamentos.Add(agendamentoExistente);
            await _context.SaveChangesAsync();

            var dto = new AgendamentoCreateDto
            {
                PacienteId = paciente.Id,
                DataHora = dataHora,
                Tipo = 0,
                Descricao = "Segunda consulta"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CriarAsync(dto));
        }

        [Fact]
        public async Task ListarAsync_DeveRetornarTodosAgendamentos()
        {
            // Arrange
            var paciente1 = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Paciente 1",
                Documento = "11111111111",
                DataNascimento = new DateTime(1990, 1, 1),
                Telefone = "11111111111",
                Email = "pac1@teste.com"
            };
            var paciente2 = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Paciente 2",
                Documento = "22222222222",
                DataNascimento = new DateTime(1995, 2, 2),
                Telefone = "22222222222",
                Email = "pac2@teste.com"
            };
            _context.Pacientes.AddRange(paciente1, paciente2);

            var agendamento1 = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = paciente1.Id,
                DataHora = DateTime.UtcNow.AddDays(1),
                Tipo = TipoAgendamento.Consulta,
                Descricao = "Consulta 1",
                Confirmado = false,
                DataCriacao = DateTime.UtcNow
            };
            var agendamento2 = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = paciente2.Id,
                DataHora = DateTime.UtcNow.AddDays(2),
                Tipo = TipoAgendamento.ProcedimentoCirurgico,
                Descricao = "Cirurgia 1",
                Confirmado = false,
                DataCriacao = DateTime.UtcNow
            };
            _context.Agendamentos.AddRange(agendamento1, agendamento2);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ListarAsync();

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.Count());
        }

        [Fact]
        public async Task BuscarPorIdAsync_DeveRetornarAgendamento_QuandoExiste()
        {
            // Arrange
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Carlos Oliveira",
                Documento = "33333333333",
                DataNascimento = new DateTime(1988, 3, 10),
                Telefone = "11977777777",
                Email = "carlos@teste.com"
            };
            _context.Pacientes.Add(paciente);

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = paciente.Id,
                DataHora = DateTime.UtcNow.AddDays(3),
                Tipo = TipoAgendamento.Consulta,
                Descricao = "Consulta especializada",
                Confirmado = false,
                DataCriacao = DateTime.UtcNow
            };
            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.BuscarPorIdAsync(agendamento.Id);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(agendamento.Id, resultado.Id);
            Assert.Equal(paciente.Nome, resultado.Paciente.Nome);
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
        public async Task ConfirmarAsync_DeveConfirmarEPublicarEvento_QuandoAgendamentoExiste()
        {
            // Arrange
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Ana Paula",
                Documento = "44444444444",
                DataNascimento = new DateTime(1992, 7, 20),
                Telefone = "11966666666",
                Email = "ana@teste.com"
            };
            _context.Pacientes.Add(paciente);

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = paciente.Id,
                DataHora = DateTime.UtcNow.AddDays(4),
                Tipo = TipoAgendamento.ProcedimentoCirurgico,
                Descricao = "Cirurgia cardíaca",
                Confirmado = false,
                DataCriacao = DateTime.UtcNow,
                Emergencial = true
            };
            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ConfirmarAsync(agendamento.Id);

            // Assert
            Assert.True(resultado);
            var agendamentoAtualizado = await _context.Agendamentos.FindAsync(agendamento.Id);
            Assert.True(agendamentoAtualizado!.Confirmado);
            _mockProducer.Verify(p => p.Publicar(It.IsAny<AgendamentoConfirmadoEvent>()), Times.Once);
        }

        [Fact]
        public async Task ConfirmarAsync_DeveRetornarFalse_QuandoAgendamentoNaoExiste()
        {
            // Act
            var resultado = await _service.ConfirmarAsync(Guid.NewGuid());

            // Assert
            Assert.False(resultado);
            _mockProducer.Verify(p => p.Publicar(It.IsAny<AgendamentoConfirmadoEvent>()), Times.Never);
        }

        [Fact]
        public async Task CancelarAsync_DeveRemoverAgendamento_QuandoExiste()
        {
            // Arrange
            var paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                Nome = "Pedro Souza",
                Documento = "55555555555",
                DataNascimento = new DateTime(1980, 11, 5),
                Telefone = "11955555555",
                Email = "pedro@teste.com"
            };
            _context.Pacientes.Add(paciente);

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                PacienteId = paciente.Id,
                DataHora = DateTime.UtcNow.AddDays(5),
                Tipo = TipoAgendamento.Consulta,
                Descricao = "Consulta a cancelar",
                Confirmado = false,
                DataCriacao = DateTime.UtcNow
            };
            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.CancelarAsync(agendamento.Id);

            // Assert
            Assert.True(resultado);
            var agendamentoRemovido = await _context.Agendamentos.FindAsync(agendamento.Id);
            Assert.Null(agendamentoRemovido);
        }

        [Fact]
        public async Task CancelarAsync_DeveRetornarFalse_QuandoNaoExiste()
        {
            // Act
            var resultado = await _service.CancelarAsync(Guid.NewGuid());

            // Assert
            Assert.False(resultado);
        }
    }
}
