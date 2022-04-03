

CREATE TABLE investmenttracker.consumerpriceindex
(
  id serial NOT NULL primary key,
  year int,
  month int,
  indexval numeric(10,2),
  growthrate numeric(6,4)
);
ALTER TABLE investmenttracker.consumerpriceindex
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.consumerpriceindex TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.consumerpriceindex TO postgres;
GRANT ALL ON TABLE investmenttracker.consumerpriceindex TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.consumerpriceindex TO dbbackup;
GRANT ALL ON TABLE investmenttracker.consumerpriceindex TO application_service;
GRANT SELECT ON TABLE investmenttracker.consumerpriceindex TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;


