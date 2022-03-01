using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class Valuation
    {
        public Guid Id { get; set; }
        public InvestmentVehicle InvestmentVehicle { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Price { get; set; }

        public Valuation()
        {
            
        }
        public Valuation(InvestmentVehicle investmentVehicle, DateTimeOffset date, decimal price)
        {
            InvestmentVehicle = investmentVehicle;
            Date = date;
            Price = price;
            Id = Guid.NewGuid();
            if (!DataAccessLayer.IsValuationInDb(this))
            {
                DataAccessLayer.WriteNewValuationToDb(this);
            }
        }
    }
}
