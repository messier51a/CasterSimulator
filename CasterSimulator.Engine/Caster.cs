using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text.Json;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

/// <summary>
/// Represents a continuous casting machine (CCM) that transforms molten steel into solid slabs.
/// Manages the interaction between various components including the turret, ladle, tundish, mold, strand, and torch.
/// Also controls the flow rates between components and monitors cooling sections.
/// </summary>
public class Caster : IDisposable
{
    private bool _disposed;

    private IDisposable? _ladleFlowRateSubscription;
    private IDisposable _tundishFlowRateSubscription;
    private CoolingSectionController _coolingSectionController;

    /// <summary>
    /// Gets the turret component that holds and rotates ladles into casting position.
    /// </summary>
    public Turret Turret { get; private set; }

    /// <summary>
    /// Gets the tundish component that acts as an intermediate reservoir between the ladle and mold.
    /// </summary>
    public Tundish Tundish { get; private set; }

    /// <summary>
    /// Gets the mold component that shapes the initial solidification of the steel strand.
    /// </summary>
    public Mold Mold { get; private set; }

    /// <summary>
    /// Gets the strand component that represents the continuous steel product being cast.
    /// </summary>
    public Strand Strand { get; private set; }

    /// <summary>
    /// Gets the torch component that cuts the strand into finished products.
    /// </summary>
    public Torch Torch { get; private set; }

    /// <summary>
    /// Gets the collection of cooling sections that control the solidification process along the strand path.
    /// </summary>
    public ConcurrentDictionary<int, CoolingSection> CoolingSections { get; private set; }

    /// <summary>
    /// Gets the ladle currently in the casting position on the turret.
    /// </summary>
    public Ladle Ladle => Turret.LadleInCastPosition;


    private double _previousTundishWeight;

    /// <summary>
    /// Event that fires when the casting process is complete.
    /// </summary>
    public event EventHandler? CastingFinished;

    private Action<HeatMin> _ladleSteelPouredHandler;
    private EventHandler _tundishWeightThresholdHandler;
    private Action<HeatMin> _tundishSteelPouredHandler;
    private EventHandler _strandAdvancedHandler;
    private EventHandler<Product> _torchCutDoneHandler;
    private EventHandler? _turretRotatedHandler;
    private EventHandler? _moldWeightThresholdHandler;
    private EventHandler<int>? _moldEmptyHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="Caster"/> class, representing the continuous casting machine (CCM).
    /// Loads configuration from a JSON file and initializes all components of the casting system.
    /// </summary>
    public Caster()
    {
        Configuration.Load();
        CoolingSections =
            new ConcurrentDictionary<int, CoolingSection>(
                Configuration.Cooling.Sections.ToDictionary(x => x.Id));

        Turret = new Turret();
        Tundish = new Tundish("Tundish001", 0.127);
        Mold = new Mold("Mold001", 1.56, 0.103, 1.0, 0.8);
        Strand = new Strand(Configuration.Caster.TargetCastSpeed, Configuration.Caster.SpeedRampDuration);
        Torch = new Torch(Configuration.Caster.TorchLocation);

        _coolingSectionController = new CoolingSectionController(
            Configuration.Cooling.BaseFlowLps,
            Configuration.Cooling.FlowPerSpeedLps);

        _coolingSectionController.StartCoolingMonitoring(CoolingSections);

        RegisterEvents();
    }

    /// <summary>
    /// Registers all event handlers for the various components of the casting system.
    /// </summary>
    private void RegisterEvents()
    {
        RegisterTurretEvents();
        RegisterTundishEvents();
        RegisterMoldEvents();
        RegisterStrandEvent();
        RegisterTorchEvents();
    }

    /// <summary>
    /// Registers event handlers for the turret component.
    /// </summary>
    private void RegisterTurretEvents()
    {
        _turretRotatedHandler = (s, e) =>
        {
            if (Turret.LadleInCastPosition.State != LadleState.New) return;
            RegisterLadleEvents();
        };
        Turret.Rotated += _turretRotatedHandler;
    }

