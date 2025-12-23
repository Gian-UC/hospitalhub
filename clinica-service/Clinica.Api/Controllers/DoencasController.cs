using Clinica.Api.Domain.Entities;
using Clinica.Api.DTOs;
using Clinica.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.Api.Controllers
{    
    [ApiController]
    [Route("api/[controller]")]
    public class DoencasController : ControllerBase
    {
        private readonly IDoencaService _service;

        public DoencasController(IDoencaService service)
        {
            _service = service;
        }

        [Authorize(Policy = "MedicoOnly")]
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            return Ok(await _service.ListarAsync());
        }

        [Authorize(Policy = "MedicoOnly")]
        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPorId(Guid id)
        {
            var doenca = await _service.BuscarPorIdAsync(id);
            if (doenca == null) return NotFound();            
            return Ok(doenca);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] DoencaCreateDto dto)
        {
            var doenca = new Doenca
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao
            };

            var criado = await _service.CriarAsync(doenca);
            return CreatedAtAction(nameof(BuscarPorId), new { id = criado.Id }, criado);
        }
    }
}