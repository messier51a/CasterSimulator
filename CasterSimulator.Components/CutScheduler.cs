using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CasterSimulator.Common.Collections;
using CasterSimulator.Enums;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    public static class CutScheduler
    {
        private static double GetAdditionalSteel(double remainingSteel, double aimLength, double maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(4, remainingSteel);

            var maxAddable = maxLength - aimLength;
            var additionalSteel = Math.Min(remainingSteel, maxAddable);

            var newRemainingSteel = remainingSteel - additionalSteel;

            if (newRemainingSteel is > 0 and < 4)
            {
                additionalSteel -= (4 - newRemainingSteel);
            }

            return additionalSteel;
        }

        public static Queue<Product?> Optimize(double steelInStrand, Queue<Product?> cutSchedule)
        {
            ArgumentNullException.ThrowIfNull(cutSchedule);
            ArgumentOutOfRangeException.ThrowIfZero(cutSchedule.Count);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steelInStrand);

            if (cutSchedule.Any(cut => cut.LengthAimMeters == 0 || cut.LengthMin == 0 || cut.LengthMax == 0))
                throw new ArgumentException($"Invalid cutSchedule data.", nameof(cutSchedule));

            var optimizedSchedule = new Queue<Product?>();

            var remainingSteel = steelInStrand;

            Console.WriteLine($"Optimizing cutSchedule. Steel length: {remainingSteel}, original schedule:");

            foreach (var product in cutSchedule)
            {
                Console.WriteLine(
                    $"Product Id: {product.ProductId}, length aim: {product.LengthAimMeters}, length min: {product.LengthMin}, length max: {product.LengthMax}");
            }

            var idx = 1;

            while (remainingSteel > cutSchedule.Sum(x => x.LengthAimMeters))
            {
                Console.WriteLine($"Adding extra product {idx++}. Steel length: {steelInStrand}");
                var last = cutSchedule.Last();
                cutSchedule.Enqueue(new Product(last) { ProductId = $"{last.SequenceId}-{idx:D2}", IsPlanned = false });
                remainingSteel -= last.LengthAimMeters;
                idx++;
            }

            while (remainingSteel > 0)
            {
                if (remainingSteel < 4 && optimizedSchedule.TryPeek(out var product))
                {
                    var removeSteel = 4 - remainingSteel;
                    product.LengthAimMeters -= removeSteel;
                    optimizedSchedule.Enqueue(new Product()
                    {
                        ProductId = $"{product.SequenceId}-TAIL", IsPlanned = false,
                        LengthAimMeters = 4
                    });
                    remainingSteel = 0;
                    break;
                }

                if (!cutSchedule.TryDequeue(out var nextProduct)) break;

                optimizedSchedule.TryPeek(out var lastProduct);

                if (remainingSteel >= nextProduct.LengthAimMeters)
                {
                    optimizedSchedule.Enqueue(new Product(nextProduct));
                    remainingSteel -= nextProduct.LengthAimMeters;
                }
                else
                    switch (remainingSteel)
                    {
                        case >= 4 when remainingSteel >= nextProduct.LengthMin:
                            optimizedSchedule.Enqueue(new Product(nextProduct) { LengthAimMeters = remainingSteel });
                            remainingSteel = 0;
                            break;
                        case >= 4 when lastProduct != null &&
                                       lastProduct.LengthMax - lastProduct.LengthAimMeters > 0:
                            lastProduct.LengthAimMeters = lastProduct.LengthMax - lastProduct.LengthAimMeters;
                            remainingSteel -= lastProduct.LengthAimMeters;
                            break;
                        case >= 4:
                            optimizedSchedule.Enqueue(new Product()
                            {
                                ProductId = $"{nextProduct.SequenceId}-TAIL", IsPlanned = false,
                                LengthAimMeters = 4
                            });
                            remainingSteel = 0;
                            break;
                    }
            }

            Console.WriteLine($"CutSchedule optimized. Steel reamining: {remainingSteel}, new schedule:");

            foreach (var product in optimizedSchedule)
            {
                Console.WriteLine(
                    $"Product Id: {product.ProductId}, length aim: {product.LengthAimMeters}, length min: {product.LengthMin}, length max: {product.LengthMax}");
            }

            return optimizedSchedule.Count > 0 ? optimizedSchedule : cutSchedule;
        }
    }
}