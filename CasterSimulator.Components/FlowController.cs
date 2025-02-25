public class FlowController
{
    private readonly double _targetLevelMm;  // Target mold level in mm
    private readonly double _minFlowRateKgPerSec;
    private readonly double _maxFlowRateKgPerSec;
    
    public FlowController(
        double targetLevelMm,
        double minFlowRateKgPerSec,
        double maxFlowRateKgPerSec)
    {
        _targetLevelMm = targetLevelMm;
        _minFlowRateKgPerSec = minFlowRateKgPerSec;
        _maxFlowRateKgPerSec = maxFlowRateKgPerSec;
    }

    public double ComputeFlowRate(
        double monitoredLevelMm, 
        double currentFlowRate, 
        double targetLevelMm, 
        double tolerancePercent)
    {
        double toleranceMm = targetLevelMm * (tolerancePercent / 100.0); // Convert percentage to mm
        double error = monitoredLevelMm - targetLevelMm;

        // Compute correction factor based on error magnitude
        double correctionFactor = Math.Max(0.5, Math.Abs(error) / toleranceMm);
        double correction = -correctionFactor * error;

        // Compute dynamic flow rate change limit based on tolerance
        double flowRateChangeLimit = Math.Max(10, _maxFlowRateKgPerSec * (tolerancePercent / 100.0));

        // Adjust flow relative to current flow rate
        double targetFlow = currentFlowRate + correction;

        // Prevent excessive jumps based on dynamic limit
        double adjustedFlow = Math.Clamp(targetFlow, currentFlowRate - flowRateChangeLimit, currentFlowRate + flowRateChangeLimit);

        // Ensure flow stays within system limits
        return Math.Clamp(adjustedFlow, _minFlowRateKgPerSec, _maxFlowRateKgPerSec);
    }
    
}