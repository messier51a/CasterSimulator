using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CasterSimulator.Enums;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public static class CutScheduler
    {
        public static Queue<Product> Optimize(double steelInStrand, Queue<Product> cutSchedule)
        {
            ArgumentNullException.ThrowIfNull(cutSchedule);
            ArgumentOutOfRangeException.ThrowIfZero(cutSchedule.Count);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steelInStrand);

            if (cutSchedule.Any(cut => cut.LengthAimMeters == 0 || cut.LengthMin == 0 || cut.LengthMax == 0))
                throw new ArgumentException($"Invalid cutSchedule data.", nameof(cutSchedule));

            var optimizedSchedule = new Queue<Product>();
            var remainingSteel = steelInStrand;
            int dynamicProductCounter = 1;

            Console.WriteLine($"Optimizing cutSchedule. Steel length: {steelInStrand}");
            foreach (var product in cutSchedule)
            {
                Console.WriteLine($"Product Id: {product.ProductId}, length aim: {product.LengthAimMeters}");
            }

            while (remainingSteel > 0)
            {
                var nextCut = cutSchedule.TryDequeue(out var product)
                    ? new Product(product)
                    : new Product(optimizedSchedule.Last())
                    {
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
                    nextCut.ProductId = $"{nextCut.SequenceId}-{dynamicProductCounter++:D2}E";

                optimizedSchedule.Enqueue(nextCut);
                remainingSteel -= nextCut.LengthAimMeters;

                if (remainingSteel == 0)
                    return optimizedSchedule;
            }

            Product lastCut;

            if (optimizedSchedule.Count > 0)
            {
                lastCut = optimizedSchedule.Last();
                lastCut.ProductId = $"{lastCut.SequenceId}-{dynamicProductCounter++:D2}E";
                lastCut.IsPlanned = false;
            }
            else
            {
                lastCut = cutSchedule.Last();
            }

            if (remainingSteel >= 4)
            {
                optimizedSchedule.Enqueue(new Product(lastCut)
                {
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
                optimizedSchedule.Enqueue(new Product
                {
                    SequenceId = lastCut.SequenceId,
                    ProductId = $"{lastCut.SequenceId}-TAIL",
                    ProductType = ProductType.Tail,
                    IsPlanned = false,
                    LengthAimMeters = 4
                    
                });
            }

            return optimizedSchedule;
        }
    }
}