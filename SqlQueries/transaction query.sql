SELECT 
	--t.id, 
	t.transactiondate, 
	at.accounttype,
	a.name as account, 
	tt.transactiontype, 
	iv.name as investmentvehicle, 
	iv.symbol,
	t.quantity, 
	t.cashpricetotaltransaction
FROM investmenttracker.transaction t
left join investmenttracker.transactiontype tt
	on t.transactiontype = tt.id
left join investmenttracker.account a
	on t.account = a.id
left join investmenttracker.accounttype at
	on a.accounttype = at.id
left join investmenttracker.investmentvehicle iv
	on t.investmentvehicle = iv.id
where 1=1
and (
	-- 401 k contributions in 2021
	    at.accounttype in ('TRADITIONAL_401_K', 'ROTH_401_K')
	and t.transactiondate between '2021-01-01' and '2022-01-01'
	and tt.transactiontype <> 'SALE'
	and a.name in (
		'TD 401K Contributions'--,
		--'Ally Company Retirement Contribution',
		--'Ally Employee Elective Contribution',
		--'Ally Roth 401(k) Contribution'
		)
)
order by t.transactiondate desc
;


/*

helper queries

select * from investmenttracker.accounttype;

select * from investmenttracker.account;


'Ally Company Matching Contribution'
'Ally Discretionary Company Contribution'


*/