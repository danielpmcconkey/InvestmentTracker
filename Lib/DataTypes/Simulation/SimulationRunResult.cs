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

        private void logNetWorthSchedule()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("********************************************************************************");
            sb.AppendLine("|    DATE   |    NET WORTH   |      CASH     |     STOCKS     |     BONDS      |");
            sb.AppendLine("********************************************************************************");
            foreach (NetWorth n in netWorthSchedule)
            {
                string simulationRunDateFormatted = n.dateWithinSim.ToShortDateString().PadRight(11);
                string totalCashOnHandFormatted = n.totalCashOnHand.ToString("C").PadLeft(15);
                string totalNetWorthFormatted = n.totalNetWorth.ToString("C").PadLeft(16);
                string totalStocksFormatted = n.totalStocks.ToString("C").PadLeft(16);
                string totalBondsFormatted = n.totalBonds.ToString("C").PadLeft(16);
                string worthLine = string.Format("|{0}|{1}|{2}|{3}|{4}|", simulationRunDateFormatted,
                    totalNetWorthFormatted, totalCashOnHandFormatted, totalStocksFormatted, totalBondsFormatted);
                sb.AppendLine(worthLine);
            }
            sb.AppendLine("********************************************************************************");
            Logger.info(sb.ToString());
        }
    }
}
