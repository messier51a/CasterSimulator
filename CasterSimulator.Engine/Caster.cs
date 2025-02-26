using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public class Caster : IDisposable
{
    private bool _disposed;
    private readonly Configuration _configuration;

    private IDisposable? _ladleFlowRateSubscription;
    private IDisposable _tundishFlowRateSubscription;
    public Turret Turret { get; private set; }
    public Tundish Tundish { get; private set; }
    public Mold Mold { get; private set; }
    public Strand Strand { get; private set; }
    public Torch Torch { get; private set; }
    public Ladle Ladle => Turret.LadleInCastPosition;


    private double _previousTundishWeight;

    public event EventHandler? CastingFinished;

    private Action<int, double> _ladleSteelPouredHandler;
    private EventHandler _tundishWeightThresholdHandler;
    private Action<int, double> _tundishSteelPouredHandler;
    private EventHandler _strandAdvancedHandler;
    private EventHandler<Product> _torchCutDoneHandler;
    private EventHandler? _turretRotatedHandler;
    private EventHandler? _moldWeightThresholdHandler;
    private EventHandler<int>? _moldEmptyHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="Caster"/> class, representing the continuous casting machine (CCM).
    /// </summary>
    /// <param name="configuration">The configuration settings for the CCM. Cannot be null.</param>
    /// <param name="tundish">The tundish used in the casting process. Cannot be null.</param>
    /// <param name="mold">The mold used for shaping the strand. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="configuration"/>, <paramref name="tundish"/>, or <paramref name="mold"/> is null.
    /// </exception>
    public Caster(Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
        Turret = new Turret();
        Tundish = new Tundish("Tundish001", 0.127);
        Mold = new Mold("Mold001", 1.56, 0.103, 1.0, 0.8);
        Strand = new Strand(_configuration.TargetCastSpeed, _configuration.SpeedRampDuration);
        Torch = new Torch(_configuration.TorchLocation);
        
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        RegisterTurretEvents();
        RegisterTundishEvents();
        RegisterMoldEvents();
        RegisterStrandEvent();
        RegisterTorchEvents();
    }


    private void RegisterTurretEvents()
    {
        _turretRotatedHandler = (s, e) =>
        {
            if (Turret.LadleInCastPosition.State != LadleState.New) return;
            RegisterLadleEvents();
        };
        Turret.Rotated += _turretRotatedHandler;
    }

    private void RegisterLadleEvents()
    {
        if (_ladleSteelPouredHandler is not null) Ladle.SteelPoured -= _ladleSteelPouredHandler;
        _ladleSteelPouredHandler = (s, pouredSteel) =>
        {
            if (Ladle.HeatId == 0) throw new Exception("No heat in ladle.");
            Tundish.AddSteel(Ladle.HeatId, pouredSteel);
        };
        Ladle.SteelPoured += _ladleSteelPouredHandler;
    }

    private void RegisterTundishEvents()
    {
        _tundishWeightThresholdHandler = (s, e) =>
        {
            AdjustLadleFlowRate();
            _ = Tundish.PourAsync();
        };

        _tundishSteelPouredHandler = (heatId, weight) =>
        {
            Console.WriteLine($"Add steel to mold {weight} kgs.");
            Mold.AddSteel(heatId, weight);
        };

        Tundish.WeightThresholdReached += _tundishWeightThresholdHandler;
        Tundish.SteelPoured += _tundishSteelPouredHandler;
    }

    /*private async void HandlePouring(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine("Threshold reached, starting pouring...");
            await Tundish.PourAsync();
            Console.WriteLine("Pouring process completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PourAsync: {ex.Message}");
        }
    }*/

    private void RegisterMoldEvents()
    {
        _moldWeightThresholdHandler = (s, e) =>
        {
            Strand.Start();
            AdjustTundishFlowRate();
        };

        _moldEmptyHandler = (s, e) => { Strand.SetMode(StrandMode.Tailout); };

        Mold.WeightThresholdReached += _moldWeightThresholdHandler;
        Mold.ContainerEmptied += _moldEmptyHandler;
    }

    private void RegisterStrandEvent()
    {
        _strandAdvancedHandler = (s, e) =>
        {
            if (Strand.Mode != StrandMode.Tailout)
            {
                var crossSectionalArea = Mold.CrossSectionalArea; // m²
                var massFlow = crossSectionalArea * Strand.CastLengthIncrement * _configuration.SteelDensity; // kg
                Mold.RemoveSteel(massFlow);
            }

            Torch.Measure(Strand.CastLengthIncrement);

            if (Strand.TailDistanceFromMold > Torch.TorchLocation)
            {
                Strand.Stop();
                CastingFinished?.Invoke(this, EventArgs.Empty);
            }
        };

        Strand.Advanced += _strandAdvancedHandler;
    }

    private void RegisterTorchEvents()
    {
        _torchCutDoneHandler = (s, e) => { Strand.HeadFromMoldMeters = Torch.TorchLocation; };

        Torch.CutDone += _torchCutDoneHandler;
    }

    private void AdjustTundishFlowRate()
    {
        Console.WriteLine("Tundish threshold reached, starting flow rate adjustment...");
        _tundishFlowRateSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeWhile(_ => Tundish.NetWeightKgs > 0)
            .Subscribe(_ =>
            {
                //var moldLevel = Mold.GetLevel();
                //var newFlowRate = _moldLevelController.AdjustFlowRate(moldLevel, Tundish.FlowRate);
                var newFlowRate = FlowController.ComputeFlowRate(Mold.LevelMm, Tundish.FlowRateKgSec, Tundish.MaxFlowRateKgSec,825, 5);
                Tundish.SetFlowRate(newFlowRate);
            });
    }

    private void AdjustLadleFlowRate()
    {
        _ladleFlowRateSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeWhile(_ => Tundish.NetWeightKgs > 0)
            .Subscribe(_ =>
            {
                if (Ladle.NetWeightKgs == 0) return;
                var newFlowRate = FlowController.ComputeFlowRate(Tundish.LevelMm, Ladle.FlowRateKgSec, Ladle.MaxFlowRateKgSec,453, 10);
                Ladle.SetFlowRate(newFlowRate);
            });
    }

    /*private void AdjustLadleFlowRate()
    {
        /*var weightError = Tundish.NetWeight - _configuration.MaxTundishWeight;
        var weightDifference = Tundish.NetWeight - _previousTundishWeight;
        _previousTundishWeight = Tundish.NetWeight;

        // If within tolerance, maintain steady-state pouring
        if (Math.Abs(weightError) <= _configuration.TundishWeightFluctuationTolerance)
        {
            Ladle.SetPouringRate(_configuration.SteadyStateRate);
            return;
        }

        // Adjust pouring rate dynamically based on weight error
        var adjustment =
            -weightError *
            _configuration.TundishWeightCorrectionFactor; // Negative means reducing pouring if overweight
        var newRate = _configuration.SteadyStateRate + adjustment;

        // Ensure rate stays within limits
        newRate = Math.Clamp(newRate, _configuration.LowPouringRate, _configuration.HighPouringRate);#1#

        //var newRate = _ladleFlowController.ComputeFlowRate(Tundish.NetWeight, Tundish.FlowRate);

    }
    */

    public void Dispose()
    {
        Dispose(true); // Explicit disposal
        GC.SuppressFinalize(this); // Suppress finalization
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _ladleFlowRateSubscription?.Dispose();

            _tundishFlowRateSubscription?.Dispose();

            Ladle.SteelPoured -= _ladleSteelPouredHandler;

            Tundish.WeightThresholdReached -= _tundishWeightThresholdHandler;

            Tundish.SteelPoured -= _tundishSteelPouredHandler;

            Mold.WeightThresholdReached -= _moldWeightThresholdHandler;

            Mold.ContainerEmptied -= _moldEmptyHandler;

            Strand.Advanced -= _strandAdvancedHandler;

            Torch.CutDone -= _torchCutDoneHandler;

            Turret.Rotated -= _turretRotatedHandler;
        }

        _disposed = true;
    }

    ~Caster()
    {
        Dispose(false);
    }
}