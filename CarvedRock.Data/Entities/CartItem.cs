namespace CarvedRock.Data.Entities;

public class CartItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
