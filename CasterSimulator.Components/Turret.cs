using System;

namespace CasterSimulator.Components
{
    public class Turret
    {
        private Ladle activeLadle; // Ladle currently aligned with the tundish
        private Ladle nextLadle; // Ladle waiting in the alternate position
        private bool isRotating; // Indicates if the turret is rotating
        private double rotationTimeRemaining; // Time left for the turret to complete rotation (in seconds)
        private readonly double rotationDuration; // Total time required for turret rotation (in seconds)

        public Ladle ActiveLadle => activeLadle; // Expose the active ladle
        public bool IsRotating => isRotating; // Expose rotation status

        public Turret(Ladle initialLadle, double rotationDuration = 60.0)
        {
            if (initialLadle == null)
                throw new ArgumentNullException(nameof(initialLadle), "An initial ladle must be provided.");

            activeLadle = initialLadle;
            nextLadle = null;
            isRotating = false;
            this.rotationDuration = rotationDuration;
            rotationTimeRemaining = 0.0;
        }

        // Loads the next ladle into the turret
        public void LoadLadle(Ladle ladle)
        {
            if (ladle == null)
                throw new ArgumentNullException(nameof(ladle), "A ladle must be provided to load.");

            if (nextLadle != null)
                throw new InvalidOperationException("The turret already has a ladle waiting in the alternate position.");

            nextLadle = ladle;
        }

        // Starts turret rotation to switch the ladle positions
        public void StartRotation()
        {
            if (isRotating)
                throw new InvalidOperationException("The turret is already rotating.");

            if (nextLadle == null)
                throw new InvalidOperationException("No ladle is available in the alternate position for rotation.");

            isRotating = true;
            rotationTimeRemaining = rotationDuration;
            Console.WriteLine("Turret rotation started.");
        }

        // Updates the turret state for a given time interval
        public void Update(double deltaTimeSeconds)
        {
            if (!isRotating || deltaTimeSeconds <= 0)
                return;

            rotationTimeRemaining -= deltaTimeSeconds;

            if (rotationTimeRemaining <= 0)
            {
                CompleteRotation();
            }
        }

        // Completes the turret rotation and switches the ladle positions
        public void CompleteRotation()
        {
            if (!isRotating)
                return;

            isRotating = false;
            rotationTimeRemaining = 0.0;

            // Switch ladle positions
            activeLadle = nextLadle;
            nextLadle = null;

            Console.WriteLine("Turret rotation completed. New active ladle: " + activeLadle.HeatId);
        }
    }
}
