using Lib.DataTypes.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Lib.Engine.MontyCarlo
{
    public class Simulation
    {

        #region data
        private List<Asset> assets;
        private MarketDataSimulator marketDataSimulator;
        #endregion data

        #region sim controls
        private SimulationRunResult simulationRunResult;
        //private List<DateTime> recessions;
        private DateTime birthDate;
        private DateTime deathDate;
        private DateTime simulationRunDate;
        private DateTime rmdDate;
        private DateTime startDate;
        private DateTime retirementDate;
        //private SimulationParameters simParams;
        private bool shouldMoveEquitySurplussToFillBondGapAlways;
        private SimulationFeatureToggles featureToggles;
        #endregion sim controls

        #region sim values
        private long totalCashOnHand = 0;
        private long monthlyNetSocialSecurityIncome = 0;
        private long monthlySpendLifeStyleToday = 0;
        private long monthlyInvestRoth401k = 0;
        private long monthlyInvestTraditional401k = 0;
        private long monthlyInvestBrokerage = 0;
        private long monthlyInvestHSA = 0;
        private long annualRSUInvestment = 0;
        private decimal minBondPercentPreRetirement = 0m;
        private decimal maxBondPercentPreRetirement = 0m;
        private decimal xMinusAgeStockPercentPreRetirement = 0m;
        private decimal numYearsCashBucketInRetirement = 0;
        private decimal numYearsBondBucketInRetirement = 0;
        private long monthlyGrossIncomePreRetirement = 0;
        private long thisYearsTaxableIncomeInRetirement = 0;
        private long thisYearsCapitalGainsInRetirement = 0;
        private long equityBalanceAtRetirement = 0;
        private decimal recessionLifestyleAdjustment = 0m;
        private decimal recessionRecoveryPercent = 0m;
        const decimal stateIncomeTaxRate = 0.0525m;
        const long standardDeduction = 140500000;
        private decimal maxSpendingPercentWhenBelowRetirementLevelEquity = .9m;
        private bool _isBankrupt = false;
        #endregion sim values


        #region volatility and market history
        const decimal annualInflationLow = 0.005m;
        const decimal annualInflationHi = 0.05m;
        const int maxLifeInYears = 102;
        #endregion volatility and market history


        #region public functions
        public void init(SimulationParameters simParams, List<Asset> assetsGoingIn, SimulationFeatureToggles featureToggles)
        {
            // load parameters of the run
            //this.simParams = simParams;
            monthlySpendLifeStyleToday = convertFloatAmountToSafeCalcInt(simParams.monthlySpendLifeStyleToday);
            totalCashOnHand = 0;
            monthlyNetSocialSecurityIncome = convertFloatAmountToSafeCalcInt(simParams.monthlyNetSocialSecurityIncome);
            monthlySpendLifeStyleToday = convertFloatAmountToSafeCalcInt(simParams.monthlySpendLifeStyleToday);
            monthlyInvestRoth401k = convertFloatAmountToSafeCalcInt(simParams.monthlyInvestRoth401k);
            monthlyInvestTraditional401k = convertFloatAmountToSafeCalcInt(simParams.monthlyInvestTraditional401k);
            monthlyInvestBrokerage = convertFloatAmountToSafeCalcInt(simParams.monthlyInvestBrokerage);
            monthlyInvestHSA = convertFloatAmountToSafeCalcInt(simParams.monthlyInvestHSA);
            annualRSUInvestment = convertFloatAmountToSafeCalcInt(simParams.annualRSUInvestment);
            minBondPercentPreRetirement = simParams.minBondPercentPreRetirement;
            maxBondPercentPreRetirement = simParams.maxBondPercentPreRetirement;
            xMinusAgeStockPercentPreRetirement = simParams.xMinusAgeStockPercentPreRetirement;
            numYearsCashBucketInRetirement = simParams.numYearsCashBucketInRetirement;
            numYearsBondBucketInRetirement = simParams.numYearsBondBucketInRetirement;
            monthlyGrossIncomePreRetirement = convertFloatAmountToSafeCalcInt(simParams.monthlyGrossIncomePreRetirement);
            birthDate = simParams.birthDate;
            rmdDate = birthDate.AddYears(72);
            startDate = simParams.startDate;
            shouldMoveEquitySurplussToFillBondGapAlways = simParams.shouldMoveEquitySurplussToFillBondGapAlways;
            recessionLifestyleAdjustment = simParams.recessionLifestyleAdjustment;
            recessionRecoveryPercent = simParams.recessionRecoveryPercent;
            maxSpendingPercentWhenBelowRetirementLevelEquity = simParams.maxSpendingPercentWhenBelowRetirementLevelEquity;

            // set retirement date to the first day of the next month
            retirementDate = simParams.retirementDate;
            retirementDate = retirementDate.AddDays(1 - retirementDate.Day).AddMonths(1);


            // copy input param assets to assets list
            assets = new List<Asset>();
            foreach (Asset a in assetsGoingIn)
            {
                assets.Add(a.clone());
            }

            this.featureToggles = featureToggles;


            int deathAge = RNG.getRandomInt(65, maxLifeInYears);
            if (simParams.deathAgeOverride > 0) deathAge = simParams.deathAgeOverride;
            deathDate = birthDate.AddYears(deathAge);
            // set deathdate to the first of the next month
            deathDate = deathDate.AddDays(1 - deathDate.Day).AddMonths(1);



            // create an object to store the results that we
            // can add to along the way
            simulationRunResult = new SimulationRunResult()
            {
                startdate = startDate,
                retirementdate = retirementDate,
                deathdate = deathDate,
            };


            // create an imaginary market over the next N years
            DateTime minDateJan1 = startDate.AddDays(1 - startDate.DayOfYear).AddYears(-1);
            DateTime maxDateJan1 = deathDate.AddDays(1 - deathDate.DayOfYear).AddYears(1);
            marketDataSimulator = new MarketDataSimulator(minDateJan1, maxDateJan1);
            marketDataSimulator.createMarketHistory();
            //recessions = marketDataSimulator.recessions;
            //simulationRunResult.numberofrecessions = recessions.Count;
        }
        public SimulationRunResult run()
        {

            // start on the first day of next month
            simulationRunDate = startDate.AddDays(1 - startDate.Day).AddMonths(1);
            while (simulationRunDate <= deathDate)
            {
                // things to do everyday
                accrueInterest();

                // pre-retirement
                if (simulationRunDate < retirementDate)
                {
                    // first day of every year
                    if (simulationRunDate.Month == 1 && simulationRunDate.Day == 1)
                    {
                        // add inflation
                        decimal annualInflationPercent = RNG.getRandomDecimalWeighted(annualInflationLow, annualInflationHi);
                        updateSpendForInflation(annualInflationPercent);
                        updateInvestmentForInflation(annualInflationPercent);

                        // add RSU vesting
                        invest(annualRSUInvestment, TaxBucket.TAXABLE, null);
                        calculateNetWorth();
                    }
                    // first day of the month
                    if (simulationRunDate.Day == 1)
                    {
                        // get paid, make 401k contributions
                        if (monthlyInvestRoth401k > 0)
                            invest(monthlyInvestRoth401k, TaxBucket.TAXFREE, null);
                        if (monthlyInvestTraditional401k > 0)
                            invest(monthlyInvestTraditional401k, TaxBucket.TAXDEFERRED, rmdDate);
                        // add 401k match
                        invest(
                            Convert.ToInt64(Math.Round(monthlyGrossIncomePreRetirement * 0.06f)),
                            TaxBucket.TAXDEFERRED, rmdDate);
                        // make brokerage account investments
                        invest(monthlyInvestBrokerage, TaxBucket.TAXABLE, null);
                        // make HSA investment
                        invest(monthlyInvestHSA, TaxBucket.TAXFREE, null);
                    }
                }
                // retirement day
                if (simulationRunDate == retirementDate)
                {
                    if (_isBankrupt)
                    {
                        equityBalanceAtRetirement = 0;
                        simulationRunResult.wealthAtRetirement = 0;
                        calculateNetWorth();
                    }
                    else
                    {
                        equityBalanceAtRetirement = assets
                        .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                        .Sum(y => y.amountCurrent);

                        rebalanceBuckets();
                        calculateNetWorth();

                        simulationRunResult.wealthAtRetirement = convertLongAmountToReadableFloat(
                            simulationRunResult.netWorthSchedule
                            .OrderByDescending(x => x.dateWithinSim).First().totalNetWorth);
                    }
                    

                }

                // post-retirement

                if (simulationRunDate >= retirementDate)
                {
                    // first day of every year
                    if (simulationRunDate.Month == 1 && simulationRunDate.Day == 1)
                    {
                        // add inflation
                        decimal annualInflationPercent = RNG.getRandomDecimalWeighted(annualInflationLow, annualInflationHi);
                        updateSpendForInflation(annualInflationPercent);
                        // pay taxes
                        payTaxes();
                        // rebalance buckets
                        rebalanceBuckets();
                        // get rid of $0 balance assets
                        assets = assets.Where(x => x.amountCurrent > 0).ToList();

                        calculateNetWorth();

                    }
                    // first day of the month
                    if (simulationRunDate.Day == 1)
                    {
                        if (!_isBankrupt)
                        {
                            // get paid (Social Security)
                            totalCashOnHand += monthlyNetSocialSecurityIncome;
                            thisYearsTaxableIncomeInRetirement += monthlyNetSocialSecurityIncome;
                            // pay bills
                            payBillsInRetirement();
                        }
                    }
                    // things to do every day
                    // ??
                }





                // done processing for the day
                simulationRunDate = simulationRunDate.AddMonths(1);
            }
            // sim over, your money outlived you
            calculateNetWorth();
            if (!_isBankrupt)
            {
                simulationRunResult.wasSuccessful = true;
                simulationRunResult.bankruptcydate = null;
                simulationRunResult.wealthAtDeath = convertLongAmountToReadableFloat(simulationRunResult.netWorthSchedule
                    .OrderByDescending(x => x.dateWithinSim).First().totalNetWorth);
            }




            return simulationRunResult;
        }
        #endregion public functions

        #region sim functions
        private void accrueInterest()
        {
            if (_isBankrupt) return;
            try
            {
                decimal percentGrowthEquity = marketDataSimulator.getMovementAtDateEquity(simulationRunDate);
                decimal percentGrowthBond = marketDataSimulator.getMovementAtDateBond(simulationRunDate);


                foreach (Asset a in assets)
                {
                    if (a.investmentIndex == InvestmentIndex.EQUITY)
                    {
                        a.amountCurrent += Convert.ToInt64(Math.Round(a.amountCurrent * percentGrowthEquity, 0));
                    }
                    if (a.investmentIndex == InvestmentIndex.BOND)
                    {
                        a.amountCurrent += Convert.ToInt64(Math.Round(a.amountCurrent * percentGrowthBond, 0));
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        private void calculateNetWorth()
        {
            if (simulationRunResult.netWorthSchedule == null) simulationRunResult.netWorthSchedule = new List<NetWorth>();

            if(_isBankrupt)
            {
                simulationRunResult.netWorthSchedule.Add(new NetWorth()
                {
                    dateWithinSim = simulationRunDate,
                    totalCashOnHand = 0,
                    totalNetWorth = 0,
                    totalStocks = 0,
                    totalBonds = 0,
                });
                return;
            }
            long totalStocks = assets
                .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                .Sum(y => y.amountCurrent);

            long totalBonds = assets
                .Where(x => x.investmentIndex == InvestmentIndex.BOND)
                .Sum(y => y.amountCurrent);

            long totalNetWorth = totalCashOnHand + assets.Sum(x => x.amountCurrent);





            NetWorth worth = new NetWorth()
            {
                dateWithinSim = simulationRunDate,
                totalCashOnHand = totalCashOnHand,
                totalNetWorth = totalNetWorth,
                totalStocks = totalStocks,
                totalBonds = totalBonds,
            };
            simulationRunResult.netWorthSchedule.Add(worth);

        }
        /// <summary>
        /// debits the cash on hand and makes the amount disappear. tries to rebalance if not enough cash to cover
        /// </summary>
        /// <param name="amount"></param>
        private void payWithCash(long amount)
        {
            totalCashOnHand -= amount;
            if (totalCashOnHand < 0)
            {
                // ran out of cash on hand. Try to pull from bonds
                long amountNeededToCover = totalCashOnHand * -1;

                long totalBonds = assets
                    .Where(x => x.investmentIndex == InvestmentIndex.BOND)
                    .Sum(y => y.amountCurrent);

                if (totalBonds >= amountNeededToCover)
                {
                    drawFromInvestments(amountNeededToCover, InvestmentIndex.BOND);
                }
                else
                {
                    // try to pull from stocks instead
                    long totalStocks = assets
                        .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                        .Sum(y => y.amountCurrent);

                    if (totalStocks >= amountNeededToCover)
                    {
                        drawFromInvestments(amountNeededToCover, InvestmentIndex.EQUITY);
                    }
                    else
                    {
                        // might as well call it, you broke, kid
                        declareBankruptcy();
                    }
                }
            }
        }
        private void declareBankruptcy()
        {
            _isBankrupt = true;
            
            simulationRunResult.wasSuccessful = false;
            simulationRunResult.bankruptcydate = simulationRunDate;
            simulationRunResult.wealthAtDeath = 0;
            simulationRunResult.ageAtBankruptcy = (decimal)((simulationRunDate - birthDate).TotalDays / 365.25);

        }
        /// <summary>
        /// pull from a specific asset and add to cash on hand
        /// </summary>
        /// <param name="a"></param>
        /// <param name="amount"></param>
        private void drawFromAsset(Asset a, long amount)
        {
            if (amount > a.amountCurrent)
            {
                throw new Exception("Asked to withdraw more from an asset than it has");
            }
            long priorAmountInAsset = a.amountCurrent;
            a.amountCurrent -= amount;
            totalCashOnHand += amount;

            long profit = 0;
            if (a.amountCurrent >= a.amountContributed)
            {
                profit = amount; // it's all profit
            }
            else
            {
                // everything but the orig contribution is profit
                // how much did we dig into the contribution
                profit = priorAmountInAsset - a.amountContributed;
                if (profit <= 0) profit = 0;
                // not tracking investment losses. 
                // eating into contribution should just be tax free, not a tax write-off
            }
            if (a.taxBucket == TaxBucket.TAXABLE) thisYearsCapitalGainsInRetirement += profit;
            if (a.taxBucket == TaxBucket.TAXDEFERRED) thisYearsTaxableIncomeInRetirement += profit;
            if (a.taxBucket == TaxBucket.TAXFREE)
            {
                // enjoy the sweet life, buddy
            }
        }
        /// <summary>
        /// draw from multiple investments and add to cash on hand
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="index"></param>
        private void drawFromInvestments(long amount, InvestmentIndex index)
        {
            //decimal amountRounded = (decimal) Math.Round(amount, 2);
            if (amount < 0)
            {
                //bool burp = true;
                throw new Exception("Asked to withdraw < 0 amount from investments");
                //return;
            }
            long amountDrawn = 0;
            long amountLeft = amount;

            // tax deferred first
            List<Asset> orderedAssets = assets
                    .Where(x => x.investmentIndex == index
                        && x.taxBucket == TaxBucket.TAXDEFERRED
                        && x.amountCurrent > 0)
                    .OrderBy(y => y.created)
                    .ToList();
            // then taxable
            orderedAssets.AddRange(assets
                    .Where(x => x.investmentIndex == index
                        && x.taxBucket == TaxBucket.TAXABLE
                        && x.amountCurrent > 0)
                    .OrderBy(y => y.created)
                    .ToList());
            // then tax free
            orderedAssets.AddRange(assets
                    .Where(x => x.investmentIndex == index
                        && x.taxBucket == TaxBucket.TAXFREE
                        && x.amountCurrent > 0)
                    .OrderBy(y => y.created)
                    .ToList());

            foreach (Asset a in orderedAssets)
            {
                if (amountDrawn < amount)
                {
                    if (a.amountCurrent >= amountLeft)
                    {
                        drawFromAsset(a, amountLeft);
                        amountDrawn += amountLeft;
                        amountLeft = 0;
                        return;
                    }
                    else
                    {
                        long amountToDraw = a.amountCurrent;
                        drawFromAsset(a, amountToDraw);
                        amountDrawn += amountToDraw;
                        amountLeft -= amountToDraw;
                    }
                }
            }
        }
        private void invest(long amount, TaxBucket taxBucket, DateTime? rmdDate)
        {
            if (_isBankrupt) return;

            if (amount <= 0)
            {
                throw new Exception("Investment amount <= 0");
            }

            // what is our current equity mix?
            long totalEquityValue = assets.Where(x => x.investmentIndex == InvestmentIndex.EQUITY).Sum(y => y.amountCurrent);
            long totalBondValue = assets.Where(x => x.investmentIndex == InvestmentIndex.BOND).Sum(y => y.amountCurrent);

            // what is our target equity mix?
            long ageInYears = Convert.ToInt16(Math.Round((simulationRunDate - birthDate).TotalDays / 365.25));
            decimal targetEquityPercent = (xMinusAgeStockPercentPreRetirement - ageInYears) * 0.01m;

            // put money in an asset class that best gets us to that target mix
            long totalValueAfterInvestment = totalEquityValue + totalBondValue + amount;
            long newEquityTargetAmount = Convert.ToInt64(Math.Round(totalValueAfterInvestment * targetEquityPercent));
            long newBondTargetAmount = Convert.ToInt64(Math.Round(totalValueAfterInvestment * (1 - targetEquityPercent)));

            // possible scenarios
            // scenario 1: amount invested should go entirely into stocks (is too low to get us to the target equity %)
            if (amount + totalEquityValue <= newEquityTargetAmount)
            {
                // put it all into stocks
                assets.Add(new Asset()
                {
                    created = simulationRunDate,
                    amountContributed = amount,
                    amountCurrent = amount,
                    investmentIndex = InvestmentIndex.EQUITY,
                    taxBucket = taxBucket,
                    rmdDate = rmdDate,
                });
            }
            // scenario 2: amount invested should go entirely into bonds
            else if (amount + totalBondValue <= newBondTargetAmount)
            {
                // put it all into bonds
                assets.Add(new Asset()
                {
                    created = simulationRunDate,
                    amountContributed = amount,
                    amountCurrent = amount,
                    investmentIndex = InvestmentIndex.BOND,
                    taxBucket = taxBucket,
                    rmdDate = rmdDate,
                });
            }
            // scenario 3: amount invested should be spread out across stocks and bonds
            else
            {
                long stockPurchase = newEquityTargetAmount - totalEquityValue;
                long bondPurchase = newBondTargetAmount - totalBondValue;
                assets.Add(new Asset()
                {
                    created = simulationRunDate,
                    amountContributed = stockPurchase,
                    amountCurrent = stockPurchase,
                    investmentIndex = InvestmentIndex.EQUITY,
                    taxBucket = taxBucket,
                    rmdDate = rmdDate,
                });
                assets.Add(new Asset()
                {
                    created = simulationRunDate,
                    amountContributed = bondPurchase,
                    amountCurrent = bondPurchase,
                    investmentIndex = InvestmentIndex.BOND,
                    taxBucket = taxBucket,
                    rmdDate = rmdDate,
                });
            }

        }
        private void payBillsInRetirement()
        {
            if (_isBankrupt) return;

            long thisMonthsBills = 0;
            if (recessionLifestyleAdjustment == 1.0m)
            {
                thisMonthsBills = monthlySpendLifeStyleToday;
            }
            else
            {
                // check if in a recession
                long todaysValue = Convert.ToInt64(Math.Round(marketDataSimulator.getPriceAtDateEquity(simulationRunDate)));
                DateTime lastYear = simulationRunDate.AddYears(-1);
                long lastYearsValue = Convert.ToInt64(Math.Round(marketDataSimulator.getPriceAtDateEquity(lastYear)));

                bool isInRecession = (todaysValue < lastYearsValue) ? true : false;

                if (isInRecession)
                {
                    thisMonthsBills = Convert.ToInt64(Math.Round(
                        monthlySpendLifeStyleToday * recessionLifestyleAdjustment));
                }
                else
                {
                    thisMonthsBills = monthlySpendLifeStyleToday;
                }
            }
            // if equity has gone below retirement level, spend less
            long totalEquity = assets.Where(x => x.investmentIndex == InvestmentIndex.EQUITY).Sum(y => y.amountCurrent);
            if (totalEquity < equityBalanceAtRetirement)
            {
                long cooledJets = Convert.ToInt64(
                    Math.Round(monthlySpendLifeStyleToday * maxSpendingPercentWhenBelowRetirementLevelEquity, 0));
                if (cooledJets < thisMonthsBills) thisMonthsBills = cooledJets;
            }
            payWithCash(thisMonthsBills);
        }
        private void payTaxes()
        {
            if (_isBankrupt) return;

            // set up tax brackets
            List<(decimal rate, long from, long to)> federalIncomeTaxBrackets = new List<(decimal, long, long)>();
            federalIncomeTaxBrackets.Add((0.1m, 0, 197500000));
            federalIncomeTaxBrackets.Add((0.12m, 197500001, 802500000));
            federalIncomeTaxBrackets.Add((0.22m, 802500001, 1710500000));
            federalIncomeTaxBrackets.Add((0.24m, 1710500001, 3266000000));
            federalIncomeTaxBrackets.Add((0.32m, 3266000001, 4147000000));
            federalIncomeTaxBrackets.Add((0.35m, 4147000001, 6220500000));
            federalIncomeTaxBrackets.Add((0.37m, 6220500001, 1000000000000));

            List<(decimal rate, long from, long to)> federalCapitalGainsTaxBrackets = new List<(decimal, long, long)>();
            federalCapitalGainsTaxBrackets.Add((0.0m, 00000, 808000000));
            federalCapitalGainsTaxBrackets.Add((0.15m, 808000001, 5016000000));
            federalCapitalGainsTaxBrackets.Add((0.20m, 5016000001, 10000000000000));


            // calcualate earned income
            long earnedIncome = thisYearsTaxableIncomeInRetirement - standardDeduction;
            // pay fed tax
            foreach ((decimal rate, long from, long to) bracket in federalIncomeTaxBrackets)
            {
                long taxableAmountInThisBracket = 0;
                if (earnedIncome < bracket.from)
                {
                    // no tax in this bracket
                    taxableAmountInThisBracket = 0;
                }
                else if (earnedIncome >= bracket.from && earnedIncome >= bracket.to)
                {
                    // pay the entire amount in this bracket
                    taxableAmountInThisBracket = bracket.to - bracket.from;

                }
                else if (earnedIncome <= bracket.to)
                {
                    // this is our last bracket
                    taxableAmountInThisBracket = earnedIncome - bracket.from;
                }
                payWithCash(Convert.ToInt64(Math.Round(taxableAmountInThisBracket * bracket.rate)));
            }

            // pay state tax
            payWithCash(Convert.ToInt64(Math.Round(earnedIncome * stateIncomeTaxRate)));

            // pay capital gains tax
            // capital gains are derived by earned income + capital gains
            // if the sum of those 2 slots between the from and to values
            // then all capital gains are taxed at this bracket's rate
            // (I think)
            long amountToAssess = earnedIncome + thisYearsCapitalGainsInRetirement;
            foreach ((decimal rate, long from, long to) bracket in federalCapitalGainsTaxBrackets)
            {
                long taxableAmountInThisBracket = 0;
                if (amountToAssess >= bracket.from && amountToAssess <= bracket.to)
                {
                    taxableAmountInThisBracket = thisYearsCapitalGainsInRetirement;
                }

                payWithCash(Convert.ToInt64(Math.Round(taxableAmountInThisBracket * bracket.rate)));
            }
            // reset income and capital gains
            thisYearsTaxableIncomeInRetirement = 0;
            thisYearsCapitalGainsInRetirement = 0;
        }
        private void rebalanceBuckets()
        {
            if (_isBankrupt) return;

            long yearlySpend = monthlySpendLifeStyleToday * 12;
            long targetCashAmount = Convert.ToInt64(Math.Round(numYearsCashBucketInRetirement * yearlySpend));
            long targetBondAmount = Convert.ToInt64(Math.Round(numYearsBondBucketInRetirement * yearlySpend));
            long cashNeeded = targetCashAmount - totalCashOnHand;
            long totalEquitiesWorth = 0;
            long totalBondsWorth = 0;

            //error be here
            List<DateTime> recessionsLastDecade = marketDataSimulator.recessions.Where(x =>
                x >= simulationRunDate.AddYears(-10)
                && x <= simulationRunDate
                ).ToList();

            if (recessionsLastDecade.Count == 0)
            {
                // no recessions in the last 10 years draw cash and get buckets to their optimal settings

                // pull from equities
                totalEquitiesWorth = assets
                    .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                    .Sum(y => y.amountCurrent);
                if (totalEquitiesWorth >= cashNeeded)
                    drawFromInvestments(cashNeeded, InvestmentIndex.EQUITY);
                else
                {
                    // pull what you can from stocks and the rest from bonds
                    drawFromInvestments(totalEquitiesWorth, InvestmentIndex.EQUITY);
                    drawFromInvestments(cashNeeded - totalEquitiesWorth, InvestmentIndex.BOND);
                }

                topOffBondBucket(targetBondAmount);
            }
            else
            {
                // if market has completely recovered from any recessions in the last decade, pull cash from equities

                bool hasRecovered = true;   // keep this true until you find it false
                long marketNow = Convert.ToInt64(Math.Round(marketDataSimulator.getPriceAtDateEquity(simulationRunDate)));
                foreach (DateTime r in recessionsLastDecade)
                {
                    long marketAtRecessionStart = Convert.ToInt64(Math.Round(marketDataSimulator.getPriceAtDateEquity(r)));
                    if (marketNow <= Convert.ToInt64(Math.Round(marketAtRecessionStart * recessionRecoveryPercent)))
                    {
                        hasRecovered = false;
                    }
                }
                if (!hasRecovered)
                {
                    // pull from bonds, give equity more time to heal
                    drawFromInvestments(cashNeeded, InvestmentIndex.BOND);
                }
                else
                {
                    // pull from equity, but only replenish one year of bonds
                    totalEquitiesWorth = assets
                        .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                        .Sum(y => y.amountCurrent);
                    if (totalEquitiesWorth >= cashNeeded)
                        drawFromInvestments(cashNeeded, InvestmentIndex.EQUITY);
                    else
                    {
                        // pull what you can from stocks and the rest from bonds
                        drawFromInvestments(totalEquitiesWorth, InvestmentIndex.EQUITY);
                        drawFromInvestments(cashNeeded - totalEquitiesWorth, InvestmentIndex.BOND);
                    }
                    // now top off bonds
                    totalBondsWorth = assets
                        .Where(x => x.investmentIndex == InvestmentIndex.BOND)
                        .Sum(y => y.amountCurrent);
                    // add one year of spend back to total bonds
                    topOffBondBucket(totalBondsWorth + yearlySpend);
                }
            }
            if (shouldMoveEquitySurplussToFillBondGapAlways)
            {
                // after all is said and done, if I still have more in my equities than I did on retirement day 1
                // and I have less that the bond target, move it anyway, regardless of recession
                totalEquitiesWorth = assets
                        .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                        .Sum(y => y.amountCurrent);
                totalBondsWorth = assets
                        .Where(x => x.investmentIndex == InvestmentIndex.BOND)
                        .Sum(y => y.amountCurrent);
                if (totalBondsWorth < targetBondAmount && totalEquitiesWorth > equityBalanceAtRetirement)
                {
                    long diffBetweenBondTargetAndBondActual = targetBondAmount - totalBondsWorth;
                    long equitySurpluss = totalEquitiesWorth - equityBalanceAtRetirement;
                    if (equitySurpluss <= diffBetweenBondTargetAndBondActual)
                    {
                        // move all surpluss over
                        topOffBondBucket(equitySurpluss);
                    }
                    else
                    {
                        // move only the diff over
                        topOffBondBucket(diffBetweenBondTargetAndBondActual);
                    }
                }
            }
        }
        private void reclassInvestments(long amount, InvestmentIndex from, InvestmentIndex to, bool shouldMoveTaxable = true)
        {
            long amountMoved = 0;

            // order of movement:
            //      tax deferred
            //      taxable
            //      tax free

            List<Asset> assetsToMove = assets
                    .Where(x => x.investmentIndex == from && x.taxBucket == TaxBucket.TAXDEFERRED)
                    .OrderBy(y => y.amountCurrent)
                    .ToList();
            if (shouldMoveTaxable)
            {
                assetsToMove.AddRange(assets
                    .Where(x => x.investmentIndex == from && x.taxBucket == TaxBucket.TAXABLE)
                    .OrderBy(y => y.amountCurrent)
                    .ToList());
            }
            assetsToMove.AddRange(assets
                    .Where(x => x.investmentIndex == from && x.taxBucket == TaxBucket.TAXFREE)
                    .OrderBy(y => y.amountCurrent)
                    .ToList());

            foreach (Asset a in assetsToMove)
            {
                if (amountMoved <= amount)
                {
                    if (a.taxBucket != TaxBucket.TAXABLE)
                    {
                        // no tax consequences to moving things around in an IRA
                        a.investmentIndex = to;
                        amountMoved += a.amountCurrent;
                    }
                    else
                    {
                        // sell the from, buy the to
                        long amountToMove = a.amountCurrent;
                        drawFromAsset(a, a.amountCurrent);
                        // now buy the new asset in the to class
                        totalCashOnHand -= amountToMove;
                        assets.Add(new Asset()
                        {
                            created = simulationRunDate,
                            amountContributed = amountToMove,
                            amountCurrent = amountToMove,
                            investmentIndex = to,
                            taxBucket = a.taxBucket,
                            rmdDate = null,
                        });

                        amountMoved += amountToMove;
                    }

                }
            }
        }
        private void topOffBondBucket(long targetBondAmount)
        {
            long totalEquitiesWorth = assets
                .Where(x => x.investmentIndex == InvestmentIndex.EQUITY)
                .Sum(y => y.amountCurrent);
            long totalBondsWorth = assets
                .Where(x => x.investmentIndex == InvestmentIndex.BOND)
                .Sum(y => y.amountCurrent);
            long amountNeededToAddToBonds = targetBondAmount - totalBondsWorth;

            if (amountNeededToAddToBonds >= 0)
            {
                if (totalEquitiesWorth >= amountNeededToAddToBonds)
                {
                    // move everything you need
                    reclassInvestments(amountNeededToAddToBonds, InvestmentIndex.EQUITY, InvestmentIndex.BOND);
                }
                else
                {
                    // move everything you got
                    reclassInvestments(totalEquitiesWorth, InvestmentIndex.EQUITY, InvestmentIndex.BOND);
                }
            }
            else
            {
                // bonds are already more than target.
                // rebalance to equity if no tax hit
                reclassInvestments(amountNeededToAddToBonds * -1, InvestmentIndex.BOND, InvestmentIndex.EQUITY, false);
            }
        }
        /// <summary>
        /// update the monthlyInvestBrokerage amount once per year
        /// </summary>
        private void updateInvestmentForInflation(decimal annualInflationPercent)
        {
            if (_isBankrupt) return;
            monthlyInvestBrokerage += Convert.ToInt64(Math.Round(monthlyInvestBrokerage * annualInflationPercent));
            annualRSUInvestment += Convert.ToInt64(Math.Round(annualRSUInvestment * annualInflationPercent));
            monthlyGrossIncomePreRetirement += Convert.ToInt64(Math.Round(
                monthlyGrossIncomePreRetirement * annualInflationPercent));
        }
        /// <summary>
        /// update the monthlySpendLifeStyleToday amount once per year
        /// </summary>
        private void updateSpendForInflation(decimal annualInflationPercent)
        {
            if (_isBankrupt) return;
            monthlySpendLifeStyleToday += Convert.ToInt64(Math.Round(monthlySpendLifeStyleToday * annualInflationPercent));
        }
        #endregion sim functions


        #region utility functions

        private void logMessage(string message)
        {
            if (featureToggles.shouldLog)
            {
                Logger.info(message);
            }
        }
        public long convertFloatAmountToSafeCalcInt(decimal inVal)
        {
            const long safeCalcCurrencyConversion = 10000;
            decimal calcVal = inVal * safeCalcCurrencyConversion;
            long outVal = Convert.ToInt64(Math.Round(calcVal, 0));
            return outVal;
        }
        public decimal convertLongAmountToReadableFloat(long inVal)
        {
            const decimal safeCalcCurrencyConversion = 10000m;
            decimal calcVal = inVal / safeCalcCurrencyConversion;
            decimal outVal = Math.Round(calcVal, 2);
            return outVal;
        }

        #endregion utility functions


    }
}
