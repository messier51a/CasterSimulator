using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using CasterSimulator.Components;
using CasterSimulator.Models;

namespace CasterSimulator.Engine;

public class Caster : IDisposable
{
    private bool _disposed;
    private readonly Configuration _configuration;

    private IDisposable? _ladleFlowRateSubscription;
    private IDisposable _tundishFlowRateSubscription;
    private CoolingSectionController _coolingSectionController;
    public Turret Turret { get; private set; }
    public Tundish Tundish { get; private set; }
    public Mold Mold { get; private set; }
    public Strand Strand { get; private set; }
    public Torch Torch { get; private set; }

    public ConcurrentDictionary<int, CoolingSection> CoolingSections { get; private set; }
    public Ladle Ladle => Turret.LadleInCastPosition;


    private double _previousTundishWeight;

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
    /// </summary>
    /// <param name="configuration">The configuration settings for the CCM. Cannot be null.</param>
    /// <param name="tundish">The tundish used in the casting process. Cannot be null.</param>
    /// <param name="mold">The mold used for shaping the strand. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="configuration"/>, <paramref name="tundish"/>, or <paramref name="mold"/> is null.
    /// </exception>
    public Caster()
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, "configuration.json");
        var configurationJson = File.ReadAllText(filePath);
        _configuration = JsonSerializer.Deserialize<Configuration>(configurationJson);

        CoolingSections =
            new ConcurrentDictionary<int, CoolingSection>(
                _configuration.CoolingSectionConfiguration.Sections.ToDictionary(x => x.Id));

        Turret = new Turret();
        Tundish = new Tundish("Tundish001", 0.127);
        Mold = new Mold("Mold001", 1.56, 0.103, 1.0, 0.8);
        Strand = new Strand(_configuration.TargetCastSpeed, _configuration.SpeedRampDuration);
        Torch = new Torch(_configuration.TorchLocation);

        _coolingSectionController = new CoolingSectionController(
            _configuration.CoolingSectionConfiguration.BaseFlowLps,
            _configuration.CoolingSectionConfiguration.FlowPerSpeedLps);
        _coolingSectionController.StartCoolingMonitoring(CoolingSections);

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
        _ladleSteelPouredHandler = (heat) => { Tundish.AddSteel(heat); };
        Ladle.SteelPoured += _ladleSteelPouredHandler;
    }

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

            if (Strand.TailFromMoldMeters > Torch.TorchLocation)
            {
                Strand.Stop();
                CastingFinished?.Invoke(this, EventArgs.Empty);
            }

            _coolingSectionController.ActivateSections(Strand.HeadFromMoldMeters, Strand.TailFromMoldMeters, Strand.CastSpeedMetersMin);
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
                var newFlowRate = FlowController.ComputeFlowRate(Mold.LevelMm, Tundish.FlowRateKgSec,
                    Tundish.MaxFlowRateKgSec, 825, 5);
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
                var newFlowRate = FlowController.ComputeFlowRate(Tundish.LevelMm, Ladle.FlowRateKgSec,
                    Ladle.MaxFlowRateKgSec, 453, 10);
                Ladle.SetFlowRate(newFlowRate);
            });
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