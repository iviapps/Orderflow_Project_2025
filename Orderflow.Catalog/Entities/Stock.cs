namespace Orderflow.Catalog.Entities
{
    public class Stock
    {
        public int Id { get; set; }

        //foreign key to Product
        public int ProductId { get; set; }
        //This ensures EF Core understands it's required and avoids null navigation confusion.
        public Product Product { get; set; } = null!;

        //quantity available in stock
        public int QuantityAvailable { get; set; }
        public int QuantityReserved { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
