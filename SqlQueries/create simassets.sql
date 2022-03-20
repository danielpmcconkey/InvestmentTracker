-- Table: investmenttracker.investmentvehicle

-- DROP TABLE investmenttracker.investmentvehicle;

CREATE TABLE investmenttracker.simassets
(
  id serial NOT NULL primary key,
  serializedself json
);
ALTER TABLE investmenttracker.simassets
  OWNER TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.simassets TO mcduck_app;
GRANT ALL ON TABLE investmenttracker.simassets TO postgres;
GRANT ALL ON TABLE investmenttracker.simassets TO mcduck_app_dev;
GRANT ALL ON TABLE investmenttracker.simassets TO dbbackup;
GRANT ALL ON TABLE investmenttracker.simassets TO application_service;
GRANT SELECT ON TABLE investmenttracker.simassets TO dbreadonly;

grant usage on all sequences in schema investmenttracker TO mcduck_app;
grant usage on all sequences in schema investmenttracker TO mcduck_app_dev;
