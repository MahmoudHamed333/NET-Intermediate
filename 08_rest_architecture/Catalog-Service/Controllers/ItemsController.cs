using Catalog_Service.DTOs;
using Catalog_Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Catalog_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly ICatalogRepository _repository;

    public ItemsController(ICatalogRepository repository)
    {
        _repository = repository;
    }

    // GET: api/items?categoryId=1&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemDto>>> GetItems(
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        if (categoryId.HasValue && !await _repository.CategoryExistsAsync(categoryId.Value))
        {
            return BadRequest("Invalid category ID.");
        }

        var pagedItems = await _repository.GetItemsAsync(categoryId, page, pageSize);
        var itemDtos = pagedItems.Items.Select(i => new ItemDto
        {
            Id = i.Id,
            Name = i.Name,
            Description = i.Description,
            Price = i.Price,
            CategoryId = i.CategoryId,
            CategoryName = i.Category?.Name ?? "",
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        });

        var result = new PagedResult<ItemDto>
        {
            Items = itemDtos,
            TotalCount = pagedItems.TotalCount,
            PageNumber = pagedItems.PageNumber,
            PageSize = pagedItems.PageSize
        };

        return Ok(result);
    }

    // GET: api/items/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetItem(int id)
    {
        var item = await _repository.GetItemByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        var itemDto = new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? "",
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

        return Ok(itemDto);
    }

    // POST: api/items
    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItem(CreateItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!await _repository.CategoryExistsAsync(request.CategoryId))
        {
            return BadRequest("Invalid category ID.");
        }

        var item = new Item
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId
        };

        item = await _repository.CreateItemAsync(item);
        var itemDto = new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? "",
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, itemDto);
    }
    // PUT: api/items/5
    [HttpPut("{id}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int id, UpdateItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var item = await _repository.GetItemByIdAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        if (!await _repository.CategoryExistsAsync(request.CategoryId))
        {
            return BadRequest("Invalid category ID.");
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.CategoryId = request.CategoryId;

        item = await _repository.UpdateItemAsync(item);
        var itemDto = new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name ?? "",
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

        return Ok(itemDto);
    }

    // DELETE: api/items/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var result = await _repository.DeleteItemAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}