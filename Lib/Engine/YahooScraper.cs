using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Lib.DataTypes;
using Utilities;

namespace Lib.Engine
{
    public static class YahooScraper
    {
        private static DateTime _lastScrapeTime;
        private static TimeSpan _minTimeBetweenScrapes;

        static YahooScraper()
        {
            // set the last scrape time to five minutes ago
            _lastScrapeTime = DateTime.Now.AddMinutes(-5);
            _minTimeBetweenScrapes = ConfigManager.GetTimeSpan("MinTimeSpanBetweenYahooScrapes");
        }
        public static List<Valuation> GetHistoryPrices(
            string symbol, DateTimeOffset beginRange, DateTimeOffset endRange, int retries = 0)
        {
            Logger.info(string.Format("Scraping history prices for {0} from {1} to {2}.", symbol, beginRange, endRange));
            List<Valuation> prices = new List<Valuation>();

            // yahoo needs timestamps 
            long beginTimestamp = beginRange.ToUnixTimeSeconds();
            long endTimestamp = endRange.ToUnixTimeSeconds();

            // form the URL
            string urlWithVariables = "https://finance.yahoo.com/quote/{0}/history?";
            //urlWithVariables += "period1={1}&period2={2}&interval=1mo&filter=history&frequency=1mo&includeAdjustedClose=true";
            urlWithVariables += "period1={1}&period2={2}&interval=1d&filter=history&frequency=1mo&includeAdjustedClose=true";
            string url = string.Format(urlWithVariables, symbol, beginTimestamp, endTimestamp);

            // make sure we're not blitzing Yahoo
            WaitForNecessaryCoolDown();

            try
            {
                // scrape the page
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);

                _lastScrapeTime = DateTime.Now;

                // grab the security name while we're here
                var h1Nodes = doc.DocumentNode.SelectNodes("//h1");
                string securityName = h1Nodes[0].InnerText;

                // grab the current price
                var finStreamerNodes = doc.DocumentNode.SelectNodes("//fin-streamer [@class='Fw(b) Fz(36px) Mb(-4px) D(ib)']");
                //< fin - streamer class="Fw(b) Fz(36px) Mb(-4px) D(ib)" data-symbol="VSMAX" data-test="qsp-price" data-field="regularMarketPrice" data-trend="none" data-pricehint="2" value="97.18" active="">97.18</fin-streamer>
                string todaysPriceString = finStreamerNodes[0].InnerText;
                Valuation valToday = new Valuation(
                    InvestmentVehiclesList.investmentVehicles.Where(x =>
                            x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                            && x.Value.Symbol == symbol).FirstOrDefault().Value,
                    new DateTimeOffset(
                    DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc),
                    new TimeSpan(0, 0, 0)),
                    Decimal.Parse(todaysPriceString)
                    );


                prices.Add(valToday);

