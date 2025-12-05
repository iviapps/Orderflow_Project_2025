namespace Orderflow.Catalog.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int CategoryId { get; set; }
    //could not belong to a category
    public Category? Category { get; set; }
    //must belong to a category

    public Stock Stock { get; set; } = null!;
}
