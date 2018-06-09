using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;



namespace AccessLog
{
    public class Util
    {
        #region Variable
        int CounterRecords = 0;

        #endregion

        #region Config
        public bool readConfig(string[] config)
        {
            bool returnStatus = true;
            try
            {

                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = config[0];
                Configuration configFile = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                AppSettingsSection section = (AppSettingsSection)configFile.GetSection("appSettings");

                ConfigurationSectionGroup configSection = configFile.SectionGroups["Mappings"];
                ConfigurationSection SectionHT = configSection.Sections["HistoryTBLQuery"];
                string nameHT = SectionHT.SectionInformation.GetRawXml();
                XmlDocument docEventHandler = new XmlDocument();
                docEventHandler.Load(XmlReader.Create(new StringReader(nameHT)));
                Constants.HistoryTBLQuery = docEventHandler.ChildNodes[0].InnerText.Replace("\n", "");

                ConnectionStringsSection connSection = (ConnectionStringsSection)configFile.GetSection("connectionStrings");

                Constants.LogFileDateFormat = Convert.ToString(section.Settings["LogFileDateFormat"].Value);
                Constants.LogPath = Convert.ToString(section.Settings[("LogPath")].Value) + "AccessLog_" + DateTime.Now.ToUniversalTime().ToString(Constants.LogFileDateFormat) + ".log";
                Constants.Debug = Convert.ToString(section.Settings[("Debug")].Value);
                Constants.connectionString = Convert.ToString(connSection.ConnectionStrings["conn_string"].ConnectionString);
                Constants.connectionStringAuditDB = Convert.ToString(connSection.ConnectionStrings["conn_stringAudit"].ConnectionString);

            }
            catch (Exception exe)
            {
                Logger.LogMessageToFile("Error occured while reading the Configuration files.");
                Logger.LogMessageToFile("Error message : " + exe.Message);
                returnStatus = false;
            }

            return returnStatus;
        }
        #endregion

        #region DBOperations

