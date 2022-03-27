
create table investmenttracker.config (
id serial primary key,
name varchar(100),
value varchar(100)
);

ALTER TABLE investmenttracker.config
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.config TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.config TO postgres;
GRANT ALL ON TABLE investmenttracker.config TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.config TO dbbackup;
GRANT ALL ON TABLE investmenttracker.config TO application_service;
GRANT SELECT ON TABLE investmenttracker.config TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;


insert into investmenttracker.config (name,value) values('annualInflationHi', '0.075');
insert into investmenttracker.config (name,value) values('annualInflationLow', '0.01');
insert into investmenttracker.config (name,value) values('clutchSleepCheck', '0.00:05:00');
insert into investmenttracker.config (name,value) values('DataDirectory', 'E:/InvestmentTracker/Data/');
insert into investmenttracker.config (name,value) values('dbMaxRetries', '5');
insert into investmenttracker.config (name,value) values('dbRetrySleepMilliseconds', '1000');
insert into investmenttracker.config (name,value) values('deathAgeOverride', '95');
insert into investmenttracker.config (name,value) values('FidelityTransactionsFile', 'forwardTransactions/Accounts_History-1.csv');
insert into investmenttracker.config (name,value) values('HealthEquityTransactionsFile', 'forwardTransactions/InvestmentReport_All.csv');
insert into investmenttracker.config (name,value) values('livingLargeLifestyleSpendMultiplier', '2.380');
insert into investmenttracker.config (name,value) values('livingLargeThreashold', '2.864');
insert into investmenttracker.config (name,value) values('maxSpendingPercentWhenBelowRetirementLevelEquity', '0.769');
insert into investmenttracker.config (name,value) values('MinTimeSpanBetweenYahooScrapes', '0.00:00:02');
insert into investmenttracker.config (name,value) values('monthlySpendCoreToday', '4000');
insert into investmenttracker.config (name,value) values('numberOfSimsToRun', '1000');
insert into investmenttracker.config (name,value) values('numMonteCarloBatchesToRun', '50');
insert into investmenttracker.config (name,value) values('numMonthsToEvaluateRecession', '3');
insert into investmenttracker.config (name,value) values('numYearsBondBucketInRetirement', '0.97');
insert into investmenttracker.config (name,value) values('numYearsCashBucketInRetirement', '0.87');
insert into investmenttracker.config (name,value) values('recessionLifestyleAdjustment', '0.001');
insert into investmenttracker.config (name,value) values('recessionPricePercentThreshold', '0.9');
insert into investmenttracker.config (name,value) values('recessionRecoveryPercent', '1.22');
insert into investmenttracker.config (name,value) values('retirementLifestyleAdjustment', '0.693');
insert into investmenttracker.config (name,value) values('shouldMoveEquitySurplussToFillBondGapAlways', 'true');
insert into investmenttracker.config (name,value) values('socialSecurityCollectionAge', '72.0');
insert into investmenttracker.config (name,value) values('TimeSpanForDateNearnessEvaluation', '3.00:00:00');
insert into investmenttracker.config (name,value) values('TRowePriceTransactionsFile', 'forwardTransactions/acc_history_details.csv');
insert into investmenttracker.config (name,value) values('xMinusAgeStockPercentPreRetirement', '280.787');


select * from investmenttracker.config;