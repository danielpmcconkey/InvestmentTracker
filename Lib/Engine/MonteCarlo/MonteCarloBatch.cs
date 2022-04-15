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
        public const string monteCarloVersion = "2022.04.04.015";
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
            
            DataAccessLayer.updateMonteCarloBatchInDb(this);

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
                DataAccessLayer.writeMonteCarloBatchToDb(this);
            }



        }
        public string serializeSelf()
        {
            string jsonString = DataAccessLayer.SerializeType<MonteCarloBatch>(this);
            return jsonString;
        }
        private decimal GetMedianTotalLifeStyleSpend()
        {
            return GetTotalLifeStyleSpendAtBottomNPercentile(50);
        }
        private decimal GetTotalLifeStyleSpendAtBottomNPercentile(int N)
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
            analytics.bottom10PercentLifeStyleSpend = GetTotalLifeStyleSpendAtBottomNPercentile(10);
            analytics.successRateOverall = analytics.totalRunsWithoutBankruptcy
                / (analytics.totalRunsWithBankruptcy + (decimal)analytics.totalRunsWithoutBankruptcy);

            
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

                analytics.averageLifeStyleSpendBadYears = badYearRuns.Average(x => x.totalLifeStyleSpend);
                var successfulBadYears = badYearRuns.Where(x => x.wasSuccessful);
                analytics.averageLifeStyleSpendSuccessfulBadYears = (successfulBadYears.Count() > 0) ?
                    successfulBadYears.Average(y => y.totalLifeStyleSpend)
                    : -10M;

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
        
    }
}