        // TBD : SourceHostIP and ActorHostIP are now saved as empty string, What Ip value will be saved is to be disscussed and implement
        // As per discussion we are saving ip address of the machine where code is deployed.
        public void TransferHistoryRecordsToAccessLogs(string[] config)
        {

            DataSet dsHistoryTabletbl = new DataSet();

            StringBuilder InsertQuery = new StringBuilder();
            InsertQuery.Append(" Insert All ");
            CounterRecords = 0;

            try
            {
                // get ip address of the machine where code is deployed
                string ip = string.Empty;
                ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();

                // to access config file for dyname Letter_Generation_ID_ColmunName and ELG_Request_ID_ColmunName
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = config[0];
                Configuration configFile = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                AppSettingsSection section = (AppSettingsSection)configFile.GetSection("appSettings");
                ConnectionStringsSection connSection = (ConnectionStringsSection)configFile.GetSection("connectionStrings");

                dsHistoryTabletbl = GetHistoryTableDetails();
                string LastEntryDateTimeAccesslog_E10_Access = GetLastEntryDateTimeAccesslog().ToString();
                for (int i = 0; i < dsHistoryTabletbl.Tables[0].Rows.Count; i++)
                {

                    DataSet dsHistoryRecords = new DataSet();
                    using (OracleConnection conn = new OracleConnection(Constants.connectionString))
                    {
                        using (OracleCommand cmd = new OracleCommand(" Select * from " + dsHistoryTabletbl.Tables[0].Rows[i]["History_Table_Name"] + " where  HISTORYTIMESTAMP>to_date( '" + LastEntryDateTimeAccesslog_E10_Access + "', 'yyyy-mm-dd hh24:mi:ss')", conn))
                        {
                            cmd.CommandType = CommandType.Text;
                            conn.Open();
                            LogMessage("Database connection created successfully");
                            OracleDataAdapter da = new OracleDataAdapter(cmd);
                            da.Fill(dsHistoryRecords);
                            conn.Close();
                            if (dsHistoryTabletbl != null)
                            {
                                if (dsHistoryRecords.Tables.Count > 0)
                                {
                                    LogMessage("Total Number of records fetched from Hitory table " + dsHistoryTabletbl.Tables[0].Rows[i]["History_Table_Name"] + " : " + dsHistoryRecords.Tables[0].Rows.Count.ToString());
                                }
                            }

                            for (int j = 0; j < dsHistoryRecords.Tables[0].Rows.Count; j++)
                            {
                                DateTime dtHISTORYTIMESTAMP = Convert.ToDateTime(dsHistoryRecords.Tables[0].Rows[j]["HISTORYTIMESTAMP"]);
                                string HISTORYTIMESTAMP = "TO_CHAR(TO_TIMESTAMP('" + dtHISTORYTIMESTAMP.ToString("yyyy-MM-dd HH:mm:ss") + "', 'YYYY-mm-DD HH24:MI:SS'),'YYYY-mm-DD\"T\"HH24:MI:SS.ff7')||tz_offset(sessiontimezone)";

                                try
                                {
                                    string Letter_Generation_ID = string.Empty;
                                    string ELG_Request_ID = string.Empty;

                                    string OUSERID = string.Empty;
                                    string HISTORYID = string.Empty;

                                    if (dsHistoryRecords.Tables[0].Rows[j][dsHistoryTabletbl.Tables[0].Rows[i]["Letter_Generation_ID"].ToString()] != null)
                                    {
                                        Letter_Generation_ID = dsHistoryRecords.Tables[0].Rows[j][dsHistoryTabletbl.Tables[0].Rows[i]["Letter_Generation_ID"].ToString()].ToString();
                                    }
                                    if (dsHistoryRecords.Tables[0].Rows[j][dsHistoryTabletbl.Tables[0].Rows[i]["ELG_Request_ID"].ToString()] != null)
                                    {
                                        ELG_Request_ID = dsHistoryRecords.Tables[0].Rows[j][dsHistoryTabletbl.Tables[0].Rows[i]["ELG_Request_ID"].ToString()].ToString();
                                    }
                                    if (dsHistoryRecords.Tables[0].Rows[j]["OUSERID"] != null)
                                    {
                                        OUSERID = dsHistoryRecords.Tables[0].Rows[j]["OUSERID"].ToString();
                                    }
                                    if (dsHistoryRecords.Tables[0].Rows[j]["HISTORYID"] != null)
                                    {
                                        HISTORYID = dsHistoryRecords.Tables[0].Rows[j]["HISTORYID"].ToString();
                                    }
                                    string DetailText = "User " + OUSERID + " generated letter containing protected information with Letter_Generation_ID=" + Letter_Generation_ID + ", ELG_Request_ID=" + ELG_Request_ID + ", History_ID= " + HISTORYID + "";
                                    InsertQuery.Append(" INTO Accesslog (\"LEVEL\",\"EVENTCODE\",\"SOURCEHOSTIP\",\"ACTORHOSTIP\",\"ACTOR\",\"DETAILTEXT\",\"EVENTTIME\") VALUES ('INFO','E10:Access','" + ip + "','" + ip + "','" + OUSERID + "','" + DetailText + "'," + HISTORYTIMESTAMP + ") ");
                                    CounterRecords++;
                                }
                                catch (Exception)
                                {
                                    LogMessage(" Exception occured in generating insert query with history records from " + dsHistoryTabletbl.Tables[0].Rows[i]["History_Table_Name"].ToString());
                                    throw;
                                }

                                // Start inserting into database if InsertQuery has 1000 insert entries. 
                                if (CounterRecords == 1000)
                                {
                                    InsertQuery.Append(" Select * from Dual ");
                                    if (InsertQuery.ToString().ToUpper().Contains("INTO ACCESSLOG"))
                                    {
                                        ExecuteNonQuery(InsertQuery.ToString());
                                        LogMessage("Total Number of records inserted into Accesslog table : " + CounterRecords);
                                    }

                                    CounterRecords = 0;
                                    InsertQuery = new StringBuilder();
                                    InsertQuery.Append(" Insert All ");
                                }

                            }

                        }
                    }
                }
                // Insert entries into database. 
                InsertQuery.Append(" Select * from Dual ");
                if (InsertQuery.ToString().ToUpper().Contains("INTO ACCESSLOG"))
                {
                    ExecuteNonQuery(InsertQuery.ToString());
                    LogMessage("Total Number of records inserted into Accesslog table : " + CounterRecords);
                }
            }
            catch (Exception exe)
            {
                LogMessage("Error Occurred while transfering history records into Accesslog table");
                LogMessage("Error Message : " + exe.Message);
                throw exe;
            }
        }

