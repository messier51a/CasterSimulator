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
            while (optimizedSchedule.Count > 0 && steelInStrand < optimizedSchedule.Sum(x => x.LengthAim))
            {
                optimizedSchedule.RemoveAt(optimizedSchedule.Count - 1);
            }

            var remainingSteel =
                steelInStrand - cutSchedule.Sum(x => x.LengthAim); // Calculate remainingSteel directly

            // Adjust last product's LengthAim if remainingSteel is between 0 and 4
            if (remainingSteel is > 0 and < 4)
            {
                var lastProduct = optimizedSchedule.Last();

                // Check if we can add all remainingSteel to LengthAim while staying under LengthMax
                double possibleIncrease = lastProduct.LengthMax - lastProduct.LengthAim;
                if (remainingSteel <= possibleIncrease)
                {
                    // Add all remaining steel to LengthAim
                    lastProduct.LengthAim += remainingSteel;
                    remainingSteel = 0;
                }
                else
                {
                    // Calculate adjustment needed to make remainingSteel exactly 4
                    double adjustmentNeeded = 4 - remainingSteel;

                    // Ensure adjustment does not reduce LengthAim below LengthMin
                    if (lastProduct.LengthAim - adjustmentNeeded >= lastProduct.LengthMin)
                    {
                        lastProduct.LengthAim -= adjustmentNeeded;
                        remainingSteel = 4;
                    }
                    else
                    {
                        // Adjust as much as possible within LengthMin limit
                        double actualAdjustment = lastProduct.LengthAim - lastProduct.LengthMin;
                        lastProduct.LengthAim -= actualAdjustment;
                        remainingSteel += actualAdjustment; // Adjust remainingSteel accordingly
                    }
                }
            }

            // Handle additional slabs while remainingSteel is greater than or equal to 4
            while (remainingSteel >= 4)
            {
                var lastProduct = optimizedSchedule.Last();
                var product = new Product(lastProduct.SequenceId,lastProduct.CutNumber + 1, Guid.NewGuid().ToString(), lastProduct.LengthAim,
                    lastProduct.LengthMin,
                    lastProduct.LengthMax);

                // Use as much remainingSteel as possible, but do not exceed LengthMax
                product.LengthAim = Math.Min(remainingSteel, product.LengthAim);
                optimizedSchedule.Add(product);
                remainingSteel -= product.LengthAim;
            }

            return optimizedSchedule;
        }
    }
}