using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace GraphLib.Utilities
{
    internal struct GraphScale
    {
        internal object min;
        internal object max;
        internal int numDivisions;
        internal DateRoundFunctionDelegate dateRoundingFuction;

        internal GraphScale(object min, object max, int numDivisions, DateRoundFunctionDelegate dateRoundingFuction)
        {
            this.min = min;
            this.max = max;
            this.numDivisions = numDivisions;
            this.dateRoundingFuction = dateRoundingFuction;
        }
        internal GraphScale(object min, object max, int numDivisions)
        {
            this.min = min;
            this.max = max;
            this.numDivisions = numDivisions;
            dateRoundingFuction = null;
        }
    }
}
