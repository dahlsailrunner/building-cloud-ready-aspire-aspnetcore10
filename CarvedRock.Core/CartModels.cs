namespace CarvedRock.Core;

public record AddToCartModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public record CartItemModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public double Price { get; set; }
    public double Total { get; set; }
}
