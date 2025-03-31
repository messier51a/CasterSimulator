namespace CasterSimulator.Components;

/// <summary>
/// Controls the speed ramping process for the casting strand.
/// Manages the transition from initial speed to target speed over a specified duration,
/// and maintains speed consistency with minor variations once the target is reached.
/// </summary>
public class SpeedController
{
    private readonly double _startSpeed;
    private readonly double _targetSpeed;
    private readonly double _duration;
    private Random _random = new Random();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SpeedController"/> class.
    /// Controls the speed ramping process over a specified duration.
    /// </summary>
    /// <param name="startSpeed">The initial speed in meters per minute. Must be ≥ 0.</param>
    /// <param name="targetSpeed">The target speed in meters per minute. Must be between 1 and 10 (inclusive).</param>
    /// <param name="duration">The time in seconds over which the speed changes. Must be between 0 and 90 (inclusive).</param>
    /// <exception cref="ArgumentException">Thrown if any parameter is out of range.</exception>
    public SpeedController(double startSpeed, double targetSpeed, double duration)
    {
        if (startSpeed < 0) throw new ArgumentException("Invalid start speed. Must be ≥ 0.");
        if (targetSpeed is < 1 or > 10) throw new ArgumentException("Invalid target speed. Must be between 1 and 10.");
        if (duration is < 0 or > 90) throw new ArgumentException("Invalid duration. Must be between 0 and 90 seconds.");

        _startSpeed = startSpeed;
        _targetSpeed = targetSpeed;
        _duration = duration;
    }

    private double _elapsedTime = 0;

    /// <summary>
    /// Calculates the current speed based on elapsed time and configured parameters.
    /// Implements a linear ramp from start speed to target speed over the specified duration.
    /// Once target speed is reached, returns the steady target speed.
    /// </summary>
    /// <returns>
    /// The current speed in meters per minute. During ramp-up, this will be a value between
    /// the start speed and target speed. After the duration has elapsed, this will be the target speed.
    /// </returns>
    /// <remarks>
    /// The method includes commented code for introducing small speed variations once the target is reached,
    /// which could be uncommented to simulate real-world speed fluctuations.
    /// </remarks>
    public double CalculateCurrentSpeed()
    {
        if (_elapsedTime >= _duration)
        {
            //Speed has reached target, now introduce small variations (±5-10%)
            var fluctuatedSpeed = _targetSpeed;

            /*if (_random.NextDouble() < 0.2)  // 20% chance per second
            {
                var variation = _random.Next(-4, 5) / 100.0; // ±4% variation
                fluctuatedSpeed *= (1 + variation);
            }*/

            return fluctuatedSpeed;
        }
        _elapsedTime++;
        var progress = Math.Min(_elapsedTime / _duration, 1.0);
        return _startSpeed + (progress * (_targetSpeed - _startSpeed));
    }
}