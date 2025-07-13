using Catalog_Service.DTOs;
using Catalog_Service.Models;

namespace Catalog_Service;

public interface ICatalogRepository
{
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category> CreateCategoryAsync(Category category);
    Task<Category> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<PagedResult<Item>> GetItemsAsync(int? categoryId, int page, int pageSize);
    Task<Item?> GetItemByIdAsync(int id);
    Task<Item> CreateItemAsync(Item item);
    Task<Item> UpdateItemAsync(Item item);
    Task<bool> DeleteItemAsync(int id);
    Task<bool> CategoryExistsAsync(int id);
}
