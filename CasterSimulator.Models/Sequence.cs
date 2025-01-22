namespace CasterSimulator.Models;

public class Sequence
{
    public Sequence(long id)
    {
        Id = id;
    }

    public long Id { get;}
    public Queue<Heat> Heats = new();
    public Queue<Product> Products = new();
}