        private static DataSet GetHistoryTableDetails()
        {
            DataSet dsHistoryTabletbl = new DataSet();
            try
            {
                using (OracleConnection conn = new OracleConnection(Constants.connectionString))
                {
                    using (OracleCommand cmd = new OracleCommand(Constants.HistoryTBLQuery, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        LogMessage("Database connection created successfully");
                        OracleDataAdapter da = new OracleDataAdapter(cmd);
                        da.Fill(dsHistoryTabletbl);
                        conn.Close();
                        if (dsHistoryTabletbl != null)
                        {
                            if (dsHistoryTabletbl.Tables.Count > 0)
                            {
                                LogMessage("Total Number of records fetched from database : " + dsHistoryTabletbl.Tables[0].Rows.Count.ToString());
                            }
                        }
                    }
                }

            }

            catch (Exception exe)
            {
                LogMessage("Error Occurred while transfering history records into Accesslog table");
                LogMessage("Error Message : " + exe.Message);
                throw exe;
            }
            return dsHistoryTabletbl;
        }

        private static void ExecuteNonQuery(string Query)
        {
            DataSet dsHistoryTabletbl = new DataSet();
            try
            {

                using (OracleConnection conn = new OracleConnection(Constants.connectionStringAuditDB))
                {
                    using (OracleCommand cmd = new OracleCommand(Query, conn))
                    {
                        cmd.CommandType = CommandType.Text;

                        conn.Open();
                        LogMessage("Audit database connection created successfully");
                        cmd.ExecuteNonQuery();
                        conn.Close();

                    }
                }

            }
            catch (Exception exe)
            {
                LogMessage("Error while inserting records into database");
                LogMessage("Error Message : " + exe.Message);
                throw exe;
            }
        }

        private static string GetLastEntryDateTimeAccesslog()
        {
            string LastEntryDateTimeAccesslog;
            DataSet dsAccesslog = new DataSet();

            try
            {

                using (OracleConnection conn = new OracleConnection(Constants.connectionStringAuditDB))
                {
                    using (OracleCommand cmd = new OracleCommand(" SELECT Max(EVENTTIME) as \"EVENTTIME\"   FROM ELGDBO.ACCESSLOG WHERE EVENTCODE = 'E10:Access' ORDER BY EVENTTIME DESC", conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        conn.Open();
                        LogMessage("Audit database connection created successfully");
                        OracleDataAdapter da = new OracleDataAdapter(cmd);
                        da.Fill(dsAccesslog);
                        conn.Close();
                        if (dsAccesslog != null)
                        {
                            if (dsAccesslog.Tables.Count > 0)
                            {
                                LogMessage("Total Number of records fetched from database : " + dsAccesslog.Tables[0].Rows.Count.ToString());
                            }
                        }
                    }
                    if (dsAccesslog.Tables[0].Rows.Count > 0)
                    {
                        if (dsAccesslog.Tables[0].Rows[0]["EVENTTIME"].ToString() != "")
                        {
                            DateTime dtEVENTTIME = Convert.ToDateTime(dsAccesslog.Tables[0].Rows[0]["EVENTTIME"]);
                            LastEntryDateTimeAccesslog = dtEVENTTIME.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            // set to current date minus one day instead to seed the database
                            //LastEntryDateTimeAccesslog = "1990-01-01 00:00:00";
                            LastEntryDateTimeAccesslog = System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");

                        }
                    }
                    else
                    {
                        // set to current date minus one day instead to seed the database
                        // LastEntryDateTimeAccesslog = "1990-01-01 00:00:00";
                        LastEntryDateTimeAccesslog = System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");
                    }

                }

            }
            catch (Exception exe)
            {
                LogMessage("Error while inserting records into database");
                LogMessage("Error Message : " + exe.Message);
                throw exe;
            }
            return LastEntryDateTimeAccesslog;
        }
        #endregion

        #region Logging
        public static void LogMessage(string strMessage)
        {
            Logger.LogMessageToFile(strMessage);
            Console.Error.WriteLine(strMessage);
        }
        #endregion

    }
    public static class Logger
    {
        private static readonly object sync = new object();
        public static void LogMessageToFile(string msg)
        {
            lock (sync)
            {
                using (StreamWriter sw = new StreamWriter(Constants.LogPath, true))
                {
                    string logLine = String.Format("{0:G}: {1}.", System.DateTime.Now, msg);
                    sw.WriteLine(logLine);
                }
            }
        }
    }
    public class Constants
    {
        public static string ExceptionPath;
        public static string Application;
        public static string LogPath;
        public static string Debug;
        public static string connectionString;
        public static string connectionStringAuditDB;
        public static int intSeverity;
        public static string LogFileDateFormat;
        public static string HistoryTBLQuery;
    }
}
