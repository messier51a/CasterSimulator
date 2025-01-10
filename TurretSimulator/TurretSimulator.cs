using System;

namespace SteelCastingSimulation
{
    public class TurretSimulator
    {
        private LadleSimulator position1;
        private LadleSimulator position2;
        private Random random = new Random();
        private readonly int minRotateSeconds = 30;
        private readonly int maxRotateSeconds = 90;

        // The currently pouring ladle (on the "active" side).
        public LadleSimulator ActiveLadle { get; private set; }

        public TurretSimulator(LadleSimulator initialLadle)
        {
            // Let’s place the first ladle at position2 so it’s "active".
            position2 = initialLadle;
            ActiveLadle = position2;
        }

        // We load the next ladle into the "non-active" position (position1).
        public void LoadLadle(LadleSimulator newLadle)
        {
            position1 = newLadle;
        }

        // Return how many seconds rotation would take, no actual sleeping here.
        public int PrepareRotation()
        {
            return random.Next(minRotateSeconds, maxRotateSeconds + 1);
        }

        // Swap the positions so that the new ladle becomes active.
        public void CompleteRotation()
        {
            var temp = position2;
            position2 = position1;
            position1 = temp;
            ActiveLadle = position2;
        }
    }
}