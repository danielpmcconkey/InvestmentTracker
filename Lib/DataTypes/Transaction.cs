using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class Transaction
    {
        public TransactionType TransactionType { get; set; }
        public InvestmentVehicle InvestmentVehicle { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Quantity { get; set; } // number of shares for traded; quantity for private
        public decimal CashPriceTotalTransaction { get; set; } // price for all units of quantity. A positive amount is what you put in
    }
}
