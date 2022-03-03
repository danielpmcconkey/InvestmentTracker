using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Lib.DataTypes.Simulation
{
    public class SimulationRunResult
    {
        public bool wasSuccessful { get; set; }
        public DateTime startdate { get; set; }
        public DateTime retirementdate { get; set; }
        public DateTime deathdate { get; set; }
        public DateTime? bankruptcydate { get; set; }
        public decimal? ageAtBankruptcy { get; set; }
        public int numberofrecessions { get; set; }
        public decimal wealthAtRetirement { get; set; }
        public decimal wealthAtDeath { get; set; }
        public List<NetWorth> netWorthSchedule { get; set; }
        public decimal totalLifeStyleSpend { get; set; }
        public DateTime retirementDateHistoricalAnalog { get; set; }
    }
}
