using System.Reactive.Linq;

namespace CasterSimulator.Components
{
    public enum ArmsEnum
    {
        Arm1,
        Arm2
    }

    public class Turret : IDisposable
    {
        private bool _disposed;
        public bool IsRotating { get; private set; }

        private readonly double _rotationDuration;

        private readonly Ladle[] _ladles = new Ladle[2];

        public ArmsEnum ArmNumInCastPosition { get; private set; } = ArmsEnum.Arm1;
        public ArmsEnum ArmInLoadPosition => ArmNumInCastPosition == ArmsEnum.Arm1 ? ArmsEnum.Arm2 : ArmsEnum.Arm1;
        public Ladle LadleInCastPosition => _ladles[(int)ArmNumInCastPosition];
        public Ladle LadleInLoadPosition => _ladles[(int)ArmInLoadPosition];
        public event EventHandler? Rotated; // Event triggered when the strand advances
        public event EventHandler? LadleArrivedAtCastPosition; // Event triggered when the strand advances
        
        public Turret(double rotationDuration = 10.0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(rotationDuration, 10);
            _rotationDuration = rotationDuration;
        }

        /// <summary>
        /// Attempts to remove a ladle from the specified turret arm.
        /// </summary>
        /// <param name="armNumber">The arm from which to remove the ladle.</param>
        /// <returns>
        /// <c>true</c> if the ladle was successfully removed; <c>false</c> if the arm is in the cast position or there is no ladle to remove.
        /// </returns>
        public Ladle RemoveLadle(ArmsEnum armNumber)
        {
            if (ArmNumInCastPosition == armNumber)
                throw new ArgumentException($"Arm {armNumber} is in casting position.");

            if (_ladles[(int)armNumber] is null)
                throw new ArgumentException($"No ladle found at Arm {armNumber}.");

            var ladle = _ladles[(int)armNumber];
            _ladles[(int)armNumber] = null;
            return ladle;
        }

        /// <summary>
        /// Attempts to add a new ladle to the turret arm currently in the load position.  
        /// The ladle must meet the minimum weight requirement.
        /// </summary>
        /// <param name="newLadle">The ladle to be added. Cannot be null and must weigh at least 20,000 kg.</param>
        /// <returns>
        /// <c>true</c> if the ladle was successfully added; otherwise, an exception is thrown.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="newLadle"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newLadle"/> weighs less than 20,000 kg.</exception>
        public void AddLadle(Ladle newLadle)
        {
            ArgumentNullException.ThrowIfNull(newLadle);
            ArgumentOutOfRangeException.ThrowIfLessThan(newLadle.NetWeightKgs, 20000);
            if (IsRotating) throw new InvalidOperationException($"Cannot add ladle to a rotating turret");
            _ladles[(int)ArmInLoadPosition] = newLadle;
            
        }


        /// <summary>
        /// Rotates the turret to switch arms. Ensures that rotation does not occur while another rotation is in progress
        /// and that the ladle in the cast position is closed before proceeding.
        /// </summary>
        /// <returns>A task that completes after the rotation duration.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the turret is already rotating or if the ladle in the cast position is open.
        /// </exception>
        public async Task Rotate()
        {
            if (IsRotating)
                return;

            var ladleCasting = _ladles[(int)ArmNumInCastPosition];
            if (ladleCasting is not null && ladleCasting.State == LadleState.Open)
                return;

            IsRotating = true;
            await Task.Delay(TimeSpan.FromSeconds(_rotationDuration));
            IsRotating = false;

            ArmNumInCastPosition = ArmNumInCastPosition == ArmsEnum.Arm1 ? ArmsEnum.Arm2 : ArmsEnum.Arm1;
            Rotated?.Invoke(this, EventArgs.Empty);
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
         
            }

            _disposed = true;
        }

        ~Turret()
        {
            Dispose(false);
        }
    }
}