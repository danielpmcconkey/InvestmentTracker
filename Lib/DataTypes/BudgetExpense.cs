using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class BudgetExpense
    {
        public int id { get; set; }
        public string expenseAccount { get; set; }
        public DateTime transactionDate { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public decimal amount { get; set; }

    }
}
