/**********************************************************************
do we have any duplicate valuations?


select 
	  count(p.id)
	, v.name
	, v.symbol
	, p.valdate
from investmenttracker.valuation p
left join investmenttracker.investmentvehicle v on p.investmentvehicle = v.id
group by 
	  v.name
	, v.symbol
	, p.valdate
having count(p.id) > 1
limit 100;

how many duplicate rows?

with duprows as (
	select 
		  count(p.id) - 1 as dupCount
		, p.investmentvehicle
		, p.valdate
	from investmenttracker.valuation p
	group by 
		  p.investmentvehicle
		, p.valdate
	having count(p.id) > 1
) 
select sum(dupcount) from dupRows;

1389 rows

**********************************************************************/




/**********************************************************************
delete the sumbitches
**********************************************************************/



DELETE FROM investmenttracker.valuation
WHERE id IN
    (SELECT id
    FROM 
        (SELECT id,
         ROW_NUMBER() OVER( PARTITION BY investmentvehicle, valdate
        ORDER BY  id ) AS row_num
        FROM investmenttracker.valuation ) v
        WHERE v.row_num > 1 );

-- Query returned successfully: 1389 rows affected, 33 msec execution time.