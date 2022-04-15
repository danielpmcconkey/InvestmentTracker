update investmenttracker.config
set value = '' -- don't commit to git
where name = 'PrimaryResidenceMortgageBalance';


update investmenttracker.config
set value = '3500' -- don't commit to git
where name = 'monthlySpendCoreToday';


update investmenttracker.config
set value = '' -- don't commit to git
where name = 'monthlySpendLifeStyleToday';



select * from investmenttracker.config