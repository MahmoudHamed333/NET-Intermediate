using Catalog_Service.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog_Service.Unit_Tests;

public class CatalogRepositoryTests
{
    private CatalogDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnCategory_WhenValidData()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new CatalogRepository(context);
        var category = new Category { Name = "Test Category", Description = "Test Description" };

        // Act
        var result = await repository.CreateCategoryAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Category", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnAllCategories()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new CatalogRepository(context);
        await repository.CreateCategoryAsync(new Category { Name = "Category 1" });
        await repository.CreateCategoryAsync(new Category { Name = "Category 2" });

        // Act
        var result = await repository.GetCategoriesAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DeleteCategory_ShouldDeleteCategoryAndItems()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new CatalogRepository(context);
        var category = await repository.CreateCategoryAsync(new Category { Name = "Test Category" });
        await repository.CreateItemAsync(new Item
        {
            Name = "Test Item",
            Price = 10.99m,
            CategoryId = category.Id
        });

        // Act
        var result = await repository.DeleteCategoryAsync(category.Id);

        // Assert
        Assert.True(result);
        var deletedCategory = await repository.GetCategoryByIdAsync(category.Id);
        Assert.Null(deletedCategory);
    }

    [Fact]
    public async Task GetItems_ShouldReturnPaginatedResults()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new CatalogRepository(context);
        var category = await repository.CreateCategoryAsync(new Category { Name = "Test Category" });

        for (int i = 1; i <= 15; i++)
        {
            await repository.CreateItemAsync(new Item
            {
                Name = $"Item {i}",
                Price = i * 1.99m,
                CategoryId = category.Id
            });
        }

        // Act
        var result = await repository.GetItemsAsync(null, 1, 10);

        // Assert
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }
}