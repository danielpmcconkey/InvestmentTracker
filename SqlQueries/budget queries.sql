
/***********************************************
fix category queries
***********************************************/
update investmenttracker.budgetexpense set category = 'Entertainment' where category = 'Electronics' and description = 'Apple';
update investmenttracker.budgetexpense set category = 'Pets/Pet Care' where category = 'pets' and expenseaccount = 'Amazon';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'SECU Checking' and description = 'Audible';
update investmenttracker.budgetexpense set category = 'Healthcare/Medical' where category = 'healthcare' and expenseaccount = 'Amazon';
update investmenttracker.budgetexpense set category = 'Clothing/Shoes' where category = 'clothing' and expenseaccount = 'Amazon';
update investmenttracker.budgetexpense set category = 'Food' where category = 'Groceries' and expenseaccount = 'Fidelity Credit Card';
update investmenttracker.budgetexpense set category = 'Household' where category = 'Home Improvement' ;
update investmenttracker.budgetexpense set category = 'Campaign' where category = 'campaign' ;
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'SECU Checking' and category = 'ATM/Cash';
update investmenttracker.budgetexpense set category = 'Household' where category = 'Home Maintenance'  and expenseaccount = 'Fidelity Credit Card';
update investmenttracker.budgetexpense set category = 'Campaign' where id = 198;
update investmenttracker.budgetexpense set category = 'Household' where category = 'household'  and expenseaccount = 'Amazon';
update investmenttracker.budgetexpense set category = 'Pets/Pet Care' where category = 'Pets' and expenseaccount = 'Fidelity Credit Card';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'SECU Checking' and description = 'Glam Bag';
update investmenttracker.budgetexpense set category = 'Household' where category = 'Personal Care'  and expenseaccount = 'Fidelity Credit Card';
update investmenttracker.budgetexpense set category = 'Debt Pay-Off' where expenseaccount = 'SECU Checking' and category = 'Mortgages';
update investmenttracker.budgetexpense set category = 'Food' where category = 'Restaurants' and expenseaccount = 'Fidelity Credit Card';
update investmenttracker.budgetexpense set category = 'Insurance' where expenseaccount = 'SECU Checking' and description = 'Lifelock';
update investmenttracker.budgetexpense set category = 'Insurance' where expenseaccount = 'SECU Checking' and description = 'Norton Antivirus';--formerly lifelock
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'SECU Checking' and description = 'Wix.com';
update investmenttracker.budgetexpense set category = 'Utilities' where id = 38;
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'SECU Checking' and description = 'I B I S Inc';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'SECU Checking' and description = 'Point Of Sale Debit L340 Date X3-05 Google *ibis Inc G Co/helppay';
update investmenttracker.budgetexpense set category = 'Household' where id = 220;--'Google One'
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'SECU Checking' and description = 'Dividend Earned';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'SECU Checking' and category = 'Service Charges/Fees';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and category = 'Service Charges/Fees';
update investmenttracker.budgetexpense set category = 'Household' where category = 'gift';
update investmenttracker.budgetexpense set category = 'Investment' where expenseaccount = 'SECU Checking' and description = 'Fidelity Investments' and category = 'Transfers';
update investmenttracker.budgetexpense set category = 'Investment' where expenseaccount = 'SECU Checking' and description = 'Fidelity Investments' and category = 'Securities Trades';
update investmenttracker.budgetexpense set category = 'Rocks' where expenseaccount = 'Fidelity Credit Card' and description = 'Paypal *rknminerals Xxx-x35-7' and category = 'Transfers';
update investmenttracker.budgetexpense set category = 'Food' where description = ' indian Trail Abc Board Indian Trail Nc' and expenseaccount = 'Fidelity Credit Card';
update investmenttracker.budgetexpense set category = 'Household' where category = 'Postage & Shipping' and description = 'Usps'; 
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Usps';
update investmenttracker.budgetexpense set category = 'Fidelity Card Payment' where category = 'Credit Card Payments' and expenseaccount = 'SECU Checking' and description = 'Cardmember Services';
update investmenttracker.budgetexpense set category = 'Amazon Card Payment' where expenseaccount = 'SECU Checking' and description = 'Amazon';
update investmenttracker.budgetexpense set category = 'Fidelity Card Payment' where category = 'Credit Card Payments' and expenseaccount = 'Fidelity Credit Card' and description = 'Payment Thank You';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'Amazon' and category = 'entertainment';
update investmenttracker.budgetexpense set category = 'Food' where expenseaccount = 'Amazon' and category = 'food';
update investmenttracker.budgetexpense set category = 'Utilities' where expenseaccount = 'SECU Checking' and description = 'Sprint';
update investmenttracker.budgetexpense set category = 'Utilities' where expenseaccount = 'Fidelity Credit Card' and description = 'Sprint';
update investmenttracker.budgetexpense set category = 'Utilities' where expenseaccount = 'Fidelity Credit Card' and description = 'Google Fi';
update investmenttracker.budgetexpense set category = 'Debt Pay-Off' where expenseaccount = 'SECU Checking' and description = 'Funds Transfer Loan Payments Funds Transfer To Xxxxxx9001';
update investmenttracker.budgetexpense set category = 'Debt Pay-Off' where expenseaccount = 'SECU Checking' and description = 'Clearbalance';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Ach Debit Ars Brothers Air Hvac/plumb Xxxxxxxxxxx0114';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Ach Debit Ars Brothers Air Hvac/plumb Xxxxxxxxxxx7924';
update investmenttracker.budgetexpense set category = 'Amazon Card Payment' where expenseaccount = 'Amazon' and description = 'Automatic Payment - Thank You';
update investmenttracker.budgetexpense set category = 'Amazon Card Payment' where expenseaccount = 'Amazon' and description = 'AUTOMATIC PAYMENT - THANK YOU';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Amazon' and description = 'Amazon Pay';
update investmenttracker.budgetexpense set category = 'Rocks' where expenseaccount = 'Amazon' and category = 'rocks';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'Amazon' and description = 'Kindle Unlimited';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'Fidelity Credit Card' and category = 'Hobbies';
update investmenttracker.budgetexpense set category = 'Entertainment' where expenseaccount = 'Fidelity Credit Card' and description = 'Airgas';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Point Of Sale Debit L340 Date X5-12 Sq *danas Place Indian Trail';--dana
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Sq *brookstonebang Indian Tr';--dana
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Sq *brookstonebangs Indian Tr';--dana
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Sq *brookstonebang-s Indian Tr';--dana
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Sq *brookstonebang- Indian Tr';--dana
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Sq *danas Place Indian Tr';--dana
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Big Lots';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Champions';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Cost Plus World Market';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Dollar Tree';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Ebay';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Etsy';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Target';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Target';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Tuesday Morning';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Office Depot';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Fidelity Credit Card' and description = 'Walmart';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Walmart';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Dollar General';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Ebay';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Etsy';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Paypal *jademariebo Xxx-x35-7';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Paypal *laurelmount Xxx-x35-7';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Paypal *stitchedpix Xxx-x35-7';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Paypal *thdailydeal Xxx-x35-7';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Popshelf';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'SECU Checking' and description = 'Popshelf';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Pp*american Heart Xxx-x96-4';
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'Fidelity Credit Card' and description = 'Shein.com Us.shein.';
update investmenttracker.budgetexpense set category = 'Pets/Pet Care' where expenseaccount = 'Fidelity Credit Card' and description = 'Reptile Basics Inc Xxx-x08-5';
update investmenttracker.budgetexpense set category = 'Utilities' where expenseaccount = 'Fidelity Credit Card' and description = 'Union County';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description like 'Ach Debit Ars Brothers Air Hvac/plumb Xxxxxxxxxxx%' and amount = -20.27;
update investmenttracker.budgetexpense set category = 'Other' where expenseaccount = 'SECU Checking' and description = 'Ally Bank';
update investmenttracker.budgetexpense set category = 'Food' where expenseaccount = 'SECU Checking' and category = 'Restaurants';
update investmenttracker.budgetexpense set category = 'Food' where expenseaccount = 'SECU Checking' and category = 'Groceries';
update investmenttracker.budgetexpense set category = 'Debt Pay-Off' where expenseaccount = 'SECU Checking' and category = 'Credit Card Payments';
update investmenttracker.budgetexpense set category = 'Utilities' where expenseaccount = 'SECU Checking' and description = 'Google Fi';
update investmenttracker.budgetexpense set category = 'Deposits' where expenseaccount = 'SECU Checking' and category = 'Paychecks/Salary';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'SECU Checking' and description = 'Michaels';
update investmenttracker.budgetexpense set category = 'Utilities' where id in( 1600,1293,1204,1097,978,861,761,680,600 ); 

