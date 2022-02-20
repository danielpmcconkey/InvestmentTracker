using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public enum AccountType
    {
        TAXABLE_BROKERAGE,
        TRADITIONAL_401_K,
        ROTH_401_K,
        TRADITIONAL_IRA,
        ROTH_IRA,
        HSA,
        OTHER
    }
}
