using GraphLib;
using GraphLib.Utilities;
using Lib.DataTypes;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Lib.Engine
{
    public enum WorthGroupType
    {
        INDIVIDUAL_STOCKS,
        MUTUAL_FUNDS,
        PRIVATE_ASSETS,
        ALL,
    }
    public static class WorthEngine
    {
        #region public methods
        public static GraphData GetSPDRComparisonGraphData(List<Account> accounts, bool isIndidivualStocksOnly)
        {
            bool shouldLogPositions = false;
            if (shouldLogPositions)
            {
                Logger.info("Loggin' dem sumbitches!");
                Logger.info(string.Format("\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", "Date",
                            "stockSymbol", "quantity", "price per", "numSpyShares", "spyPrice", "spyPos", "spyWealth"));
            }

                // set up an SPY investment vehicle for use in adding prices
                InvestmentVehicle spyV = InvestmentVehiclesList.investmentVehicles.Where(x =>
                            x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                            && x.Value.Symbol == "SPY").FirstOrDefault().Value;


            // get the min date of all transactions
            DateTimeOffset minDate = accounts
                        .SelectMany(x => x.Transactions)
                        .Where(y => y.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                && (!y.InvestmentVehicle.IsIndexFund || !isIndidivualStocksOnly))
                        .Min(y => y.Date);

            // set up each graph series
            GraphSeries realitySeries = new GraphSeries("Individual stocks",
                TypeHelper.dateTimeOffsetType, TypeHelper.decimalType);
            realitySeries.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = ColorHelper.deeppurple,
                strokeWidthInPx = 3d
            };

            GraphSeries spySeries = new GraphSeries("SPDR S&P 500 ETF",
                TypeHelper.dateTimeOffsetType, TypeHelper.decimalType);
            spySeries.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = ColorHelper.amber,
                strokeWidthInPx = 3d
            };

            // set up a dict of positions
            Dictionary<string, decimal> positions = new Dictionary<string, decimal>();

            // cycle through mindate to present, grab each new transaction, and calculate worth for that day
            DateTimeOffset thisDate = minDate;
            DateTimeOffset todaysDate = new DateTimeOffset(
                    DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc), new TimeSpan(0, 0, 0));
            while (thisDate <= todaysDate)
            {
                // get the transactions for this date
                var transactions = accounts
                            .SelectMany(x => x.Transactions)
                            .Where(y => y.Date == thisDate
                                && y.InvestmentVehicle.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                && (!y.InvestmentVehicle.IsIndexFund || !isIndidivualStocksOnly));
                foreach (var t in transactions)
                {
                    string stockSymbol = t.InvestmentVehicle.Symbol;
                    decimal quantity = t.Quantity;
                    decimal cashValue = t.CashPriceTotalTransaction;
                    decimal spyPrice = PricingEngine.GetPriceAtDate(spyV, t.Date).Price;
                    decimal numSpyShares = cashValue / spyPrice;
                    if(t.TransactionType == TransactionType.SALE)
                    {
                        quantity *= -1;
                        numSpyShares *= -1;
                    }

                    // update the positions
                    if(positions.ContainsKey(stockSymbol))
                    {
                        positions[stockSymbol] += quantity;
                    }
                    else
                    {
                        positions.Add(stockSymbol, quantity);
                    }
                    if (positions.ContainsKey("SPY"))
                    {
                        positions["SPY"] += numSpyShares;
                    }
                    else
                    {
                        positions.Add("SPY", numSpyShares);
                    }

                    if (shouldLogPositions)
                    {
                        Logger.info(string.Format("\t{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", thisDate.ToString("yyyy-MM-dd"),
                            stockSymbol, quantity, cashValue / quantity, numSpyShares, spyPrice, positions["SPY"], spyPrice * positions["SPY"]));
                    }
                }

                decimal totalSpyWorthToday = 0;
                decimal totalOtherWorthToday = 0;

                // now calculate wealth
                foreach (var p in positions)
                {
                    decimal price = PricingEngine.GetPriceAtDate(
                        InvestmentVehiclesList.investmentVehicles.Where(x =>
                            x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                            && x.Value.Symbol == p.Key).FirstOrDefault().Value, thisDate).Price;
                    decimal totalValue = price * p.Value;
                    if (p.Key == "SPY") totalSpyWorthToday += totalValue;
                    else totalOtherWorthToday += totalValue;
                }

                realitySeries.data.Add((thisDate, totalOtherWorthToday));
                spySeries.data.Add((thisDate, totalSpyWorthToday));

                // finally, increment thisDate and loop
                thisDate = thisDate.AddDays(1);
            }


            GraphData graphData = new GraphData(TypeHelper.dateTimeOffsetType, TypeHelper.decimalType);


            

            

            graphData.AddSeries(realitySeries);
            graphData.AddSeries(spySeries);

            return graphData;
        }
        public static GraphData GetStockGraphData(List<Account> accounts)
        {
            List<Position> stockPositions = GetAllIndividualStockPositions(accounts);
            return GetGraphDataForEachSymbol(stockPositions);
        }
        public static GraphData GetTotalNetWorthByTaxBucket(List<Account> accounts)
        {
            GraphData graphData = new GraphData(
                TypeHelper.dateTimeOffsetType, TypeHelper.decimalType);

            // tax buckets are:
            //     taxable (TAXABLE_BROKERAGE, OTHER)
            //     tax deferred (TRADITIONAL_401_K, TRADITIONAL_IRA)
            //     non-taxable (ROTH_401_K, , ROTH_IRA, HSA)

            // add taxable
            var taxableAccounts = accounts.Where(x => x.AccountType == AccountType.TAXABLE_BROKERAGE
                || x.AccountType == AccountType.OTHER).ToList();
            List<Position> taxablePublicPositions = GetTaxablePositions(taxableAccounts);
            List<Position> taxablePrivatePositions = GetAllPrivatePositions(taxableAccounts);
            GraphSeries taxablePublicWorth = GetGraphSeriesFromPositions(taxablePublicPositions);
            GraphSeries taxablePrivateWorth = GetGraphSeriesFromPositions(taxablePrivatePositions);
            List<GraphSeries> taxableWorthList = new List<GraphSeries>();
            taxableWorthList.Add(taxablePublicWorth);
            taxableWorthList.Add(taxablePrivateWorth);
            GraphSeries taxableWorth = CombineGraphSeries(taxableWorthList);
            taxableWorth.name = "Fully taxable";
            taxableWorth.seriesPrefs.strokeHexColor = "0000FF";
            graphData.AddSeries(taxableWorth);

            // add tax deferred
            var taxDeferredAccounts = accounts.Where(x => x.AccountType == AccountType.TRADITIONAL_401_K
                || x.AccountType == AccountType.TRADITIONAL_IRA).ToList();
            List<Position> taxDeferredPositions = GetTaxablePositions(taxDeferredAccounts);
            GraphSeries taxDeferredWorth = GetGraphSeriesFromPositions(taxDeferredPositions);
            taxDeferredWorth.name = "Tax deferred";
            taxDeferredWorth.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = "00cc00",
                strokeWidthInPx = 3d
            };
            graphData.AddSeries(taxDeferredWorth);

            // add tax free
            var taxFreeAccounts = accounts.Where(x => x.AccountType == AccountType.ROTH_401_K
                || x.AccountType == AccountType.ROTH_IRA
                || x.AccountType == AccountType.HSA).ToList();
            List<Position> taxFreePositions = GetTaxablePositions(taxFreeAccounts);
            GraphSeries taxFreeWorth = GetGraphSeriesFromPositions(taxFreePositions);
            taxFreeWorth.name = "Tax free";
            taxFreeWorth.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = "ff0000",
                strokeWidthInPx = 3d
            };
            graphData.AddSeries(taxFreeWorth);

            // add them all together
            List<GraphSeries> combinedSeries = new List<GraphSeries>();
            combinedSeries.Add(taxableWorth);
            combinedSeries.Add(taxDeferredWorth);
            combinedSeries.Add(taxFreeWorth);
            GraphSeries totalWorth = CombineGraphSeries(combinedSeries);
            graphData.AddSeries(totalWorth);


            return graphData;
        }
        public static GraphData GetTotalNetWorthByType(List<Account> accounts)
        {
            GraphData graphData = new GraphData(
                TypeHelper.dateTimeOffsetType, TypeHelper.decimalType);


            // types are individual stocks, index funds, and private assets
            // add individual stocks
            List<Position> stockPositions = GetAllIndividualStockPositions(accounts);
            GraphSeries stocksWorth = GetGraphSeriesFromPositions(stockPositions);
            stocksWorth.name = "Individual stocks";
            stocksWorth.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = "0000FF",
                strokeWidthInPx = 3d
            };
            graphData.AddSeries(stocksWorth);

            // add index funds
            List<Position> indexPositions = GetAllIndexedPositions(accounts);
            GraphSeries indexWorth = GetGraphSeriesFromPositions(indexPositions);
            indexWorth.name = "Index funds";
            indexWorth.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = "00CC00",
                strokeWidthInPx = 3d
            };
            graphData.AddSeries(indexWorth);

            // add private assets
            List<Position> privatePositions = GetAllPrivatePositions(accounts);
            GraphSeries privateWorth = GetGraphSeriesFromPositions(privatePositions);
            privateWorth.name = "Private assets (less debt)";
            privateWorth.seriesPrefs = new SeriesPrefs()
            {
                strokeHexColor = "FF0000",
                strokeWidthInPx = 3d
            };
            graphData.AddSeries(privateWorth);

            // add them all together
            List<GraphSeries> combinedSeries = new List<GraphSeries>();
            combinedSeries.Add(stocksWorth);
            combinedSeries.Add(indexWorth);
            combinedSeries.Add(privateWorth);
            GraphSeries totalWorth = CombineGraphSeries(combinedSeries);

            graphData.AddSeries(totalWorth);

            return graphData;
        }
        #endregion public methods

        #region private methods

        private static GraphSeries CombineGraphSeries(List<GraphSeries> graphSeries)
        {
            // get min and max dates
            var minDate = DateTimeOffset.MaxValue; // set it to highest first
            var maxDate = DateTimeOffset.MinValue;

            foreach (var series in graphSeries)
            {
                var thisMin = (DateTimeOffset)series.data.Min(row => row.x);
                var thisMax = (DateTimeOffset)series.data.Max(row => row.x);
                if (thisMin < minDate) minDate = thisMin;
                if (thisMax > maxDate) maxDate = thisMax;
            }

            // then  create the shell
            GraphSeries totalWorth = new GraphSeries()
            {
                name = "Total",
                xType = TypeHelper.dateTimeOffsetType,
                yType = TypeHelper.decimalType,
                seriesPrefs = new SeriesPrefs()
                {
                    strokeHexColor = "FF00FF",
                    strokeWidthInPx = 3d
                },
                data = new List<(object x, object y)>()
            };

            // then go over each date between min and max and add up all the worth vals

            var thisDate = minDate;
            while (thisDate <= maxDate)
            {
                decimal totalWorthThisDate = 0M;
                foreach (var series in graphSeries)
                {
                    var positionsAtDate = series.data.Where(row => (DateTimeOffset)(row.x) == thisDate);
                    if (positionsAtDate.Count() > 0) totalWorthThisDate += (decimal)(positionsAtDate.First().y);
                }

                totalWorth.data.Add((thisDate, totalWorthThisDate));

                thisDate = thisDate.AddDays(1);
            }

            return totalWorth;
        }
        private static List<Position> GetTaxablePositions(List<Account> accounts)
        {

            List<InvestmentDuration> list = PricingEngine.GetPublicInvestmentDurationsFromAccounts(accounts);
            return GetPositionsFromInvestmentDurations(list, accounts);
        }
        private static List<Position> GetAllIndexedPositions(List<Account> accounts)
        {

            List<InvestmentDuration> list = PricingEngine.GetPublicInvestmentDurationsFromAccounts(accounts)
                .Where(x => x.investmentVehicle.IsIndexFund == true).ToList();

            return GetPositionsFromInvestmentDurations(list, accounts);
        }
        private static List<Position> GetAllIndividualStockPositions(List<Account> accounts)
        {

            List<InvestmentDuration> list = PricingEngine.GetPublicInvestmentDurationsFromAccounts(accounts)
                .Where(x => x.investmentVehicle.IsIndexFund == false).ToList();

            return GetPositionsFromInvestmentDurations(list, accounts);
        }
        private static List<Position> GetAllPrivatePositions(List<Account> accounts)
        {
            List<InvestmentDuration> list = PricingEngine.GetPrivateInvestmentDurationsFromAccounts(accounts);
            return GetPositionsFromInvestmentDurations(list, accounts);
        }
        private static GraphData GetGraphDataForEachSymbol(List<Position> positions)
        {
            GraphData graphData = new GraphData(
                TypeHelper.dateTimeOffsetType,
                TypeHelper.decimalType);

            var symbolGroups = positions
                .GroupBy(x => new { x.InvestmentVehicle.Symbol })
                .Select(g => new { symbol = g.Key, positionsAtSymbol = g });

            int i = 0;

            foreach (var symbolGroup in symbolGroups)
            {
                string hexColor = ColorHelper.GetColor(i);
                GraphSeries series = GetGraphSeriesFromPositions(symbolGroup.positionsAtSymbol.ToList());
                series.name = symbolGroup.symbol.Symbol.ToString();
                series.seriesPrefs.strokeHexColor = hexColor;
                series.seriesPrefs.strokeWidthInPx = 3d;
                graphData.AddSeries(series);
                i++;
            }

            return graphData;
        }
        private static GraphSeries GetGraphSeriesFromPositions(List<Position> positions)
        {
            GraphSeries graphSeries = new GraphSeries();
            graphSeries.yType = TypeHelper.decimalType;
            graphSeries.xType = TypeHelper.dateTimeOffsetType;
            Dictionary<DateTimeOffset, decimal> wealthDict = new Dictionary<DateTimeOffset, decimal>();

            foreach (var position in positions.OrderBy(x => x.Date))
            {
                if (position.Quantity != 0)
                {
                    try
                    {
                        decimal priceAtDate = PricingEngine.GetPriceAtDate(
                            position.InvestmentVehicle, position.Date).Price;
                        decimal value = priceAtDate * position.Quantity;



                        // if wealthDict contains the date already, add to it
                        if (wealthDict.ContainsKey(position.Date))
                        {
                            wealthDict[position.Date] += value;
                        }
                        // else create a new one
                        else
                        {
                            wealthDict.Add(position.Date, value);
                        }
                    }
                    catch (Exception)
                    {
                        Logger.error(string.Format("Unable to find a price for {0} at {1}",
                            position.InvestmentVehicle.Symbol, position.Date));
                        throw;
                    }
                }

            }
            // turn the dictionary into graphSeries.data
            graphSeries.data = new List<(object x, object y)>();
            foreach (var row in wealthDict)
            {
                graphSeries.data.Add((row.Key, row.Value));
            }
            return graphSeries;
        }
        private static List<Position> GetPositionsFromInvestmentDurations(
            List<InvestmentDuration> durations, List<Account> accounts)
        {
            List<Position> positions = new List<Position>();
            foreach (var row in durations)
            {

                DateTimeOffset thisDate = row.minDate;
                DateTimeOffset todaysDate = new DateTimeOffset(
                    DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc), new TimeSpan(0, 0, 0));
                while (thisDate <= todaysDate)
                {
                    var transactionsToDate = accounts
                        .SelectMany(x => x.Transactions)
                        .Where(v => v.InvestmentVehicle.Equals(row.investmentVehicle)
                            && v.Date <= thisDate);
                    // first add the purchases
                    decimal quantityToDate =
                        transactionsToDate.Where(x => x.TransactionType == TransactionType.PURCHASE)
                            .Sum(x => x.Quantity);
                    // then subtract the sales
                    quantityToDate -=
                        transactionsToDate.Where(x => x.TransactionType == TransactionType.SALE)
                            .Sum(x => x.Quantity);
                    // now create the position
                    positions.Add(new Position()
                    {
                        Date = thisDate,
                        InvestmentVehicle = row.investmentVehicle,
                        Quantity = quantityToDate
                    });
                    thisDate = thisDate.AddDays(1);
                }
            }
            return positions;
        }

        #endregion private methods

    }
}
