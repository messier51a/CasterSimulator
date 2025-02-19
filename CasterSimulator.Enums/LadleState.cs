/// <summary>
/// Represents the state of a ladle in the continuous casting process.
/// </summary>
public enum LadleState
{
    /// <summary>
    /// The ladle has been added but has not started pouring.
    /// </summary>
    New,

    /// <summary>
    /// The ladle is in use but currently closed, meaning no steel is being poured.
    /// </summary>
    Closed,

    /// <summary>
    /// The ladle is actively pouring steel into the tundish.
    /// </summary>
    Open
}