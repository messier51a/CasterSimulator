using System;
using System.Collections.Generic;
using System.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public static class CutScheduler
    {
        public static List<Product> Optimize(double steelInStrand, List<Product> cutSchedule)
        {
            var optimizedSchedule = cutSchedule.OrderBy(x => x.CutNumber).ToList();
            // Initialize with cutSchedule for efficiency
            while (optimizedSchedule.Count > 1 && steelInStrand < optimizedSchedule.Sum(x => x.LengthAimMeters))
            {
                optimizedSchedule.RemoveAt(optimizedSchedule.Count - 1);
            }

            if (optimizedSchedule.Count == 1)
            {
                optimizedSchedule.FirstOrDefault().LengthAimMeters = steelInStrand;
                return optimizedSchedule;
            }

            var remainingSteel =
                steelInStrand - optimizedSchedule.Sum(x => x.LengthAimMeters); // Calculate remainingSteel directly

            // Adjust last product's LengthAim if remainingSteel is between 0 and 4
            if (remainingSteel is > 0 and < 4)
            {
                var lastProduct = optimizedSchedule.Last();

                // Check if we can add all remainingSteel to LengthAim while staying under LengthMax
                var possibleIncrease = lastProduct.LengthMax - lastProduct.LengthAimMeters;
                if (remainingSteel <= possibleIncrease)
                {
                    // Add all remaining steel to LengthAim
                    lastProduct.LengthAimMeters += remainingSteel;
                    remainingSteel = 0;
                }
                else
                {
                    // Calculate adjustment needed to make remainingSteel exactly 4
                    var adjustmentNeeded = 4 - remainingSteel;

                    // Ensure adjustment does not reduce LengthAim below LengthMin
                    if (lastProduct.LengthAimMeters - adjustmentNeeded >= lastProduct.LengthMin)
                    {
                        lastProduct.LengthAimMeters -= adjustmentNeeded;
                        remainingSteel = 4;
                    }
                    else
                    {
                        // Adjust as much as possible within LengthMin limit
                        var actualAdjustment = lastProduct.LengthAimMeters - lastProduct.LengthMin;
                        lastProduct.LengthAimMeters -= actualAdjustment;
                        remainingSteel += actualAdjustment; // Adjust remainingSteel accordingly
                    }
                }
            }

            // Handle additional slabs while remainingSteel is greater than or equal to 4
            while (remainingSteel >= 4)
            {
                var lastProduct = optimizedSchedule.Last();
                var product = new Product(lastProduct.SequenceId, lastProduct.CutNumber + 1, Guid.NewGuid().ToString(),
                    lastProduct.LengthAimMeters,
                    lastProduct.LengthMin,
                    lastProduct.LengthMax);

                // Use as much remainingSteel as possible, but do not exceed LengthMax
                product.LengthAimMeters = Math.Min(remainingSteel, product.LengthAimMeters);
                optimizedSchedule.Add(product);
                remainingSteel -= product.LengthAimMeters;
            }

            return optimizedSchedule;
        }
    }
}