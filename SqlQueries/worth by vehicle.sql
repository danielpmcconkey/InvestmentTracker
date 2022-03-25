
--,,,,,,,,,

with maxdates as (
	select 
		iv.id as investmentvehicle
		, iv.name 
		, max(v.valdate) maxdate
	from investmenttracker.investmentvehicle iv
	left join investmenttracker.valuation v on 
	iv.id = v.investmentvehicle
	group by 
		iv.id
		, iv.name 
) , worthbyvehicle as (
	select 
		  iv.id
		, iv.name 
		, ivt.investmentvehicletype
		, iv.symbol
		, iv.isindexfund
		, ib.name as investmentbucket 
		, sum(case when tt.transactiontype = 'PURCHASE' then t.quantity when tt.transactiontype = 'SALE' then t.quantity * -1 end) as numberofshares
		, ib.target as investmentbuckettarget
		, md.maxdate as latestvaluationdate
		, v.price as pricepershare 
		, v.price *  sum(case when tt.transactiontype = 'PURCHASE' then t.quantity when tt.transactiontype = 'SALE' then t.quantity * -1 end) as totalvalue
	from investmenttracker.investmentvehicle iv
	left join investmenttracker.investmentvehicletype ivt on iv.investmentvehicletype = ivt.id
	left join investmenttracker.investmentbucket ib on iv.investmentbucket = ib.id
	left join investmenttracker.transaction t on iv.id = t.investmentvehicle
	left join investmenttracker.transactiontype tt on t.transactiontype = tt.id 
	left join maxdates md on iv.id = md.investmentvehicle
	left join investmenttracker.valuation v on iv.id = v.investmentvehicle and md.maxdate = v.valdate
	group by
		  iv.id
		, iv.name 
		, ivt.investmentvehicletype
		, iv.symbol
		, iv.isindexfund
		, ib.name
		, ib.target
		, md.maxdate
		, v.price
)
select * from worthbyvehicle

