using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public TransactionType TransactionType { get; set; }
        public InvestmentVehicle InvestmentVehicle { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Quantity { get; set; } // number of shares for traded; quantity for private
        public decimal CashPriceTotalTransaction { get; set; } // price for all units of quantity. A positive amount is what you put in

        public Transaction()
        {

        }
        public Transaction(
            TransactionType tType, InvestmentVehicle vehicle, DateTimeOffset date, decimal cashVal, decimal qty)
        {
            TransactionType = tType;
            InvestmentVehicle = vehicle;
            Date = date;
            CashPriceTotalTransaction = cashVal;
            Quantity = qty;

            Id = Guid.NewGuid();
            // don't write to the DB here because we don't have an account ID
        }
    }
}
