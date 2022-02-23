using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class RNG
    {
        public static decimal getRandomDecimal(decimal min, decimal max)
        {
            decimal intToDoubleMultiplier = (decimal)100000; // just about guarantees a whole number
            int bigMin = (int)(min * intToDoubleMultiplier);
            int bigMax = (int)(max * intToDoubleMultiplier);
            int bigRandom = getRandomInt(bigMin, bigMax);
            return (decimal)((decimal)bigRandom / intToDoubleMultiplier);
        }
        public static decimal getRandomDecimalWeighted(decimal min, decimal max)
        {
            //
            // gets a random number between min and max (inclusively) but 
            // weights it so that the more extreme values are less likely 
            // 
            // tiers
            //------------------------------------------------
            //    >=        <      adj_min adj_max     odds
            //------------------------------------------------
            //  0         0.53125     45      55      0.53125
            //  0.53125   0.65625     40      45      0.12500
            //  0.65625   0.78125     55      60      0.12500
            //  0.78125   0.84375     30      40      0.06250
            //  0.84375   0.90625     60      70      0.06250
            //  0.90625   0.93750     10      30      0.03125
            //  0.93750   0.96875     70      90      0.03125
            //  0.96875   0.98438      0      10      0.01563
            //  0.98438   1           90     100      0.01563

            // set up your tier boundaries
            const decimal boundary0 = 0m;
            const decimal boundary1 = (1m / 2m) + (1m / 32m);
            const decimal boundary2 = boundary1 + (1m / 8m);
            const decimal boundary3 = boundary2 + (1m / 8m);
            const decimal boundary4 = boundary3 + (1m / 16m);
            const decimal boundary5 = boundary4 + (1m / 16m);
            const decimal boundary6 = boundary5 + (1m / 32m);
            const decimal boundary7 = boundary6 + (1m / 32m);
            const decimal boundary8 = boundary7 + (1m / 64m);
            const decimal boundary9 = 1m;
            // set up your actual tiers
            const decimal tier0 = 0m;
            const decimal tier1 = .10m;
            const decimal tier2 = .20m;
            const decimal tier3 = .30m;
            const decimal tier4 = .40m;
            //const decimal tier5 = .50m;    // not used
            const decimal tier6 = .60m;
            const decimal tier7 = .70m;
            const decimal tier8 = .80m;
            const decimal tier9 = .90m;
            const decimal tier10 = 1.00m;
            // get a decimal between 0 and 1
            decimal tierRand = getRandomDecimal(0.0m, 1.0m);
            // set up the top and bottom you'll ultimately use to return your random number
            decimal weightedBottom = 0m;
            decimal weightedTop = 0m;
            // set up the percentages of the min and max we'll used
            decimal adjustedMinPercent = 0m;
            decimal adjustedMaxPercent = 0m;

            if (tierRand >= boundary0 && tierRand < boundary1)
            {
                adjustedMinPercent = tier4;
                adjustedMaxPercent = tier6;
            }
            else if (tierRand >= boundary1 && tierRand < boundary2)
            {
                // 1 level down
                adjustedMinPercent = tier3;
                adjustedMaxPercent = tier4;
            }
            else if (tierRand >= boundary2 && tierRand < boundary3)
            {
                // 1 level up
                adjustedMinPercent = tier6;
                adjustedMaxPercent = tier7;
            }
            else if (tierRand >= boundary3 && tierRand < boundary4)
            {
                // 2 levels down
                adjustedMinPercent = tier2;
                adjustedMaxPercent = tier3;
            }
            else if (tierRand >= boundary4 && tierRand < boundary5)
            {
                // 2 levels up
                adjustedMinPercent = tier7;
                adjustedMaxPercent = tier8;
            }
            else if (tierRand >= boundary5 && tierRand < boundary6)
            {
                // 3 level down
                adjustedMinPercent = tier1;
                adjustedMaxPercent = tier2;
            }
            else if (tierRand >= boundary6 && tierRand < boundary7)
            {
                // 3 levels up
                adjustedMinPercent = tier8;
                adjustedMaxPercent = tier9;
            }
            else if (tierRand >= boundary7 && tierRand < boundary8)
            {
                // 4 levels down
                adjustedMinPercent = tier0;
                adjustedMaxPercent = tier1;
            }
            else if (tierRand >= boundary8 && tierRand < boundary9)
            {
                // 4 levels up
                adjustedMinPercent = tier9;
                adjustedMaxPercent = tier10;
            }

            weightedBottom = min + ((max - min) * adjustedMinPercent);
            weightedTop = min + ((max - min) * adjustedMaxPercent);

            // finally, return a random float between your weighted bottom and top
            return getRandomDecimal(weightedBottom, weightedTop);
        }
        public static int getRandomInt(int minInclusive, int maxInclusive)
        {
            CryptoRandom cr = new CryptoRandom();
            return cr.Next(minInclusive, maxInclusive + 1);
        }
        public static bool getRandomBool()
        {
            if (getRandomInt(0, 99) > 49) return true;
            return false;
        }
    }
}
