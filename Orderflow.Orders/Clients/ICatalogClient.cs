namespace Orderflow.Orders.Clients;

public interface ICatalogClient
{
    Task<ProductInfo?> GetProductAsync(int productId);
    Task<bool> ReserveStockAsync(int productId, int quantity);
    Task<bool> ReleaseStockAsync(int productId, int quantity);
}

public record ProductInfo(
    int Id,
    string Name,
    decimal Price,
    int QuantityAvailable,
    bool IsActive);