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
        private List<(DateTime period, decimal price, decimal movement)> actualHistoryData;
        private Dictionary<DateTime, (decimal price, decimal movement)> simulatedEquityData;
        private Dictionary<DateTime, (decimal price, decimal movement)> simulatedBondData;
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
            createSimulatedEquityData();
            populateRecessionsList();
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
        private void createSimulatedEquityData()
        {
            simulatedEquityData = new Dictionary<DateTime, (decimal price, decimal movement)>();
            populateActualHistory();

            // create a random offset
            int numHistoricalPoints = actualHistoryData.Count;
            int offset = RNG.getRandomInt(0, numHistoricalPoints);

            DateTime pointer = start;
            decimal priorValue = marketHistoryStartValue;
            
            var roundedRetirement = DateTimeHelper.RoundToMonth(
                ConfigManager.GetDateTime("RetirementDate"), RoundDateDirection.CLOSEST)
                .Date;
            
            while (pointer <= end)
            {
                offset = (actualHistoryData.Count > offset) ? offset : 0;
                decimal movement = actualHistoryData[offset].movement;
                decimal newValue = priorValue + (movement * priorValue);
                priorValue = newValue;
                simulatedEquityData.Add(pointer, (newValue, movement));
                
                // figure out when our retirement date analog is
                if (pointer == roundedRetirement) retirementDateHistoricalAnalog = actualHistoryData[offset].period;
                
                // and move everything forward by 1
                pointer = pointer.AddMonths(1);
                offset++;
            }
        }
        public decimal getMovementAtDateBond(DateTime period)
        {
            return simulatedBondData[period].movement;
        }
        public decimal getPriceAtDateBond(DateTime period)
        {
            return simulatedBondData[period].price;
        }
        public decimal getMovementAtDateEquity(DateTime period)
        {
            return simulatedEquityData[period].movement;
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
        private void populateActualHistory()
        {
            actualHistoryData = DataAccessLayer.ReadSAndPIndexFromDb();
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
