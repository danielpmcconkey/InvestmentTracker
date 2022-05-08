
drop TABLE investmenttracker.budgetexpense;
CREATE TABLE investmenttracker.budgetexpense
(
  id serial NOT NULL primary key,
  expenseaccount varchar(50),
  transactiondate timestamp without time zone,
  description varchar(250),
  category varchar(50),
  amount numeric(10,2)
);
ALTER TABLE investmenttracker.budgetexpense
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.budgetexpense TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.budgetexpense TO postgres;
GRANT ALL ON TABLE investmenttracker.budgetexpense TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.budgetexpense TO dbbackup;
GRANT ALL ON TABLE investmenttracker.budgetexpense TO application_service;
GRANT SELECT ON TABLE investmenttracker.budgetexpense TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;


CREATE TABLE investmenttracker.budgetcategory
(
  id serial NOT NULL primary key,
  name varchar(50)
);
ALTER TABLE investmenttracker.budgetcategory
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.budgetcategory TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.budgetcategory TO postgres;
GRANT ALL ON TABLE investmenttracker.budgetcategory TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.budgetcategory TO dbbackup;
GRANT ALL ON TABLE investmenttracker.budgetcategory TO application_service;
GRANT SELECT ON TABLE investmenttracker.budgetcategory TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;

insert into investmenttracker.budgetcategory(name)values('Pets/Pet Care');
insert into investmenttracker.budgetcategory(name)values('Healthcare/Medical');
insert into investmenttracker.budgetcategory(name)values('Gasoline/Fuel');
insert into investmenttracker.budgetcategory(name)values('Entertainment');
insert into investmenttracker.budgetcategory(name)values('Rocks');
insert into investmenttracker.budgetcategory(name)values('Food');
insert into investmenttracker.budgetcategory(name)values('Insurance');
insert into investmenttracker.budgetcategory(name)values('Utilities');
insert into investmenttracker.budgetcategory(name)values('Deposits');
insert into investmenttracker.budgetcategory(name)values('Clothing/Shoes');
insert into investmenttracker.budgetcategory(name)values('Other');
insert into investmenttracker.budgetcategory(name)values('Household');
insert into investmenttracker.budgetcategory(name)values('Campaign');
insert into investmenttracker.budgetcategory(name)values('Debt Pay-Off');
insert into investmenttracker.budgetcategory(name)values('Paypal');
insert into investmenttracker.budgetcategory(name)values('Investment');
insert into investmenttracker.budgetcategory(name)values('Fidelity Card Payment');
insert into investmenttracker.budgetcategory(name)values('Amazon Card Payment');
insert into investmenttracker.budgetcategory(name)values('Travel');
insert into investmenttracker.budgetcategory(name)values('Transfers');

select * from investmenttracker.budgetcategory;