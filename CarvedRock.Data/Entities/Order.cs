namespace CarvedRock.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public double Total { get; set; }
    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
}
