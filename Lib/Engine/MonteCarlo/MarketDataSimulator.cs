using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Lib.Engine.MonteCarlo
{
    public class MarketDataSimulator
    {
        private List<(DateTime period, decimal price, decimal movement)> actualHistoryDataStockIndex;
        private List<(DateTime period, decimal price, decimal movement)> actualHistoryDataCPI;
        private Dictionary<DateTime, (decimal price, decimal movement)> simulatedEquityData;
        private Dictionary<DateTime, (decimal price, decimal movement)> simulatedBondData;
        private Dictionary<DateTime, decimal> simulatedInflationData;
        private DateTime start;
        private DateTime end;
        public List<(DateTime start, DateTime end)> recessions;
        const decimal marketHistoryStartValue = 10000;
        const decimal bondMonthVolatilityLow = -0.01m / 12;
        const decimal bondMonthVolatilityHigh = 0.04m / 12;
        private int numMonthsToEvaluateRecession = 3;
        private decimal recessionPricePercentThreshold = 0.9m; // if the equities price is lower than last year times this value, you're in a recession
        private decimal recessionRecoveryPercent = 1.05m; // if today's price is >= this value * the price at last recessions start, recession is over
        public DateTime retirementDateHistoricalAnalog;

        public MarketDataSimulator(DateTime start, DateTime end)
        {
            this.start = start;
            this.end = end;
            numMonthsToEvaluateRecession = ConfigManager.GetInt("numMonthsToEvaluateRecession");
            recessionRecoveryPercent = ConfigManager.GetDecimal("recessionRecoveryPercent");
            recessionPricePercentThreshold = ConfigManager.GetDecimal("recessionPricePercentThreshold");
        }
        public void createMarketHistory()
        {
            createSimulatedBondData();
            //createSimulatedEquityData();
            //createSimulatedInflationData();
            createSimulatedCombinedData();
            populateRecessionsList();
        }
        public decimal getMovementAtDateBond(DateTime period)
        {
            return simulatedBondData[period].movement;
        }
        public decimal getMovementAtDateEquity(DateTime period)
        {
            return simulatedEquityData[period].movement;
        }
        public decimal getMovementAtDateInflation(DateTime period)
        {
            return simulatedInflationData[period];
        }
        public decimal getPriceAtDateBond(DateTime period)
        {
            return simulatedBondData[period].price;
        }
        public decimal getPriceAtDateEquity(DateTime period)
        {
            return simulatedEquityData[period].price;
        }
        public bool isInRecession(DateTime period)
        {
            if (recessions.Where(x => x.start <= period && x.end >= period).Count() > 0)
            {
                return true;
            }
            return false;
        }
        private void createSimulatedBondData()
        {
            simulatedBondData = new Dictionary<DateTime, (decimal price, decimal movement)>();

            DateTime pointer = start;
            decimal priorValue = marketHistoryStartValue;
            while (pointer <= end)
            {
                decimal movement = RNG.getRandomDecimalWeighted(bondMonthVolatilityLow, bondMonthVolatilityHigh);
                decimal newValue = priorValue + (movement * priorValue);
                priorValue = newValue;
                simulatedBondData.Add(pointer, (newValue, movement));
                pointer = pointer.AddMonths(1);
            }
        }
        private void createSimulatedCombinedData()
        {
            simulatedEquityData = new Dictionary<DateTime, (decimal price, decimal movement)>();
            simulatedInflationData = new Dictionary<DateTime, decimal>();

            // get the liklihood of year-over-year movement
            List<(int countL, decimal sAndPGrowthRate, decimal cpiGrowthRate)> historicalRates = DataAccessLayer
                .ReadAnnualGrowthRateOccuranceCounts();
            
            // now create a rates table that is populated by the rates based on their probability
            int totalRateTableRows = historicalRates.Sum(x => x.countL); // 75
            (decimal sAndPGrowthRate, decimal cpiGrowthRate)[] ratesTable = new
                (decimal sAndPGrowthRate, decimal cpiGrowthRate)[totalRateTableRows];

            int ratesTableIndex = 0;
            foreach (var rateRow in historicalRates)
            {
                for (int i = 0; i < rateRow.countL; i++)
                {
                    ratesTable[ratesTableIndex] = (rateRow.sAndPGrowthRate, rateRow.cpiGrowthRate);
                    ratesTableIndex++;
                }
            }
            // now build imaginary years forward
            var justPrices = new Dictionary<DateTime, (decimal equityPrice, decimal inflationPrice)>();
            // first draw straight lines
            DateTime pointer = start;
            decimal thisYearsSAndPVal = marketHistoryStartValue;
            decimal nextYearsSAndPVal = thisYearsSAndPVal; // placeholder. update in the while loop
            decimal thisYearsCpiVal = marketHistoryStartValue;
            decimal nextYearsCpiVal = thisYearsCpiVal; // placeholder. update in the while loop

            int currentMonth = pointer.Month;
            int monthInYear = 0; // use this to rebaseline this month to 0
            decimal growthInYearSandP = 0;
            decimal growthPerMonthSandP = 0;
            decimal growthInYearCpi = 0;
            decimal growthPerMonthCpi = 0;

            while (pointer <= end)
            {
                if(pointer.Month == currentMonth)
                {
                    monthInYear = 0;
                    // exchange values
                    thisYearsSAndPVal = nextYearsSAndPVal;
                    thisYearsCpiVal= nextYearsCpiVal;
                    // re-target
                    ratesTableIndex = RNG.getRandomInt(0, totalRateTableRows - 1);
                    decimal sAndPGrowthRate = ratesTable[ratesTableIndex].sAndPGrowthRate;
                    decimal cpiGrowthRate = ratesTable[ratesTableIndex].cpiGrowthRate;
                    
                    growthInYearSandP = (thisYearsSAndPVal * sAndPGrowthRate);
                    growthPerMonthSandP = growthInYearSandP / 12M;

                    growthInYearCpi = (thisYearsCpiVal * cpiGrowthRate);
                    growthPerMonthCpi = growthInYearCpi / 12M;

                    nextYearsSAndPVal += growthInYearSandP;
                    nextYearsCpiVal += growthInYearCpi;
                }

                decimal sAndPValAtMonth = thisYearsSAndPVal + (growthPerMonthSandP * monthInYear);
                decimal cpiValAtMonth = thisYearsCpiVal + (growthPerMonthCpi * monthInYear);
                justPrices.Add(pointer, (sAndPValAtMonth, cpiValAtMonth));

                pointer = pointer.AddMonths(1);
                monthInYear++;
            }
            
            // now add random fluctuations
            pointer = start;
            while (pointer <= end)
            {
                decimal randFlucSandP = RNG.getRandomDecimal(-0.03M, 0.03M);
                decimal randFlucCpi = RNG.getRandomDecimal(-0.01M, 0.01M);
                justPrices[pointer] = (
                    justPrices[pointer].equityPrice + (justPrices[pointer].equityPrice * randFlucSandP),
                    justPrices[pointer].inflationPrice + (justPrices[pointer].inflationPrice * randFlucCpi)
                    );

                pointer = pointer.AddMonths(1);
            }
            /*
             * log just prices
             * 
             * 
             * 
             * 
             * 
             * */
            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine(string.Format("{0},{1},{2}", "date", "equityPrice", "inflationPrice"));
            //foreach (var row in justPrices)
            //{
            //    sb.AppendLine(string.Format("{0},{1},{2}", row.Key.ToString("d"), row.Value.equityPrice, row.Value.inflationPrice));
            //}
            //Logger.info(sb.ToString());
            /*
             * 
             * 
             * 
             * 
             * 
             * end log just prices
             * */

            // now build the simulated equity and inflation tables
            simulatedEquityData = new Dictionary<DateTime, (decimal price, decimal movement)>();
            simulatedInflationData = new Dictionary<DateTime, decimal>();
            decimal priorSandPVal = justPrices[start].equityPrice;
            decimal priorCpiVal = justPrices[start].inflationPrice;
            pointer = start;
            while (pointer <= end)
            {
                var thisRow = justPrices[pointer];
                decimal growthSandP = (thisRow.equityPrice - priorSandPVal) / priorSandPVal;
                decimal growthCpi = (thisRow.inflationPrice - priorCpiVal) / priorCpiVal;

                simulatedEquityData.Add(pointer,(thisRow.equityPrice, growthSandP));
                simulatedInflationData.Add(pointer, growthCpi);

                priorSandPVal = thisRow.equityPrice;
                priorCpiVal = thisRow.inflationPrice;

                pointer = pointer.AddMonths(1);
            }
        }
        private void createSimulatedEquityData()
        {
            simulatedEquityData = new Dictionary<DateTime, (decimal price, decimal movement)>();
            populateActualHistory();

            // create a random offset
            int numHistoricalPoints = actualHistoryDataStockIndex.Count;
            int offset = RNG.getRandomInt(0, numHistoricalPoints);

            DateTime pointer = start;
            decimal priorValue = marketHistoryStartValue;

            var roundedRetirement = DateTimeHelper.RoundToMonth(
                ConfigManager.GetDateTime("RetirementDate"), RoundDateDirection.CLOSEST)
                .Date;

            while (pointer <= end)
            {
                offset = (actualHistoryDataStockIndex.Count > offset) ? offset : 0;
                decimal movement = actualHistoryDataStockIndex[offset].movement;
                decimal newValue = priorValue + (movement * priorValue);
                priorValue = newValue;
                simulatedEquityData.Add(pointer, (newValue, movement));

                // figure out when our retirement date analog is
                if (pointer == roundedRetirement) retirementDateHistoricalAnalog = actualHistoryDataStockIndex[offset].period;

                // and move everything forward by 1
                pointer = pointer.AddMonths(1);
                offset++;
            }
        }
        private void createSimulatedInflationData()
        {
            simulatedInflationData = new Dictionary<DateTime, decimal>();


            // create a random offset
            // this is a different offset from stock index to truly get variation
            // this may not reflect reality as often the stock market
            // and inflation affect each other, but it gives us more 
            // more variability in our sims
            int numHistoricalPoints = actualHistoryDataCPI.Count;
            int offset = RNG.getRandomInt(0, numHistoricalPoints);

            DateTime pointer = start;
            //decimal priorValue = marketHistoryStartValue;

            var roundedRetirement = DateTimeHelper.RoundToMonth(
                ConfigManager.GetDateTime("RetirementDate"), RoundDateDirection.CLOSEST)
                .Date;

            while (pointer <= end)
            {
                offset = (actualHistoryDataStockIndex.Count > offset) ? offset : 0;
                decimal movement = actualHistoryDataStockIndex[offset].movement;
                simulatedInflationData.Add(pointer, movement);
                                
                // and move everything forward by 1
                pointer = pointer.AddMonths(1);
                offset++;
            }
        }
        private void populateActualHistory()
        {
            actualHistoryDataStockIndex = DataAccessLayer.ReadSAndPIndexFromDb();
            actualHistoryDataCPI = DataAccessLayer.ReadConsumerPriceIndexFromDb();
        }
        private void populateRecessionsList()
        {
            // definition of recession https://www.investopedia.com/terms/r/recession.asp
            // hint: it varies
            // definition used here average stock price of the prior
            // 3 months is higher than average stock price of next
            // 3 months and a year-over-year decline of 10% or more 
            recessions = new List<(DateTime start, DateTime end)>();

            DateTime currentOpenRecessionStart = DateTime.MinValue;
            

            decimal priceAtStartOfLastRecession = 0m;
            DateTime pointer = start.AddMonths(numMonthsToEvaluateRecession);
            DateTime endLoop = end.AddMonths(numMonthsToEvaluateRecession * -1);
            bool isAlreadyInRecession = false;
            while (pointer <= endLoop)
            {
                decimal priceAtPointer = getPriceAtDateEquity(pointer);
                DateTime checkPeriodEndDate = pointer.AddMonths(numMonthsToEvaluateRecession);
                if (isAlreadyInRecession)
                {
                    // already in one, see if we've gotten out of it
                    if (priceAtPointer >= priceAtStartOfLastRecession * recessionRecoveryPercent)
                    {
                        isAlreadyInRecession = false;
                        recessions.Add((currentOpenRecessionStart, checkPeriodEndDate));
                    }

                }
                else
                {
                    // market average prior three months 
                    decimal marketAvgPriorTime = simulatedEquityData
                        .Where(x => x.Key <= pointer && x.Key > pointer.AddMonths(numMonthsToEvaluateRecession * -1))
                        .Average(y => y.Value.price);
                    decimal marketAvgAfterTime = 0;

                    try
                    {
                        // market average next three months
                        marketAvgAfterTime = simulatedEquityData
                                        .Where(x => x.Key > pointer && x.Key <= checkPeriodEndDate)
                                        .Average(y => y.Value.price);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    if (marketAvgAfterTime < marketAvgPriorTime)
                    {
                        // we're in decline. Is it enough?
                        // check if our market is is 10% lower than it was a year ago
                        DateTime yearAgo = pointer.AddYears(-1);
                        decimal priceLastYear = 0;
                        decimal priceAtEndOfCheckPeriod = 0;
                        if (simulatedEquityData.ContainsKey(yearAgo))
                        {
                            priceLastYear = getPriceAtDateEquity(yearAgo);
                        }
                        else
                        {
                            priceLastYear = getPriceAtDateEquity(start);
                        }
                        if (simulatedEquityData.ContainsKey(checkPeriodEndDate))
                        {
                            priceAtEndOfCheckPeriod = getPriceAtDateEquity(checkPeriodEndDate);
                        }
                        else
                        {
                            priceAtEndOfCheckPeriod = getPriceAtDateEquity(end);
                        }
                        if (priceAtEndOfCheckPeriod <= priceLastYear * recessionPricePercentThreshold)
                        {
                            // we're in recession
                            isAlreadyInRecession = true;
                            currentOpenRecessionStart = pointer;
                            priceAtStartOfLastRecession = (priceAtPointer >= priceLastYear) ? priceAtPointer : priceLastYear;
                        }
                    }
                }
                pointer = pointer.AddMonths(numMonthsToEvaluateRecession);
            }

            if(isAlreadyInRecession) recessions.Add((currentOpenRecessionStart, endLoop));
        }
    }
}
