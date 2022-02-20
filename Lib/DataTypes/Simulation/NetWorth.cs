using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes.Simulation
{
    public class NetWorth
    {
        public DateTime dateWithinSim { get; set; }
        public long totalCashOnHand { get; set; }
        public long totalNetWorth { get; set; }
        public long totalStocks { get; set; }
        public long totalBonds { get; set; }
    }
}
