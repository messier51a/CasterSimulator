using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public class Caster : IDisposable
{
    private bool _disposed;
    private readonly Configuration _configuration;
    private IDisposable? _pouringRateSubscription;
    public Turret Turret { get; private set; }
    public Tundish Tundish { get; private set; }
    public Mold Mold { get; private set; }
    public Strand Strand { get; private set; }
    public Torch Torch { get; private set; }
    public Ladle Ladle => Turret.LadleInCastPosition;
    
    
    private double _previousTundishWeight;
    
    public event EventHandler? CastingFinished;

    private EventHandler<double> _ladleSteelPouredHandler;
    private EventHandler _tundishWeightThresholdHandler;
    private EventHandler _tundishEmptyHandler;
    private EventHandler _strandAdvancedHandler;
    private EventHandler<Product> _torchCutDoneHandler;
    private EventHandler? _turretRotatedHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="Caster"/> class, representing the continuous casting machine (CCM).
    /// </summary>
    /// <param name="configuration">The configuration settings for the CCM. Cannot be null.</param>
    /// <param name="tundish">The tundish used in the casting process. Cannot be null.</param>
    /// <param name="mold">The mold used for shaping the strand. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="configuration"/>, <paramref name="tundish"/>, or <paramref name="mold"/> is null.
    /// </exception>
    public Caster(Configuration configuration, Tundish tundish, Mold mold)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(tundish);
        ArgumentNullException.ThrowIfNull(mold);

        _configuration = configuration;
        Turret = new Turret();
        Tundish = tundish;
        Mold = mold;
        Strand = new Strand(_configuration.TargetCastSpeed, _configuration.SpeedRampDuration);
        Torch = new Torch(_configuration.TorchLocation);

        RegisterEvents();

        _pouringRateSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .Where(_ => Turret.LadleInCastPosition?.State == LadleState.Open)
            .Subscribe(_ => AdjustPouringRate());
    }

    private void RegisterEvents()
    {
        RegisterTorchEvents();
        RegisterTurretEvents();
        RegisterTundishEvents();
        RegisterStrandEvent();
    }

    private void RegisterTorchEvents()
    {
        _torchCutDoneHandler = (s, e) =>
        {
            Strand.HeadDistanceFromMold = Torch.TorchLocation;
        };

        Torch.CutDone += _torchCutDoneHandler;
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
        _ladleSteelPouredHandler = (s, pouredSteel) => { Tundish.AddSteel(pouredSteel); };
        Ladle.SteelPoured += _ladleSteelPouredHandler;
    }

    private void RegisterTundishEvents()
    {
        _tundishWeightThresholdHandler = (s, e) =>
        {
            Strand.Start();
        };
        _tundishEmptyHandler = (s, e) =>
        {
            Strand.SetMode(StrandMode.Tailout);
        };

        Tundish.WeightThresholdReached += _tundishWeightThresholdHandler;
        Tundish.Empty += _tundishEmptyHandler;
    }

    private void RegisterStrandEvent()
    {
        _strandAdvancedHandler = (s, e) =>
        {

            if (Strand.Mode != StrandMode.Tailout)
            {
                var crossSectionalArea = Mold.GetCrossSectionalArea(); // m²
                var massFlow = crossSectionalArea * Strand.CastLengthIncrement * _configuration.SteelDensity; // kg
                Tundish.RemoveSteel(massFlow);
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

    private void AdjustPouringRate()
    {
        double weightError = Tundish.NetWeight - _configuration.MaxTundishWeight;
        double weightDifference = Tundish.NetWeight - _previousTundishWeight;
        _previousTundishWeight = Tundish.NetWeight;

        // If within tolerance, maintain steady-state pouring
        if (Math.Abs(weightError) <= _configuration.TundishWeightFluctuationTolerance)
        {
            Ladle.SetPouringRate(_configuration.SteadyStateRate);
            return;
        }

        // Adjust pouring rate dynamically based on weight error
        double adjustment = -weightError * _configuration.TundishWeightCorrectionFactor; // Negative means reducing pouring if overweight
        double newRate = _configuration.SteadyStateRate + adjustment;

        // Ensure rate stays within limits
        newRate = Math.Clamp(newRate, _configuration.LowPouringRate, _configuration.HighPouringRate);

        Ladle.SetPouringRate(newRate);
    }
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
            _pouringRateSubscription?.Dispose();

            Ladle.SteelPoured -= _ladleSteelPouredHandler;

            Tundish.WeightThresholdReached -= _tundishWeightThresholdHandler;

            Tundish.Empty -= _tundishEmptyHandler;

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