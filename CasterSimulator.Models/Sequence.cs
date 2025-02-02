namespace CasterSimulator.Models;

public class Sequence
{
    public Sequence(long id, double width, double thickness, double steelDensity)
    {
        Id = id;
        Width = width;
        Thickness = thickness;
        SteelDensity = steelDensity;
    }

    public long Id { get;}
    public Queue<Heat> Heats = new();
    public Queue<Product> Products = new();
    public double Width { get; }
    public double Thickness { get; }

    public double SteelDensity { get; }
}