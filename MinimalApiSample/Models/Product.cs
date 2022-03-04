namespace MinimalApiSample.Models;

public class Product
{
    public Guid Id { get; set; }

    //[Required]
    //[MaxLength(50)]
    public string Name { get; set; }

    public double Price { get; set; }
}