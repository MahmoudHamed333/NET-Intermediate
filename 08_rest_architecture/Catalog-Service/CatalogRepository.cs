using Catalog_Service.DTOs;
using Catalog_Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog_Service;

public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _context;

    public CatalogRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateCategoryAsync(Category category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<Item>> GetItemsAsync(int? categoryId, int page, int pageSize)
    {
        var query = _context.Items.Include(i => i.Category).AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Item>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<Item?> GetItemByIdAsync(int id)
    {
        return await _context.Items
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item> CreateItemAsync(Item item)
    {
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return await GetItemByIdAsync(item.Id) ?? item;
    }

    public async Task<Item> UpdateItemAsync(Item item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        _context.Items.Update(item);
        await _context.SaveChangesAsync();
        return await GetItemByIdAsync(item.Id) ?? item;
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null) return false;

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CategoryExistsAsync(int id)
    {
        return await _context.Categories.AnyAsync(c => c.Id == id);
    }
}
