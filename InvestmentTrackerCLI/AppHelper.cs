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
using Lib.Engine.MonteCarlo;

namespace InvestmentTrackerCLI
{
    internal static class AppHelper
    {
        // <add key="ConnectionString" value="Host=localhost;Username=mcduck_app_dev;Password='R#x8QA4tGV?zB^|h';Database=HouseholdBudget;Timeout=15;Command Timeout=300;" />
       


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
                    FEATURETOGGLE.SHOULDREADINITALCSVDATA = true;
                    
                }
                if (arg.ToLower() == "shouldreadinitalcsvdata:false")
                {
                    FEATURETOGGLE.SHOULDREADINITALCSVDATA = false;
                }
                if (arg.ToLower() == "shouldwritejsondata:true")
                {
                    FEATURETOGGLE.SHOULDWRITEJSONDATA = true;
                }
                if (arg.ToLower() == "shouldwritejsondata:false")
                {
                    FEATURETOGGLE.SHOULDWRITEJSONDATA = false;
                }
                if (arg.ToLower() == "shouldreadjsonaccountdata:true")
                {
                    FEATURETOGGLE.SHOULDREADJSONACCOUNTDATA = true;
                }
                if (arg.ToLower() == "shouldreadjsonaccountdata:false")
                {
                    FEATURETOGGLE.SHOULDREADJSONACCOUNTDATA = false;
                }
                if (arg.ToLower() == "shouldreadjsonpricingdata:true")
                {
                    FEATURETOGGLE.SHOULDREADJSONPRICINGDATA = true;
                }
                if (arg.ToLower() == "shouldreadjsonpricingdata:false")
                {
                    FEATURETOGGLE.SHOULDREADJSONPRICINGDATA = false;
                }
                if (arg.ToLower() == "shouldcatchuppricingdata:true")
                {
                    FEATURETOGGLE.SHOULDCATCHUPPRICINGDATA = true;
                }
                if (arg.ToLower() == "shouldcatchuppricingdata:false")
                {
                    FEATURETOGGLE.SHOULDCATCHUPPRICINGDATA = false;
                }
                if (arg.ToLower() == "shouldblendpricingdata:true")
                {
                    FEATURETOGGLE.SHOULDBLENDPRICINGDATA = true;
                }
                if (arg.ToLower() == "shouldblendpricingdata:false")
                {
                    FEATURETOGGLE.SHOULDBLENDPRICINGDATA = false;
                }
                if (arg.ToLower() == "shouldprintnetworth:true")
                {
                    FEATURETOGGLE.SHOULDPRINTNETWORTH = true;
                }
                if (arg.ToLower() == "shouldprintnetworth:false")
                {
                    FEATURETOGGLE.SHOULDPRINTNETWORTH = false;
                }
            }
            Logger.info("Feature toggles:");
            Logger.info(string.Format("     _shouldReadInitalCSVData set to {0}", FEATURETOGGLE.SHOULDREADINITALCSVDATA));
            Logger.info(string.Format("     _shouldWriteJSONData set to {0}", FEATURETOGGLE.SHOULDWRITEJSONDATA));
            Logger.info(string.Format("     _shouldReadJSONAccountData set to {0}", FEATURETOGGLE.SHOULDREADJSONACCOUNTDATA));
            Logger.info(string.Format("     _shouldReadJSONPricingData set to {0}", FEATURETOGGLE.SHOULDREADJSONPRICINGDATA));
            Logger.info(string.Format("     _shouldCatchUpPricingData set to {0}", FEATURETOGGLE.SHOULDCATCHUPPRICINGDATA));
            Logger.info(string.Format("     _shouldBlendPricingData set to {0}", FEATURETOGGLE.SHOULDBLENDPRICINGDATA));
            Logger.info(string.Format("     _shouldPrintNetWorth set to {0}", FEATURETOGGLE.SHOULDPRINTNETWORTH));
        }
        internal static void Run()
        {
            Logger.info("Beginning run.");

            try
            {
                List <Account> accounts = new List<Account>();
                List<Valuation> prices = new List<Valuation>();

                

                if (FEATURETOGGLE.SHOULDREADINITALCSVDATA)
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
                if (FEATURETOGGLE.SHOULDREADJSONACCOUNTDATA)
                {
                    Logger.info("De-serializing account and transaction data");
                    accounts.AddRange(DataSerializationHandler.DeSerializeAccountsData());
                    Logger.info("Finished de-serializing account and transaction data");
                }
                if (FEATURETOGGLE.SHOULDREADJSONPRICINGDATA)
                {
                    Logger.info("De-serializing pricing data");
                    prices.AddRange(DataSerializationHandler.DeSerializePricingData());
                    Logger.info("Finished de-serializing pricing data");
                }
                // create a pricing engine for use in further operations                 
                PricingEngine pricingEngine = new PricingEngine(prices);
                if (FEATURETOGGLE.SHOULDREADJSONACCOUNTDATA)
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

                if (FEATURETOGGLE.SHOULDCATCHUPPRICINGDATA)
                {
                    Logger.info("Catching up pricing data");
                    prices = pricingEngine.CatchUpPrices(accounts);
                    Logger.info("Finished catching up pricing data");
                }
                if (FEATURETOGGLE.SHOULDBLENDPRICINGDATA)
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
                if (FEATURETOGGLE.SHOULDWRITEJSONDATA)
                {
                    // serialize data back to files
                    Logger.info("Serializing data back to files");
                    DataSerializationHandler.SerializeData(accounts, prices);
                    Logger.info("Finished serializing data to files");
                }
                StringBuilder sbOutput = new StringBuilder();
                double captionWidth = 0;

                if (FEATURETOGGLE.SHOULDPRINTNETWORTH)
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
                if (FEATURETOGGLE.SHOULDRUNMONTECARLO)
                {
                    Logger.info("Running Monte Carlo simulation");
                    MonteCarloBatch mcBatch = MonteCarloHelper.RunMonteCarlo(accounts, pricingEngine);
                    GraphData mcGraphData = MonteCarloHelper.GetMonteCarloGraphData(mcBatch);
                    GraphPrefs graphPrefs = GetDefaultGraphPrefs("Monte Carlo Simulation Results");
                    graphPrefs.graphFillHexColor = "F9F5EC";
                    LineChartPrefs lineChartPrefs = GetDefaultLineChartPrefs();
                    //lineChartPrefs.shouldPrintLegend = false;
                    lineChartPrefs.maxY = 10000000M;
                    MonteCarloLineChart svgMonteCarlo = new MonteCarloLineChart(
                        graphPrefs, lineChartPrefs,
                        mcGraphData, "MonteCarloGraph");
                    sbOutput.AppendLine("<div>");
                    string monteCarloInstructions = @" 
                    <h1>Monte Carlo Simulation</h1>
                    <p class='mcanalytics'>A Monte Carlo simulation is a simulation that iterates multiple times, each with different random
                    perturbations to assess probability of outcome. In this case, we take the same financial input (the 
                    infomation arrived at in the above graphs) plus certain assumptions, and run them through a mock future</p>
                    <p class='mcanalytics'>The mock future we mean here is that we project forward, into the future, certain market values.
                    We base those values on the past history of the S&P500. We have the S&P500 monthly data from 
                    January 1928 to August 2021 and we pick a random point within that timeframe to start our
                    imaginary future. If our random point is, say July of 1943, and July '43 dropped 0.03% from June '43,
                    then our simulation's next month (" +
                    DateTimeHelper.RoundToMonth(DateTime.Now, RoundDateDirection.UP).ToString("MMMM yyyy")
                    + @") will have dropped 0.03% from last month (" +
                    DateTimeHelper.RoundToMonth(DateTime.Now, RoundDateDirection.DOWN).ToString("MMMM yyyy")
                    + @"). And we'll continue forward, using the same growth as actual history.</p>
                    <p class='mcanalytics'>In this case, we ran the simulation " + mcBatch.numberOfSimsToRun.ToString() + @" times and each time
                    introduced a little randomness, like inflation and bond yields. Thus we arrive at a higher confidence in
                    our retirement planning.</p>
                    <p>So what are the assumptions and rules?</p>
                    <ul class='mcanalytics'>
                    ";
                    monteCarloInstructions += String.Format("<li>Your were born {0}</li>", mcBatch.simParams.birthDate.ToShortDateString());
                    monteCarloInstructions += String.Format("<li>You plan to retire {0}</li>", mcBatch.simParams.retirementDate.ToShortDateString());
                    monteCarloInstructions += String.Format("<li>You currently spend {0} per month on 'core' needs and another {1} on your lifestyle</li>", mcBatch.simParams.monthlySpendCoreToday.ToString("c"), mcBatch.simParams.monthlySpendLifeStyleToday.ToString("c"));
                    monteCarloInstructions += String.Format("<li>You will continue to spend that much every month until you die, which will be at age {0}</li>", mcBatch.simParams.deathAgeOverride.ToString());
                    monteCarloInstructions += String.Format("<li>However, that spend will go up with inflation, which will be between {0}% and {1}% year over year</li>", (mcBatch.simParams.annualInflationLow * 100).ToString("###.00"), (mcBatch.simParams.annualInflationHi * 100).ToString("###.00"));
                    monteCarloInstructions += String.Format("<li>Between now and retirement, you'll earn {0} per month at your day job</li>", mcBatch.simParams.monthlyGrossIncomePreRetirement.ToString("c"));
                    monteCarloInstructions += String.Format("<li>You'll be saving {0} a month in your Roth 401(k)</li>", mcBatch.simParams.monthlyInvestRoth401k.ToString("c"));
                    monteCarloInstructions += String.Format("<li>You'll be saving {0} a month in your Traditional 401(k)</li>", mcBatch.simParams.monthlyInvestTraditional401k.ToString("c"));
                    monteCarloInstructions += String.Format("<li>You'll be saving {0} a month in your HSA</li>", mcBatch.simParams.monthlyInvestHSA.ToString("c"));
                    monteCarloInstructions += String.Format("<li>You'll be saving {0} a month in your taxable brokerage account</li>", mcBatch.simParams.monthlyInvestBrokerage.ToString("c"));
                    monteCarloInstructions += String.Format("<li>And once a year, you'll be putting your equity bonus (RSU) of {0} (after tax) into your brokerage account</li>", mcBatch.simParams.annualRSUInvestmentPreTax.ToString("c"));
                    monteCarloInstructions += String.Format("<li>Your regular cash bonus isn't counted because you like to spend it too much</li>", 0);
                    monteCarloInstructions += String.Format("<li>As you invest, you will target stocks as being N percent with N being {0} - your age</li>", mcBatch.simParams.xMinusAgeStockPercentPreRetirement.ToString());
                    monteCarloInstructions += String.Format("<li>Once you hit retirement, you'll stop investing and you will move to a bucket strategy, keeping {0} years of your inflation-adjusted lifestyle spend in cash, {1} years in bonds, and the rest in stocks</li>", mcBatch.simParams.numYearsCashBucketInRetirement.ToString("#0.0"), mcBatch.simParams.numYearsBondBucketInRetirement.ToString("#0.0"));
                    monteCarloInstructions += String.Format("<li>Also at retirement, you'll drop your lifestyle spend to {0}% of what it was while working</li>", (mcBatch.simParams.retirementLifestyleAdjustment * 100).ToString("##.#")); 
                    monteCarloInstructions += String.Format("<li>After age {0}, you will receive {1} per month in Social Security</li>", mcBatch.simParams.socialSecurityCollectionAge.ToString("##.#"), mcBatch.simParams.monthlyNetSocialSecurityIncome.ToString("c"));
                    monteCarloInstructions += String.Format("<li>A recession is defined as when the average of the prior {0} months of market values is higher than the next {0} months and also when the market price of the simulation date is <= the market price from one year prior * {1}</li>", ConfigManager.GetInt("numMonthsToEvaluateRecession"), ConfigManager.GetDecimal("recessionPricePercentThreshold").ToString("0.00"));
                    monteCarloInstructions += String.Format("<li>We're considered still in a recession if the market price at the simulation date is <= the price at the start of the recession * {0}</li>", ConfigManager.GetDecimal("recessionRecoveryPercent"));
                    monteCarloInstructions += "<li>While we're in a recession, you'll fill your cash bucket entirely from bonds to allow your equity bucket to \"heal\"</li>";
                    monteCarloInstructions += String.Format("<li>Also while in a recession, you'll party just a little less hard, dropping your lifestyle spend to {0}% of normal times</li>", (mcBatch.simParams.recessionLifestyleAdjustment * 100).ToString("###"));
                    monteCarloInstructions += "<li>When not in a recession, you'll fill your cash bucket from equities and \"top off\" your bond bucket to desired amounts from equities</li>";
                    monteCarloInstructions += String.Format("<li>You'll also cool your jets a bit if your total equity balance drops below your retirement balance, dropping your lifestyle spend to {0}% of fat times</li>", (mcBatch.simParams.maxSpendingPercentWhenBelowRetirementLevelEquity * 100).ToString("###"));
                    monteCarloInstructions += "<li>All along the way, you'll pay taxes based on 2021 tax laws</li>";
                    if(mcBatch.simParams.shouldMoveEquitySurplussToFillBondGapAlways)
                    {
                        monteCarloInstructions += "<li>If, in retirement, you still have more in equities than you did on retirement day, and your total bonds are less that the bond bucket target, top off your bond bucket from equities</li>";

                    }
                    sbOutput.AppendLine(monteCarloInstructions);
                    sbOutput.AppendLine("</ul>");
                    sbOutput.AppendLine(svgMonteCarlo.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows the imaginary net worth by age from multiple simulation runs. Lines that make it to age 95 before reaching zero mean you outlive your money. Lines that hit zero before age 95 means you would have to tighten your belt (or die sooner). Assumptions and detailed results are printed below. </p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div class='mcanalytics'>");
                    sbOutput.AppendLine("<h3>Monte Carlo Analytics</h3>");
                    sbOutput.AppendLine(string.Format("<p>Average wealth at retirement: {0}</p>", mcBatch.averageWealthAtRetirement.ToString("c")));
                    sbOutput.AppendLine(string.Format("<p>Total runs with bankruptcy: {0}</p>", mcBatch.totalRunsWithBankruptcy.ToString("#,###")));
                    sbOutput.AppendLine(string.Format("<p>Total runs without bankruptcy: {0}</p>", mcBatch.totalRunsWithoutBankruptcy.ToString("#,###")));
                    sbOutput.AppendLine(string.Format("<p>Average age at bankruptcy: {0}</p>", mcBatch.averageAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Minimum age at bankruptcy: {0}</p>", mcBatch.minAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Max age at bankruptcy: {0} (oldest age of bankruptcy)</p>", mcBatch.maxAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average number of recessions in bankruptcy runs: {0}</p>", mcBatch.averageNumberOfRecessionsInBankruptcyRuns.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average number of recessions in non-bankruptcy runs: {0}</p>", mcBatch.averageNumberOfRecessionsInNonBankruptcyRuns.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average wealth at death: {0}</p>", mcBatch.averageWealthAtDeath.ToString("c")));
                    sbOutput.AppendLine(string.Format("<p>Success rate in \"bad\" years: {0}%</p>", (mcBatch.successRateBadYears * 100).ToString("###.00")));
                    sbOutput.AppendLine(string.Format("<p>Success rate in \"good\" years: {0}%</p>", (mcBatch.successRateGoodYears * 100).ToString("###.00")));
                    sbOutput.AppendLine("</div>");
                    Logger.info("Finished running Monte Carlo simulation");
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
            HTML.AppendLine("font-size:18px;");
            HTML.AppendLine("margin-top:0px;");
            HTML.AppendLine("}");
            HTML.AppendLine(".mcanalytics {");
            HTML.AppendLine(String.Format(" width: {0}px;", captionWidth));
            HTML.AppendLine("font-size:18px;");
            HTML.AppendLine("}");
            HTML.AppendLine(".mcanalytics h3 {");
            HTML.AppendLine("font-size:21px;");
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
