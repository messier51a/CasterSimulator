using System;
using System.Collections.Generic;
using System.Linq;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public class CutScheduler
    {
        public event Action<object, List<Product>> ScheduleUpdated;

        public List<Product> Optimize(double steelInStrand, List<Product> cutSchedule)
        {
            var optimizedSchedule = new List<Product>();
            double remainingSteel = steelInStrand;

            // Case 1: Excess steel
            var totalScheduleLength = cutSchedule.Sum(x => x.LengthAim);
            Console.WriteLine(totalScheduleLength);
            Console.WriteLine(remainingSteel);

            if (remainingSteel >= totalScheduleLength)
            {
                optimizedSchedule.AddRange(cutSchedule);
                remainingSteel -= totalScheduleLength;

                if (remainingSteel < 4)
                {
                    var lastProduct = optimizedSchedule.Last();
                    lastProduct.LengthAim -= 4 - remainingSteel;
                    remainingSteel = 0;
                }

                while (remainingSteel >= 4)
                {
                    var lastProduct = optimizedSchedule.Last();
                    var product = new Product(Guid.NewGuid().ToString(), lastProduct.LengthAim, lastProduct.LengthMin,
                        lastProduct.LengthMax);
                    product.LengthAim = remainingSteel >= product.LengthMax ? product.LengthMax : remainingSteel;
                    optimizedSchedule.Add(product);
                    remainingSteel -= product.LengthAim;
                }
            }

            ScheduleUpdated?.Invoke(this, optimizedSchedule);
            return optimizedSchedule;
        }
    }
}