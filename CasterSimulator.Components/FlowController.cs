namespace CasterSimulator.Components;

public static class FlowController
{
    public static double ComputeFlowRate(
        double monitoredLevelMm, 
        double currentFlowRate, 
        double maxFlowRateKgPerSec,
        double targetLevelMm, 
        double tolerancePercent)
    {
        var toleranceMm = targetLevelMm * (tolerancePercent / 100.0); // Convert percentage to mm
        var error = monitoredLevelMm - targetLevelMm;

        // Compute correction factor based on error magnitude
        var correctionFactor = Math.Max(0.5, Math.Abs(error) / toleranceMm);
        var correction = -correctionFactor * error;

        // Compute dynamic flow rate change limit based on tolerance
        var flowRateChangeLimit = Math.Max(10, maxFlowRateKgPerSec * (tolerancePercent / 100.0));

        // Adjust flow relative to current flow rate
        var targetFlow = currentFlowRate + correction;

        // Prevent excessive jumps based on dynamic limit
        var adjustedFlow = Math.Clamp(targetFlow, currentFlowRate - flowRateChangeLimit, currentFlowRate + flowRateChangeLimit);

        // Ensure flow stays within system limits
        return Math.Clamp(adjustedFlow, 0, maxFlowRateKgPerSec);
    }
    
}