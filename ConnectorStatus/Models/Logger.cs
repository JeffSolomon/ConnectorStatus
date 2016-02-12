using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace ConnectorStatus.Models
{
    public class Logger
    {
        
        public static void Log (string message)
        {
            string JeffTestLogPath = @"C:\DEV\ConnectorStatus\"; 
            string LogPath = @"C:\inetpub\wwwroot\logs";

            if (!Directory.Exists(LogPath))
                LogPath = JeffTestLogPath;

            string FilePath = Path.Combine(LogPath, "ConnectorStatus_log.txt");

            if (Directory.Exists(LogPath))
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {
                        sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " => " + message);
                    }
                }
                catch(Exception e)
                {

                }
            }
            
        }

    }
}