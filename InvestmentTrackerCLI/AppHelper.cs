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
                // no args to worry about
            }
            Logger.info("Feature toggles:");
            Logger.info(string.Format("     SHOULDPRINTNETWORTH set to {0}", FEATURETOGGLE.SHOULDPRINTNETWORTH));
            Logger.info(string.Format("     SHOULDRUNMONTECARLO set to {0}", FEATURETOGGLE.SHOULDRUNMONTECARLO));
        }
        internal static void Run()
        {
            Logger.info("Beginning run.");

            // ConfigManager.ReWriteSecrets(); 


            try
            {

                InvestmentVehiclesList.investmentVehicles = DataAccessLayer.ReadInvestmentVehiclesFromDb();
                List<Account> accounts = DataAccessLayer.ReadAccountsFromDb();
                List<Valuation> prices = DataAccessLayer.ReadValuationsFromDb();
                List<BudgetExpense> budgetExpenses = DataAccessLayer.ReadBudgetExpensesFromDb(
                    DateTime.Parse("2021-11-01"), DateTime.Now);

                string budgetExpensesCharts = printBudgetExpenses(budgetExpenses);
                // create a pricing engine for use in further operations                 
                PricingEngine.AddValuations(prices);
                
                
                StringBuilder sbOutput = new StringBuilder();
                double captionWidth = 0;

                if (FEATURETOGGLE.SHOULDPRINTNETWORTH)
                {
                    Logger.info("Printing net worth");

                    GraphPrefs graphPrefs = GetDefaultGraphPrefs("Total Net Worth by Investment Type");
                    LineChartPrefs lineChartPrefs = GetDefaultLineChartPrefs();

                    GraphData graphDataTotalWorth = WorthEngine.GetTotalNetWorthByType(accounts);
                    LineChart svgTotalWorth = new LineChart(graphPrefs, lineChartPrefs, graphDataTotalWorth, "NetWorthGraphAll");

                    GraphPrefs graphPrefsTaxBuckets = GetDefaultGraphPrefs("Total Net Worth by Tax Buckets");
                    GraphData graphDataTaxBuckets = WorthEngine.GetTotalNetWorthByTaxBucket(accounts);
                    LineChart svgTaxBuckets = new LineChart(graphPrefsTaxBuckets, lineChartPrefs, graphDataTaxBuckets, "TaxBucketsGraph");

                    GraphPrefs graphPrefsStocks1 = GetDefaultGraphPrefs("Individual stock worth (1 of 2)");
                    GraphPrefs graphPrefsStocks2 = GetDefaultGraphPrefs("Individual stock worth (2 of 2)");
                    GraphData graphDataStocksAll = WorthEngine.GetStockGraphData(accounts);
                    GraphData graphDataStocks1 = new GraphData();
                    graphDataStocks1.xType = graphDataStocksAll.xType;
                    graphDataStocks1.yType = graphDataStocksAll.yType;
                    graphDataStocks1.series = new List<GraphSeries>();
                    GraphData graphDataStocks2 = new GraphData();
                    graphDataStocks2.xType = graphDataStocksAll.xType;
                    graphDataStocks2.yType = graphDataStocksAll.yType;
                    graphDataStocks2.series = new List<GraphSeries>();
                    // find the median and put the top half all together
                    decimal medianVal = GetMedianYValueFromGraphSeriesList<decimal>(graphDataStocksAll.series);
                    for (int i = 0; i < graphDataStocksAll.series.Count; i++)
                    {
                        decimal currentValue = 
                            GetCurrentYValueFromGraphSeries<decimal>(graphDataStocksAll.series[i]);
                        if(currentValue >= medianVal) graphDataStocks1.AddSeries(graphDataStocksAll.series[i]);
                        else graphDataStocks2.AddSeries(graphDataStocksAll.series[i]);
                    }
                    LineChartPrefs stocklLineChartPrefs = GetDefaultLineChartPrefs();
                    stocklLineChartPrefs.xColumnLabelsTextFormat = "yyyy-MM-dd";
                    LineChart svgStocks1 = new LineChart(graphPrefsStocks1, stocklLineChartPrefs, graphDataStocks1, "StocksGraph1");
                    LineChart svgStocks2 = new LineChart(graphPrefsStocks2, stocklLineChartPrefs, graphDataStocks2, "StocksGraph2");

                    GraphPrefs graphPrefsIndividualStocksComparison = GetDefaultGraphPrefs("Individual stocks compared to S&P 500");
                    GraphData graphDataStocksComparison = WorthEngine
                        .GetSPDRComparisonGraphData(accounts, true);
                    LineChart svgStockComparison = new LineChart(
                        graphPrefsIndividualStocksComparison, stocklLineChartPrefs, 
                        graphDataStocksComparison, "IndStockCompareGraph");

                    GraphPrefs graphPrefsAllPublicComparison = GetDefaultGraphPrefs("All publicly traded investments compared to S&P 500");
                    GraphData graphDataAllPublicComparison = WorthEngine
                        .GetSPDRComparisonGraphData(accounts, false);
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
                    sbOutput.AppendLine(svgStocks1.MakeXML());
                    sbOutput.AppendLine(svgStocks2.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>These charts show your net worth in each individual stock position.</p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgStockComparison.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows your net worth in individual stocks versus what it would look like if you'd just put everything into the SPY S&P 500 ETF instead.</p>");
                    sbOutput.AppendLine("</div>");
                    sbOutput.AppendLine("<div>");
                    sbOutput.AppendLine(svgAllPublicComparison.MakeXML());
                    sbOutput.AppendLine("<p class='caption'>This chart shows your net worth in all publicly-traded investments versus what it would look like if you'd just put everything into the SPY S&P 500 ETF instead.</p>");
                    sbOutput.AppendLine("</div>");

                    sbOutput.AppendLine(budgetExpensesCharts);
                    
                    //Logger.info(Environment.NewLine + svg.xml + Environment.NewLine);
                    Logger.info("Finished printing net worth");
                }

                // create sim assets list to use for all sim runs here on out
                List<Asset> assetsGoingIn = MonteCarloHelper.CreateSimAssetsFromAccounts(accounts);
                DataAccessLayer.OverwriteSimAssets(assetsGoingIn);

                if (FEATURETOGGLE.SHOULDRUNMONTECARLO)
                {
                    
                    Logger.info("Running Monte Carlo simulation");
                    MonteCarloBatch mcBatch = MonteCarloHelper.RunMonteCarlo(assetsGoingIn);
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
                    sbOutput.AppendLine(string.Format("<p>Average wealth at retirement: {0}</p>", mcBatch.analytics.averageWealthAtRetirement.ToString("c")));
                    sbOutput.AppendLine(string.Format("<p>Total runs with bankruptcy: {0}</p>", mcBatch.analytics.totalRunsWithBankruptcy.ToString("#,###")));
                    sbOutput.AppendLine(string.Format("<p>Total runs without bankruptcy: {0}</p>", mcBatch.analytics.totalRunsWithoutBankruptcy.ToString("#,###")));
                    sbOutput.AppendLine(string.Format("<p>Average age at bankruptcy: {0}</p>", mcBatch.analytics.averageAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Minimum age at bankruptcy: {0}</p>", mcBatch.analytics.minAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average total lifestyle spend: {0}</p>", mcBatch.analytics.averageLifeStyleSpend.ToString("c")));
                    sbOutput.AppendLine(string.Format("<p>Max age at bankruptcy: {0} (oldest age of bankruptcy)</p>", mcBatch.analytics.maxAgeAtBankruptcy.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average number of recessions in bankruptcy runs: {0}</p>", mcBatch.analytics.averageNumberOfRecessionsInBankruptcyRuns.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average number of recessions in non-bankruptcy runs: {0}</p>", mcBatch.analytics.averageNumberOfRecessionsInNonBankruptcyRuns.ToString("##.00")));
                    sbOutput.AppendLine(string.Format("<p>Average wealth at death: {0}</p>", mcBatch.analytics.averageWealthAtDeath.ToString("c")));
                    sbOutput.AppendLine(string.Format("<p>Success rate in \"bad\" years: {0}%</p>", (mcBatch.analytics.successRateBadYears * 100).ToString("###.00")));
                    sbOutput.AppendLine(string.Format("<p>Success rate in \"good\" years: {0}%</p>", (mcBatch.analytics.successRateGoodYears * 100).ToString("###.00")));
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

        private static string printBudgetExpenses(List<BudgetExpense> budgetExpenses)
        {
            StringBuilder sb = new StringBuilder();

            // create months to group on
            var months = budgetExpenses
                .GroupBy(x => new { x.transactionDate.Month, x.transactionDate.Year })
                .Select(y => new { month = y.Key.Month, year = y.Key.Year })
                .OrderBy(z => z.year)
                .ThenBy(w => w.month)
                .ToList();

            // pull categories
            List<(string, string)> categories = DataAccessLayer.ReadBudgetCategoriesFromDb();

            // create output table data
            List<(string monthLabel, decimal income, decimal outgo, 
                Dictionary<string, decimal> catExpenses)>
                ouptputTableVals = new List<(string monthLabel, decimal income, decimal outgo, 
                Dictionary<string, decimal> catExpenses)>();

            foreach (var month in months)
            {
                int yearNum = month.year;
                int monthNum = month.month;
                string monthLabel = string.Format("{0}-{1}", yearNum, monthNum.ToString("0#"));
                var expensesThisMonth = budgetExpenses
                    .Where(x => x.transactionDate.Year == yearNum && x.transactionDate.Month == monthNum);

                decimal income = expensesThisMonth.Where(x => x.category == "Deposits").Sum(y => y.amount);
                decimal outgo = expensesThisMonth.Where(x => x.category != "Deposits").Sum(y => y.amount);
                Dictionary<string, decimal> catExpenses = new Dictionary<string, decimal>();
                foreach ((string, string) category in categories)
                {
                    decimal catExpense = expensesThisMonth
                        .Where(x => x.category == category.Item1).Sum(y => y.amount);
                    catExpenses.Add(category.Item1, catExpense);
                }
                ouptputTableVals.Add((monthLabel, income, outgo, catExpenses));
            }

            // create output string
            sb.AppendLine("<h1>Budget values</h1>");
            // income v outgo
            sb.AppendLine("<h3>Income versus outgo</h3>");
            sb.AppendLine("<table class=\"budgettable\">");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Month</th>");
            sb.AppendLine("<th>Income</th>");
            sb.AppendLine("<th>Outgo</th>");
            sb.AppendLine("<th>Surplus</th>");
            sb.AppendLine("</tr>");
            foreach (var row in ouptputTableVals)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine(string.Format("<td>{0}</td>", row.monthLabel));
                sb.AppendLine(string.Format("<td class=\"numval\">{0}</td>", row.income.ToString("#,##0.00")));
                sb.AppendLine(string.Format("<td class=\"numval\">{0}</td>", row.outgo.ToString("#,##0.00")));
                sb.AppendLine(string.Format("<td class=\"numval\">{0}</td>", (row.income + row.outgo).ToString("#,##0.00")));
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");

            // category spend
            sb.AppendLine("<h3>Spend by category by month</h3>");
            sb.AppendLine("<table class=\"budgettable\">");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Month</th>");
            foreach ((string, string) category in categories)
            {
                sb.AppendLine(string.Format("<th>{0}</th>", category.Item2));
            }
            sb.AppendLine("</tr>");
            foreach (var row in ouptputTableVals)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine(string.Format("<td>{0}</td>", row.monthLabel));
                foreach ((string, string) category in categories)
                {
                    sb.AppendLine(string.Format("<td class=\"numval\">{0}</td>", 
                        row.catExpenses[category.Item1].ToString("#,##0.00")));
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");



            return sb.ToString();
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
            HTML.AppendLine(".budgettable {");
            HTML.AppendLine("font-size:21px;");
            HTML.AppendLine(String.Format(" width: {0}px;", captionWidth));
            HTML.AppendLine("}");
            HTML.AppendLine(".budgettable th{");
            HTML.AppendLine("padding:5px 10px 2px 10px;");
            HTML.AppendLine("margin:0px;");
            HTML.AppendLine("font-size:12px;");
            HTML.AppendLine("font-weight:bold");
            HTML.AppendLine("}");
            HTML.AppendLine(".budgettable td{");
            HTML.AppendLine("padding:5px 10px 2px 10px;");
            HTML.AppendLine("margin:0px;");
            HTML.AppendLine("font-size:12px;");
            HTML.AppendLine("border: solid 1px");
            HTML.AppendLine("}");
            HTML.AppendLine(".budgettable .numval{");
            HTML.AppendLine("text-align:right");
            HTML.AppendLine("}");

            HTML.AppendLine(@"        </style>
                        </head> 
                    ");
            HTML.AppendLine("<body>");
            HTML.AppendLine("<h1>Dan's Wealth Tracker</h1>");
            HTML.AppendLine(string.Format("<h3>Created: {0}</h3>", DateTime.Now.ToString("MMMM dd, yyyy HH:mm")));
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
        private static T GetCurrentYValueFromGraphSeries<T>(GraphSeries s)
        {
            var latestEntry = s.data.OrderByDescending(rows => rows.x).FirstOrDefault();
            return (T)latestEntry.y;
        }
        private static T GetMedianYValueFromGraphSeriesList<T>(List<GraphSeries> l)
        {
            List<T> yValueList = new List<T>();
            foreach (GraphSeries s in l)
            {
                yValueList.Add(GetCurrentYValueFromGraphSeries<T>(s));
            }

            return MathHelper.GetMedian<T>(yValueList.OrderBy(x => x).ToList());
        }

    }
}
