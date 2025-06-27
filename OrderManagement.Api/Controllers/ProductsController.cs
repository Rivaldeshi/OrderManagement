using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Entities;
using AutoMapper;

namespace OrderManagement.Api.Controllers;

/// <summary>
/// Controller for managing products
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductsController(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    /// <returns>List of products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
        return Ok(productDtos);
    }

    /// <summary>
    /// Gets a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        
        if (product == null)
            return NotFound($"Product with ID {id} not found");

        var productDto = _mapper.Map<ProductDto>(product);
        return Ok(productDto);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="createProductDto">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto createProductDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var product = _mapper.Map<Product>(createProductDto);
            var createdProduct = await _unitOfWork.Products.CreateAsync(product);
            await _unitOfWork.SaveChangesAsync();

            var productDto = _mapper.Map<ProductDto>(createdProduct);
            return CreatedAtAction(nameof(GetProduct), new { id = productDto.Id }, productDto);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create product: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateProductDto">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, UpdateProductDto updateProductDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
        if (existingProduct == null)
            return NotFound($"Product with ID {id} not found");

        try
        {
            // Update properties
            existingProduct.Name = updateProductDto.Name;
            existingProduct.Description = updateProductDto.Description;
            existingProduct.Price = updateProductDto.Price;
            existingProduct.Stock = updateProductDto.Stock;
            existingProduct.Category = updateProductDto.Category;
            existingProduct.SKU = updateProductDto.SKU;
            existingProduct.IsActive = updateProductDto.IsActive;

            var updatedProduct = await _unitOfWork.Products.UpdateAsync(existingProduct);
            await _unitOfWork.SaveChangesAsync();

            var productDto = _mapper.Map<ProductDto>(updatedProduct);
            return Ok(productDto);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update product: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates product stock
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateStockDto">Stock update data</param>
    /// <returns>Updated product</returns>
    [HttpPatch("{id}/stock")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ProductDto>> UpdateProductStock(int id, UpdateProductStockDto updateStockDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
        if (existingProduct == null)
            return NotFound($"Product with ID {id} not found");

        try
        {
            existingProduct.Stock = updateStockDto.NewStock;
            var updatedProduct = await _unitOfWork.Products.UpdateAsync(existingProduct);
            await _unitOfWork.SaveChangesAsync();

            var productDto = _mapper.Map<ProductDto>(updatedProduct);
            return Ok(productDto);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update product stock: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a product (soft delete)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            return NotFound($"Product with ID {id} not found");

        try
        {
            await _unitOfWork.Products.DeleteAsync(id); // Soft delete
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to delete product: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets products with low stock
    /// </summary>
    /// <param name="threshold">Stock threshold (default: 10)</param>
    /// <returns>Products with low stock</returns>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts([FromQuery] int threshold = 10)
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var lowStockProducts = products.Where(p => p.Stock <= threshold && p.Stock > 0);
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(lowStockProducts);
        return Ok(productDtos);
    }

    /// <summary>
    /// Gets out of stock products
    /// </summary>
    /// <returns>Out of stock products</returns>
    [HttpGet("out-of-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetOutOfStockProducts()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var outOfStockProducts = products.Where(p => p.Stock == 0);
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(outOfStockProducts);
        return Ok(productDtos);
    }
}