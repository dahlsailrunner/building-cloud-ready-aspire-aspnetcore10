namespace CarvedRock.Core;

public record NewOrderModel
{
    public string Email { get; set; } = null!;
}

public record OrderDetailModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double LineTotal { get; set; }
}

public record OrderModel
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public double Total { get; set; }
    public List<OrderDetailModel> Details { get; set; } = [];
}
