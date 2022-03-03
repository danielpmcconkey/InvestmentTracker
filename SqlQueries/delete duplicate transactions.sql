/**********************************************************************
do we have any duplicate transactions?


select 
	  count(*)
	, a.name
	, at.accounttype
	, tt.transactiontype
	, v.name
	, v.symbol
	, v.investmentvehicletype
	, t.quantity
	, t.cashpricetotaltransaction
	, t.transactiondate
	, v.id
	
from investmenttracker.transaction t
left join investmenttracker.account a on t.account = a.id
left join investmenttracker.accounttype at on a.accounttype = at.id
left join investmenttracker.transactiontype tt on t.transactiontype = tt.id
left join investmenttracker.investmentvehicle v on t.investmentvehicle = v.id
left join investmenttracker.investmentvehicletype vt on v.investmentvehicletype = vt.id
group by 
	a.name
	, at.accounttype
	, tt.transactiontype
	, v.name
	, v.symbol
	, v.investmentvehicletype
	, t.quantity
	, t.cashpricetotaltransaction
	, t.transactiondate
	, v.id
having count(*) > 1
;
**********************************************************************/
-- looks like 9 extra 

/**********************************************************************
delete the sumbitches
**********************************************************************/



DELETE FROM investmenttracker.transaction
WHERE id IN
    (SELECT id
    FROM 
        (SELECT id,
         ROW_NUMBER() OVER( PARTITION BY account, transactiontype, investmentvehicle, transactiondate, quantity, cashpricetotaltransaction
        ORDER BY  id ) AS row_num
        FROM investmenttracker.transaction ) t
        WHERE t.row_num > 1 );

-- Query returned successfully: 9 rows affected, 18 msec execution time.