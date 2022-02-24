using Lib.DataTypes.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Lib.Engine.MonteCarlo
{
    public class MonteCarloBatch
    {
        public Guid runId { get; set; }
        public string monteCarloVersion { get { return _monteCarloVersion; } set { } }
        private const string _monteCarloVersion = "2022.02.23.014";
        public DateTime runDate { get; set; }
        public SimulationParameters simParams { get; set; }
        public List<SimulationRunResult> simRuns { get; set; }
        public List<Asset> assetsGoingIn { get; set; }
        public int numberOfSimsToRun { get; set; }
        public Analytics analytics { get; set; }

        public MonteCarloBatch()
        {
            // only here for deserialization purposes
            analytics = new Analytics();
        }
        public MonteCarloBatch(SimulationParameters simParams, List<Asset> assetsGoingIn,
            int numberOfSimsToRun)
        {
            //monteCarloVersion = "2021.08.16.009"; 
            runId = Guid.NewGuid();
            this.simParams = simParams;
            this.assetsGoingIn = assetsGoingIn;
            this.numberOfSimsToRun = numberOfSimsToRun;
            analytics = new Analytics();
        }
        public void extendRun(int newCount)
        {
            Logger.info(string.Format("Run ID: {0}", runId.ToString()));

            int oldCount = simRuns.Count;

            // first add blank SimulationRunResults so we can update each my index
            for (int i2 = oldCount; i2 < newCount; i2++)
            {
                simRuns.Add(new SimulationRunResult());
            }

            Parallel.For(oldCount, newCount, i =>
            {
                try
                {
                    Simulation sim = new Simulation();

                    sim.init(simParams, assetsGoingIn);
                    SimulationRunResult simResult = sim.run();
                    try
                    {
                        simRuns[i] = simResult;
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Logger.error("Exception caught in parallel processing of simulation runn.", ex);
                    throw;
                }
            });
            numberOfSimsToRun = newCount;
            populateAnalyticsFromRunResults();

            updateSelfInDb();

        }
        public void runBatch()
        {
            Logger.info(string.Format("Run ID: {0}", runId.ToString()));

            runDate = DateTime.Now;

            simRuns = new List<SimulationRunResult>();

            if (FEATURETOGGLE.MULTITHREAD)
            {
                // first add blank SimulationRunResults so we can update each my index
                for (int i2 = 0; i2 < numberOfSimsToRun; i2++)
                {
                    simRuns.Add(new SimulationRunResult());
                }

                Parallel.For(0, numberOfSimsToRun, i =>
                {
                    try
                    {
                        Simulation sim = new Simulation();

                        sim.init(simParams, assetsGoingIn);
                        SimulationRunResult simResult = sim.run();
                        try
                        {
                            simRuns[i] = simResult;
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.error("Exception caught in parallel processing of simulation runn.", ex);
                        throw;
                    }
                });
            }
            else
            {
                for (int i = 0; i < numberOfSimsToRun; i++)
                {
                    try
                    {
                        Simulation sim = new Simulation();

                        sim.init(simParams, assetsGoingIn);
                        SimulationRunResult simResult = sim.run();
                        simRuns.Add(simResult);
                    }
                    catch (Exception ex)
                    {
                        Logger.error("Exception caught in serial processing of simulation runn.", ex);
                        throw;
                    }
                }
            }
            Logger.info(string.Format("Run ID: {0} has completed.", runId.ToString()));

            populateAnalyticsFromRunResults();

            if (FEATURETOGGLE.NO_WRITE == false)
            {
                writeSelfToDb();
            }



        }
        public string serializeSelf()
        {
            string jsonString = DataSerializationHandler.SerializeMonteCarloBatch(this);
            return jsonString;
        }
        private decimal GetMedianTotalLifeStyleSpend()
        {
            return GetTotalLifeStyleSpendAtNPercentile(50);
        }
        private decimal GetTotalLifeStyleSpendAtNPercentile(int N)
        {
            var orderedRuns = simRuns.OrderBy(x => x.totalLifeStyleSpend);
            int runCount = orderedRuns.Count();
            int index = (int) Math.Round(runCount * N * 0.01M, 0);
            return orderedRuns.ElementAt(index).totalLifeStyleSpend;
        }
        public void populateAnalyticsFromRunResults()
        {
            List<SimulationRunResult> bankruptcyRuns = simRuns.Where(x => x.wasSuccessful == false).ToList();
            analytics.totalRunsWithBankruptcy = bankruptcyRuns.Count();
            var successfulRuns = simRuns.Where(x => x.wasSuccessful == true);
            analytics.totalRunsWithoutBankruptcy = successfulRuns.Count();
            analytics.averageLifeStyleSpend = simRuns.Average(x => x.totalLifeStyleSpend);
            analytics.medianLifeStyleSpend = GetMedianTotalLifeStyleSpend();
            analytics.successRateOverall = analytics.totalRunsWithoutBankruptcy
                / (analytics.totalRunsWithBankruptcy + (decimal)analytics.totalRunsWithoutBankruptcy);

            //bankruptcyCountsByRetirementAnalogYear = new Dictionary<int, int>();

            if (analytics.totalRunsWithBankruptcy > 0)
            {
                analytics.averageAgeAtBankruptcy = (decimal)bankruptcyRuns.Average(x => x.ageAtBankruptcy);
                analytics.minAgeAtBankruptcy = (decimal)bankruptcyRuns.Min(x => x.ageAtBankruptcy);
                analytics.maxAgeAtBankruptcy = (decimal)bankruptcyRuns.Max(x => x.ageAtBankruptcy);

                analytics.averageNumberOfRecessionsInBankruptcyRuns = (decimal)bankruptcyRuns.Average(x => x.numberofrecessions);

                List<SimulationRunResult> bankruptcies90Percent = getWorstNPerecentRuns(90);
                List<SimulationRunResult> bankruptcies95Percent = getWorstNPerecentRuns(95);
                List<SimulationRunResult> bankruptcies99Percent = getWorstNPerecentRuns(99);

                analytics.bankruptcyAge90Percent = (bankruptcies90Percent.Count < 1) ? -10.0m
                    : (decimal)bankruptcies90Percent.Min(x => x.ageAtBankruptcy);
                analytics.bankruptcyAge95Percent = (bankruptcies90Percent.Count < 1) ? -10.0m
                    : (decimal)bankruptcies95Percent.Min(x => x.ageAtBankruptcy);
                analytics.bankruptcyAge99Percent = (bankruptcies90Percent.Count < 1) ? -10.0m
                    : (decimal)bankruptcies99Percent.Min(x => x.ageAtBankruptcy);

                List<SimulationRunResult> successes90Percent = getWorstNPerecentRuns(90, true);
                List<SimulationRunResult> successes95Percent = getWorstNPerecentRuns(95, true);
                analytics.wealthAtDeath90Percent = (successes90Percent.Count < 1) ? -10.0m
                    : successes90Percent.Max(x => x.wealthAtDeath);
                analytics.wealthAtDeath95Percent = (successes95Percent.Count < 1) ? -10.0m
                    : successes95Percent.Max(x => x.wealthAtDeath);

                /* get stats for the worst years to retire
                 * according to a run of 50,000 with fairly 
                 * vanilla settings, the worst years to retire
                 * were 1928 - 1938, 1974-1975, 2013-2020
                 */
                int[] badYears = new int[] { 1928, 1929, 1930, 1931, 1932, 1933, 1934, 1935, 1936, 1937, 1938, 1939, 1974, 1975, 2013, 2014, 2015, 2016, 2017, 2018, 2019, 2020 };
                var badYearRuns = simRuns.Where(x => 
                    badYears.Contains(x.retirementDateHistoricalAnalog.Year));
                var goodYearRuns = simRuns.Where(x => 
                    (badYears.Contains(x.retirementDateHistoricalAnalog.Year)) == false);
                var badYearBankruptcies = badYearRuns.Where(x => x.wasSuccessful == false).Count();
                var badYearSuccesses = badYearRuns.Where(x => x.wasSuccessful == true).Count();
                var goodYearBankruptcies = goodYearRuns.Where(x => x.wasSuccessful == false).Count();
                var goodYearSuccesses = goodYearRuns.Where(x => x.wasSuccessful == true).Count();


                analytics.successRateBadYears = badYearSuccesses / (decimal)(badYearSuccesses + badYearBankruptcies);
                analytics.successRateGoodYears = goodYearSuccesses / (decimal)(goodYearSuccesses + goodYearBankruptcies);

            }
            else
            {
                analytics.averageAgeAtBankruptcy = -1;
                analytics.minAgeAtBankruptcy = -1;
                analytics.maxAgeAtBankruptcy = -1;
                analytics.averageNumberOfRecessionsInBankruptcyRuns = -1;
                analytics.bankruptcyAge90Percent = -1;
                analytics.bankruptcyAge95Percent = -1;
                analytics.bankruptcyAge99Percent = -1;
            }
            if (analytics.totalRunsWithoutBankruptcy > 0)
            {
                analytics.averageNumberOfRecessionsInNonBankruptcyRuns = (decimal)simRuns
                    .Where(x => x.wasSuccessful == true)
                    .Average(x => x.numberofrecessions);
                analytics.averageWealthAtDeath = (decimal)simRuns.Average(x => x.wealthAtDeath);
            }
            else
            {
                analytics.averageNumberOfRecessionsInNonBankruptcyRuns = -10.0m;
                analytics.averageWealthAtDeath = -10.0m;
            }
            analytics.averageWealthAtRetirement = (decimal)simRuns.Average(x => x.wealthAtRetirement);
        }
        private List<SimulationRunResult> getWorstNPerecentRuns(float percent, bool wasSuccessful = false)
        {
            List<SimulationRunResult> outList = new List<SimulationRunResult>();

            List<SimulationRunResult> orderedRuns = new List<SimulationRunResult>();

            if (!wasSuccessful)
            {
                orderedRuns = simRuns
                    .Where(x => x.wasSuccessful == wasSuccessful)
                    .OrderByDescending(y => y.bankruptcydate).ToList();
            }
            else
            {
                orderedRuns = simRuns
                    .Where(x => x.wasSuccessful == wasSuccessful)
                    .OrderBy(y => y.wealthAtDeath).ToList();
            }

            int numToPull = (int)Math.Round((double)(simRuns.Count * 1 - percent), 0);
            for (int i = 0; i < numToPull; i++)
            {
                if (orderedRuns.Count >= i + 1)
                {
                    if (wasSuccessful || orderedRuns[i].bankruptcydate != null)
                    {
                        // don't add non-bankrupt results for requests to pull unsuccessful runs
                        outList.Add(orderedRuns[i]);
                    }
                }
            }

            return outList;
        }
        
        #region DB Functions
        public void writeParametersToDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {

                string qParams = @"

                INSERT INTO public.montecarlosimparameters(
                            runid, startdate, birthdate, retirementdate, monthlygrossincomepreretirement, 
                            monthlynetsocialsecurityincome, monthlyspendlifestyletoday, monthlyspendcoretoday, 
                            monthlyinvestroth401k, monthlyinvesttraditional401k, monthlyinvestbrokerage, 
                            monthlyinvesthsa, annualrsuinvestmentpretax, xminusagestockpercentpreretirement, 
                            numyearscashbucketinretirement, numyearsbondbucketinretirement, 
                            recessionrecoverypercent, shouldmoveequitysurplusstofillbondgapalways, 
                            deathageoverride, recessionlifestyleadjustment, retirementlifestyleadjustment, 
                            maxspendingpercentwhenbelowretirementlevelequity, annualinflationlow, 
                            annualinflationhi, socialsecuritycollectionage, livinglargethreashold,
                            livinglargelifestylespendmultiplier)
                    VALUES (
                        @runId, 
                        @startDate, 
                        @birthDate, 
                        @retirementDate,
                        @monthlyGrossIncomePreRetirement ,
                        @monthlyNetSocialSecurityIncome ,
                        @monthlySpendLifeStyleToday ,
                        @monthlySpendCoreToday ,
                        @monthlyInvestRoth401k ,
                        @monthlyInvestTraditional401k ,
                        @monthlyInvestBrokerage ,
                        @monthlyInvestHSA ,
                        @annualRSUInvestmentPreTax ,
                        @xMinusAgeStockPercentPreRetirement ,
                        @numYearsCashBucketInRetirement ,
                        @numYearsBondBucketInRetirement ,
                        @recessionRecoveryPercent ,
                        @shouldMoveEquitySurplussToFillBondGapAlways ,
                        @deathAgeOverride ,
                        @recessionLifestyleAdjustment,
                        @retirementLifestyleAdjustment,
                        @maxSpendingPercentWhenBelowRetirementLevelEquity ,
                        @annualInflationLow,
                        @annualInflationHi,
                        @socialSecurityCollectionAge,
                        @livingLargeThreashold,
                        @livingLargeLifestyleSpendMultiplier
                        );

                        ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(qParams, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runId", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "startDate", DbType = ParamDbType.Timestamp, Value = simParams.startDate });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "birthDate", DbType = ParamDbType.Timestamp, Value = simParams.birthDate });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "retirementDate", DbType = ParamDbType.Timestamp, Value = simParams.retirementDate });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyGrossIncomePreRetirement", DbType = ParamDbType.Numeric, Value = simParams.monthlyGrossIncomePreRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyNetSocialSecurityIncome", DbType = ParamDbType.Numeric, Value = simParams.monthlyNetSocialSecurityIncome });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlySpendLifeStyleToday", DbType = ParamDbType.Numeric, Value = simParams.monthlySpendLifeStyleToday });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlySpendCoreToday", DbType = ParamDbType.Numeric, Value = simParams.monthlySpendCoreToday });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestRoth401k", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestRoth401k });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestTraditional401k", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestTraditional401k });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestBrokerage", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestBrokerage });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestHSA", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestHSA });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "annualRSUInvestmentPreTax", DbType = ParamDbType.Numeric, Value = simParams.annualRSUInvestmentPreTax });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "xMinusAgeStockPercentPreRetirement", DbType = ParamDbType.Numeric, Value = simParams.xMinusAgeStockPercentPreRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "numYearsCashBucketInRetirement", DbType = ParamDbType.Numeric, Value = simParams.numYearsCashBucketInRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "numYearsBondBucketInRetirement", DbType = ParamDbType.Numeric, Value = simParams.numYearsBondBucketInRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "recessionRecoveryPercent", DbType = ParamDbType.Numeric, Value = simParams.recessionRecoveryPercent });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "shouldMoveEquitySurplussToFillBondGapAlways", DbType = ParamDbType.Boolean, Value = simParams.shouldMoveEquitySurplussToFillBondGapAlways });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "deathAgeOverride", DbType = ParamDbType.Integer, Value = simParams.deathAgeOverride });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "recessionLifestyleAdjustment", DbType = ParamDbType.Numeric, Value = simParams.recessionLifestyleAdjustment });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "retirementLifestyleAdjustment", DbType = ParamDbType.Numeric, Value = simParams.retirementLifestyleAdjustment });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "maxSpendingPercentWhenBelowRetirementLevelEquity", DbType = ParamDbType.Numeric, Value = simParams.maxSpendingPercentWhenBelowRetirementLevelEquity });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "annualInflationLow", DbType = ParamDbType.Numeric, Value = simParams.annualInflationLow });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "annualInflationHi", DbType = ParamDbType.Numeric, Value = simParams.annualInflationHi });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "socialSecurityCollectionAge", DbType = ParamDbType.Numeric, Value = simParams.socialSecurityCollectionAge });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "livingLargeThreashold", DbType = ParamDbType.Numeric, Value = simParams.livingLargeThreashold });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "livingLargeLifestyleSpendMultiplier", DbType = ParamDbType.Numeric, Value = simParams.livingLargeLifestyleSpendMultiplier });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.writeSelfToDb (parameters) data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public void writeSelfToDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    INSERT INTO public.montecarlobatch(
                        runid, 
                        montecarloversion, 
                        rundate, 
                        serializedself, 
                        numberofsimstorun, 
                        analytics
                    )
                    VALUES (
                        @runid, 
                        @montecarloversion, 
                        @rundate, 
                        @serializedself, 
                        @numberofsimstorun, 
                        @analytics
                    );
                    ";

                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "montecarloversion", DbType = ParamDbType.Varchar, Value = monteCarloVersion });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "rundate", DbType = ParamDbType.Timestamp, Value = runDate });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "serializedself", DbType = ParamDbType.Text, Value = serializeSelf() });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "numberofsimstorun", DbType = ParamDbType.Integer, Value = numberOfSimsToRun });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "analytics", DbType = ParamDbType.Json, Value = analytics });
                    
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "totalrunswithbankruptcy", DbType = ParamDbType.Integer, Value = totalRunsWithBankruptcy });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "totalrunswithoutbankruptcy", DbType = ParamDbType.Integer, Value = totalRunsWithoutBankruptcy });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "averageageatbankruptcy", DbType = ParamDbType.Numeric, Value = averageAgeAtBankruptcy });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "minageatbankruptcy", DbType = ParamDbType.Numeric, Value = minAgeAtBankruptcy });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage90percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge90Percent });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage95percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge95Percent });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage99percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge99Percent });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "maxageatbankruptcy", DbType = ParamDbType.Numeric, Value = maxAgeAtBankruptcy });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagenumberofrecessionsinbankruptcyruns", DbType = ParamDbType.Numeric, Value = averageNumberOfRecessionsInBankruptcyRuns });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagenumberofrecessionsinnonbankruptcyruns", DbType = ParamDbType.Numeric, Value = averageNumberOfRecessionsInNonBankruptcyRuns });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagewealthatretirement", DbType = ParamDbType.Numeric, Value = averageWealthAtRetirement });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagewealthatdeath", DbType = ParamDbType.Numeric, Value = averageWealthAtDeath });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "wealthatdeath90percent", DbType = ParamDbType.Numeric, Value = wealthAtDeath90Percent });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "wealthatdeath95percent", DbType = ParamDbType.Numeric, Value = wealthAtDeath95Percent });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "successrateinbadyears", DbType = ParamDbType.Numeric, Value = successRateBadYears });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "successrateingoodyears", DbType = ParamDbType.Numeric, Value = successRateGoodYears });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "averageLifeStyleSpend", DbType = ParamDbType.Numeric, Value = averageLifeStyleSpend });
                    //cmd.AddParameter(new DbCommandParameter() { ParameterName = "medianLifeStyleSpend", DbType = ParamDbType.Numeric, Value = medianLifeStyleSpend });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.writeSelfToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
                writeParametersToDb();
            }

        }
        public void updateSelfInDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    update public.montecarlobatch
                        set  
                        montecarloversion = @montecarloversion, 
                        rundate = @rundate, 
                        serializedself = @serializedself, 
                        numberofsimstorun = @numberofsimstorun, 
                        analytics = @analytics
                    where runid = @runid
                    ;
                    ";
                PostgresDAL.openConnection(conn);
                using (var cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "montecarloversion", DbType = ParamDbType.Varchar, Value = monteCarloVersion });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "rundate", DbType = ParamDbType.Timestamp, Value = runDate });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "serializedself", DbType = ParamDbType.Text, Value = serializeSelf() });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "numberofsimstorun", DbType = ParamDbType.Integer, Value = numberOfSimsToRun });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "analytics", DbType = ParamDbType.Json, Value = analytics });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.writeSelfToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public void updateParamsInDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    update public.montecarlobatch
                    set
                    monthlyInvestBrokerage = @monthlyInvestBrokerage,
                    xMinusAgeStockPercentPreRetirement = @xMinusAgeStockPercentPreRetirement,
                    numYearsBondBucketInRetirement = @numYearsBondBucketInRetirement,
                    recessionRecoveryPercent = @recessionRecoveryPercent,
                    recessionLifestyleAdjustment = @recessionLifestyleAdjustment,
                    retirementDate = @retirementDate
                    where runid = @runid
                    ";
                PostgresDAL.openConnection(conn);
                using (var cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "monthlyInvestBrokerage",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.monthlyInvestBrokerage
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "xMinusAgeStockPercentPreRetirement",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.xMinusAgeStockPercentPreRetirement
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "numYearsBondBucketInRetirement",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.numYearsBondBucketInRetirement
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "recessionRecoveryPercent",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.recessionRecoveryPercent
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "recessionLifestyleAdjustment",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.recessionLifestyleAdjustment
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "retirementDate",
                        DbType = ParamDbType.Timestamp,
                        Value = simParams.retirementDate
                    });
                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.updateParamsInDb data update returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public static MonteCarloBatch readAndDeserializeFromDb(Guid runId)
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select serializedself from public.MonteCarloBatch where runid = @runId
					;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "runId",
                        DbType = ParamDbType.Uuid,
                        Value = runId
                    }
                    );
                    object result = PostgresDAL.executeScalar(cmd.npgsqlCommand);
                    string serializedMonteCarlo = Convert.ToString(result);


                    return DataSerializationHandler.DeserializeMonteCarloBatch(serializedMonteCarlo);
                }
            }
        }
        public static List<Guid> getTopNRunsWithRunCountLessThanY(int N, int Y)
        {
            List<Guid> outList = new List<Guid>();

            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select 
	                    runid,
	                    (numberofsimstorun - totalrunswithbankruptcy) / cast(numberofsimstorun as numeric(6,0)) as successRate
                    from public.MonteCarloBatch 
                    where 1=1
                    and monteCarloVersion = @mcVersion
                    and numberofsimstorun < @Y
                    order by 
	                    successRate desc
                    limit(@N)
                    ;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "mcVersion",
                        DbType = ParamDbType.Varchar,
                        Value = _monteCarloVersion
                    }
                    );
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "Y",
                        DbType = ParamDbType.Integer,
                        Value = Y
                    }
                    );
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "N",
                        DbType = ParamDbType.Integer,
                        Value = N
                    }
                    );
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            Guid runId = PostgresDAL.getGuid(reader, "runid");
                            outList.Add(runId);
                        }
                    }
                }
            }

            return outList;

        }
        #endregion DB Functions
    }
}
