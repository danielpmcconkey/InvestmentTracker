using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class InvestmentDuration
    {
        public InvestmentVehicle investmentVehicle;
        public DateTimeOffset minDate;
        public DateTimeOffset maxDate;

        public InvestmentDuration(InvestmentVehicle investmentVehicle, DateTimeOffset minDate, DateTimeOffset maxDate)
        {
            this.investmentVehicle = investmentVehicle;
            this.minDate = minDate;
            this.maxDate = maxDate;
        }
    }
}
