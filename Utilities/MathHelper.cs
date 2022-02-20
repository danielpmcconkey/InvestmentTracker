using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class MathHelper
    {
        public static int GetOrderOfMagnitude(double value)
        {
            double absValue = Math.Abs(value);
            int exponent = 0;

            if (absValue < 1)
            {
                while (exponent > Double.NegativeInfinity)
                {
                    if (absValue <= Math.Pow(10, exponent)
                        && absValue > Math.Pow(10, exponent - 1))
                    {
                        return exponent;
                    }
                    exponent--;
                }
            }
            else
            {
                while (exponent < Double.PositiveInfinity)
                {
                    if (absValue >= Math.Pow(10, exponent)
                        && absValue < Math.Pow(10, exponent + 1))
                    {
                        return exponent + 1;
                    }
                    exponent++;
                }
            }
            return exponent;
        }
    }
}
