using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.DataTypes.Simulation
{
    public class Analytics
    {
        // results
        public int totalRunsWithBankruptcy { get; set; }
        public int totalRunsWithoutBankruptcy { get; set; }
        public decimal averageAgeAtBankruptcy { get; set; }
        public decimal minAgeAtBankruptcy { get; set; }
        public decimal bankruptcyAge90Percent { get; set; }
        public decimal bankruptcyAge95Percent { get; set; }
        public decimal bankruptcyAge99Percent { get; set; }
        public decimal maxAgeAtBankruptcy { get; set; }
        public decimal averageNumberOfRecessionsInBankruptcyRuns { get; set; }
        public decimal averageNumberOfRecessionsInNonBankruptcyRuns { get; set; }
        public decimal averageWealthAtRetirement { get; set; }
        public decimal averageWealthAtDeath { get; set; }
        public decimal averageLifeStyleSpend { get; set; }
        public decimal averageLifeStyleSpendBadYears { get; set; }
        public decimal averageLifeStyleSpendSuccessfulBadYears { get; set; }
        public decimal medianLifeStyleSpend { get; set; }
        public decimal bottom10PercentLifeStyleSpend { get; set; }
        public decimal wealthAtDeath90Percent { get; set; }   // best wealth at death for the worst 10% of success runs
        public decimal wealthAtDeath95Percent { get; set; }   // best wealth at death for the worst 5% of success runs
        public decimal successRateBadYears { get; set; }
        public decimal successRateGoodYears { get; set; }
        public decimal successRateOverall { get; set; }
        public Analytics()
        {

        }
    }
}
