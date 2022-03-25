﻿with maxdates as (
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
		, ib.id as investmentbucketid
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
		, ib.id 
) , worthbybucket as (
	select 
		  wbv.investmentbucket
		, sum(wbv.totalvalue) as totalvalue
		, ib.target as buckettarget
	from worthbyvehicle wbv
	left join investmenttracker.investmentbucket ib on wbv.investmentbucketid  = ib.id
	group by 
		  wbv.investmentbucket
		, ib.target
) , totalworthnotinnabucket as (
	select sum(totalvalue) as sumtotal from worthbybucket where investmentbucket <> 'NA'
)
select 
	  wbb.investmentbucket
	, cast(tw.sumtotal as numeric(10, 2)) as totalworthnotinnabucket
	, cast(tw.sumtotal * wbb.buckettarget as numeric(10, 2)) as targetvalue
	, cast(wbb.totalvalue as numeric(10, 2)) as actualvalue
	, cast(100 * wbb.buckettarget as numeric(10, 2)) as targetpercent
	, cast(100 * (wbb.totalvalue / tw.sumtotal) as numeric(10, 2)) as actualpercent
	, cast((tw.sumtotal * wbb.buckettarget) - wbb.totalvalue as numeric(10, 2)) as amounttobuy
from worthbybucket wbb
cross join totalworthnotinnabucket tw
where wbb.investmentbucket <> 'NA'
order by wbb.buckettarget desc