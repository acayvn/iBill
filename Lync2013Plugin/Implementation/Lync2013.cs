﻿using Lync2013Plugin.Interfaces;
using LyncBillingBase.DataModels;
using PhoneCallsProcessor.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LyncBillingBase.DataAccess;
using System.Data.OleDb;
using LyncBillingBase.DataMappers;
using LyncBillingBase;
using System.Data;
using LyncBillingBase.Helpers;
using System.Data.SqlTypes;
using System.Collections.Concurrent;


namespace Lync2013Plugin.Implementation
{
    public class Lync2013 : ICallProcessor
    {
        private static DBLib DBRoutines = new DBLib();
        private static OleDbConnection SourceDBConnector;
        private static OleDbConnection DestinationDBConnector;

        private ENUMS enums = new ENUMS();
        
        private string PhoneCallsTableName = string.Empty;
        
        public string Name
        {
            get { return "Lync2013"; }
        }

        public string Description
        {
            get { return "Imports and Process Lync 2013 Logs"; }
        }

        public string Version
        {
            get { return "1.0.0"; }
        }

        public Lync2013() 
        {
            try
            {
                SourceDBConnector = new OleDbConnection(ConstructConnectionString());
                DestinationDBConnector = new OleDbConnection(DBLib.ConnectionString_Lync);
            }
            catch (Exception e) { throw e.InnerException; }
        }


        public void ProcessPhoneCalls()
        {
            PhoneCallsImpl phoneCallsFunc = new PhoneCallsImpl();

            DataTable ToBeInsertedDataTable;
            OleDbDataReader dataReader = null;

            var exceptions = new ConcurrentQueue<Exception>();
          
            string column = string.Empty;

            DateTime lastImportedPhoneCallDate = DateTime.MinValue;
            
            //OPEN CONNECTIONS
            SourceDBConnector.Open();
            DestinationDBConnector.Open();

            dataReader = DBRoutines.EXECUTEREADER(SQLs.GetLastImportedPhonecallDate(PhoneCallsTableName,isRemote:false), DestinationDBConnector);

            if (dataReader.Read() && !dataReader.IsDBNull(0))
            {
                lastImportedPhoneCallDate = dataReader.GetDateTime(dataReader.GetOrdinal("SessionIdTime"));
                lastImportedPhoneCallDate = lastImportedPhoneCallDate.AddDays(+1);
            }
            else
            {
                //Table is empty in this case we need to read from the source that we will import the data from
                dataReader = DBRoutines.EXECUTEREADER(SQLs.GetLastImportedPhonecallDate("DialogsView",isRemote:true), SourceDBConnector);

                if (dataReader.Read() && !dataReader.IsDBNull(0))
                {
                    lastImportedPhoneCallDate = dataReader.GetDateTime(dataReader.GetOrdinal("SessionIdTime"));
                }
            }

            while (lastImportedPhoneCallDate <= DateTime.Now)
            {
                //Construct CREATE_IMPORT_PHONE_CALLS_QUERY
                string SQL = SQLs.CreateImportCallsQueryLync2013(lastImportedPhoneCallDate);

                dataReader = DBRoutines.EXECUTEREADER(SQL, SourceDBConnector);

                if (lastImportedPhoneCallDate > DateTime.MinValue)
                    Console.WriteLine("Importing PhoneCalls from " + PhoneCallsTableName + " since " + lastImportedPhoneCallDate);
                else
                    Console.WriteLine("Importing PhoneCalls from " + PhoneCallsTableName + " since the begining");

                
                //DataTable ImportingDataTable = new DataTable();

                //Load data into Datatable
                //ImportingDataTable.BeginLoadData();
                //ImportingDataTable.Load(dataReader);
                //ImportingDataTable.EndLoadData();

                //Read DB and map it to List of PhoneCalls
                var phoneCalls = DB.ReadSqlData<PhoneCall>(dataReader, DB.PhoneCallsSelector).ToList();

                
                //ImportingDataTable = new DataTable();

                //Load data into Datatable
                //ImportingDataTable.Load(dataReader);


                if (phoneCalls.Count() > 0)
                {
                    object status = new object();

                    var partitionsize = Partitioner.Create(0, phoneCalls.Count());

                    Parallel.ForEach(partitionsize, (range, loopStet) =>
                    {
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            phoneCallsFunc.ProcessPhoneCall(phoneCalls[i]);
                        }

                    });

                    // Bulk insert
                    ToBeInsertedDataTable = phoneCalls.ConvertToDataTable<PhoneCall>();
                    ToBeInsertedDataTable.BulkInsert(PhoneCallsTableName);

                    ToBeInsertedDataTable.Dispose();
    
                    Console.WriteLine("   [+] Imported: " + phoneCalls.Count + " phone calls.");
                }


                // Increment the datetime object by 1 day.
                lastImportedPhoneCallDate = lastImportedPhoneCallDate.AddDays(+1);


                //GarbageCollect the datatable
                //ImportingDataTable.Dispose();
                dataReader.Close();
                dataReader.Dispose();
                GC.Collect();
            }

            //Close All Connection and DataReaders
            SourceDBConnector.Close();
            DestinationDBConnector.Close();

            if (dataReader.IsClosed == false) { dataReader.Close(); }

            Console.WriteLine("Finish importing Calls from " + PhoneCallsTableName);

        }

        public void PluginInfo()
        {
            Console.WriteLine("Name: {0}", Name);
            Console.WriteLine("Description: {0}", Description);
            Console.WriteLine("Version: {0}", Version);
            Console.Write("DB Connection: {0}", ConstructConnectionString());

        }

        private string ConstructConnectionString() 
        {
            MonitoringServersDataMapper msDataMapper = new MonitoringServersDataMapper();

            var info = Repo.monitoringServerInfo.Where(item => item.Key == this.GetType().Name).Select(item => (MonitoringServerInfo)item.Value).First() as MonitoringServerInfo;
            PhoneCallsTableName = info.PhoneCallsTable;

            return msDataMapper.CreateConnectionString(info);

        }



        
    }
}
