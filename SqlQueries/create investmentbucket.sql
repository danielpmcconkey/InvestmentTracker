
create table investmenttracker.investmentbucket (
id serial primary key,
name varchar(25),
target numeric(4,3)
);

ALTER TABLE investmenttracker.investmentbucket
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.investmentbucket TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.investmentbucket TO postgres;
GRANT ALL ON TABLE investmenttracker.investmentbucket TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.investmentbucket TO dbbackup;
GRANT ALL ON TABLE investmenttracker.investmentbucket TO application_service;
GRANT SELECT ON TABLE investmenttracker.investmentbucket TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;


insert into investmenttracker.investmentbucket (name,target) values('Total market',	0.6);
insert into investmenttracker.investmentbucket (name,target) values('International',	0.1);
insert into investmenttracker.investmentbucket (name,target) values('Small cap',	0.2);
insert into investmenttracker.investmentbucket (name,target) values('Emerging market',	0.05);
insert into investmenttracker.investmentbucket (name,target) values('Real estate',	0.05);
insert into investmenttracker.investmentbucket (name,target) values('N/A',	0.0);



select * from investmenttracker.investmentbucket;