using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CasterSimulator.Common.Collections;
using CasterSimulator.Enums;
using CasterSimulator.Models;

namespace CasterSimulator.Components
{
    /// <summary>
    /// Provides static methods for optimizing the cutting schedule of products from the continuous cast strand.
    /// Aims to maximize material utilization by adjusting product lengths and adding additional products
    /// when necessary to minimize waste.
    /// </summary>
    public static class CutScheduler
    {
        /// <summary>
        /// Calculates the additional steel that can be added to a product within its allowable length range.
        /// Takes into account the minimum scrap requirement (4 meters) for the remaining steel.
        /// </summary>
        /// <param name="remainingSteel">The amount of steel remaining in the strand, in meters.</param>
        /// <param name="aimLength">The target length of the product, in meters.</param>
        /// <param name="maxLength">The maximum allowable length for the product, in meters.</param>
        /// <returns>
        /// The amount of additional steel that can be allocated to the product, in meters,
        /// while ensuring any leftover steel is either zero or at least 4 meters.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if remainingSteel is less than 4 meters, which is the minimum processable length.
        /// </exception>
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

        /// <summary>
        /// Optimizes the cutting schedule for a strand with a specific amount of steel remaining.
        /// Adjusts product lengths and adds additional products as needed to maximize material utilization
        /// while respecting minimum and maximum product length constraints.
        /// </summary>
        /// <param name="steelInStrand">The total length of steel in the strand to be cut, in meters.</param>
        /// <param name="cutSchedule">The original queue of products scheduled to be cut.</param>
        /// <returns>
        /// An optimized queue of products with adjusted lengths and possibly additional products
        /// to efficiently utilize the available steel. If optimization fails, returns the original schedule.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if cutSchedule is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if cutSchedule is empty or steelInStrand is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if any product in the cut schedule has invalid length specifications
        /// (aim length, minimum length, or maximum length equal to zero).
        /// </exception>
        /// <remarks>
        /// The optimization process follows these general steps:
        /// 1. If there's more steel than needed for planned products, add extra products based on the last product specs
        /// 2. Allocate steel to products in order, respecting their length constraints
        /// 3. Handle remaining steel by either:
        ///    - Creating a reduced-length product if at least 4 meters remain
        ///    - Adding the remaining steel to the last product if within its maximum length
        ///    - Creating a special "tail" product for small remnants
        /// 4. Ensure any remaining steel less than 4 meters is properly handled to avoid waste
        /// 
        /// The method logs details of the optimization process to the console for debugging.
        /// </remarks>
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

            var cutScheduleCopy = new Queue<Product>();
            
            double accumulatedLength = 0;

            // Copy products that can fit within the available steel length
            foreach (var product in cutSchedule)
            {
                cutScheduleCopy.Enqueue(product);
                accumulatedLength += product.LengthAimMeters;

                if (accumulatedLength > remainingSteel)
                    break;
            }

            // If we have more steel than needed for planned products,
            // add extra products based on the last product specifications
            while (remainingSteel > cutScheduleCopy.Sum(x => x.LengthAimMeters))
            {
                Console.WriteLine($"Adding extra product {idx++}. Steel length: {steelInStrand}");
                var last = cutScheduleCopy.Last();
                cutScheduleCopy.Enqueue(new Product(last) { ProductId = $"{last.SequenceId}-{idx:D2}", IsPlanned = false });
                remainingSteel -= last.LengthAimMeters;
                idx++;
            }
            
            Console.WriteLine($"Pre optimized cutSchedule. Steel length: {remainingSteel}:");
            foreach (var product in cutScheduleCopy)
            {
                Console.WriteLine(
                    $"Product Id: {product.ProductId}, length aim: {product.LengthAimMeters}, length min: {product.LengthMin}, length max: {product.LengthMax}");
            }

            // Main optimization loop
            while (remainingSteel > 0)
            {
                // Handle very small remaining pieces (less than 4 meters)
                if (remainingSteel < 4 && optimizedSchedule.TryPeek(out var product))
                {
                    Console.WriteLine($"Condition 0");
                    var removeSteel = 4 - remainingSteel;
                    product.LengthAimMeters -= removeSteel;
                    optimizedSchedule.Enqueue(new Product()
                    {
                        SequenceId = product.SequenceId,
                        ProductId = $"{product.SequenceId}-TAIL", IsPlanned = false,
                        LengthAimMeters = 4
                    });
                    remainingSteel = 0;
                    break;
                }

                if (!cutScheduleCopy.TryDequeue(out var nextProduct)) break;

                optimizedSchedule.TryPeek(out var lastProduct);

                // Case: Enough steel for the full product
                if (remainingSteel >= nextProduct.LengthAimMeters)
                {
                    Console.WriteLine($"Condition 1");
                    optimizedSchedule.Enqueue(new Product(nextProduct));
                    remainingSteel -= nextProduct.LengthAimMeters;
                }
                else
                    switch (remainingSteel)
                    {
                        // Case: Enough steel for a reduced-length product (at least minimum length)
                        case >= 4 when remainingSteel >= nextProduct.LengthMin:
                            Console.WriteLine($"Condition 2");
                            optimizedSchedule.Enqueue(new Product(nextProduct) { LengthAimMeters = remainingSteel });
                            remainingSteel = 0;
                            break;
                        // Case: Can add remaining steel to the previous product (within its maximum length)
                        case >= 4 when lastProduct != null &&
                                       lastProduct.LengthMax - lastProduct.LengthAimMeters > 0.0:
                            Console.WriteLine($"Condition 3");
                            lastProduct.LengthAimMeters += lastProduct.LengthMax - lastProduct.LengthAimMeters;
                            remainingSteel -= lastProduct.LengthAimMeters;
                            break;
                        // Case: Create a special tail product for the remaining steel
                        case >= 4:
                            Console.WriteLine($"Condition 4");
                            optimizedSchedule.Enqueue(new Product()
                            {
                                SequenceId = nextProduct.SequenceId,
                                ProductId = $"{nextProduct.SequenceId}-TAIL", IsPlanned = false,
                                LengthAimMeters = remainingSteel
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