    /// <summary>
    /// Registers event handlers for the ladle currently in casting position.
    /// Establishes the connection between the ladle and tundish for steel flow.
    /// </summary>
    private void RegisterLadleEvents()
    {
        if (_ladleSteelPouredHandler is not null) Ladle.SteelPoured -= _ladleSteelPouredHandler;
        _ladleSteelPouredHandler = (heat) => { Tundish.AddSteel(heat); };
        Ladle.SteelPoured += _ladleSteelPouredHandler;
    }

    /// <summary>
    /// Registers event handlers for the tundish component.
    /// Establishes the connection between the tundish and mold for steel flow.
    /// </summary>
    private void RegisterTundishEvents()
    {
        _tundishWeightThresholdHandler = (s, e) =>
        {
            AdjustLadleFlowRate();
            _ = Tundish.PourAsync();
        };

        _tundishSteelPouredHandler = (heat) => { Mold.AddSteel(heat); };

        Tundish.WeightThresholdReached += _tundishWeightThresholdHandler;
        Tundish.SteelPoured += _tundishSteelPouredHandler;
    }

    /// <summary>
    /// Registers event handlers for the mold component.
    /// Controls strand movement based on mold conditions.
    /// </summary>
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

    /// <summary>
    /// Registers event handlers for the strand component.
    /// Manages steel removal from the mold, torch measurements, and cooling section activation.
    /// </summary>
    private void RegisterStrandEvent()
    {
        _strandAdvancedHandler = (s, e) =>
        {
            if (Strand.Mode != StrandMode.Tailout)
            {
                var crossSectionalArea = Mold.CrossSectionalArea; // m²
                var massFlow = crossSectionalArea * Strand.CastLengthIncrement * Configuration.Caster.SteelDensity; // kg
                Mold.RemoveSteel(massFlow);
            }

            Torch.Measure(Strand.CastLengthIncrement, Strand.TailFromMoldMeters);

            if (Strand.TailFromMoldMeters > Torch.TorchLocation)
            {
                Strand.Stop();
                CastingFinished?.Invoke(this, EventArgs.Empty);
            }

            _coolingSectionController.ActivateSections(Strand.HeadFromMoldMeters, Strand.TailFromMoldMeters,
                Strand.CastSpeedMetersMin);
        };

        Strand.Advanced += _strandAdvancedHandler;
    }

    /// <summary>
    /// Registers event handlers for the torch component.
    /// Updates strand position when a cut is made.
    /// </summary>
    private void RegisterTorchEvents()
    {
        _torchCutDoneHandler = (s, e) => { Strand.HeadFromMoldMeters = Torch.TorchLocation; };

        Torch.CutDone += _torchCutDoneHandler;
    }

    /// <summary>
    /// Sets up a subscription to adjust the tundish flow rate based on mold level.
    /// Runs periodically until the tundish is empty.
    /// </summary>
    private void AdjustTundishFlowRate()
    {
        Console.WriteLine("Tundish threshold reached, starting flow rate adjustment...");
        _tundishFlowRateSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeWhile(_ => Tundish.NetWeightKgs > 0)
            .Subscribe(_ =>
            {
                //var moldLevel = Mold.GetLevel();
                //var newFlowRate = _moldLevelController.AdjustFlowRate(moldLevel, Tundish.FlowRate);
                var newFlowRate = FlowController.ComputeFlowRate(Mold.LevelMm, Tundish.FlowRateKgSec,
                    Tundish.MaxFlowRateKgSec, 825, 5);
                Tundish.SetFlowRate(newFlowRate);
            });
    }

    /// <summary>
    /// Sets up a subscription to adjust the ladle flow rate based on tundish level.
    /// Runs periodically until the tundish is empty.
    /// </summary>
    private void AdjustLadleFlowRate()
    {
        _ladleFlowRateSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeWhile(_ => Tundish.NetWeightKgs > 0)
            .Subscribe(_ =>
            {
                if (Ladle.NetWeightKgs == 0) return;
                var newFlowRate = FlowController.ComputeFlowRate(Tundish.LevelMm, Ladle.FlowRateKgSec,
                    Ladle.MaxFlowRateKgSec, 453, 10);
                Ladle.SetFlowRate(newFlowRate);
            });
    }

    /// <summary>
    /// Disposes of resources used by the Caster and its components.
    /// </summary>
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

            _coolingSectionController?.Dispose();
        }

        _disposed = true;
    }

    ~Caster()
    {
        Dispose(false);
    }
}