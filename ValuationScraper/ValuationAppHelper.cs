using Lib;
using Lib.DataTypes;
using Lib.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace ValuationScraper
{
    internal static class ValuationAppHelper
    {
        internal static void Init(string[] args)
        {
            Logger.initialize("Valuation Scraper CLI");
            Logger.info("**********************************************************************************");
            Logger.info("*                         BEGINNING VALUATION SCRAPER CLI                        *");
            Logger.info("**********************************************************************************");
            printAssemblyInfo();
            foreach (string arg in args)
            {
                // no args
            }
            
        }
        internal static void Run()
        {
            try
            {
                InvestmentVehiclesList.investmentVehicles = DataAccessLayer.ReadInvestmentVehiclesFromDb();
                List<Account> accounts = DataAccessLayer.ReadAccountsFromDb();
                List<Valuation> prices = DataAccessLayer.ReadValuationsFromDb();



                // create a pricing engine for use in further operations                 
                PricingEngine.AddValuations(prices);
                if (FEATURETOGGLE.SHOULDREADNEWTRANSACTIONFILES)
                {
                    Logger.info("Updating Fidelity transaction data");
                    accounts = FidelityScraper.GetTransactions(accounts);
                    Logger.info("Finished updating Fidelity transaction data");
                    Logger.info("Updating T.Rowe Price transaction data");
                    accounts = TRoweScraper.GetTransactions(accounts);
                    Logger.info("Finished updating T.Rowe Price transaction data");
                    Logger.info("Updating Health Equity transaction data");
                    accounts = HealthEquityScraper.GetTransactions(accounts);
                    Logger.info("Finished updating Health Equity transaction data");
                }

                if (FEATURETOGGLE.SHOULDCATCHUPPRICINGDATA)
                {
                    Logger.info("Catching up pricing data");
                    prices = PricingEngine.CatchUpPrices(accounts);
                    Logger.info("Finished catching up pricing data");
                }
                if (FEATURETOGGLE.SHOULDBLENDPRICINGDATA)
                {
                    //const bool logPrices = true;
                    const bool logPrices = false;
                    if (logPrices)
                    {
                        Logger.info("Printing prices before blend");
                        PricingEngine.PrintPrices();
                    }
                    prices = PricingEngine.BlendPricesWithRealTransactions(accounts);
                    if (logPrices)
                    {
                        Logger.info("Printing prices after adding real transactions");
                        PricingEngine.PrintPrices();
                    }
                    prices = PricingEngine.BlendPricesDaily(accounts);
                    if (logPrices)
                    {
                        Logger.info("Printing prices after full blend");
                        PricingEngine.PrintPrices();
                    }
                }

                
                

            }
            catch (Exception ex)
            {
                Logger.fatal("Uncaught exception in Investment Tracker CLI", ex);
            }
            finally
            {
                Logger.info("Exiting Valuation Scraper CLI");
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
