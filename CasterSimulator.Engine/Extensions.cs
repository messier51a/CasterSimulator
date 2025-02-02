using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public static class Extensions
{
    public static void Open(this Heat heat)
    { 
        heat.HeatStartUtcTime = DateTime.UtcNow;
        heat.Status = HeatStatus.Pouring;
    }

    public static void Close(this Heat heat)
    {
        heat.HeatEndUtcTime = DateTime.UtcNow;
        heat.Status = HeatStatus.Closed;
    }
}