-- ones you want to do after all the others
update investmenttracker.budgetexpense set category = 'Paypal' where description = 'Paypal';
update investmenttracker.budgetexpense set category = 'Household' where expenseaccount = 'Amazon' and description = 'Amazon Marketplace';
update investmenttracker.budgetexpense set category = 'Household' where id = 1704; -- furnace purchase
--update investmenttracker.budgetexpense set category = 'Household' where id =1288;
--update investmenttracker.budgetexpense set category = 'Other' where id = 936; 
--update investmenttracker.budgetexpense set category = 'Other' where id in( 408,1221,1183,1158,915,894,805 ); 
--update investmenttracker.budgetexpense set category = 'Gasoline/Fuel' where id = 1048;
--update investmenttracker.budgetexpense set category = 'Travel' where id = 1816;
--update investmenttracker.budgetexpense set category = 'Entertainment' where id = 1069;
--update investmenttracker.budgetexpense set category = 'Food' where id = 864;


/***********************************************
wrong category query
***********************************************/
select 
	e.*
	,c.*
from investmenttracker.budgetexpense e
left join investmenttracker.budgetcategory c on e.category = c.name
where c.name is null
order by expenseaccount, description
;

/***********************************************
individual category select queries
***********************************************/
select * from investmenttracker.budgetexpense where category = 'Pets/Pet Care'
select * from investmenttracker.budgetexpense where category = 'Healthcare/Medical'
select * from investmenttracker.budgetexpense where category = 'Gasoline/Fuel'
select * from investmenttracker.budgetexpense where category = 'Entertainment'
select * from investmenttracker.budgetexpense where category = 'Rocks'
select * from investmenttracker.budgetexpense where category = 'Food'
select * from investmenttracker.budgetexpense where category = 'Insurance'
select * from investmenttracker.budgetexpense where category = 'Utilities'
select * from investmenttracker.budgetexpense where category = 'Deposits'
select * from investmenttracker.budgetexpense where category = 'Clothing/Shoes'
select * from investmenttracker.budgetexpense where category = 'Other'
select * from investmenttracker.budgetexpense where category = 'Household'
select * from investmenttracker.budgetexpense where category = 'Campaign'
select * from investmenttracker.budgetexpense where category = 'Debt Pay-Off'
select * from investmenttracker.budgetexpense where category = 'Paypal'
select * from investmenttracker.budgetexpense where category = 'Investment'
select * from investmenttracker.budgetexpense where category = 'Fidelity Card Payment'
select * from investmenttracker.budgetexpense where category = 'Amazon Card Payment'
select * from investmenttracker.budgetexpense where category = 'Travel'
select * from investmenttracker.budgetexpense where category = 'Transfers'

