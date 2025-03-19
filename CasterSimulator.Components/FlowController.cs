namespace CasterSimulator.Components;

/// <summary>
/// Provides static methods for controlling and adjusting fluid flow rates in the casting process.
/// Used to maintain target levels in steel containers by dynamically adjusting flow rates
/// based on current levels, target levels, and system constraints.
/// </summary>
public static class FlowController
{
    /// <summary>
    /// Computes an adjusted flow rate to maintain a target level in a steel container.
    /// Uses a control algorithm that applies corrections based on the error between 
    /// the current level and target level, while respecting system limits.
    /// </summary>
    /// <param name="monitoredLevelMm">The current level being monitored in millimeters.</param>
    /// <param name="currentFlowRate">The current flow rate in kilograms per second.</param>
    /// <param name="maxFlowRateKgPerSec">The maximum allowable flow rate in kilograms per second.</param>
    /// <param name="targetLevelMm">The desired level to maintain in millimeters.</param>
    /// <param name="tolerancePercent">The acceptable deviation from target, expressed as a percentage.</param>
    /// <returns>
    /// The adjusted flow rate in kilograms per second, constrained by the maximum flow rate
    /// and rate-of-change limits to prevent excessive fluctuations.
    /// </returns>
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