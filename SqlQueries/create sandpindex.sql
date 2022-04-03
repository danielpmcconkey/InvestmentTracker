

CREATE TABLE investmenttracker.sandpindex
(
  id serial NOT NULL primary key,
  year int,
  month int,
  indexval numeric(10,2),
  growthrate numeric(6,4)
);
ALTER TABLE investmenttracker.sandpindex
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.sandpindex TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.sandpindex TO postgres;
GRANT ALL ON TABLE investmenttracker.sandpindex TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.sandpindex TO dbbackup;
GRANT ALL ON TABLE investmenttracker.sandpindex TO application_service;
GRANT SELECT ON TABLE investmenttracker.sandpindex TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;


