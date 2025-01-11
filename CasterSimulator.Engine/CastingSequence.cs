using System;
using CasterSimulator.Components;

namespace CasterSimulator.Engine
{
    public class CastingSequence
    {
        private readonly Tundish _tundish;
        private readonly Ladle[] _ladles;
        private readonly Mold _mold;
        private readonly Strand _strand;
        private int _currentLadleIndex;
        private bool _isRunning;

        public CastingSequence(Ladle[] ladlesArray, Mold mold, Tundish tundish)
        {
            if (ladlesArray == null || ladlesArray.Length == 0)
                throw new ArgumentException("At least one ladle is required.", nameof(ladlesArray));

            _mold = mold ?? throw new ArgumentNullException(nameof(mold));
            _tundish = tundish ?? throw new ArgumentNullException(nameof(tundish));
            _ladles = ladlesArray;
            _strand = new Strand(_mold);

            _currentLadleIndex = 0;

            RegisterLadleEvents(_ladles[0]);
            RegisterTundishEvents();
            RegisterStrandEvents();
        }

        public void Run()
        {
            Console.WriteLine("Starting Steel Casting Simulation...");
            _ladles[_currentLadleIndex].OpenLadle();
           
            _isRunning = true;

            while (_isRunning)
            {
                // Simulation logic can go here if needed
            }
        }

        private void RegisterLadleEvents(Ladle ladle)
        {
            ladle.SteelPoured += (s, pouredSteel) =>
            {
                _tundish.AddSteel(pouredSteel);
                Console.WriteLine($"Ladle {ladle.HeatId} poured {pouredSteel:F2} lbs.");
            };

            ladle.LadleEmpty += (s, e) =>
            {
                Console.WriteLine($"Ladle {ladle.HeatId} is empty.");
                SwitchLadle();
            };
        }

        private void RegisterTundishEvents()
        {
            _tundish.CastingThresholdReached += (s, e) =>
            {
                _strand.StartCasting(3.0 / 60.0); // Start with an initial speed of 3 m/min
                Console.WriteLine("Casting started.");
            };
            _tundish.TundishEmpty += (s, e) =>
            {
                Console.WriteLine("Casting ended.");
                _strand.TailOut();
                _isRunning = false;
            };
        }

        private void RegisterStrandEvents()
        {
            _strand.StrandUpdated += (s, e) =>
            {
                // Calculate mass flow based on strand's length increment
                double crossSectionalArea = _mold.GetCrossSectionalArea(); // m²
                double steelDensity = 7850; // kg/m³
                double massFlow = crossSectionalArea * _strand.LastIncrement * steelDensity * 2.20462; // lbs

                // Remove steel from the tundish
                _tundish.RemoveSteel(massFlow);
                Console.WriteLine($"Tundish updated: Removed {massFlow:F2} lbs. Remaining weight: {_tundish.CurrentSteelWeight:F2} lbs.");
                Console.WriteLine($"Strand advanced by {_strand.LastIncrement:F2} m. Total strand length: {_strand.StrandLength:F2} m.");
            };

            _strand.SlabCut += (s, e) =>
            {
                Console.WriteLine($"Slab cut. Current strand length: {_strand.StrandLength:F2} m, Total cast length: {_strand.CastLength:F2} m.");
            };
        }

        private void SwitchLadle()
        {
            if (++_currentLadleIndex >= _ladles.Length)
            {
                Console.WriteLine("No more ladles available.");
                _isRunning = false;
                return;
            }

            var nextLadle = _ladles[_currentLadleIndex];
            RegisterLadleEvents(nextLadle);
            nextLadle.OpenLadle();
        }
    }
}
