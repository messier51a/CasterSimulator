using System;
using System.Collections.Generic;
using System.Linq;
using CasterSimulator.Enums;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public static class CutScheduler
    {
        public static List<Product> Optimize(double steelInStrand, List<Product> cutSchedule)
        {
            ArgumentNullException.ThrowIfNull(cutSchedule);
            ArgumentOutOfRangeException.ThrowIfZero(cutSchedule.Count);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steelInStrand);

            if (cutSchedule.Any(cut => cut.LengthAimMeters == 0 || cut.LengthMin == 0 || cut.LengthMax == 0))
                throw new ArgumentException($"Invalid cutSchedule data.", nameof(cutSchedule));

            var optimizedSchedule = new List<Product>();
            var sortedCutSchedule = new Queue<Product>(cutSchedule.OrderBy(x => x.CutNumber));
            var remainingSteel = steelInStrand;
            int dynamicProductCounter = 0;

            while (remainingSteel > 0)
            {
                var nextCut = sortedCutSchedule.TryDequeue(out var product)
                    ? new Product(product)
                    : new Product(optimizedSchedule.Last())
                    {
                        CutNumber = optimizedSchedule.Last().CutNumber + 1,
                        IsPlanned = false
                    };

                if (remainingSteel - nextCut.LengthAimMeters < 0)
                {
                    if (remainingSteel >= nextCut.LengthMin)
                    {
                        nextCut.LengthAimMeters = remainingSteel;
                    }
                    else
                    {
                        break;
                    }
                }

                if (!nextCut.IsPlanned)
                    nextCut.ProductId = $"{nextCut.ProductId}-{dynamicProductCounter++:D2}";

                optimizedSchedule.Add(nextCut);
                remainingSteel -= nextCut.LengthAimMeters;

                if (remainingSteel == 0) return optimizedSchedule;
            }

            var lastCut = optimizedSchedule[^1];

            if (remainingSteel >= 4)
            {
                optimizedSchedule.Add(new Product(lastCut)
                {
                    ProductId = $"{lastCut.ProductId}-{dynamicProductCounter++:D2}",
                    CutNumber = lastCut.CutNumber + 1,
                    IsPlanned = false,
                    LengthAimMeters = remainingSteel,
                    LengthMin = remainingSteel,
                    LengthMax = remainingSteel
                });

                return optimizedSchedule;
            }
            
            if (lastCut.LengthAimMeters + remainingSteel <= lastCut.LengthMax)
            {
                lastCut.LengthAimMeters += remainingSteel;
            }
            else
            {
                var excessSteel = 4 - remainingSteel;
                lastCut.LengthAimMeters -= excessSteel;
                optimizedSchedule.Add(new Product
                {
                    SequenceId = lastCut.SequenceId,
                    ProductId = $"{lastCut.SequenceId}-TAIL",
                    ProductType = ProductType.Tail,
                    IsPlanned = false,  
                    LengthAimMeters = 4,
                    CutNumber = optimizedSchedule.Last().CutNumber + 1
                });
            }
            
            return optimizedSchedule;
        }
    }
}