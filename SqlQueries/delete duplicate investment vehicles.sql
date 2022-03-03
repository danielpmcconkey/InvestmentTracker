
/*******************************************************************
step 0. do you have duplicates?
*******************************************************************/

select * 
from investmenttracker.investmentvehicle


select * 
from investmenttracker.investmentvehicle
WHERE id IN
    (SELECT id
    FROM 
        (SELECT id,
         ROW_NUMBER() OVER( PARTITION BY symbol, name, investmentvehicletype
        ORDER BY  id ) AS row_num
        FROM investmenttracker.investmentvehicle ) v
        WHERE v.row_num > 1 );

-- 228 rows


/*******************************************************************
step 1. create a temporary table of the first vehicles
*******************************************************************/

create temporary table keepers as (
	select *     
	--delete 
	FROM investmenttracker.investmentvehicle
	WHERE id IN
	    (SELECT id
	    FROM 
		(
		SELECT id,
		 ROW_NUMBER() OVER( PARTITION BY investmentvehicle.symbol --investmentvehicle.namee, investmentvehicle.symbol, investmentvehicle.investmentvehicletype
		ORDER BY  id ) AS row_num
		FROM investmenttracker.investmentvehicle 
		) t
		WHERE t.row_num = 1 
	)
	order by symbol
);

-- Query returned successfully: 35 rows affected, 16 msec execution time.




/*******************************************************************
step 2. update transactions to only use the investment vehicles we
want to keep
*******************************************************************/

/*
this query shows you which transactions use a vehicle that isn't in the keepers list

select 
	  t.id
	, tt.transactiontype
	, keepers.id as keeperIVGuid
	, t.investmentvehicle as tIVGuid
	, keepers.name
	, keepers.symbol
	, keepers.investmentvehicletype
	, t.quantity
	, t.cashpricetotaltransaction
from investmenttracker.transaction t
left join investmenttracker.transactiontype tt on t.transactiontype = tt.id
left join investmenttracker.investmentvehicle v on t.investmentvehicle = v.id
left join keepers on v.symbol = keepers.symbol and v.name = keepers.name and v.investmentvehicletype = keepers.investmentvehicletype
where keepers.id <> t.investmentvehicle
;
*/


--40 rows need changing

UPDATE investmenttracker.transaction
SET investmentvehicle = subquery.keeperIVGuid

FROM (
			select 
				  t.id as transactionId
				, tt.transactiontype
				, keepers.id as keeperIVGuid
				, t.investmentvehicle as tIVGuid
				, keepers.name
				, keepers.symbol
				, keepers.investmentvehicletype
				, t.quantity
				, t.cashpricetotaltransaction
			from investmenttracker.transaction t
			left join investmenttracker.transactiontype tt on t.transactiontype = tt.id
			left join investmenttracker.investmentvehicle v on t.investmentvehicle = v.id
			left join keepers on v.symbol = keepers.symbol and v.name = keepers.name and v.investmentvehicletype = keepers.investmentvehicletype
			where keepers.id <> t.investmentvehicle
      ) AS subquery
WHERE id = subquery.transactionId

--Query returned successfully: 40 rows affected, 18 msec execution time.

/*
run this same query again to make sure the count is 0

select 
	  t.id
	, tt.transactiontype
	, keepers.id as keeperIVGuid
	, t.investmentvehicle as tIVGuid
	, keepers.name
	, keepers.symbol
	, keepers.investmentvehicletype
	, t.quantity
	, t.cashpricetotaltransaction
from investmenttracker.transaction t
left join investmenttracker.transactiontype tt on t.transactiontype = tt.id
left join investmenttracker.investmentvehicle v on t.investmentvehicle = v.id
left join keepers on v.symbol = keepers.symbol and v.name = keepers.name and v.investmentvehicletype = keepers.investmentvehicletype
where keepers.id <> t.investmentvehicle
;
*/


/*******************************************************************
step 3. update valuations to only use the investment vehicles we
want to keep
*******************************************************************/

/*
this query shows you which valuations use a vehicle that isn't in the keepers list

select 
	  p.id
	, v.name
	, v.symbol
	, v.id as valuationVehicleGuid
	, keepers.id as keeperIVGuid
	, v.investmentvehicletype
	, p.valdate
	, p.price
from investmenttracker.valuation p
left join investmenttracker.investmentvehicle v on p.investmentvehicle = v.id
left join keepers on v.symbol = keepers.symbol and v.name = keepers.name and v.investmentvehicletype = keepers.investmentvehicletype
where keepers.id <> p.investmentvehicle
order by v.symbol, p.valdate desc
;
-- 2509 rows need to be changed

select count(*) from investmenttracker.valuation
22430
*/



UPDATE investmenttracker.valuation
SET investmentvehicle = subquery.keeperIVGuid

FROM (
			select 
				  p.id as valuationId
				, v.name
				, v.symbol
				, v.id as valuationVehicleGuid
				, keepers.id as keeperIVGuid
				, v.investmentvehicletype
				, p.valdate
				, p.price
			from investmenttracker.valuation p
			left join investmenttracker.investmentvehicle v on p.investmentvehicle = v.id
			left join keepers on v.symbol = keepers.symbol and v.name = keepers.name and v.investmentvehicletype = keepers.investmentvehicletype
			where keepers.id <> p.investmentvehicle
      ) AS subquery
WHERE id = subquery.valuationId

-- Query returned successfully: 2509 rows affected, 54 msec execution time.

/*
run this same query again to make sure the count is 0

select 
	  p.id
	, v.name
	, v.symbol
	, v.id as valuationVehicleGuid
	, keepers.id as keeperIVGuid
	, v.investmentvehicletype
	, p.valdate
	, p.price
from investmenttracker.valuation p
left join investmenttracker.investmentvehicle v on p.investmentvehicle = v.id
left join keepers on v.symbol = keepers.symbol and v.name = keepers.name and v.investmentvehicletype = keepers.investmentvehicletype
where keepers.id <> p.investmentvehicle
order by v.symbol, p.valdate desc
;
*/


/*******************************************************************
step 4. delete the extraneous vehicle rows
*******************************************************************/

/*
run this query to show you which investmentvehicle records should be kept

select * 
from investmenttracker.investmentvehicle v
left join keepers k on v.id = k.id
where k.id is not null

-- 35 rows

run this query to show you which investmentvehicle records should be deleted

select * 
from investmenttracker.investmentvehicle v
left join keepers k on v.id = k.id
where k.id is null

-- 228 rows

*/
delete from investmenttracker.investmentvehicle
where id in
(
	select v.id 
	from investmenttracker.investmentvehicle v
	left join keepers k on v.id = k.id
	where k.id is null
);

-- Query returned successfully: 228 rows affected, 214 msec execution time.


/*
run this query to make sure you don't have any duplicate vehicles

select 
	  count(v.id)
	, v.name
	, v.symbol
	, v.investmentvehicletype
from investmenttracker.investmentvehicle v
group by 
	  v.name
	, v.symbol
	, v.investmentvehicletype
having count(v.id) > 1
limit 100;


*/


/*******************************************************************
step 5. clean-up
*******************************************************************/


drop table keepers;