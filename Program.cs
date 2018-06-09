using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessLog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Util util = new Util();
            DateTime startTime = DateTime.Now;
            //args = new string[1];
            //args[0] = @"C:\AccessLog\AccessLog\App.config";

            try
            {
                if (!args.Length.Equals(0))
                {
                    if (!util.readConfig(args))
                    {
                        Constants.intSeverity = 30;
                        Console.WriteLine("Error Occured while fetching the SecureSync Configurations");
                        LogMessage("Error Occured while fetching the Configurations");
                        return;
                    }
                    else
                    {
                        LogMessage("xxxxxxxxxxxxxxxxxxxxxxxxxxxx");
                        LogMessage("History records transfer process started");
                        LogMessage("xxxxxxxxxxxxxxxxxxxxxxxxxxxx");
                        util.TransferHistoryRecordsToAccessLogs(args);
                    }
                }
                else
                {
                    Console.WriteLine("The Config File Name should be passed");
                    Constants.intSeverity = 30;
                    LogMessage("The Config File Name should be passed");
                    LogMessage("Severity = 30");
                    return;
                }
            }
            catch (Exception exe)
            {
                LogMessage("Exception occured");
                LogMessage("Exception LogMessage : " + exe.Message);
            }
            finally
            {
                LogMessage("Execution time :- " + Convert.ToString(DateTime.Now.Subtract(startTime).TotalSeconds) + "second(s)");
            }
        }

        #region Logging
        public static void LogMessage(string strMessage)
        {
            Logger.LogMessageToFile(strMessage);
            Console.Error.WriteLine(strMessage);
        }
        #endregion

    }
}