--select * from investmenttracker.budgetexpense where amount > 0

/***********************************************
spend by category query
***********************************************/
with transactions_plus_month as (
	select 
		e.category,
		e.amount,
		concat(trim(TO_CHAR(e.transactiondate, 'Month')),', ', cast(date_part('year',e.transactiondate) as char(4))) as tran_month,
		(date_part('year',e.transactiondate) * 12) + (date_part('month',e.transactiondate)) as sort_month
	from investmenttracker.budgetexpense e
	where e.transactiondate >= '2021-11-01'
)
select 
	c.name
	, t.tran_month
	,sum(t.amount)
from investmenttracker.budgetcategory c
left join transactions_plus_month t on t.category = c.name
group by c.name, t.tran_month,t.sort_month
order by c.name,t.sort_month
;


/***********************************************
income vs outgo
***********************************************/
with transactions_plus_month as (
	select 
		e.category,
		e.amount,
		concat(trim(TO_CHAR(e.transactiondate, 'Month')),', ', cast(date_part('year',e.transactiondate) as char(4))) as tran_month,
		(date_part('year',e.transactiondate) * 12) + (date_part('month',e.transactiondate)) as sort_month
	from investmenttracker.budgetexpense e
	where e.transactiondate >= '2021-11-01'
), summary_data as (
	select 
		'Income' as Type,
		t.tran_month,
		sort_month,
		sum(t.amount)
	from transactions_plus_month t
	where t.category = 'Deposits'
	group by t.tran_month,sort_month
	union all
	select 
		'Outgo' as Type,
		t.tran_month,
		sort_month,
		sum(t.amount)
	from transactions_plus_month t
	where t.category <> 'Deposits'
	group by t.tran_month,sort_month
)
select 
	s1.tran_month, 
	s1.sum as total_income,
	s2.sum as total_outgo,
	s1.sum + s2.sum as surplus
from summary_data s1
left join summary_data s2 on s1.sort_month = s2.sort_month and s2.type = 'Outgo'
where s1.type = 'Income'
order by s1.sort_month

-- need to move audible to fido card
