using CasterSimulator.Models;

namespace CasterSimulator.Components
{
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


    public class Ladle
    {
        private readonly double _initialSteelWeight;
        public Heat Heat { get; private set; }

        public LadleState State { get; private set; } = LadleState.New;
        public event EventHandler<int>? LadleOpened;
        public event EventHandler<double>? SteelPoured;
        public event EventHandler<int>? LadleClosed;

        public string Id { get; private set; }
        public double NetWeight { get; private set; }
        public double PouringRate { get; private set; }

        /// <summary>
        /// Represents a ladle containing molten steel, managing pouring operations.
        /// </summary>
        /// <param name="id">The unique identifier for the ladle. Must be greater than zero.</param>
        /// <param name="heat">The heat of steel contained in the ladle. Cannot be null.</param>
        /// <param name="pouringRate">
        /// The rate at which steel is poured in kilograms per second. Must be greater than zero. Default is 200 kg/s.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="id"/> or <paramref name="pouringRate"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="heat"/> is null.
        /// </exception>
        public Ladle(string id, Heat heat, double pouringRate = 200.0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
            ArgumentNullException.ThrowIfNull(heat, nameof(heat));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pouringRate, nameof(pouringRate));

            Id = id;
            Heat = heat;
            _initialSteelWeight = heat.NetWeight;
            NetWeight = heat.NetWeight;
            PouringRate = pouringRate;
        }


        public async Task<int> PourSteel(double initialFlowRate)
        {
            PouringRate = initialFlowRate;

            State = LadleState.Open;

            LadleOpened?.Invoke(this, Heat.Id);
            while (NetWeight > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var pouredSteel = Math.Min(PouringRate, NetWeight);
                NetWeight -= pouredSteel;
                SteelPoured?.Invoke(this, pouredSteel);
            }

            State = LadleState.Closed;
            LadleClosed?.Invoke(this, Heat.Id);
            return Heat.Id;
        }

        public void SetPouringRate(double newRate)
        {
            PouringRate = newRate;
        }
    }
}