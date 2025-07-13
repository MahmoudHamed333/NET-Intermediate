﻿using System.ComponentModel.DataAnnotations;

namespace Catalog_Service.DTOs;

public class UpdateCategoryRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}

