SELECT 
	b.runid, 
	b.montecarloversion, 
	b.rundate, 
	b.numberofsimstorun,
	--analytics
        (b.analytics->'medianLifeStyleSpend')::varchar(17)::numeric(14,2) as medianLifeStyleSpend,
        (b.analytics->'averageLifeStyleSpendBadYears')::varchar(17)::numeric(14,2) as averageLifeStyleSpendBadYears,
        (b.analytics->'averageLifeStyleSpendSuccessfulBadYears')::varchar(17)::numeric(14,2) as averageLifeStyleSpendSuccessfulBadYears,
        (b.analytics->'successRateBadYears')::varchar(17)::numeric(4,3) as successRateBadYears,
        (b.analytics->'bottom10PercentLifeStyleSpend')::varchar(17)::numeric(14,2) as bottom10PercentLifeStyleSpend,
        (b.analytics->'averageLifeStyleSpend')::varchar(17)::numeric(14,2) as averageLifeStyleSpend,
        (b.analytics->'averageWealthAtRetirement')::varchar(17)::numeric(14,2) as averageWealthAtRetirement,
        (b.analytics->'averageWealthAtDeath')::varchar(17)::numeric(14,2) as averageWealthAtDeath,
        (b.analytics->'successRateGoodYears')::varchar(17)::numeric(4,3) as successRateGoodYears,
        (b.analytics->'successRateOverall')::varchar(17)::numeric(4,3) as successRateOverall,
        (b.analytics->'bankruptcyAge90Percent')::varchar(17)::numeric as bankruptcyAge90Percent,
        (b.analytics->'bankruptcyAge95Percent')::varchar(17)::numeric as bankruptcyAge95Percent,
        (b.analytics->'bankruptcyAge99Percent')::varchar(17)::numeric as bankruptcyAge99Percent,
        (b.analytics->'maxAgeAtBankruptcy')::varchar(17)::numeric as maxAgeAtBankruptcy,
        (b.analytics->'minAgeAtBankruptcy')::varchar(17)::numeric as minAgeAtBankruptcy,
        (b.analytics->'averageNumberOfRecessionsInBankruptcyRuns')::varchar(17)::numeric as averageNumberOfRecessionsInBankruptcyRuns,
        (b.analytics->'averageNumberOfRecessionsInNonBankruptcyRuns')::varchar(17)::numeric as averageNumberOfRecessionsInNonBankruptcyRuns,
        (b.analytics->'wealthAtDeath90Percent')::varchar(17)::numeric(14,2) as wealthAtDeath90Percent,
        (b.analytics->'wealthAtDeath95Percent')::varchar(17)::numeric(14,2) as wealthAtDeath95Percent,
        (b.analytics->'totalRunsWithBankruptcy')::varchar(17)::numeric as totalRunsWithBankruptcy,
        (b.analytics->'totalRunsWithoutBankruptcy')::varchar(17)::numeric as totalRunsWithoutBankruptcy,
        (b.analytics->'averageAgeAtBankruptcy')::varchar(17)::numeric as averageAgeAtBankruptcy,
	-- parameters
	p.retirementdate,
        p.monthlySpendLifeStyleToday,
        p.monthlySpendCoreToday,
        p.xMinusAgeStockPercentPreRetirement,
        p.numYearsCashBucketInRetirement ,
        p.numYearsBondBucketInRetirement ,
        p.recessionRecoveryPercent ,
        p.shouldMoveEquitySurplussToFillBondGapAlways,
        p.recessionLifestyleAdjustment,
        p.retirementLifestyleAdjustment,
	p.livingLargeThreashold,
	p.livingLargeLifestyleSpendMultiplier,
	p.monthlyInvestRoth401k,
	p.monthlyInvestTraditional401k,
	p.monthlyInvestBrokerage,
	p.monthlyInvestHSA,
	p.annualRSUInvestmentPreTax,
	p.deathAgeOverride,
	p.maxSpendingPercentWhenBelowRetirementLevelEquity,
	b.numberOfSimsToRun,
	p.annualInflationLow,
	p.annualInflationHi,
	p.socialSecurityCollectionAge
FROM investmenttracker.montecarlobatch b
left join investmenttracker.montecarlosimparameters p on b.runid = p.runid
where 1=1
and b.montecarloversion = '2022.02.23.014'
--and b.runid = '4f59d491-45b4-402f-b082-87131175177c'
--and numberofsimstorun > 1000
and numberofsimstorun < 1100
--and rundate > '2022-03-01 00:00'
order by ((b.analytics->'averageLifeStyleSpendSuccessfulBadYears')::varchar(17)::numeric * (b.analytics->'successRateBadYears')::varchar(17)::numeric) desc
--order by rundate desc
limit 100
;
