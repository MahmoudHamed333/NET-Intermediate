using Catalog_Service.DTOs;
using Catalog_Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICatalogRepository _repository;

    public CategoriesController(ICatalogRepository repository)
    {
        _repository = repository;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _repository.GetCategoriesAsync();
        var categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });

        return Ok(categoryDtos);
    }

    // GET: api/categories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _repository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return Ok(categoryDto);
    }

    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        try
        {
            category = await _repository.CreateCategoryAsync(category);
            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }
        catch (DbUpdateException)
        {
            return Conflict("A category with this name already exists.");
        }
    }

    // PUT: api/categories/5
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var category = await _repository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        category.Name = request.Name;
        category.Description = request.Description;

        try
        {
            category = await _repository.UpdateCategoryAsync(category);
            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return Ok(categoryDto);
        }
        catch (DbUpdateException)
        {
            return Conflict("A category with this name already exists.");
        }
    }

    // DELETE: api/categories/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _repository.DeleteCategoryAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}

