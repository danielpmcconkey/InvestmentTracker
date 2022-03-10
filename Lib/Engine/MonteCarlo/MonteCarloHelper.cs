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

namespace Lib.Engine.MonteCarlo
{
    public static class MonteCarloHelper
    {
        #region public methods
        public static void EvolveBestRuns(string monteCarloVersion, int numBatchesToRun, List<Account> accounts)
        {
            var assetsGoingIn = CreateSimAssetsFromAccounts(accounts);

            var batches = DataAccessLayer.GetRunsToEvolve(numBatchesToRun, 1100, monteCarloVersion);
            foreach (var batch in batches)
            {
                Guid oldId = batch.runId;
                MonteCarloBatch evolvedBatch = EvolveBatch(batch);
                evolvedBatch.assetsGoingIn = assetsGoingIn;
                evolvedBatch.numberOfSimsToRun = 1000;
                evolvedBatch.runBatch();
                Logger.info(String.Format("Evolved {0} into {1}", oldId.ToString(),
                    batch.runId.ToString()));
            }

        }
        public static void ExtendBestRuns(string monteCarloVersion, List<Account> accounts)
        {
            var assetsGoingIn = CreateSimAssetsFromAccounts(accounts);
            var batches = DataAccessLayer.GetRunsToExtend(10, 1100, monteCarloVersion);
            foreach (var batch in batches)
            {
                batch.runId = Guid.NewGuid();   // create a new GUID for this run
                batch.numberOfSimsToRun = 20000;
                batch.assetsGoingIn = assetsGoingIn;
                batch.runBatch();
            }
        }
        public static List<Decimal> GetGradientBetweenMinAndMax(decimal minValue, decimal maxValue, int numberOfGrades)
        {
            decimal minRound = Math.Round(minValue, 4);
            decimal maxRound = Math.Round(maxValue, 4);
            decimal incrementRound = Math.Round(((maxValue - minValue) / (numberOfGrades - 1)), 4);
            List<decimal> gradient = new List<decimal>();
            for (decimal i = minRound; i <= maxRound; i += incrementRound)
            {
                gradient.Add(i);
            }
            return gradient;
        }
        public static MonteCarloBatch GetMonteCarloBatchFromDb(Guid runId)
        {
            return DataAccessLayer.readAndDeserializeMCBatchFromDb(runId);
        }
        public static GraphData GetMonteCarloGraphData(MonteCarloBatch monteCarloBatch)
        {
            GraphData graphData = new GraphData(
                TypeHelper.int32Type,
                TypeHelper.int64Type);


            for (int i = 0; i < monteCarloBatch.simRuns.Count; i++)
            {
                SimulationRunResult r = monteCarloBatch.simRuns[i];
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
                            (n.dateWithinSim - monteCarloBatch.simParams.birthDate).TotalDays / 365.25, 0);
                        long dollarAmount = Convert.ToInt64(Math.Round(
                            n.totalNetWorth / 10000f
                            , 2));
                        graphSeries.data.Add((age, dollarAmount));
                    }
                }
                graphData.AddSeries(graphSeries);
            }
            graphData.AddSeries(GetMedianNetWorthGraphFromSimResults(monteCarloBatch.simRuns,
                monteCarloBatch.simParams.birthDate));

            var nintiethPercentiles = Get90PercentFromSimResults(monteCarloBatch.simRuns,
                monteCarloBatch.simParams.birthDate);
            graphData.AddSeries(nintiethPercentiles.Item1);
            graphData.AddSeries(nintiethPercentiles.Item2);

            // add a vertical "retirement" bar
            GraphSeries retirementBar = new GraphSeries("Retirement bar", TypeHelper.int32Type, TypeHelper.int64Type);
            retirementBar.seriesPrefs.strokeHexColor = ColorHelper.red;
            retirementBar.seriesPrefs.strokeWidthInPx = 6;
            retirementBar.data.Add((65, (long)0));
            retirementBar.data.Add((65, (long)15000000));
            graphData.AddSeries(retirementBar);

            return graphData;
        }
        public static MonteCarloBatch RunMonteCarlo(List<Account> accounts)
        {
            SimulationParameters simParams = new SimulationParameters()
            {
                startDate = DateTime.Now.Date,
                retirementDate = ConfigManager.GetDateTime("RetirementDate"),
                birthDate = ConfigManager.GetDateTime("BirthDate"),
                monthlyGrossIncomePreRetirement = ConfigManager.GetDecimal("AnnualIncome") / 12.0m,
                monthlyNetSocialSecurityIncome = ConfigManager.GetDecimal("monthlyNetSocialSecurityIncome"),
                monthlySpendLifeStyleToday = ConfigManager.GetDecimal("monthlySpendLifeStyleToday"),
                monthlySpendCoreToday = ConfigManager.GetDecimal("monthlySpendCoreToday"),
                monthlyInvestRoth401k = ConfigManager.GetDecimal("monthlyInvestRoth401k"),
                monthlyInvestTraditional401k = ConfigManager.GetDecimal("monthlyInvestTraditional401k"),
                monthlyInvestBrokerage = ConfigManager.GetDecimal("monthlyInvestBrokerage"),
                monthlyInvestHSA = ConfigManager.GetDecimal("monthlyInvestHSA"),
                annualRSUInvestmentPreTax = ConfigManager.GetDecimal("annualRSUInvestmentPreTax"),
                xMinusAgeStockPercentPreRetirement = ConfigManager.GetDecimal("xMinusAgeStockPercentPreRetirement"),
                numYearsCashBucketInRetirement = ConfigManager.GetDecimal("numYearsCashBucketInRetirement"),
                numYearsBondBucketInRetirement = ConfigManager.GetDecimal("numYearsBondBucketInRetirement"),
                recessionRecoveryPercent = ConfigManager.GetDecimal("recessionRecoveryPercent"),
                shouldMoveEquitySurplussToFillBondGapAlways = ConfigManager.GetBool("shouldMoveEquitySurplussToFillBondGapAlways"),
                deathAgeOverride = ConfigManager.GetInt("deathAgeOverride"),
                recessionLifestyleAdjustment = ConfigManager.GetDecimal("recessionLifestyleAdjustment"),
                retirementLifestyleAdjustment = ConfigManager.GetDecimal("retirementLifestyleAdjustment"),
                maxSpendingPercentWhenBelowRetirementLevelEquity = ConfigManager.GetDecimal("maxSpendingPercentWhenBelowRetirementLevelEquity"),
                annualInflationLow = ConfigManager.GetDecimal("annualInflationLow"),
                annualInflationHi = ConfigManager.GetDecimal("annualInflationHi"),
                socialSecurityCollectionAge = ConfigManager.GetDecimal("socialSecurityCollectionAge"),
                livingLargeThreashold = ConfigManager.GetDecimal("livingLargeThreashold"),
                livingLargeLifestyleSpendMultiplier = ConfigManager.GetDecimal("livingLargeLifestyleSpendMultiplier"),
            };

            var assetsGoingIn = CreateSimAssetsFromAccounts(accounts);
            int numberOfSimsToRun = ConfigManager.GetInt("numberOfSimsToRun");
            MonteCarloBatch mc = new MonteCarloBatch(simParams, assetsGoingIn, numberOfSimsToRun);
            mc.runBatch();
            return mc;
        }
        public static void RunMonteCarloBatches(int numBatches, List<Account> accounts)
        {

            for (int i = 0; i < numBatches; i++)
            {
            // pull from default configs
            var retirementDate = ConfigManager.GetDateTime("RetirementDate");
            var birthDate = ConfigManager.GetDateTime("BirthDate");
            var monthlyGrossIncomePreRetirement = ConfigManager.GetDecimal("AnnualIncome") / 12.0m;
            var monthlyNetSocialSecurityIncome = ConfigManager.GetDecimal("monthlyNetSocialSecurityIncome");
            var monthlySpendLifeStyleToday = ConfigManager.GetDecimal("monthlySpendLifeStyleToday");
            var monthlySpendCoreToday = ConfigManager.GetDecimal("monthlySpendCoreToday");
            var monthlyInvestRoth401k = ConfigManager.GetDecimal("monthlyInvestRoth401k");
            var monthlyInvestTraditional401k = ConfigManager.GetDecimal("monthlyInvestTraditional401k");
            var monthlyInvestBrokerage = ConfigManager.GetDecimal("monthlyInvestBrokerage");
            var monthlyInvestHSA = ConfigManager.GetDecimal("monthlyInvestHSA");
            var annualRSUInvestmentPreTax = ConfigManager.GetDecimal("annualRSUInvestmentPreTax");
            var xMinusAgeStockPercentPreRetirement = ConfigManager.GetDecimal("xMinusAgeStockPercentPreRetirement");
            var numYearsCashBucketInRetirement = ConfigManager.GetDecimal("numYearsCashBucketInRetirement");
            var numYearsBondBucketInRetirement = ConfigManager.GetDecimal("numYearsBondBucketInRetirement");
            var recessionRecoveryPercent = ConfigManager.GetDecimal("recessionRecoveryPercent");
            var shouldMoveEquitySurplussToFillBondGapAlways = ConfigManager.GetBool("shouldMoveEquitySurplussToFillBondGapAlways");
            var deathAgeOverride = ConfigManager.GetInt("deathAgeOverride");
            var recessionLifestyleAdjustment = ConfigManager.GetDecimal("recessionLifestyleAdjustment");
            var retirementLifestyleAdjustment = ConfigManager.GetDecimal("retirementLifestyleAdjustment");
            var maxSpendingPercentWhenBelowRetirementLevelEquity = ConfigManager.GetDecimal("maxSpendingPercentWhenBelowRetirementLevelEquity");
            var annualInflationLow = ConfigManager.GetDecimal("annualInflationLow");
            var annualInflationHi = ConfigManager.GetDecimal("annualInflationHi");
            var socialSecurityCollectionAge = ConfigManager.GetDecimal("socialSecurityCollectionAge");
            var livingLargeThreashold = ConfigManager.GetDecimal("livingLargeThreashold");
            var livingLargeLifestyleSpendMultiplier = ConfigManager.GetDecimal("livingLargeLifestyleSpendMultiplier");

                // now randomize some
                monthlySpendLifeStyleToday = RNG.getRandomDecimal(
                    monthlySpendLifeStyleToday * 0.5M, monthlySpendLifeStyleToday * 2M);
                xMinusAgeStockPercentPreRetirement = RNG.getRandomDecimal(
                    xMinusAgeStockPercentPreRetirement * 0.5M, xMinusAgeStockPercentPreRetirement * 2M);
                numYearsCashBucketInRetirement = RNG.getRandomDecimal(
                    numYearsCashBucketInRetirement * 0.25M, numYearsCashBucketInRetirement * 4M);
                numYearsBondBucketInRetirement = RNG.getRandomDecimal(
                    numYearsBondBucketInRetirement * 0.25M, numYearsBondBucketInRetirement * 4M);
                recessionRecoveryPercent = RNG.getRandomDecimal(0.8M, 1.25M);
                shouldMoveEquitySurplussToFillBondGapAlways = RNG.getRandomBool();
                recessionLifestyleAdjustment = RNG.getRandomDecimal(0.0M, 1.0M);
                retirementLifestyleAdjustment = RNG.getRandomDecimal(0.0M, 1.0M);
                maxSpendingPercentWhenBelowRetirementLevelEquity = RNG.getRandomDecimal(0.0M, 1.0M);
                livingLargeThreashold = RNG.getRandomDecimal(1.2M, 5.0M);
                livingLargeLifestyleSpendMultiplier = RNG.getRandomDecimal(1.2M, 5.0M);

                SimulationParameters simParams = new SimulationParameters()
                {
                    startDate = DateTime.Now.Date,
                    retirementDate = retirementDate,
                    birthDate = birthDate,
                    monthlyGrossIncomePreRetirement = monthlyGrossIncomePreRetirement,
                    monthlyNetSocialSecurityIncome = monthlyNetSocialSecurityIncome,
                    monthlySpendLifeStyleToday = monthlySpendLifeStyleToday,
                    monthlySpendCoreToday = monthlySpendCoreToday,
                    monthlyInvestRoth401k = monthlyInvestRoth401k,
                    monthlyInvestTraditional401k = monthlyInvestTraditional401k,
                    monthlyInvestBrokerage = monthlyInvestBrokerage,
                    monthlyInvestHSA = monthlyInvestHSA,
                    annualRSUInvestmentPreTax = annualRSUInvestmentPreTax,
                    xMinusAgeStockPercentPreRetirement = xMinusAgeStockPercentPreRetirement,
                    numYearsCashBucketInRetirement = numYearsCashBucketInRetirement,
                    numYearsBondBucketInRetirement = numYearsBondBucketInRetirement,
                    recessionRecoveryPercent = recessionRecoveryPercent,
                    shouldMoveEquitySurplussToFillBondGapAlways = shouldMoveEquitySurplussToFillBondGapAlways,
                    deathAgeOverride = deathAgeOverride,
                    recessionLifestyleAdjustment = recessionLifestyleAdjustment,
                    retirementLifestyleAdjustment = retirementLifestyleAdjustment,
                    maxSpendingPercentWhenBelowRetirementLevelEquity = maxSpendingPercentWhenBelowRetirementLevelEquity,
                    annualInflationLow = annualInflationLow,
                    annualInflationHi = annualInflationHi,
                    socialSecurityCollectionAge = socialSecurityCollectionAge,
                    livingLargeThreashold = livingLargeThreashold,
                    livingLargeLifestyleSpendMultiplier = livingLargeLifestyleSpendMultiplier,
                };
                List<Asset> assetsGoingIn = CreateSimAssetsFromAccounts(accounts);

                int numberOfSimsToRun = ConfigManager.GetInt("numberOfSimsToRun");
                MonteCarloBatch mc = new MonteCarloBatch(simParams, assetsGoingIn, numberOfSimsToRun);
                mc.runBatch();
            }
        }
        public static void UpdateAnalytics()
        {
            List<Guid> guids = DataAccessLayer.GetAllRunIdsForMCVersion(MonteCarloBatch.monteCarloVersion);
            for (int i = 0; i < guids.Count; i++)
            {
                var batch = MonteCarloHelper.GetMonteCarloBatchFromDb(guids[i]);
                batch.populateAnalyticsFromRunResults();
                DataAccessLayer.updateMonteCarloBatchInDb(batch);
                i++;
                if (i % 100 == 0) Logger.info(String.Format("Updated {0} analytics rows", i));
            }

        }
        #endregion



        #region private methods
        private static List<Asset> CreateSimAssetsFromAccounts(List<Account> accounts)
        {
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
                    decimal pricePerShare = PricingEngine.GetPriceAtDate(tGroup.vehicle, today).Price;
                    a.amountCurrent = (long)(Math.Round(pricePerShare * numShares * 10000, 0));
                    // RMD dates
                    DateTime birthDayAt72 = ConfigManager.GetDateTime("BirthDate").AddYears(72);
                    a.rmdDate = null;
                    if (account.AccountType == AccountType.ROTH_IRA) a.rmdDate = null;
                    if (account.AccountType == AccountType.TRADITIONAL_IRA) a.rmdDate = birthDayAt72;
                    if (account.AccountType == AccountType.ROTH_401_K) a.rmdDate = birthDayAt72;
                    if (account.AccountType == AccountType.TRADITIONAL_401_K) a.rmdDate = birthDayAt72;

                    a.amountContributed = (long)(Math.Round(
                        (allPurchases.Sum(x => x.CashPriceTotalTransaction) -
                        allSales.Sum(x => x.CashPriceTotalTransaction)) * 10000, 0)
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
                        assetsGoingIn.Add(a);

                    }
                }
            }
            return assetsGoingIn;
        }
        private static MonteCarloBatch EvolveBatch(MonteCarloBatch b)
        {
            Guid oldId = b.runId;
            b.runId = Guid.NewGuid();   // create a new GUID for this run

            // set default sim params from config

            var monthlySpendLifeStyleToday = ConfigManager.GetDecimal("monthlySpendLifeStyleToday");
            var xMinusAgeStockPercentPreRetirement = ConfigManager.GetDecimal("xMinusAgeStockPercentPreRetirement");

            bool hasChanged = false;
            bool hasChangedIndividual = false;

            // evolve each param (potentially)
            b.simParams.monthlySpendLifeStyleToday = EvolveDecimalParameter(
                b.simParams.monthlySpendLifeStyleToday,
                monthlySpendLifeStyleToday * 0.5M,
                monthlySpendLifeStyleToday * 2.5M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;
            
            b.simParams.xMinusAgeStockPercentPreRetirement = EvolveDecimalParameter(
                b.simParams.xMinusAgeStockPercentPreRetirement,
                xMinusAgeStockPercentPreRetirement * 0.5M,
                xMinusAgeStockPercentPreRetirement * 2.5M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.numYearsCashBucketInRetirement = EvolveDecimalParameter(
                b.simParams.numYearsCashBucketInRetirement,
                0,
                10,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.numYearsBondBucketInRetirement = EvolveDecimalParameter(
                b.simParams.numYearsBondBucketInRetirement,
                0,
                10,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.recessionRecoveryPercent = EvolveDecimalParameter(
                b.simParams.recessionRecoveryPercent,
                0.75M,
                2.5M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.shouldMoveEquitySurplussToFillBondGapAlways = (RNG.getRandomDecimal(0, 100) <= 15) ?
                !b.simParams.shouldMoveEquitySurplussToFillBondGapAlways :
                b.simParams.shouldMoveEquitySurplussToFillBondGapAlways;
            b.simParams.recessionLifestyleAdjustment = EvolveDecimalParameter(
                b.simParams.recessionLifestyleAdjustment,
                0.0M,
                2.5M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.retirementLifestyleAdjustment = EvolveDecimalParameter(
                b.simParams.retirementLifestyleAdjustment,
                0.0M,
                2.5M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.maxSpendingPercentWhenBelowRetirementLevelEquity = EvolveDecimalParameter(
                b.simParams.maxSpendingPercentWhenBelowRetirementLevelEquity,
                0.0M,
                1.0M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            b.simParams.livingLargeThreashold = EvolveDecimalParameter(
                b.simParams.livingLargeThreashold,
                1.25M,
                5.0M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;
            b.simParams.livingLargeLifestyleSpendMultiplier = EvolveDecimalParameter(
                b.simParams.livingLargeLifestyleSpendMultiplier,
                1.25M,
                5.0M,
                out hasChangedIndividual
                );
            if (hasChangedIndividual) hasChanged = true;

            if (hasChanged) return b;
            else return EvolveBatch(b);
        }
        private static decimal EvolveDecimalParameter(decimal current, decimal min, decimal max, 
            out bool hasChanged)
        {
            const decimal evolotionChance = 15.0M; // 15%
            const decimal movementDistancePercent = 0.01M; // 1% of the distance between max and min
            if (RNG.getRandomDecimal(0, 100) < evolotionChance)
            {
                // evolve it
                decimal movementDirection = -1M; // default to down
                if (current <= min) movementDirection = 1M;  // nowhere to go but up
                else if (current >= max) movementDirection = -1M; // nowhere to go but down
                else
                {
                    // flip a coin
                    if (RNG.getRandomBool() == false) movementDirection = -1M;
                    else movementDirection = 1M;
                }
                decimal movementDistance = (max - min) * movementDistancePercent;
                decimal newVal = current + (movementDistance * movementDirection);
                if (newVal < min) newVal = min;
                if (newVal > max) newVal = max;
                hasChanged = true;
                return newVal;
            }
            else
            {
                hasChanged = false;
                return current;
            }
        }
        private static GraphSeries GetMedianNetWorthGraphFromSimResults(List<SimulationRunResult> simResults, DateTime birthDate)
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
        #endregion
    }
}
