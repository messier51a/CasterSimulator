namespace CasterSimulator.Models;

public class Heat
{
    public int Id { get; set; } // Unique identifier for the heat
    public string Name { get; set; }
    public double NetWeight { get; set; } // Weight of the heat in kg
    public DateTime TapTime { get; set; } // Time when the heat was tapped from the furnace
    public DateTime? HeatStart { get; set; } // Time when the heat started casting
    public DateTime? HeatEnd { get; set; } // Time when the heat finished casting
    public string GradeId { get; set; } // Identifier for the steel grade
    public HeatStatus Status { get; set; } = HeatStatus.Next;
    public double MixZoneStart { get; set; }
    public double MixZoneEnd { get; set; }
    public double HeatBoundary { get; set; }

    public Heat(int heatId, string name, double netWeight, string gradeId)
    {
        if (heatId <= 0) throw new ArgumentException("Heat Id must be greater than 0.");
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Id = heatId;
        NetWeight = netWeight;
        //TapTime = tapTime;
        GradeId = gradeId ?? throw new ArgumentNullException(nameof(gradeId));
    }
}

public enum HeatStatus
{
    Next = 1,
    Pouring,
    Casting,
    Cutting,
    Cast
}