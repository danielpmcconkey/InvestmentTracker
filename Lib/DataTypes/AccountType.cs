using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes
{
    public enum AccountType
    {
        TAXABLE_BROKERAGE = 0,
        TRADITIONAL_401_K = 1,
        ROTH_401_K = 2,
        TRADITIONAL_IRA = 3,
        ROTH_IRA = 4,
        HSA = 5,
        OTHER = 6,
    }
}
