using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class Valuation
    {
        public InvestmentVehicle InvestmentVehicle { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Price { get; set; }
    }
}
