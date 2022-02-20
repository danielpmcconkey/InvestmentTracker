using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public class Account
    {
        public string Name { get; set; }
        public AccountType AccountType { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}
