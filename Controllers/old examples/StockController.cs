// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using MIC.risk.Data;
// using MIC.risk.DTOs.Stock;
// using MIC.risk.Models;
// using Microsoft.AspNetCore.Mvc;
// using MIC.risk.Mappers;
// using MIC.risk.Repositories;
// using MIC.risk.Interfaces;

// namespace MIC.risk.Controllers
// {
//     [ApiController]
//     [Route("api/stock")]
//     public class StockController : ControllerBase
//     {
//         private readonly IStockRepository _repository;

//         public StockController(IStockRepository repository)
//         {
//             _repository = repository;
//         }

//         [HttpGet]
//         public async Task<IActionResult> GetAll()
//         {
//             var stocks = await _repository.GetAllAsync();
//             return Ok(stocks);
//         }

//         [HttpGet("{id}")]
//         public async Task<IActionResult> GetById([FromRoute] int id)
//         {
//             var stock = await _repository.GetByIdAsync(id);
//             return stock == null ? NotFound() : Ok(stock);
//         }

//         [HttpPost]
//         public async Task<IActionResult> Create([FromBody] CreateStockDTO createStock)
//         {
//             var stockModel = createStock.ToStockFromCreateDto();
//             await _repository.CreateAsync(stockModel);
//             return CreatedAtAction(nameof(GetById), new { id = stockModel.Id }, stockModel.ToStockDto());
//         }

//         [HttpPut("{id}")]
//         public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateStockDto updateDto)
//         {
//             var stockModel = await _repository.UpdateAsync(id, updateDto);
//             if (stockModel == null)
//             {
//                 return NotFound();
//             }
//             return Ok(stockModel.ToStockDto());
//         }

//         [HttpDelete("{id}")]
//         public async Task<IActionResult> Delete([FromRoute] int id)
//         {
//             var stockModel = await _repository.DeleteAsync(id);
//             if (stockModel == null)
//             {
//                 return NotFound();
//             }
//             return NoContent();
//         }
//     }
// }