namespace Ordering.Domain;

public class Customer
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}