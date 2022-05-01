using Lib;
using Lib.DataTypes;
using Lib.DataTypes.Simulation;
using Lib.Engine;
using Lib.Engine.MonteCarlo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace MonteCarloCLI
{
    internal class MCAppHelper
    {

        internal static void Init(string[] args)
        {
            Logger.initialize("Monte Carlo CLI");
            Logger.info("**********************************************************************************");
            Logger.info("*                           BEGINNING MONTE CARLO CLI                            *");
            Logger.info("**********************************************************************************");
            printAssemblyInfo();
            foreach (string arg in args)
            {

                // no args support here

                
            }
        }
        internal static void Run()
        {
            /*
             * when it's time to update the Monte Carlo Version, first run UpdateConfig()
             * to copy the current best run to the config settings in the DB. Then change 
             * the version number. Then run RunMonteCarloBatchAtConfigSettings() to run one 
             * version of the simulation at new rules with prior best parameters
             * 
             */
            // UpdateConfig();
            // MonteCarloHelper.RunMonteCarloBatchAtConfigSettings();
            try
            {
                while (true)
                {
                    // repeat forever, but check the clutch throughout

                    Logger.info("Running Monte Carlo batches (totally random)");
                    int numBatchesToRun = ConfigManager.GetInt("numMonteCarloBatchesToRun");
                    MonteCarloHelper.RunMonteCarloBatches(numBatchesToRun);
                    Logger.info("Finished running Monte Carlo batches (totally random)");
                    Logger.info("Evolving Monte Carlo batches");
                    MonteCarloHelper.EvolveBestRuns(MonteCarloBatch.monteCarloVersion, numBatchesToRun);
                    Logger.info("Finished evolving Monte Carlo batches");
                    Logger.info("Extending best Monte Carlo batches");
                    MonteCarloHelper.ExtendBestRuns(MonteCarloBatch.monteCarloVersion);
                    Logger.info("Finished extending best Monte Carlo batches");
                }
                

            }
            catch (Exception ex)
            {
                Logger.fatal("Uncaught exception in Monte Carlo CLI", ex);
            }
            finally
            {
                Logger.info("Exiting Monte Carlo CLI");
            }
        }
        
        static void printAssemblyInfo()
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName assemName = assem.GetName();
            Version assemblyBuild = assemName.Version;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assem.Location);
            Logger.info($"{assemName.Name}, Version {fvi.ProductMajorPart}.{fvi.ProductMinorPart}.{fvi.ProductBuildPart}.{fvi.ProductPrivatePart}");
            Logger.info($"Build number: {assemblyBuild}");
        }
        internal static void UpdateConfig()
        {
            var mc = DataAccessLayer.GetSingleBestRun(MonteCarloBatch.monteCarloVersion);

            ConfigManager.WriteDbConfigValue("monthlySpendLifeStyleToday",
                mc.simParams.monthlySpendLifeStyleToday.ToString());

            ConfigManager.WriteDbConfigValue("xMinusAgeStockPercentPreRetirement",
                mc.simParams.xMinusAgeStockPercentPreRetirement.ToString());

            ConfigManager.WriteDbConfigValue("numYearsBondBucketInRetirement",
                mc.simParams.numYearsBondBucketInRetirement.ToString());

            ConfigManager.WriteDbConfigValue("numYearsCashBucketInRetirement",
                mc.simParams.numYearsCashBucketInRetirement.ToString());

            ConfigManager.WriteDbConfigValue("recessionRecoveryPercent",
                mc.simParams.recessionRecoveryPercent.ToString());

            ConfigManager.WriteDbConfigValue("shouldMoveEquitySurplussToFillBondGapAlways",
                mc.simParams.shouldMoveEquitySurplussToFillBondGapAlways.ToString());

            ConfigManager.WriteDbConfigValue("recessionLifestyleAdjustment",
                mc.simParams.recessionLifestyleAdjustment.ToString());

            ConfigManager.WriteDbConfigValue("retirementLifestyleAdjustment",
                mc.simParams.retirementLifestyleAdjustment.ToString());

            ConfigManager.WriteDbConfigValue("maxSpendingPercentWhenBelowRetirementLevelEquity",
                mc.simParams.maxSpendingPercentWhenBelowRetirementLevelEquity.ToString());

            ConfigManager.WriteDbConfigValue("livingLargeThreashold",
                mc.simParams.livingLargeThreashold.ToString());

            ConfigManager.WriteDbConfigValue("livingLargeLifestyleSpendMultiplier",
                mc.simParams.livingLargeLifestyleSpendMultiplier.ToString());

        }
    }
}
