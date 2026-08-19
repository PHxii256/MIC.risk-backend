using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using MIC.risk.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MIC.risk.Extensions;

namespace MIC.risk.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/risk-subcategory")]
    public class RiskSubcategoryController : ControllerBase
    {
        private readonly IRiskService _service;

        public RiskSubcategoryController(IRiskService service)
        {
            _service = service;
        }

        // GET: api/risk-subcategory/categories
        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<RiskCategoryResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCategoriesWithSubcategories()
        {
            var categories = await _service.GetAllRisksAsync();
            return Ok(categories);
        }

        // GET: api/risk-subcategory/5
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(RiskSubcategoryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var risk = await _service.GetByIdAsync(id);
            if (risk == null)
            {
                return this.NotFoundProblem($"Risk subcategory with ID {id} was not found.");
            }

            return Ok(risk);
        }

        // POST: api/risk-subcategory
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(RiskSubcategoryResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateRiskSubcategoryRequestDto dto)
        {
            try
            {
                var createdSubcategory = await _service.CreateRiskSubcategoryAsync(dto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdSubcategory.Id },
                    createdSubcategory);
            }
            catch (InvalidOperationException ex)
            {
                return this.BadRequestProblem(ex.Message);
            }
        }

        // PUT: api/risk-subcategory/5
        [HttpPut("{id:long}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(RiskSubcategoryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateRiskSubcategoryRequestDto dto)
        {
            var updated = await _service.UpdateRiskSubcategoryAsync(id, dto);
            if (updated == null)
            {
                return this.NotFoundProblem($"Risk subcategory with ID {id} was not found.");
            }

            return Ok(updated);
        }

        // DELETE: api/risk-subcategory/5
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoftDelete(long id)
        {
            var success = await _service.SoftDeleteAsync(id);
            if (!success)
            {
                return this.NotFoundProblem($"Risk subcategory with ID {id} was not found.");
            }

            return NoContent();
        }

        // GET: api/risk-subcategory/by-category/Operational
        [HttpGet("by-category/{category}")]
        [ProducesResponseType(typeof(IEnumerable<RiskSubcategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRiskSubcategoryByCategory(string category)
        {
            try
            {
                var res = await _service.GetRiskSubcategoriesAsync(category);
                if (!res.Any())
                {
                    return this.NotFoundProblem($"Risk subcategory with specified category '{category}' was not found.");
                }

                return Ok(res);
            }
            catch (InvalidOperationException ex)
            {
                return this.BadRequestProblem(ex.Message);
            }
        }
    }
}