                // now grab the table
                var rows = doc.DocumentNode.SelectNodes("//*[@id='Col1-1-HistoricalDataTable-Proxy'] / section / div[2] / table / tbody / tr");
                if(rows == null)
                {
                    Logger.warn("No data rows found on page. Skipping.");
                    return new List<Valuation>(); ;
                }
                foreach (var row in rows)
                {
                    if (row.ChildNodes.Count > 2)
                    {
                        // dividend rows only have 2 TDs
                        var dateSpan = row.SelectSingleNode("td[1] / span");
                        var openSpan = row.SelectSingleNode("td[2] / span");
                        decimal priceAtDate = 0;
                        if(openSpan == null)
                        {
                            if(row.SelectSingleNode("td[2]").InnerHtml == "-")
                            {
                                Logger.error(string.Format("Skipping {0} price pull. InnerHTML is \"-\"", symbol));
                            }
                            else Logger.error(string.Format("Skipping {0} price pull. Value is null", symbol));
                        }
                        else if (Decimal.TryParse(openSpan.InnerText, out priceAtDate))
                        {
                            // basically skip it. Index funds won't have vaules until 
                            // close of business. If you run on a first of the month
                            // before COB, you'll get "-" in Yahoo results
                            Valuation val = new Valuation(
                                InvestmentVehiclesList.investmentVehicles.Where(x =>
                                    x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                    && x.Value.Symbol == symbol).FirstOrDefault().Value,
                                new DateTimeOffset(DateTime.Parse(dateSpan.InnerText).Date,
                                    new TimeSpan(0, 0, 0)),
                                priceAtDate
                                );

                            prices.Add(val);
                        }
                        else
                        {
                            Logger.error(string.Format("Skipping {0} price pull. Price value won't parse", symbol));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                int dbMaxRetries = ConfigManager.GetInt("dbMaxRetries");
                Logger.info(string.Format("Exception hit. Retries = {0}. dbMaxRetries = {1}", retries, dbMaxRetries));
                if(retries < dbMaxRetries)
                {
                    Logger.warn("Exception thrown in YahooScraper.GetHistoryPrices. Re-trying.");
                    GetHistoryPrices(symbol, beginRange, endRange, retries + 1);
                }
                // skip this one
                Logger.error(string.Format("Skipping {0} price pull. Retries exceeded", symbol));
            }
            return prices;
        }
        public static List<Valuation> GetHistoryPricesDaily(string symbol, int retries = 0)
        {
            Logger.info(string.Format("Scraping history prices daily for {0}", symbol));
            List<Valuation> prices = new List<Valuation>();

            // form the URL
            string urlWithVariables = "https://finance.yahoo.com/quote/{0}/history?p={0}";
            string url = string.Format(urlWithVariables, symbol);

            // make sure we're not blitzing Yahoo
            WaitForNecessaryCoolDown();

            try
            {
                // scrape the page
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(url);

                _lastScrapeTime = DateTime.Now;

                // grab the security name while we're here
                var h1Nodes = doc.DocumentNode.SelectNodes("//h1");
                string securityName = h1Nodes[0].InnerText;

                // grab the current price
                var finStreamerNodes = doc.DocumentNode.SelectNodes("//fin-streamer [@class='Fw(b) Fz(36px) Mb(-4px) D(ib)']");
                //< fin - streamer class="Fw(b) Fz(36px) Mb(-4px) D(ib)" data-symbol="VSMAX" data-test="qsp-price" data-field="regularMarketPrice" data-trend="none" data-pricehint="2" value="97.18" active="">97.18</fin-streamer>
                string todaysPriceString = finStreamerNodes[0].InnerText;
                Valuation valToday = new Valuation(
                    InvestmentVehiclesList.investmentVehicles.Where(x =>
                            x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                            && x.Value.Symbol == symbol).FirstOrDefault().Value,
                    new DateTimeOffset(
                    DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc),
                    new TimeSpan(0, 0, 0)),
                    Decimal.Parse(todaysPriceString)
                    );


                prices.Add(valToday);

                // now grab the table
                var rows = doc.DocumentNode.SelectNodes("//*[@id='Col1-1-HistoricalDataTable-Proxy'] / section / div[2] / table / tbody / tr");
                if (rows == null)
                {
                    Logger.warn("No data rows found on page. Skipping.");
                    return new List<Valuation>(); ;
                }
                foreach (var row in rows)
                {
                    if (row.ChildNodes.Count > 2)
                    {
                        // dividend rows only have 2 TDs
                        var dateSpan = row.SelectSingleNode("td[1] / span");
                        var openSpan = row.SelectSingleNode("td[2] / span");
                        var closeSpan = row.SelectSingleNode("td[4] / span");
                        decimal priceAtDate = 0;
                        if (closeSpan == null)
                        {
                            if (row.SelectSingleNode("td[4]").InnerHtml == "-")
                            {
                                Logger.error(string.Format("Skipping {0} price pull. InnerHTML is \"-\"", symbol));
                            }
                            else Logger.error(string.Format("Skipping {0} price pull. Value is null", symbol));
                        }
                        else if (Decimal.TryParse(closeSpan.InnerText, out priceAtDate))
                        {
                            // basically skip it. Index funds won't have vaules until 
                            // close of business. If you run on a first of the month
                            // before COB, you'll get "-" in Yahoo results
                            Valuation val = new Valuation(
                                InvestmentVehiclesList.investmentVehicles.Where(x =>
                                    x.Value.Type == InvestmentVehicleType.PUBLICLY_TRADED
                                    && x.Value.Symbol == symbol).FirstOrDefault().Value,
                                new DateTimeOffset(DateTime.Parse(dateSpan.InnerText).Date,
                                    new TimeSpan(0, 0, 0)),
                                priceAtDate
                                );

                            prices.Add(val);
                        }
                        else
                        {
                            Logger.error(string.Format("Skipping {0} price pull. Price value won't parse", symbol));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                int dbMaxRetries = ConfigManager.GetInt("dbMaxRetries");
                Logger.info(string.Format("Exception hit. Retries = {0}. dbMaxRetries = {1}", retries, dbMaxRetries));
                if (retries < dbMaxRetries)
                {
                    Logger.warn("Exception thrown in YahooScraper.GetHistoryPrices. Re-trying.");
                    GetHistoryPricesDaily(symbol, retries + 1);
                }
                // skip this one
                Logger.error(string.Format("Skipping {0} price pull. Retries exceeded", symbol));
            }
            return prices;
        }

        private static void WaitForNecessaryCoolDown()
        {
            var nowTime = DateTime.Now;
            TimeSpan distance = nowTime - _lastScrapeTime;
            if (distance < _minTimeBetweenScrapes)
            {
                double randomAdd = (double)DateTime.Now.Millisecond / (double)500;
                TimeSpan sleepTime = _minTimeBetweenScrapes - distance;
                double sleepSeconds = sleepTime.TotalSeconds + randomAdd;
                Logger.info(string.Format("Sleeping for {0} seconds to keep Yahoo from getting angry.",
                   sleepSeconds));
                int sleepMiliseconds = (int)(Math.Round(sleepSeconds * 1000,0));
                System.Threading.Thread.Sleep(sleepMiliseconds);
            }
        }
    }
}
