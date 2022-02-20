using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class Position
    {
        public InvestmentVehicle InvestmentVehicle { get; set; }
        public decimal Quantity { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
