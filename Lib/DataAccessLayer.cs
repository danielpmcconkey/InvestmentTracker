﻿using Lib.DataTypes;
using Lib.DataTypes.Simulation;
using Lib.Engine.MonteCarlo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using CsvHelper;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lib
{
    public static class DataAccessLayer
    {
        private static string _dataDirectory;

        #region account and transactions
        static DataAccessLayer()
        {
            _dataDirectory = ConfigManager.GetString("DataDirectory");
        }

        #region JSON functions
        public static MonteCarloBatch DeserializeMonteCarloBatch(string serializedBatch)
        {
            MonteCarloBatch batch = DeserializeType<MonteCarloBatch>(serializedBatch);

            return batch;
        }
        public static T DeserializeType<T>(string serializedItem)
        {
            return JsonSerializer.Deserialize<T>(serializedItem);
        }
        public static string SerializeType<T>(T item)
        {
            return JsonSerializer.Serialize<T>(item);
        } 
        #endregion

        
        
        public static bool IsValuationInDb(Valuation v)
        {
            string q = @"
                select id, price from investmenttracker.valuation
                where investmentvehicle = @investmentvehicle
                and valdate = @valdate
                ;";

            bool isMatch = false;
            using (var conn = PostgresDAL.getConnection())
            {
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "investmentvehicle",
                        DbType = ParamDbType.Uuid,
                        Value = v.InvestmentVehicle.Id
                    });
                    cmd.AddParameter(new DbCommandParameter
                    {
                        ParameterName = "valdate",
                        DbType = ParamDbType.Timestamp,
                        Value = v.Date.DateTime
                    });
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            isMatch = true;
                            v.Id = PostgresDAL.getGuid(reader, "id");
                            v.Price = PostgresDAL.getDecimal(reader, "price");
                        }
                    }
                }
            }
            return isMatch;
        }
        public static List<Account> ReadAccountsFromDb()
        {
            List<Account> outList = new List<Account>();
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select id, name, accounttype from investmenttracker.account
					;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            Account a = new Account();
                            a.Id = PostgresDAL.getGuid(reader, "id");
                            a.Name = PostgresDAL.getString(reader, "name");
                            a.AccountType = PostgresDAL.getEnum<AccountType>(reader, "accounttype");
                            a.Transactions = ReadTransactionsFromDb(a.Id);
                            outList.Add(a);
                        }
                    }
                }
            }

            return outList;
        }
        public static Dictionary<Guid, InvestmentVehicle> ReadInvestmentVehiclesFromDb()
        {
            Dictionary<Guid, InvestmentVehicle> outList = new Dictionary<Guid, InvestmentVehicle>();
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select id, name, investmentvehicletype, symbol
                    from investmenttracker.investmentvehicle
					;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            //InvestmentVehicle v = new InvestmentVehicle();
                            var id = PostgresDAL.getGuid(reader, "id");
                            var name = PostgresDAL.getString(reader, "name");
                            var ivtype = PostgresDAL.getEnum<InvestmentVehicleType>(reader, "investmentvehicletype");
                            var symbol = PostgresDAL.getString(reader, "symbol");
                            outList.Add(id, new InvestmentVehicle(id, name, symbol, ivtype));                            
                        }
                    }
                }
            }

            return outList;
        }
        public static List<Transaction> ReadTransactionsFromDb(Guid accountId)
        {
            List<Transaction> outList = new List<Transaction>();
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select 
	                      id
	                    , transactiontype
	                    , investmentvehicle
	                    , transactiondate
	                    , quantity
	                    , cashpricetotaltransaction
                    from investmenttracker.transaction
                    where account = @accountId
					;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "accountId",
                        DbType = ParamDbType.Uuid,
                        Value = accountId
                    });
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            Transaction t = new Transaction();
                            t.Id = PostgresDAL.getGuid(reader, "id");
                            t.TransactionType = PostgresDAL.getEnum<TransactionType>(reader, "transactiontype");
                            Guid ivGuid = PostgresDAL.getGuid(reader, "investmentvehicle");
                            t.InvestmentVehicle = InvestmentVehiclesList.investmentVehicles[ivGuid];
                            t.Date = PostgresDAL.getDateTimeOffset(reader, "transactiondate");
                            t.Quantity = PostgresDAL.getDecimal(reader, "quantity");
                            t.CashPriceTotalTransaction = PostgresDAL.getDecimal(reader, "cashpricetotaltransaction");
                            outList.Add(t);
                        }
                    }
                }
            }

            return outList;
        }
        public static List<Valuation> ReadValuationsFromDb()
        {
            List<Valuation> outList = new List<Valuation>();
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select 
	                      id
	                    , investmentvehicle
	                    , valdate
	                    , price
                    from investmenttracker.valuation
					;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            Valuation v = new Valuation();
                            v.Id = PostgresDAL.getGuid(reader, "id");
                            Guid ivGuid = PostgresDAL.getGuid(reader, "investmentvehicle");
                            v.InvestmentVehicle = InvestmentVehiclesList.investmentVehicles[ivGuid];
                            v.Date = PostgresDAL.getDateTimeOffset(reader, "valdate");
                            v.Price = PostgresDAL.getDecimal(reader, "price");
                            outList.Add(v);
                        }
                    }
                }
            }

            return outList;
        }
        
        
        public static void WriteNewAccountToDb(Account a)
        {
            using (var conn = PostgresDAL.getConnection())
            {

                string qParams = @"

                    INSERT INTO investmenttracker.account(
                                id, name, accounttype)
                        VALUES (@id, @name, @accounttype);

                        ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(qParams, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "id", DbType = ParamDbType.Uuid, Value = a.Id });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "name", DbType = ParamDbType.Varchar, Value = a.Name });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "accounttype", DbType = ParamDbType.Integer, Value = (int)a.AccountType });
                    
                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("WriteAccountToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }
            // now write the transactions
            foreach(var t in a.Transactions)
            {
                WriteNewTransactionToDb(t, a.Id);
            }
        }
        public static void WriteNewInvestMentVehicleToDb(InvestmentVehicle v)
        {
            using (var conn = PostgresDAL.getConnection())
            {

                string qParams = @"

                    INSERT INTO investmenttracker.investmentvehicle(
                                id, name, investmentvehicletype, symbol)
                        VALUES (@id, @name, @investmentvehicletype, @symbol);

                        ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(qParams, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "id", DbType = ParamDbType.Uuid, Value = v.Id });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "name", DbType = ParamDbType.Varchar, Value = v.Name });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "investmentvehicletype", DbType = ParamDbType.Integer, Value = (int)v.Type });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "symbol", DbType = ParamDbType.Varchar, Value = v.Symbol });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("WriteInvestMentVehicleToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public static void WriteNewTransactionToDb(Transaction t, Guid accountId)
        {
            using (var conn = PostgresDAL.getConnection())
            {

                string qParams = @"

                    INSERT INTO investmenttracker.transaction(
                                id, account, transactiontype, investmentvehicle, transactiondate, 
                                quantity, cashpricetotaltransaction)
                        VALUES (@id, @account, @transactiontype, @investmentvehicle, @transactiondate, 
                                @quantity, @cashpricetotaltransaction);

                        ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(qParams, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "id", DbType = ParamDbType.Uuid, Value = t.Id });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "account", DbType = ParamDbType.Uuid, Value = accountId });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "transactiontype", DbType = ParamDbType.Integer, Value = (int)t.TransactionType });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "investmentvehicle", DbType = ParamDbType.Uuid, Value = t.InvestmentVehicle.Id});
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "transactiondate", DbType = ParamDbType.Timestamp, Value = t.Date.DateTime });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "quantity", DbType = ParamDbType.Numeric, Value = t.Quantity });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "cashpricetotaltransaction", DbType = ParamDbType.Numeric, Value = t.CashPriceTotalTransaction});

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("WriteAccountToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public static void WriteNewValuationToDb(Valuation v)
        {
            using (var conn = PostgresDAL.getConnection())
            {

                string qParams = @"

                    INSERT INTO investmenttracker.valuation(id, investmentvehicle, valdate, price)
                        VALUES (@id, @investmentvehicle, @valdate, @price);

                        ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(qParams, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "id", DbType = ParamDbType.Uuid, Value = v.Id });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "investmentvehicle", DbType = ParamDbType.Uuid, Value = v.InvestmentVehicle.Id });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "valdate", DbType = ParamDbType.Timestamp, Value = v.Date.DateTime });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "price", DbType = ParamDbType.Numeric, Value = v.Price });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("WriteNewValuationToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        #endregion

        #region montecarlobatch Functions
        public static List<Guid> GetAllRunIdsForMCVersion(string montecarloversion)
        {
            List<Guid> outList = new List<Guid>();
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                select runid from investmenttracker.montecarlobatch b where b.montecarloversion = @montecarloversion
                order by ((b.analytics->'medianLifeStyleSpend')::varchar(17)::numeric * (b.analytics->'successRateBadYears')::varchar(17)::numeric) desc
                ";

                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "montecarloversion",
                        DbType = ParamDbType.Varchar,
                        Value = montecarloversion
                    });
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            outList.Add(PostgresDAL.getGuid(reader, "runid"));
                        }
                    }
                }
            }
            return outList;
        }
        public static List<(Guid, MonteCarloBatch)> getTopNRunsWithRunCountLessThanY(int N, int Y, string monteCarloVersion)
        {
            List<(Guid, MonteCarloBatch)> outList = new List<(Guid, MonteCarloBatch)>();

            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    SELECT 
	                    b.runid, 
	                    b.serializedself
                    FROM investmenttracker.montecarlobatch b
                    where 1=1
                    and b.montecarloversion = @monteCarloVersion
                    and numberofsimstorun < @Y
                    order by ((b.analytics->'averageLifeStyleSpendSuccessfulBadYears')::varchar(17)::numeric * (b.analytics->'successRateBadYears')::varchar(17)::numeric) desc
                    limit(@N)
                    ;
                ";
                
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "monteCarloVersion",
                        DbType = ParamDbType.Varchar,
                        Value = monteCarloVersion
                    }
                    );
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "Y",
                        DbType = ParamDbType.Integer,
                        Value = Y
                    }
                    );
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "N",
                        DbType = ParamDbType.Integer,
                        Value = N
                    }
                    );
                    using (var reader = PostgresDAL.executeReader(cmd.npgsqlCommand))
                    {
                        while (reader.Read())
                        {
                            Guid runId = PostgresDAL.getGuid(reader, "runid");
                            var batch = DataAccessLayer.DeserializeMonteCarloBatch(
                                PostgresDAL.getString(reader, "serializedself"));
                            outList.Add((runId, batch));
                        }
                    }
                }
            }

            return outList;

        }
        public static MonteCarloBatch readAndDeserializeMCBatchFromDb(Guid runId)
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string query = @"
                    select serializedself from investmenttracker.MonteCarloBatch where runid = @runId
					;";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(query, conn))
                {
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "runId",
                        DbType = ParamDbType.Uuid,
                        Value = runId
                    }
                    );
                    object result = PostgresDAL.executeScalar(cmd.npgsqlCommand);
                    string serializedMonteCarlo = Convert.ToString(result);


                    return DataAccessLayer.DeserializeMonteCarloBatch(serializedMonteCarlo);
                }
            }
        }
        public static void updateMonteCarloBatchInDb(MonteCarloBatch batch)
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    update investmenttracker.montecarlobatch
                        set  
                        montecarloversion = @montecarloversion, 
                        rundate = @rundate, 
                        serializedself = @serializedself, 
                        numberofsimstorun = @numberofsimstorun, 
                        analytics = @analytics
                    where runid = @runid
                    ;
                    ";
                PostgresDAL.openConnection(conn);
                using (var cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = batch.runId });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "montecarloversion", DbType = ParamDbType.Varchar, Value = batch.monteCarloVersion });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "rundate", DbType = ParamDbType.Timestamp, Value = batch.runDate });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "serializedself", DbType = ParamDbType.Text, Value = batch.serializeSelf() });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "numberofsimstorun", DbType = ParamDbType.Integer, Value = batch.numberOfSimsToRun });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "analytics", DbType = ParamDbType.Json, Value = batch.analytics });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.writeSelfToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public static void updateParamsInDb(Guid runId, SimulationParameters simParams)
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    update investmenttracker.montecarlobatch
                    set
                    monthlyInvestBrokerage = @monthlyInvestBrokerage,
                    xMinusAgeStockPercentPreRetirement = @xMinusAgeStockPercentPreRetirement,
                    numYearsBondBucketInRetirement = @numYearsBondBucketInRetirement,
                    recessionRecoveryPercent = @recessionRecoveryPercent,
                    recessionLifestyleAdjustment = @recessionLifestyleAdjustment,
                    retirementDate = @retirementDate
                    where runid = @runid
                    ";
                PostgresDAL.openConnection(conn);
                using (var cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "monthlyInvestBrokerage",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.monthlyInvestBrokerage
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "xMinusAgeStockPercentPreRetirement",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.xMinusAgeStockPercentPreRetirement
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "numYearsBondBucketInRetirement",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.numYearsBondBucketInRetirement
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "recessionRecoveryPercent",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.recessionRecoveryPercent
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "recessionLifestyleAdjustment",
                        DbType = ParamDbType.Numeric,
                        Value = simParams.recessionLifestyleAdjustment
                    });
                    cmd.AddParameter(new DbCommandParameter()
                    {
                        ParameterName = "retirementDate",
                        DbType = ParamDbType.Timestamp,
                        Value = simParams.retirementDate
                    });
                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.updateParamsInDb data update returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        public static void writeMonteCarloBatchToDb(MonteCarloBatch batch)
        {
            using (var conn = PostgresDAL.getConnection())
            {
                string q = @"
                    INSERT INTO investmenttracker.montecarlobatch(
                        runid, 
                        montecarloversion, 
                        rundate, 
                        serializedself, 
                        numberofsimstorun, 
                        analytics
                    )
                    VALUES (
                        @runid, 
                        @montecarloversion, 
                        @rundate, 
                        @serializedself, 
                        @numberofsimstorun, 
                        @analytics
                    );
                    ";

                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(q, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runid", DbType = ParamDbType.Uuid, Value = batch.runId });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "montecarloversion", DbType = ParamDbType.Varchar, Value = batch.monteCarloVersion });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "rundate", DbType = ParamDbType.Timestamp, Value = batch.runDate });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "serializedself", DbType = ParamDbType.Text, Value = batch.serializeSelf() });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "numberofsimstorun", DbType = ParamDbType.Integer, Value = batch.numberOfSimsToRun });
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "analytics", DbType = ParamDbType.Json, Value = batch.analytics });

                    
                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.writeSelfToDb data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
                writeParametersToDb(batch.runId, batch.simParams);
            }

        }
        public static void writeParametersToDb(Guid runId, SimulationParameters simParams)
        {
            using (var conn = PostgresDAL.getConnection())
            {

                string qParams = @"

                INSERT INTO investmenttracker.montecarlosimparameters(
                            runid, startdate, birthdate, retirementdate, monthlygrossincomepreretirement, 
                            monthlynetsocialsecurityincome, monthlyspendlifestyletoday, monthlyspendcoretoday, 
                            monthlyinvestroth401k, monthlyinvesttraditional401k, monthlyinvestbrokerage, 
                            monthlyinvesthsa, annualrsuinvestmentpretax, xminusagestockpercentpreretirement, 
                            numyearscashbucketinretirement, numyearsbondbucketinretirement, 
                            recessionrecoverypercent, shouldmoveequitysurplusstofillbondgapalways, 
                            deathageoverride, recessionlifestyleadjustment, retirementlifestyleadjustment, 
                            maxspendingpercentwhenbelowretirementlevelequity, annualinflationlow, 
                            annualinflationhi, socialsecuritycollectionage, livinglargethreashold,
                            livinglargelifestylespendmultiplier)
                    VALUES (
                        @runId, 
                        @startDate, 
                        @birthDate, 
                        @retirementDate,
                        @monthlyGrossIncomePreRetirement ,
                        @monthlyNetSocialSecurityIncome ,
                        @monthlySpendLifeStyleToday ,
                        @monthlySpendCoreToday ,
                        @monthlyInvestRoth401k ,
                        @monthlyInvestTraditional401k ,
                        @monthlyInvestBrokerage ,
                        @monthlyInvestHSA ,
                        @annualRSUInvestmentPreTax ,
                        @xMinusAgeStockPercentPreRetirement ,
                        @numYearsCashBucketInRetirement ,
                        @numYearsBondBucketInRetirement ,
                        @recessionRecoveryPercent ,
                        @shouldMoveEquitySurplussToFillBondGapAlways ,
                        @deathAgeOverride ,
                        @recessionLifestyleAdjustment,
                        @retirementLifestyleAdjustment,
                        @maxSpendingPercentWhenBelowRetirementLevelEquity ,
                        @annualInflationLow,
                        @annualInflationHi,
                        @socialSecurityCollectionAge,
                        @livingLargeThreashold,
                        @livingLargeLifestyleSpendMultiplier
                        );

                        ";
                PostgresDAL.openConnection(conn);
                using (DbCommand cmd = new DbCommand(qParams, conn))
                {
                    cmd.AddParameter(new DbCommandParameter() { ParameterName = "runId", DbType = ParamDbType.Uuid, Value = runId });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "startDate", DbType = ParamDbType.Timestamp, Value = simParams.startDate });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "birthDate", DbType = ParamDbType.Timestamp, Value = simParams.birthDate });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "retirementDate", DbType = ParamDbType.Timestamp, Value = simParams.retirementDate });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyGrossIncomePreRetirement", DbType = ParamDbType.Numeric, Value = simParams.monthlyGrossIncomePreRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyNetSocialSecurityIncome", DbType = ParamDbType.Numeric, Value = simParams.monthlyNetSocialSecurityIncome });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlySpendLifeStyleToday", DbType = ParamDbType.Numeric, Value = simParams.monthlySpendLifeStyleToday });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlySpendCoreToday", DbType = ParamDbType.Numeric, Value = simParams.monthlySpendCoreToday });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestRoth401k", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestRoth401k });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestTraditional401k", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestTraditional401k });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestBrokerage", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestBrokerage });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "monthlyInvestHSA", DbType = ParamDbType.Numeric, Value = simParams.monthlyInvestHSA });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "annualRSUInvestmentPreTax", DbType = ParamDbType.Numeric, Value = simParams.annualRSUInvestmentPreTax });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "xMinusAgeStockPercentPreRetirement", DbType = ParamDbType.Numeric, Value = simParams.xMinusAgeStockPercentPreRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "numYearsCashBucketInRetirement", DbType = ParamDbType.Numeric, Value = simParams.numYearsCashBucketInRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "numYearsBondBucketInRetirement", DbType = ParamDbType.Numeric, Value = simParams.numYearsBondBucketInRetirement });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "recessionRecoveryPercent", DbType = ParamDbType.Numeric, Value = simParams.recessionRecoveryPercent });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "shouldMoveEquitySurplussToFillBondGapAlways", DbType = ParamDbType.Boolean, Value = simParams.shouldMoveEquitySurplussToFillBondGapAlways });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "deathAgeOverride", DbType = ParamDbType.Integer, Value = simParams.deathAgeOverride });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "recessionLifestyleAdjustment", DbType = ParamDbType.Numeric, Value = simParams.recessionLifestyleAdjustment });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "retirementLifestyleAdjustment", DbType = ParamDbType.Numeric, Value = simParams.retirementLifestyleAdjustment });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "maxSpendingPercentWhenBelowRetirementLevelEquity", DbType = ParamDbType.Numeric, Value = simParams.maxSpendingPercentWhenBelowRetirementLevelEquity });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "annualInflationLow", DbType = ParamDbType.Numeric, Value = simParams.annualInflationLow });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "annualInflationHi", DbType = ParamDbType.Numeric, Value = simParams.annualInflationHi });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "socialSecurityCollectionAge", DbType = ParamDbType.Numeric, Value = simParams.socialSecurityCollectionAge });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "livingLargeThreashold", DbType = ParamDbType.Numeric, Value = simParams.livingLargeThreashold });
                    cmd.AddParameter(new DbCommandParameter { ParameterName = "livingLargeLifestyleSpendMultiplier", DbType = ParamDbType.Numeric, Value = simParams.livingLargeLifestyleSpendMultiplier });

                    int numRowsAffected = PostgresDAL.executeNonQuery(cmd.npgsqlCommand);
                    if (numRowsAffected != 1)
                    {
                        throw new Exception(string.Format("MonteCarloBatch.writeSelfToDb (parameters) data insert returned {0} rows. Expected 1.", numRowsAffected));
                    }
                }
            }

        }
        #endregion montecarlobatch Functions
    }
}