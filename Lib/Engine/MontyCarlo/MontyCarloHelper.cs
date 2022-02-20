using GraphLib;
using GraphLib.Utilities;
using Lib.DataTypes;
using Lib.DataTypes.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Lib.Engine.MontyCarlo
{
    public static class MontyCarloHelper
    {
        public static GraphData GetMontyCarloGraphData(MontyCarloBatch montyCarloBatch)
        {
            GraphData graphData = new GraphData(
                TypeHelper.int32Type,
                TypeHelper.int64Type);


            for (int i = 0; i < montyCarloBatch.simRuns.Count; i++)
            {
                SimulationRunResult r = montyCarloBatch.simRuns[i];
                GraphSeries graphSeries = new GraphSeries();
                graphSeries.yType = TypeHelper.int64Type;
                graphSeries.xType = TypeHelper.int32Type;
                graphSeries.data = new List<(object x, object y)>();

                graphSeries.name = String.Format("Simulation run {0}", i);
                graphSeries.seriesPrefs.strokeHexColor = ColorHelper.steelgrey;
                graphSeries.seriesPrefs.strokeOpacity = 0.1;
                graphSeries.seriesPrefs.strokeWidthInPx = 0.1d;

                foreach (NetWorth n in r.netWorthSchedule)
                {
                    if (n.dateWithinSim <= r.deathdate)
                    {
                        int age = (int)Math.Round(
                            (n.dateWithinSim - montyCarloBatch.simParams.birthDate).TotalDays / 365.25, 0);
                        long dollarAmount = Convert.ToInt64(Math.Round(
                            n.totalNetWorth / 10000f
                            , 2));
                        graphSeries.data.Add((age, dollarAmount));
                    }
                }
                graphData.AddSeries(graphSeries);
            }
            graphData.AddSeries(GetMedianFromSimResults(montyCarloBatch.simRuns, 
                montyCarloBatch.simParams.birthDate));

            var nintiethPercentiles = Get90PercentFromSimResults(montyCarloBatch.simRuns,
                montyCarloBatch.simParams.birthDate);
            graphData.AddSeries(nintiethPercentiles.Item1);
            graphData.AddSeries(nintiethPercentiles.Item2);

            return graphData;
        }
        public static MontyCarloBatch RunMontyCarlo(List<Account> accounts, PricingEngine pricingEngine)
        {
            SimulationParameters simParams = new SimulationParameters()
            {
                startDate = DateTime.Now.Date,
                retirementDate = ConfigManager.GetDateTime("RetirementDate"),
                birthDate = ConfigManager.GetDateTime("BirthDate"),
                monthlyGrossIncomePreRetirement = ConfigManager.GetDecimal("AnnualIncome") / 12.0m,
                monthlyNetSocialSecurityIncome = ConfigManager.GetDecimal("monthlyNetSocialSecurityIncome"),
                monthlySpendLifeStyleToday = ConfigManager.GetDecimal("monthlySpendLifeStyleToday"),
                monthlyInvestRoth401k = ConfigManager.GetDecimal("monthlyInvestRoth401k"),
                monthlyInvestTraditional401k = ConfigManager.GetDecimal("monthlyInvestTraditional401k"),
                monthlyInvestBrokerage = ConfigManager.GetDecimal("monthlyInvestBrokerage"),
                monthlyInvestHSA = ConfigManager.GetDecimal("monthlyInvestHSA"),
                annualRSUInvestment = ConfigManager.GetDecimal("annualRSUInvestment"),
                minBondPercentPreRetirement = ConfigManager.GetDecimal("minBondPercentPreRetirement"),
                maxBondPercentPreRetirement = ConfigManager.GetDecimal("maxBondPercentPreRetirement"),
                xMinusAgeStockPercentPreRetirement = ConfigManager.GetDecimal("xMinusAgeStockPercentPreRetirement"),
                numYearsCashBucketInRetirement = ConfigManager.GetDecimal("numYearsCashBucketInRetirement"),
                numYearsBondBucketInRetirement = ConfigManager.GetDecimal("numYearsBondBucketInRetirement"),
                recessionRecoveryPercent = ConfigManager.GetDecimal("recessionRecoveryPercent"),
                shouldMoveEquitySurplussToFillBondGapAlways = ConfigManager.GetBool("shouldMoveEquitySurplussToFillBondGapAlways"),
                deathAgeOverride = ConfigManager.GetInt("deathAgeOverride"),
                recessionLifestyleAdjustment = ConfigManager.GetDecimal("recessionLifestyleAdjustment"),
                maxSpendingPercentWhenBelowRetirementLevelEquity = ConfigManager.GetDecimal("maxSpendingPercentWhenBelowRetirementLevelEquity"),
            };
            List<Asset> assetsGoingIn = new List<Asset>();

            DateTimeOffset today = DateTimeHelper.CreateDateFromParts(DateTime.Now.Year,
                DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            foreach (var account in accounts)
            {

                // group transactions by investment vehicle
                var transactionsByVehicle = account.Transactions
                    .GroupBy(x => x.InvestmentVehicle)
                    .Select(group => new { vehicle = group.Key, transactions = group.ToList() });
                foreach (var tGroup in transactionsByVehicle)
                {
                    Asset a = new Asset();
                    a.created = tGroup.transactions.Min(x => x.Date).Date;
                    var allPurchases = tGroup.transactions
                    .Where(x => x.TransactionType == TransactionType.PURCHASE);
                    var allSales = tGroup.transactions
                        .Where(x => x.TransactionType == TransactionType.SALE);
                    decimal numShares = allPurchases.Sum(y => y.Quantity);
                    numShares -= allSales.Sum(y => y.Quantity);
                    decimal pricePerShare = pricingEngine.GetPriceAtDate(tGroup.vehicle, today).Price;
                    a.amountCurrent = (long)(Math.Round(pricePerShare * numShares, 0));
                    // RMD dates
                    DateTime birthDayAt72 = ConfigManager.GetDateTime("BirthDate").AddYears(72);
                    a.rmdDate = null;
                    if (account.AccountType == AccountType.ROTH_IRA) a.rmdDate = null;
                    if (account.AccountType == AccountType.TRADITIONAL_IRA) a.rmdDate = birthDayAt72;
                    if (account.AccountType == AccountType.ROTH_401_K) a.rmdDate = birthDayAt72;
                    if (account.AccountType == AccountType.TRADITIONAL_401_K) a.rmdDate = birthDayAt72;

                    a.amountContributed = (long)(Math.Round(
                        allPurchases.Sum(x => x.CashPriceTotalTransaction) -
                        allSales.Sum(x => x.CashPriceTotalTransaction), 0)
                        );

                    // tax buckets are:
                    //     taxable (TAXABLE_BROKERAGE, OTHER)
                    //     tax deferred (TRADITIONAL_401_K, TRADITIONAL_IRA)
                    //     non-taxable (ROTH_401_K, , ROTH_IRA, HSA)
                    a.taxBucket = TaxBucket.TAXABLE;
                    if (account.AccountType == AccountType.TAXABLE_BROKERAGE
                        || account.AccountType == AccountType.OTHER)
                    {
                        a.taxBucket = TaxBucket.TAXABLE;
                    }
                    if (account.AccountType == AccountType.TRADITIONAL_401_K
                        || account.AccountType == AccountType.TRADITIONAL_IRA)
                    {
                        a.taxBucket = TaxBucket.TAXDEFERRED;
                    }
                    if (account.AccountType == AccountType.ROTH_401_K
                        || account.AccountType == AccountType.ROTH_IRA
                        || account.AccountType == AccountType.HSA)
                    {
                        a.taxBucket = TaxBucket.TAXFREE;
                    }

                    if (tGroup.vehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED)
                    {
                        a.investmentIndex = InvestmentIndex.EQUITY;
                    }
                    if (tGroup.vehicle.Type == InvestmentVehicleType.PRIVATELY_HELD)
                    {
                        a.investmentIndex = InvestmentIndex.NONE;
                    }
                    assetsGoingIn.Add(a);
                }
            }
            SimulationFeatureToggles featureToggles = new SimulationFeatureToggles()
            {
                shouldLog = false,
                shouldWriteResultsToDB = false,
                shouldRunInParallel = true,
            };
            int numberOfSimsToRun = ConfigManager.GetInt("numberOfSimsToRun");
            MontyCarloBatch mc = new MontyCarloBatch(simParams, assetsGoingIn,
                featureToggles, numberOfSimsToRun);
            mc.runBatch();
            return mc;
        }

        private static GraphSeries GetMedianFromSimResults(List<SimulationRunResult> simResults, DateTime birthDate)
        {
            GraphSeries graphSeries = new GraphSeries();
            graphSeries.yType = TypeHelper.int64Type;
            graphSeries.xType = TypeHelper.int32Type;
            graphSeries.data = new List<(object x, object y)>();

            graphSeries.name = "Median";
            graphSeries.seriesPrefs.strokeHexColor = ColorHelper.amber;
            graphSeries.seriesPrefs.strokeWidthInPx = 3d;



            // combine all net worth schedules into one giant list to make it easier to use linq 
            // to query for median values
            List<NetWorth> combinedList = new List<NetWorth>();
            foreach (SimulationRunResult r in simResults)
            {
                combinedList.AddRange(r.netWorthSchedule);
            }
            DateTime minDate = combinedList.Min(x => x.dateWithinSim);
            DateTime maxDate = combinedList.Max(x => x.dateWithinSim);

            var netWorthSchedulesByDate = combinedList.GroupBy(x => x.dateWithinSim)
                .ToDictionary(y => y.Key, y => y.ToList());
            List<DateTime> uniqueDates = netWorthSchedulesByDate.Keys.OrderBy(x => x).ToList();
            foreach (DateTime d in uniqueDates)
            {
                // only want January 1 dates
                if (d.Month == 1 && d.Day == 1)
                {
                    var allWorthsAtDate = combinedList
                        .Where(x => x.dateWithinSim == d)
                        .Select(z => z.totalNetWorth)
                        .OrderByDescending(y => y);
                    int totalWorths = allWorthsAtDate.Count();
                    int halfIndex = totalWorths / 2;
                    long median;
                    if ((totalWorths % 2) == 0)
                    {
                        median = (allWorthsAtDate.ElementAt(halfIndex) +
                            allWorthsAtDate.ElementAt(halfIndex - 1))
                            / 2;
                    }
                    else
                    {
                        median = allWorthsAtDate.ElementAt(halfIndex);
                    }

                    long totalNetWorth = Convert.ToInt64(Math.Round(median / 10000f, 2));
                    
                    int age = (int)Math.Round(
                        (d - birthDate).TotalDays / 365.25, 0);

                    graphSeries.data.Add((age, totalNetWorth));
                }

            }
            return graphSeries;
        }
        private static (GraphSeries, GraphSeries) Get90PercentFromSimResults(List<SimulationRunResult> simResults, DateTime birthDate)
        {
            GraphSeries graphSeriesMin = new GraphSeries();
            graphSeriesMin.yType = TypeHelper.int64Type;
            graphSeriesMin.xType = TypeHelper.int32Type;
            graphSeriesMin.data = new List<(object x, object y)>();
            graphSeriesMin.name = "Min";
            graphSeriesMin.seriesPrefs.strokeHexColor = ColorHelper.deeporange;
            graphSeriesMin.seriesPrefs.strokeWidthInPx = 3d;
            
            GraphSeries graphSeriesMax = new GraphSeries();
            graphSeriesMax.yType = TypeHelper.int64Type;
            graphSeriesMax.xType = TypeHelper.int32Type;
            graphSeriesMax.data = new List<(object x, object y)>();
            graphSeriesMax.name = "Max";
            graphSeriesMax.seriesPrefs.strokeHexColor = ColorHelper.indigo;
            graphSeriesMax.seriesPrefs.strokeWidthInPx = 3d;



            // combine all net worth schedules into one giant list to make it easier to use linq 
            // to query for median values
            List<NetWorth> combinedList = new List<NetWorth>();
            foreach (SimulationRunResult r in simResults)
            {
                combinedList.AddRange(r.netWorthSchedule);
            }
            DateTime minDate = combinedList.Min(x => x.dateWithinSim);
            DateTime maxDate = combinedList.Max(x => x.dateWithinSim);

            var netWorthSchedulesByDate = combinedList.GroupBy(x => x.dateWithinSim)
                .ToDictionary(y => y.Key, y => y.ToList());
            List<DateTime> uniqueDates = netWorthSchedulesByDate.Keys.OrderBy(x => x).ToList();
            foreach (DateTime d in uniqueDates)
            {
                // only want January 1 dates
                if (d.Month == 1 && d.Day == 1)
                {
                    var allWorthsAtDate = combinedList
                        .Where(x => x.dateWithinSim == d)
                        .Select(z => z.totalNetWorth)
                        .OrderByDescending(y => y);
                    int totalWorths = allWorthsAtDate.Count();


                    int top10PercentIndex = (int)(Math.Round(totalWorths * 0.1, 2));
                    int bottom10PercentIndex = (int)(Math.Round(totalWorths * 0.9, 2));
                    long top10Value = allWorthsAtDate.ElementAt(top10PercentIndex);
                    long bottom10Value = allWorthsAtDate.ElementAt(bottom10PercentIndex);



                    long totalNetWorthTop = Convert.ToInt64(Math.Round(top10Value / 10000f, 2));
                    long totalNetWorthBottom = Convert.ToInt64(Math.Round(bottom10Value / 10000f, 2));

                    int age = (int)Math.Round(
                        (d - birthDate).TotalDays / 365.25, 0);

                    graphSeriesMin.data.Add((age, totalNetWorthBottom));
                    graphSeriesMax.data.Add((age, totalNetWorthTop));
                }

            }
            return (graphSeriesMin, graphSeriesMax);
        }
    }
}
