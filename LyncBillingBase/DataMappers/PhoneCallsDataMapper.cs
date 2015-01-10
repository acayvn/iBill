﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

using DALDotNet;
using DALDotNet.DataAccess;
using LyncBillingBase.DataModels;

namespace LyncBillingBase.DataMappers
{
    public class PhoneCallsDataMapper : DataAccess<PhoneCall>
    {
        /***
         * Get the phone calls tables list from the MonitoringServersInfo table
         */
        private DataAccess<MonitoringServerInfo> _monitoringServersInfoDataMapper = new DataAccess<MonitoringServerInfo>();

        /**
         * The SQL Queries Generator
         */
        private SQLQueries.PhoneCallsSQL PHONECALLS_SQL_QUERIES = new SQLQueries.PhoneCallsSQL();

        /***
         * The list of phone calls tables
         */
        private List<string> DBTables = new List<string>();


        public PhoneCallsDataMapper() : base()
        {
            DBTables = _monitoringServersInfoDataMapper.GetAll().Select<MonitoringServerInfo, string>(item => item.PhoneCallsTable).ToList<string>();
        }


        public override int Insert(PhoneCall phoneCallObject, string dataSourceName = null, GLOBALS.DataSource.Type dataSource = GLOBALS.DataSource.Type.Default)
        {
            string finalDataSourceName = string.Empty;

            // NULL object check
            if(null == phoneCallObject)
            {
                throw new Exception("PhoneCalls#Insert: Cannot insert NULL phone call objects.");
            }

            // NULL check on the DataSource Name
            if (false == string.IsNullOrEmpty(dataSourceName))
            {
                finalDataSourceName = dataSourceName;
            }
            else
            {
                throw new Exception("PhoneCalls#Insert: Empty DataSource name. Couldn't insert phone call object.");
            }

            // Perform data insert
            try
            {
                return base.Insert(phoneCallObject, finalDataSourceName, dataSource);
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }


        public override bool Update(PhoneCall phoneCallObject, string dataSourceName = null, GLOBALS.DataSource.Type dataSource = GLOBALS.DataSource.Type.Default)
        {
            string finalDataSourceName = string.Empty;

            // NULL object check
            if (phoneCallObject == null)
            {
                throw new Exception("PhoneCalls#Update: Cannot update NULL phone call objects.");
            }

            // Decide on the value of the DataSource name
            if (false == string.IsNullOrEmpty(dataSourceName))
            {
                finalDataSourceName = dataSourceName;
            }
            else if (false == string.IsNullOrEmpty(phoneCallObject.PhoneCallsTableName))
            {
                finalDataSourceName = phoneCallObject.PhoneCallsTableName;
            }
            else
            {
                throw new Exception("PhoneCalls#Update: Both the DataSource name and the phoneCallObject.PhoneCallsTableName are NULL.");
            }

            // Perform data update 
            try
            { 
                return base.Update(phoneCallObject, finalDataSourceName, dataSource);
            }
            catch(Exception ex)
            {
                throw ex.InnerException;
            }
        }


        public override bool Delete(PhoneCall phoneCallObject, string dataSourceName = null, GLOBALS.DataSource.Type dataSource = GLOBALS.DataSource.Type.Default)
        {
            string finalDataSourceName = string.Empty;

            // NULL object check
            if(null == phoneCallObject)
            {
                throw new Exception("PhoneCalls#Delete: Cannot delete NULL phone call objects.");
            }

            // Decide on the value of the DataSource name
            if(false == string.IsNullOrEmpty(dataSourceName))
            {
                finalDataSourceName = dataSourceName;
            }
            else if(false == string.IsNullOrEmpty(phoneCallObject.PhoneCallsTableName))
            {
                finalDataSourceName = phoneCallObject.PhoneCallsTableName;
            }
            else
            {
                throw new Exception("PhoneCalls#Delete: Both the DataSource name and the phoneCallObject.PhoneCallsTableName are NULL.");
            }

            // Perform data delete
            try
            { 
                return base.Delete(phoneCallObject, dataSourceName, dataSource);
            }
            catch(Exception ex)
            {
                throw ex.InnerException;
            }
        }


        public IEnumerable<PhoneCall> GetChargableCallsPerUser(string sipAccount) 
        {
            string sqlStatemnet = PHONECALLS_SQL_QUERIES.ChargableCallsPerUser(DBTables, sipAccount);

            return base.GetAll(sqlStatemnet);
        }


        public IEnumerable<PhoneCall> GetChargeableCallsForSite(string siteName) 
        {
            string sqlStatemnet = PHONECALLS_SQL_QUERIES.ChargeableCallsForSite(DBTables, siteName);

            return base.GetAll(sqlStatemnet);
        }


        public override PhoneCall GetById(long id, string dataSourceName = null, GLOBALS.DataSource.Type dataSource = GLOBALS.DataSource.Type.Default)
        {
            throw new NotImplementedException();
        }


        public override IEnumerable<PhoneCall> GetAll(string dataSourceName = null, GLOBALS.DataSource.Type dataSourceType = GLOBALS.DataSource.Type.Default)
        {
            //string sqlStatement = sqlAccessor.GetAllPhoneCalls(dbTables);
            //return base.GetAll(SQL_QUERY: sqlStatement);

            throw new NotImplementedException();
        }

    }

}
