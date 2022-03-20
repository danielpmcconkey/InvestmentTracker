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
    }
}
