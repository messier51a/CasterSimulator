namespace SteelCastingSimulation
{
    public enum CoordinatorStatus
    {
        Normal,        // Normal operation, casting is ongoing
        NeedsRotation, // Ladle is empty, needs turret rotation
        Finished       // Casting sequence is complete (no more ladles, tundish emptied)
    }
}