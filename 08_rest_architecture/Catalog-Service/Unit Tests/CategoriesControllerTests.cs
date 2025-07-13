using Catalog_Service.Controllers;
using Catalog_Service.DTOs;
using Catalog_Service.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Catalog_Service.Unit_Tests;

public class CategoriesControllerTests
{
    private readonly Mock<ICatalogRepository> _mockRepository;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _mockRepository = new Mock<ICatalogRepository>();
        _controller = new CategoriesController(_mockRepository.Object);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnOkResult_WithCategories()
    {
        // Arrange
        var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Category 1", Description = "Description 1" },
                new Category { Id = 2, Name = "Category 2", Description = "Description 2" }
            };
        _mockRepository.Setup(r => r.GetCategoriesAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCategories = Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(okResult.Value);
        Assert.Equal(2, returnedCategories.Count());
    }

    [Fact]
    public async Task GetCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        // Act
        var result = await _controller.GetCategory(1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateCategory_ShouldReturnCreatedAtAction_WhenValidData()
    {
        // Arrange
        var request = new CreateCategoryRequest { Name = "New Category", Description = "New Description" };
        var category = new Category { Id = 1, Name = request.Name, Description = request.Description };
        _mockRepository.Setup(r => r.CreateCategoryAsync(It.IsAny<Category>())).ReturnsAsync(category);

        // Act
        var result = await _controller.CreateCategory(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedCategory = Assert.IsType<CategoryDto>(createdResult.Value);
        Assert.Equal("New Category", returnedCategory.Name);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnNoContent_WhenCategoryExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteCategoryAsync(It.IsAny<int>())).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteCategoryAsync(It.IsAny<int>())).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}

