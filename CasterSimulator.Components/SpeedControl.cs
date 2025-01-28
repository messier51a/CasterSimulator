namespace CasterSimulator.Components;

public class SpeedControl
{
    private double _startSpeed;
    private double _targetSpeed;
    private double _duration;
    private double _elapsedTime;

    public SpeedControl(double startSpeed, double targetSpeed, double duration)
    {
        _startSpeed = startSpeed;
        _targetSpeed = targetSpeed;
        _duration = duration;
        _elapsedTime = 0;
    }

    public double CalculateCurrentSpeed(double deltaTimeMilliseconds)
    {
        var deltaTimeSeconds = deltaTimeMilliseconds / 1000.0;
        
        if (_elapsedTime >= _duration)
            return _targetSpeed;

        _elapsedTime += deltaTimeSeconds;
        var progress = Math.Min(_elapsedTime / _duration, 1.0);
        return _startSpeed + (progress * (_targetSpeed - _startSpeed));
    }
}