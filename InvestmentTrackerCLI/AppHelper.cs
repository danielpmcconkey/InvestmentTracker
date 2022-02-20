using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using Utilities;
using Lib;
using Lib.DataTypes;
using Lib.Engine;
using GraphLib;
using Lib.DataTypes.Simulation;
using Lib.Engine.MontyCarlo;

namespace InvestmentTrackerCLI
{
    internal static class AppHelper
    {
        

        // https://color.adobe.com/create/color-wheel
        
        private const string _colorBgDark = "2D332D"; 
        private const string _colorBgMid = "B8D1B9";
        private const string _colorBgLight = "E0FFE1";
        private const string _colorTextBrightest = "9E1010";
        private const string _colorTextDark = "5C0909";
        private const string _colorLines = "798A79";
       

        internal static void Init(string[] args)
        {
            Logger.initialize("Investment Tracker CLI");
            Logger.info("**********************************************************************************");
            Logger.info("*                        BEGINNING INVESTMENT TRACKER CLI                        *");
            Logger.info("**********************************************************************************");
            printAssemblyInfo();
            foreach (string arg in args)
            {
                if(arg.ToLower() == "shouldreadinitalcsvdata:true")
                {
                    FEATURETOGGLE._shouldReadInitalCSVData = true;
                    
                }
                if (arg.ToLower() == "shouldreadinitalcsvdata:false")
                {
                    FEATURETOGGLE._shouldReadInitalCSVData = false;
                }
                if (arg.ToLower() == "shouldwritejsondata:true")
                {
                    FEATURETOGGLE._shouldWriteJSONData = true;
                }
                if (arg.ToLower() == "shouldwritejsondata:false")
                {
                    FEATURETOGGLE._shouldWriteJSONData = false;
                }
                if (arg.ToLower() == "shouldreadjsonaccountdata:true")
                {
                    FEATURETOGGLE._shouldReadJSONAccountData = true;
                }
                if (arg.ToLower() == "shouldreadjsonaccountdata:false")
                {
                    FEATURETOGGLE._shouldReadJSONAccountData = false;
                }
                if (arg.ToLower() == "shouldreadjsonpricingdata:true")
                {
                    FEATURETOGGLE._shouldReadJSONPricingData = true;
                }
                if (arg.ToLower() == "shouldreadjsonpricingdata:false")
                {
                    FEATURETOGGLE._shouldReadJSONPricingData = false;
                }
                if (arg.ToLower() == "shouldcatchuppricingdata:true")
                {
                    FEATURETOGGLE._shouldCatchUpPricingData = true;
                }
                if (arg.ToLower() == "shouldcatchuppricingdata:false")
                {
                    FEATURETOGGLE._shouldCatchUpPricingData = false;
                }
                if (arg.ToLower() == "shouldblendpricingdata:true")
                {
                    FEATURETOGGLE._shouldBlendPricingData = true;
                }
                if (arg.ToLower() == "shouldblendpricingdata:false")
                {
                    FEATURETOGGLE._shouldBlendPricingData = false;
                }
                if (arg.ToLower() == "shouldprintnetworth:true")
                {
                    FEATURETOGGLE._shouldPrintNetWorth = true;
                }
                if (arg.ToLower() == "shouldprintnetworth:false")
                {
                    FEATURETOGGLE._shouldPrintNetWorth = false;
                }
            }
            Logger.info("Feature toggles:");
            Logger.info(string.Format("     _shouldReadInitalCSVData set to {0}", FEATURETOGGLE._shouldReadInitalCSVData));
            Logger.info(string.Format("     _shouldWriteJSONData set to {0}", FEATURETOGGLE._shouldWriteJSONData));
            Logger.info(string.Format("     _shouldReadJSONAccountData set to {0}", FEATURETOGGLE._shouldReadJSONAccountData));
            Logger.info(string.Format("     _shouldReadJSONPricingData set to {0}", FEATURETOGGLE._shouldReadJSONPricingData));
            Logger.info(string.Format("     _shouldCatchUpPricingData set to {0}", FEATURETOGGLE._shouldCatchUpPricingData));
            Logger.info(string.Format("     _shouldBlendPricingData set to {0}", FEATURETOGGLE._shouldBlendPricingData));
            Logger.info(string.Format("     _shouldPrintNetWorth set to {0}", FEATURETOGGLE._shouldPrintNetWorth));
        }
        internal static void Run()
        {
            Logger.info("Beginning run.");

            try
            {
                List <Account> accounts = new List<Account>();
                List<Valuation> prices = new List<Valuation>();

                

                if (FEATURETOGGLE._shouldReadInitalCSVData)
                {
                    Logger.info("Reading CSV accounts");
                    List<Account> initialAccounts = DataSerializationHandler.ReadInitialAccountsCSV();
                    Logger.info("Finished reading CSV accounts");
                    Logger.info("Reading CSV transactions");
                    initialAccounts = DataSerializationHandler.AddInitialTransactionsCSVToAccounts(initialAccounts);
                    Logger.info("Finished reading CSV transactions");
                    
                    // add the CSV accounts to the accounts list
                    accounts.AddRange(initialAccounts);
                }
                if (FEATURETOGGLE._shouldReadJSONAccountData)
                {
                    Logger.info("De-serializing account and transaction data");
                    accounts.AddRange(DataSerializationHandler.DeSerializeAccountsData());
                    Logger.info("Finished de-serializing account and transaction data");
                }
                if (FEATURETOGGLE._shouldReadJSONPricingData)
                {
                    Logger.info("De-serializing pricing data");
                    prices.AddRange(DataSerializationHandler.DeSerializePricingData());
                    Logger.info("Finished de-serializing pricing data");
                }
                // create a pricing engine for use in further operations                 
                PricingEngine pricingEngine = new PricingEngine(prices);
                if (FEATURETOGGLE._shouldReadJSONAccountData)
                {
                    Logger.info("Updating Fidelity transaction data");
                    accounts = FidelityScraper.GetTransactions(accounts, pricingEngine);
                    Logger.info("Finished updating Fidelity transaction data");
                    Logger.info("Updating T.Rowe Price transaction data");
                    accounts = TRoweScraper.GetTransactions(accounts, pricingEngine);
                    Logger.info("Finished updating T.Rowe Price transaction data");
                    Logger.info("Updating Health Equity transaction data");
                    accounts = HealthEquityScraper.GetTransactions(accounts, pricingEngine);
                    Logger.info("Finished updating Health Equity transaction data");
                }

                if (FEATURETOGGLE._shouldCatchUpPricingData)
                {
                    Logger.info("Catching up pricing data");
                    prices = pricingEngine.CatchUpPrices(accounts);
                    Logger.info("Finished catching up pricing data");
                }
                if (FEATURETOGGLE._shouldBlendPricingData)
                {
                    //const bool logPrices = true;
                    const bool logPrices = false;
                    if (logPrices)
                    {
                        Logger.info("Printing prices before blend");
                        pricingEngine.PrintPrices();
                    }
                    prices = pricingEngine.BlendPricesWithRealTransactions(accounts);
                    if (logPrices)
                    {
                        Logger.info("Printing prices after adding real transactions");
                        pricingEngine.PrintPrices();
                    }
                    prices = pricingEngine.BlendPricesDaily(accounts);
                    if (logPrices)
                    {
                        Logger.info("Printing prices after full blend");
                        pricingEngine.PrintPrices();
                    }
                }
                if (FEATURETOGGLE._shouldWriteJSONData)
                {
                    // serialize data back to files
                    Logger.info("Serializing data back to files");
                    DataSerializationHandler.SerializeData(accounts, prices);
                    Logger.info("Finished serializing data to files");
                }
                StringBuilder sbOutput = new StringBuilder();
                double captionWidth = 0;

                if (FEATURETOGGLE._shouldPrintNetWorth)
                {
                    Logger.info("Printing net worth");

                    GraphPrefs graphPrefs = GetDefaultGraphPrefs("Total Net Worth by Investment Type");
                    LineChartPrefs lineChartPrefs = GetDefaultLineChartPrefs();

                    GraphData graphDataTotalWorth = WorthEngine.GetTotalNetWorthByType(accounts, pricingEngine);
                    LineChart svgTotalWorth = new LineChart(graphPrefs, lineChartPrefs, graphDataTotalWorth, "NetWorthGraphAll");

                    GraphPrefs graphPrefsTaxBuckets = GetDefaultGraphPrefs("Total Net Worth by Tax Buckets");
                    GraphData graphDataTaxBuckets = WorthEngine.GetTotalNetWorthByTaxBucket(accounts, pricingEngine);
                    LineChart svgTaxBuckets = new LineChart(graphPrefsTaxBuckets, lineChartPrefs, graphDataTaxBuckets, "TaxBucketsGraph");
                                        
                    GraphPrefs graphPrefsStocks = GetDefaultGraphPrefs("Individual stock worth");
                    GraphData graphDataStocks = WorthEngine.GetStockGraphData(accounts, pricingEngine);
                    LineChartPrefs stocklLineChartPrefs = GetDefaultLineChartPrefs();
                    stocklLineChartPrefs.xColumnLabelsTextFormat = "yyyy-MM-dd";
                    LineChart svgStocks = new LineChart(graphPrefsStocks, stocklLineChartPrefs, graphDataStocks, "StocksGraph");

                    GraphPrefs graphPrefsIndividualStocksComparison = GetDefaultGraphPrefs("Individual stocks compared to S&P 500");
                    GraphData graphDataStocksComparison = WorthEngine
                        .GetSPDRComparisonGraphData(accounts, pricingEngine, true);
                    LineChart svgStockComparison = new LineChart(
                        graphPrefsIndividualStocksComparison, stocklLineChartPrefs, 
                        graphDataStocksComparison, "IndStockCompareGraph");

                    GraphPrefs graphPrefsAllPublicComparison = GetDefaultGraphPrefs("All publicly traded investments compared to S&P 500");
                    GraphData graphDataAllPublicComparison = WorthEngine
                        .GetSPDRComparisonGraphData(accounts, pricingEngine, false);
                    LineChart svgAllPublicComparison = new LineChart(
                        graphPrefsAllPublicComparison, lineChartPrefs,
                        graphDataAllPublicComparison, "PublicCompareGraph");

                    captionWidth = graphPrefs.pictureWidthInPx - 25;



                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgTotalWorth.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows a break down by asset classes of your total net worth.</p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgTaxBuckets.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows a break down by tax buckets of your total net worth.</p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgStocks.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows your net worth in each individual stock position.</p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgStockComparison.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows your net worth in individual stocks versus what it would look like if you'd just put everything into the SPY S&P 500 ETF instead.</p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgAllPublicComparison.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows your net worth in all publicly-traded investments versus what it would look like if you'd just put everything into the SPY S&P 500 ETF instead.</p>");
                    sbOutput.AppendLine("</div>");
                    
                    //Logger.info(Environment.NewLine + svg.xml + Environment.NewLine);
                    Logger.info("Finished printing net worth");
                }
                if (FEATURETOGGLE.shouldRunMontyCarlo)
                {
                    Logger.info("Running Monty Carlo simulation");
                    MontyCarloBatch mcBatch = MontyCarloHelper.RunMontyCarlo(accounts, pricingEngine);
                    GraphData mcGraphData = MontyCarloHelper.GetMontyCarloGraphData(mcBatch);
                    GraphPrefs graphPrefs = GetDefaultGraphPrefs("Monty Carlo Simulation Results");
                    graphPrefs.graphFillHexColor = "F9F5EC";
                    LineChartPrefs lineChartPrefs = GetDefaultLineChartPrefs();
                    //lineChartPrefs.shouldPrintLegend = false;
                    lineChartPrefs.maxY = 10000000M;
                    MontyCarloLineChart svgMontyCarlo = new MontyCarloLineChart(
                        graphPrefs, lineChartPrefs,
                        mcGraphData, "MontyCarloGraph");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgMontyCarlo.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows the imaginary net worth by age from multiple simulation runs. Lines that make it to age 95 before reaching zero mean you outlive your money. Lines that hit zero before age 95 means you would have to tighten your belt (or die sooner). Assumptions and detailed results are printed below. </p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div class='mcanalytics'>");
                    sbOutput.AppendLine("<h3>Monty Carlo Analytics</h3>");
                    sbOutput.AppendLine(string.Format("<p>Average wealth at retirement: {0}</p>", mcBatch.averageWealthAtRetirement.ToString("c")));
                    sbOutput.AppendLine(string.Format("<p>Total runs with bankruptcy: {0}</p>", mcBatch.totalRunsWithBankruptcy.ToString("#,###")));
                    sbOutput.AppendLine(string.Format("<p>Total runs without bankruptcy: {0}</p>", mcBatch.totalRunsWithoutBankruptcy.ToString("#,###")));
                    sbOutput.AppendLine(string.Format("<p>Average age at bankruptcy: {0}</p>", mcBatch.averageAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Minimum age at bankruptcy: {0}</p>", mcBatch.minAgeAtBankruptcy.ToString("##.00")));
                    //sbOutput.AppendLine(string.Format("<p>Bankruptcy age 90th percentile: {0}</p>", mcBatch.bankruptcyAge90Percent.ToString("##.00")));
                    //sbOutput.AppendLine(string.Format("<p>Bankruptcy age 95th percentile: {0}</p>", mcBatch.bankruptcyAge95Percent.ToString("##.00")));
                    //sbOutput.AppendLine(string.Format("<p>Bankruptcy age 99th percentile: {0}</p>", mcBatch.bankruptcyAge99Percent.ToString("##.00")));
                    //sbOutput.AppendLine(string.Format("<p>Average number of recessions in bankruptcy runs: {0}</p>", mcBatch.averageNumberOfRecessionsInBankruptcyRuns.ToString("##.00")));
                    //sbOutput.AppendLine(string.Format("<p>Average number of recessions in non-bankruptcy runs: {0}</p>", mcBatch.averageNumberOfRecessionsInNonBankruptcyRuns.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Max age at bankruptcy: {0} (oldest age of bankruptcy)</p>", mcBatch.maxAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average wealth at death: {0}</p>", mcBatch.averageWealthAtDeath.ToString("c")));
                    //sbOutput.AppendLine(string.Format("<p>Wealth at death 90th percentile: {0}</p>", mcBatch.wealthAtDeath90Percent.ToString("c")));
                    //sbOutput.AppendLine(string.Format("<p>Wealth at death 95th percentile: {0}</p>", mcBatch.wealthAtDeath95Percent.ToString("c")));
                    sbOutput.AppendLine("</div>");
                    Logger.info("Finished running Monty Carlo simulation");
                }
                WriteHTMLFile(sbOutput, captionWidth);

            }
            catch (Exception ex)
            {
                Logger.fatal("Uncaught exception in Investment Tracker CLI", ex);
            }
            finally
            {
                Logger.info("Exiting Investment Tracker CLI");
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
        internal static void WriteHTMLFile(StringBuilder sbOutput, double captionWidth)
        {
            StringBuilder HTML = new StringBuilder();
            HTML.AppendLine("<!DOCTYPE html>");
            HTML.AppendLine(@"<html>
                        <head>
                            <style type='text/css'>
	                            body {
		                            font-family:sans-serif;
		                            color:#444444;
	                            }
	                            div { margin-bottom: 20px; }");
            HTML.AppendLine(".caption {");
            HTML.AppendLine(String.Format(" width: {0}px;", captionWidth));
            HTML.AppendLine("padding: 5px 5px 5px 20px;");
            HTML.AppendLine("background-color:#cccccc;");
            HTML.AppendLine("font-size:12px;");
            HTML.AppendLine("margin-top:0px;");
            HTML.AppendLine("}");
            HTML.AppendLine(".mcanalytics {");
            HTML.AppendLine(String.Format(" width: {0}px;", captionWidth));
            HTML.AppendLine("font-size:12px;");
            HTML.AppendLine("}");
            HTML.AppendLine(".mcanalytics h3 {");
            HTML.AppendLine("font-size:16px;");
            HTML.AppendLine(String.Format("color: #{0};", _colorTextBrightest));            
            HTML.AppendLine("}");

            HTML.AppendLine(@"        </style>
                        </head> 
                    ");
            HTML.AppendLine("<body>");
            HTML.AppendLine("<h1>Dan's Wealth Tracker</h1>");
            HTML.AppendLine(string.Format("<h3>Created: {0}</h3>", DateTime.Now.ToShortDateString()));
            HTML.AppendLine(sbOutput.ToString());
            HTML.AppendLine("</body>");
            HTML.AppendLine("</html>");


            string filePath = Path.Combine(ConfigManager.GetString("DataDirectory"), "InvestmentTracker.html");
            using (StreamWriter file = new(filePath, append: false))
            {
                file.Write(HTML.ToString());
            }
        }
        private static GraphPrefs GetDefaultGraphPrefs(string title)
        {
            return new GraphPrefs()
            {
                title = title,
                titleHexColor = _colorTextBrightest,
                pictureWidthInPx = 1300,
                pictureHeightInPx = 500,
                pictureBagroundHexColor = _colorBgDark,
                graphBorderStrokeWidthInPx = 1,
                graphFillHexColor = _colorBgLight,
                graphStrokeHexColor = _colorLines,
                paddingInPx = (5, 5, 5, 5),
                labelsHexColor = _colorTextDark,
                labelsSizeInPx = 12,
            };
        }
        private static LineChartPrefs GetDefaultLineChartPrefs()
        {
            return new LineChartPrefs()
            {
                legendBGHexColor = _colorBgMid,
                gridFillHexColor = _colorBgMid,
                gridStrokeHexColor = _colorBgLight,
                gridBorderStrokeWidthInPx = 1,
                gridLineStrokeHexColor = _colorLines,
                gridLineStrokeWidthInPx = 0.5,
            };
        }
        


    }
}
