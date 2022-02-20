using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GraphLib.Utilities
{
    internal static class GraphScaler
    {
        internal static GraphScale GetAxisScale(object min, object max, Type type)
        {
            if (type == TypeHelper.int32Type || type == TypeHelper.int64Type || type == TypeHelper.floatType ||
                type == TypeHelper.doubleType || type == TypeHelper.decimalType)
            {
                // number types. find the order of magnitude in the diff between min and max
                double difference = Convert.ToDouble(max) - Convert.ToDouble(min);
                int orderOfMagnitude = MathHelper.GetOrderOfMagnitude(difference);
                double maxAtMagnitude = Math.Pow(10d, orderOfMagnitude);
                double sizeOfDivisions = maxAtMagnitude / 10d;

                // now determine where within that order of magnitude you are approx
                // if greater than half-way, use max / 10 as your
                // distance between grid points. if less than half,
                // use max / 20 as the distince. if less than 25%,
                // use max / 40
                if (difference < maxAtMagnitude / 4) sizeOfDivisions = maxAtMagnitude / 40d;
                else if (difference < maxAtMagnitude / 2) sizeOfDivisions = maxAtMagnitude / 20d;

                // set your graph min to be the closest, without going over 
                double outMin = Math.Floor(Convert.ToDouble(min) / sizeOfDivisions) * sizeOfDivisions;
                double outMax = (Math.Floor(Convert.ToDouble(max) / sizeOfDivisions) + 1) * sizeOfDivisions;

                int numberOfDivisions = (int)Math.Round((outMax - outMin) / sizeOfDivisions, 0);

                // now cast back to original type and return
                if (type == TypeHelper.int32Type)
                {
                    int outMinCast = (int)Math.Round((double)outMin, 0);
                    int outMaxCast = (int)Math.Round((double)outMax, 0);
                    return new GraphScale(outMinCast, outMaxCast, numberOfDivisions);
                }
                if (type == TypeHelper.int64Type)
                {
                    long outMinCast = (long)Math.Round((double)outMin, 0);
                    long outMaxCast = (long)Math.Round((double)outMax, 0);
                    return new GraphScale(outMinCast, outMaxCast, numberOfDivisions);
                }
                if (type == TypeHelper.floatType)
                {
                    float outMinCast = (float)outMin;
                    float outMaxCast = (float)outMax;
                    return new GraphScale(outMinCast, outMaxCast, numberOfDivisions);
                }
                if (type == TypeHelper.doubleType)
                {
                    double outMinCast = (double)outMin;
                    double outMaxCast = (double)outMax;
                    return new GraphScale(outMinCast, outMaxCast, numberOfDivisions);
                }
                if (type == TypeHelper.decimalType)
                {
                    decimal outMinCast = (decimal)outMin;
                    decimal outMaxCast = (decimal)outMax;
                    return new GraphScale(outMinCast, outMaxCast, numberOfDivisions);
                }
                throw new NotImplementedException();
            }
            else if (type == TypeHelper.dateTimeOffsetType)
            {
                TimeSpan difference = (DateTimeOffset)max - (DateTimeOffset)min;
                if (difference <= new TimeSpan(0, 1, 0))
                {
                    // less than a minute. Convert to seconds and run it through the numbers calc
                    var numbersResult = GetAxisScale(
                        ((DateTimeOffset)min).ToUnixTimeSeconds(),
                        ((DateTimeOffset)max).ToUnixTimeSeconds(),
                        TypeHelper.int64Type);

                    return new GraphScale(
                        DateTimeOffset.FromUnixTimeSeconds((long)(numbersResult.min)),
                        DateTimeOffset.FromUnixTimeSeconds((long)(numbersResult.max)),
                        numbersResult.numDivisions,
                        DateTimeHelper.DontRound);
                }
                else if (difference <= new TimeSpan(0, 15, 0))
                {
                    // less than 15 minutes. set the difference to 1 min increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToMinute(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToMinute(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(0, 1, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions,
                        DateTimeHelper.RoundToMinute);
                }
                else if (difference <= new TimeSpan(0, 60, 0))
                {
                    // less than 60 minutes. set the difference to 5 min increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo5Minute(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo5Minute(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(0, 5, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions,DateTimeHelper.RoundTo5Minute);
                }
                else if (difference <= new TimeSpan(5, 0, 0))
                {
                    // less than 5 hours. set the difference to 30 min increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo30Minute(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo30Minute(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(0, 30, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundTo30Minute);
                }
                else if (difference <= new TimeSpan(24, 0, 0))
                {
                    // less than 24 hours. set the difference to 1 hour increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToHour(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToHour(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(1, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToHour);
                }
                else if (difference <= new TimeSpan(3, 0, 0, 0))
                {
                    // less than 3 days. set the difference to 4 hour increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo4Hour(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo4Hour(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(4, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundTo4Hour);
                }
                else if (difference <= new TimeSpan(6, 0, 0, 0))
                {
                    // less than 6 days. set the difference to 12 hour increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo12Hour(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo12Hour(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(12, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundTo12Hour);
                }
                else if (difference <= new TimeSpan(18, 0, 0, 0))
                {
                    // less than 18 days. set the difference to 1 day increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToDay(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToDay(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(1, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToDay);
                }
                else if (difference <= new TimeSpan(30, 0, 0, 0))
                {
                    // less than 30 days. set the difference to 7 day increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToWeek(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToWeek(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(7, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToWeek);
                }
                else if (difference <= new TimeSpan(120, 0, 0, 0))
                {
                    // less than 120 days. set the difference to 15 day increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToHalfMonth(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToHalfMonth(castMax, RoundDateDirection.UP);

                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(15, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToHalfMonth);
                }
                else if (difference <= new TimeSpan(18 * 30, 0, 0, 0))
                {
                    // less than 18 months. set the difference to 1 month increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToMonth(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToMonth(castMax, RoundDateDirection.UP);

                    // with months, this isn't going to be clean division
                    // I'm hoping that the rounding will just "work"
                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(30, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToMonth);
                }
                else if (difference <= new TimeSpan(36 * 30, 0, 0, 0))
                {
                    // less than 3 years. set the difference to 6 month increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToHalfYear(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToHalfYear(castMax, RoundDateDirection.UP);

                    // with months, this isn't going to be clean division
                    // I'm hoping that the rounding will just "work"
                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(30 * 6, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToHalfYear);
                }
                else if (difference <= new TimeSpan(15 * 365, 0, 0, 0))
                {
                    // less than 15 years. set the difference to 1 year increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    //double denominator = 6d;
                    //int lowerVal = (int)(Math.Floor(castMin.Month / denominator) * denominator);
                    //int upperVal = (int)((Math.Floor(castMax.Month / denominator) + 1) * denominator);

                    DateTimeOffset lowerMin = DateTimeHelper.RoundToYear(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundToYear(castMax, RoundDateDirection.UP);

                    // with years, this isn't going to be clean division
                    // I'm hoping that the rounding will just "work"
                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(365, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundToYear);
                }
                else if (difference <= new TimeSpan(30 * 365, 0, 0, 0))
                {
                    // less than 30 years. set the difference to 5 year increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo5Year(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo5Year(castMax, RoundDateDirection.UP);

                    // with years, this isn't going to be clean division
                    // I'm hoping that the rounding will just "work"
                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(365 * 5, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundTo5Year);
                }
                else if (difference <= new TimeSpan(150 * 365, 0, 0, 0))
                {
                    // less than 150 years. set the difference to 10 year increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo10Year(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo10Year(castMax, RoundDateDirection.UP);

                    // with years, this isn't going to be clean division
                    // I'm hoping that the rounding will just "work"
                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(365 * 10, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundTo10Year);
                }
                else if (difference <= new TimeSpan(300 * 365, 0, 0, 0))
                {
                    // less than 300 years. set the difference to 20 year increments
                    DateTimeOffset castMin = (DateTimeOffset)min;
                    DateTimeOffset castMax = (DateTimeOffset)max;

                    DateTimeOffset lowerMin = DateTimeHelper.RoundTo20Year(castMin, RoundDateDirection.DOWN);
                    DateTimeOffset higherMax = DateTimeHelper.RoundTo20Year(castMax, RoundDateDirection.UP);

                    // with years, this isn't going to be clean division
                    // I'm hoping that the rounding will just "work"
                    int numberOfDivisions = (int)Math.Round((higherMax - lowerMin) / new TimeSpan(365 * 20, 0, 0, 0), 0);
                    return new GraphScale(lowerMin, higherMax, numberOfDivisions, DateTimeHelper.RoundTo20Year);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }

        }
    }
}
