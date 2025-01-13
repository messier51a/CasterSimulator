namespace CasterSimulator.Models;

public class Sequence
{
    public int Id { get; set; }
    public Queue<Heat> Heats = new();
    public Queue<Product> Products = new();
}