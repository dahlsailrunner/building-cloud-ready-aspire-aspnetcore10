namespace CarvedRock.Data.Entities;

public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double LineTotal { get; set; }
    public Order Order { get; set; } = null!;
}
