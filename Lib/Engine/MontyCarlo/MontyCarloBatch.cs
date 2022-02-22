using Lib.DataTypes.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Lib.Engine.MontyCarlo
{
    public class MontyCarloBatch
    {
        public Guid runId { get; set; }
        public string montyCarloVersion { get { return _montyCarloVersion; } set { } }
        private const string _montyCarloVersion = "2022.02.22.011";
        public DateTime runDate { get; set; }
        public SimulationParameters simParams { get; set; }
        public List<SimulationRunResult> simRuns { get; set; }
        public List<Asset> assetsGoingIn { get; set; }
        public int numberOfSimsToRun { get; set; }

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
        public decimal wealthAtDeath90Percent { get; set; }   // best wealth at death for the worst 10% of success runs
        public decimal wealthAtDeath95Percent { get; set; }   // best wealth at death for the worst 5% of success runs
        //public Dictionary<int, int> bankruptcyCountsByRetirementAnalogYear { get; set; }
        //public Dictionary<int, int> successCountsByRetirementAnalogYear { get; set; }
        public decimal successRateBadYears { get; set; }
        public decimal successRateGoodYears { get; set; }


        public MontyCarloBatch()
        {
            // only here for deserialization purposes
        }
        public MontyCarloBatch(SimulationParameters simParams, List<Asset> assetsGoingIn,
            int numberOfSimsToRun)
        {
            //montyCarloVersion = "2021.08.16.009"; 
            runId = Guid.NewGuid();
            this.simParams = simParams;
            this.assetsGoingIn = assetsGoingIn;
            this.numberOfSimsToRun = numberOfSimsToRun;
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
            string jsonString = DataSerializationHandler.SerializeMontyCarloBatch(this);
            return jsonString;
        }
        private void populateAnalyticsFromRunResults()
        {
            List<SimulationRunResult> bankruptcyRuns = simRuns.Where(x => x.wasSuccessful == false).ToList();
            totalRunsWithBankruptcy = bankruptcyRuns.Count();
            var successfulRuns = simRuns.Where(x => x.wasSuccessful == true);
            totalRunsWithoutBankruptcy = successfulRuns.Count();

            //bankruptcyCountsByRetirementAnalogYear = new Dictionary<int, int>();

            if (totalRunsWithBankruptcy > 0)
            {
                averageAgeAtBankruptcy = (decimal)bankruptcyRuns.Average(x => x.ageAtBankruptcy);
                minAgeAtBankruptcy = (decimal)bankruptcyRuns.Min(x => x.ageAtBankruptcy);
                maxAgeAtBankruptcy = (decimal)bankruptcyRuns.Max(x => x.ageAtBankruptcy);
                
                averageNumberOfRecessionsInBankruptcyRuns = (decimal)bankruptcyRuns.Average(x => x.numberofrecessions);

                List<SimulationRunResult> bankruptcies90Percent = getWorstNPerecentRuns(90);
                List<SimulationRunResult> bankruptcies95Percent = getWorstNPerecentRuns(95);
                List<SimulationRunResult> bankruptcies99Percent = getWorstNPerecentRuns(99);

                bankruptcyAge90Percent = (bankruptcies90Percent.Count < 1) ? -10.0m
                    : (decimal)bankruptcies90Percent.Min(x => x.ageAtBankruptcy);
                bankruptcyAge95Percent = (bankruptcies90Percent.Count < 1) ? -10.0m
                    : (decimal)bankruptcies95Percent.Min(x => x.ageAtBankruptcy);
                bankruptcyAge99Percent = (bankruptcies90Percent.Count < 1) ? -10.0m
                    : (decimal)bankruptcies99Percent.Min(x => x.ageAtBankruptcy);

                List<SimulationRunResult> successes90Percent = getWorstNPerecentRuns(90, true);
                List<SimulationRunResult> successes95Percent = getWorstNPerecentRuns(95, true);
                wealthAtDeath90Percent = (successes90Percent.Count < 1) ? -10.0m
                    : successes90Percent.Max(x => x.wealthAtDeath);
                wealthAtDeath95Percent = (successes95Percent.Count < 1) ? -10.0m
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


                successRateBadYears = badYearSuccesses / (decimal)(badYearSuccesses + badYearBankruptcies);
                successRateGoodYears = goodYearSuccesses / (decimal)(goodYearSuccesses + goodYearBankruptcies);

            }
            else
            {
                averageAgeAtBankruptcy = -1;
                minAgeAtBankruptcy = -1;
                maxAgeAtBankruptcy = -1;
                averageNumberOfRecessionsInBankruptcyRuns = -1;
                bankruptcyAge90Percent = -1;
                bankruptcyAge95Percent = -1;
                bankruptcyAge99Percent = -1;
            }
            if (totalRunsWithoutBankruptcy > 0)
            {
                averageNumberOfRecessionsInNonBankruptcyRuns = (decimal)simRuns
                    .Where(x => x.wasSuccessful == true)
                    .Average(x => x.numberofrecessions);
                averageWealthAtDeath = (decimal)simRuns.Average(x => x.wealthAtDeath);
            }
            else
            {
                averageNumberOfRecessionsInNonBankruptcyRuns = -10.0m;
                averageWealthAtDeath = -10.0m;
            }
            averageWealthAtRetirement = (decimal)simRuns.Average(x => x.wealthAtRetirement);
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
        public void writeSelfToDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    INSERT INTO public.montycarlobatch(
                        runid, 
                        montycarloversion, 
                        rundate, 
                        serializedself, 
                        numberofsimstorun, 
                        totalrunswithbankruptcy, 
                        totalrunswithoutbankruptcy,
                        averageageatbankruptcy, 
                        minageatbankruptcy, 
                        bankruptcyage90percent, 
                        bankruptcyage95percent, 
                        bankruptcyage99percent, 
                        maxageatbankruptcy, 
                        averagenumberofrecessionsinbankruptcyruns, 
                        averagenumberofrecessionsinnonbankruptcyruns, 
                        averagewealthatretirement, 
                        averagewealthatdeath,
                        wealthatdeath90percent,
                        wealthatdeath95percent,
                        monthlyInvestBrokerage,
                        xMinusAgeStockPercentPreRetirement,
                        numYearsBondBucketInRetirement,
                        recessionRecoveryPercent,
                        recessionLifestyleAdjustment,
                        retirementDate,
                        maxSpendingPercentWhenBelowRetirementLevelEquity
                    )
                    VALUES (
                        @runid, 
                        @montycarloversion, 
                        @rundate, 
                        @serializedself, 
                        @numberofsimstorun, 
                        @totalrunswithbankruptcy, 
                        @totalrunswithoutbankruptcy,
                        @averageageatbankruptcy, 
                        @minageatbankruptcy, 
                        @bankruptcyage90percent, 
                        @bankruptcyage95percent, 
                        @bankruptcyage99percent, 
                        @maxageatbankruptcy, 
                        @averagenumberofrecessionsinbankruptcyruns, 
                        @averagenumberofrecessionsinnonbankruptcyruns, 
                        @averagewealthatretirement, 
                        @averagewealthatdeath,
                        @wealthatdeath90percent,
                        @wealthatdeath95percent,
                        @monthlyInvestBrokerage,
                        @xMinusAgeStockPercentPreRetirement,
                        @numYearsBondBucketInRetirement,
                        @recessionRecoveryPercent,
                        @recessionLifestyleAdjustment,
                        @retirementDate,
                        @maxSpendingPercentWhenBelowRetirementLevelEquity
                    );
                    ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "montycarloversion", DbType = ParamDbType.Varchar, Value = montyCarloVersion });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "rundate", DbType = ParamDbType.Timestamp, Value = runDate });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "serializedself", DbType = ParamDbType.Text, Value = serializeSelf() });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "numberofsimstorun", DbType = ParamDbType.Integer, Value = numberOfSimsToRun });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "totalrunswithbankruptcy", DbType = ParamDbType.Integer, Value = totalRunsWithBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "totalrunswithoutbankruptcy", DbType = ParamDbType.Integer, Value = totalRunsWithoutBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averageageatbankruptcy", DbType = ParamDbType.Numeric, Value = averageAgeAtBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "minageatbankruptcy", DbType = ParamDbType.Numeric, Value = minAgeAtBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage90percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge90Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage95percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge95Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage99percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge99Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "maxageatbankruptcy", DbType = ParamDbType.Numeric, Value = maxAgeAtBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagenumberofrecessionsinbankruptcyruns", DbType = ParamDbType.Numeric, Value = averageNumberOfRecessionsInBankruptcyRuns });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagenumberofrecessionsinnonbankruptcyruns", DbType = ParamDbType.Numeric, Value = averageNumberOfRecessionsInNonBankruptcyRuns });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagewealthatretirement", DbType = ParamDbType.Numeric, Value = averageWealthAtRetirement });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagewealthatdeath", DbType = ParamDbType.Numeric, Value = averageWealthAtDeath });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "wealthatdeath90percent", DbType = ParamDbType.Numeric, Value = wealthAtDeath90Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "wealthatdeath95percent", DbType = ParamDbType.Numeric, Value = wealthAtDeath95Percent });
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
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "maxSpendingPercentWhenBelowRetirementLevelEquity",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.maxSpendingPercentWhenBelowRetirementLevelEquity
                    });




                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MontyCarloBatch.writeSelfToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public void updateSelfInDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    update public.montycarlobatch
                        set  
                        montycarloversion = @montycarloversion, 
                        rundate = @rundate, 
                        serializedself = @serializedself, 
                        numberofsimstorun = @numberofsimstorun, 
                        totalrunswithbankruptcy = @totalrunswithbankruptcy, 
                        totalrunswithoutbankruptcy = @totalrunswithoutbankruptcy,
                        averageageatbankruptcy = @averageageatbankruptcy, 
                        minageatbankruptcy = @minageatbankruptcy, 
                        bankruptcyage90percent = @bankruptcyage90percent, 
                        bankruptcyage95percent = @bankruptcyage95percent, 
                        bankruptcyage99percent = @bankruptcyage99percent, 
                        maxageatbankruptcy = @maxageatbankruptcy, 
                        averagenumberofrecessionsinbankruptcyruns = @averagenumberofrecessionsinbankruptcyruns, 
                        averagenumberofrecessionsinnonbankruptcyruns = @averagenumberofrecessionsinnonbankruptcyruns, 
                        averagewealthatretirement = @averagewealthatretirement, 
                        averagewealthatdeath = @averagewealthatdeath,
                        wealthatdeath90percent = @wealthatdeath90percent,
                        wealthatdeath95percent = @wealthatdeath95percent,
                        monthlyInvestBrokerage = @monthlyInvestBrokerage,
                        xMinusAgeStockPercentPreRetirement = @xMinusAgeStockPercentPreRetirement,
                        numYearsBondBucketInRetirement = @numYearsBondBucketInRetirement,
                        recessionRecoveryPercent = @recessionRecoveryPercent,
                        recessionLifestyleAdjustment = @recessionLifestyleAdjustment,
                        retirementDate = @retirementDate,
                        maxSpendingPercentWhenBelowRetirementLevelEquity = @maxSpendingPercentWhenBelowRetirementLevelEquity
                    where runid = @runid
                    ;
                    ";
                PostgresDAL.openConnection(conn);
                using (var cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "montycarloversion", DbType = ParamDbType.Varchar, Value = montyCarloVersion });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "rundate", DbType = ParamDbType.Timestamp, Value = runDate });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "serializedself", DbType = ParamDbType.Text, Value = serializeSelf() });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "numberofsimstorun", DbType = ParamDbType.Integer, Value = numberOfSimsToRun });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "totalrunswithbankruptcy", DbType = ParamDbType.Integer, Value = totalRunsWithBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "totalrunswithoutbankruptcy", DbType = ParamDbType.Integer, Value = totalRunsWithoutBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averageageatbankruptcy", DbType = ParamDbType.Numeric, Value = averageAgeAtBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "minageatbankruptcy", DbType = ParamDbType.Numeric, Value = minAgeAtBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage90percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge90Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage95percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge95Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "bankruptcyage99percent", DbType = ParamDbType.Numeric, Value = bankruptcyAge99Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "maxageatbankruptcy", DbType = ParamDbType.Numeric, Value = maxAgeAtBankruptcy });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagenumberofrecessionsinbankruptcyruns", DbType = ParamDbType.Numeric, Value = averageNumberOfRecessionsInBankruptcyRuns });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagenumberofrecessionsinnonbankruptcyruns", DbType = ParamDbType.Numeric, Value = averageNumberOfRecessionsInNonBankruptcyRuns });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagewealthatretirement", DbType = ParamDbType.Numeric, Value = averageWealthAtRetirement });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "averagewealthatdeath", DbType = ParamDbType.Numeric, Value = averageWealthAtDeath });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "wealthatdeath90percent", DbType = ParamDbType.Numeric, Value = wealthAtDeath90Percent });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "wealthatdeath95percent", DbType = ParamDbType.Numeric, Value = wealthAtDeath95Percent });
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
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "maxSpendingPercentWhenBelowRetirementLevelEquity",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.maxSpendingPercentWhenBelowRetirementLevelEquity
                    });




                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MontyCarloBatch.writeSelfToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public void updateParamsInDb()
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    update public.montycarlobatch
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
                        throw new Exception(string.Format("MontyCarloBatch.updateParamsInDb data update returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public static MontyCarloBatch readAndDeserializeFromDb(Guid runId)
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select serializedself from public.MontyCarloBatch where runid = @runId
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
                    string serializedMontyCarlo = Convert.ToString(result);

                    
                    return DataSerializationHandler.DeserializeMontyCarloBatch(serializedMontyCarlo);
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
                    from public.MontyCarloBatch 
                    where 1=1
                    and montyCarloVersion = @mcVersion
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
                        Value = _montyCarloVersion
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
    }
}
