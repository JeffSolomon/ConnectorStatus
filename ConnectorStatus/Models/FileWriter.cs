using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Newtonsoft.Json;

namespace ConnectorStatus.Models
{
    public class FileWriter
    {
        static string JeffTestPath = @"C:\DEV\Test\ConnectorStatus\ConnectorStatus\output";
        static string LogPath = @"C:\inetpub\wwwroot\logs";
        static string JsonDataPath = @"C:\inetpub\wwwroot\data";
        static string LogFileName = "ConnectorStatus_log.txt";
        static string JsonFileName = "Builds.json";
        static string UpdateTSFileName = "UpdateTimestamp.txt";


        public static void Log (string message)
        {

            string finalLogPath = LogPath;

            if (!Directory.Exists(finalLogPath))
                finalLogPath = JeffTestPath;

            string FilePath = Path.Combine(finalLogPath, LogFileName);

            if (Directory.Exists(finalLogPath))
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

        public static void WriteJsonFile (List<ConnectorBuildItem> builds)
        {
            string finalJsonPath = JsonDataPath;

            if (!Directory.Exists(finalJsonPath))
                finalJsonPath = JeffTestPath;

            string FilePath = Path.Combine(finalJsonPath, JsonFileName);

            if (Directory.Exists(finalJsonPath))
            {
                try
                {
                    using (var sw = new StreamWriter(FilePath))
                    {
                        var settings = new JsonSerializerSettings//Use this code to troubleshoot deserialization. 
                        {
                            Error = (sender, args) =>
                            {
                                if (System.Diagnostics.Debugger.IsAttached)
                                {
                                    System.Diagnostics.Debugger.Break();
                                }
                            }
                        };
                        sw.Write(JsonConvert.SerializeObject(builds, Formatting.Indented));

                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("****" + e.Message);
                }
            }
        }

        public static List<ConnectorBuildItem> ReadJsonFile()
        {
            string finalJsonPath = JsonDataPath;

            if (!Directory.Exists(finalJsonPath))
                finalJsonPath = JeffTestPath;

            string FilePath = Path.Combine(finalJsonPath, JsonFileName);

            if (Directory.Exists(finalJsonPath) && File.Exists(FilePath))
            {
                try
                {
                    using (var sr = new StreamReader(FilePath))
                    {
                        var fileJson = sr.ReadToEnd();
                        var settings = new JsonSerializerSettings//Use this code to troubleshoot deserialization. 
                        {
                            Error = (sender, args) =>
                            {
                                if (System.Diagnostics.Debugger.IsAttached)
                                {
                                    System.Diagnostics.Debugger.Break();
                                }
                            }
                        };
                        var test = JsonConvert.DeserializeObject<List<ConnectorBuildItem>>(fileJson);
                        return JsonConvert.DeserializeObject<List<ConnectorBuildItem>>(fileJson);
                    }
                }
                catch (Exception e)
                {

                }
            }

            return null;

        }

        public static void WriteLastUpdateTime(DateTime datetime)
        {
            string finalTimestampPath = JsonDataPath;

            if (!Directory.Exists(finalTimestampPath))
                finalTimestampPath = JeffTestPath;

            string FilePath = Path.Combine(finalTimestampPath, UpdateTSFileName);

            if (Directory.Exists(finalTimestampPath))
            {
                try
                {
                    using (var sw = new StreamWriter(FilePath))
                    {
                        sw.Write(datetime.ToString("yyyy-MM-dd HH:mm"));
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        public static string GetLastUpdateTime()
        {
            string finalTimestampPath = JsonDataPath;

            if (!Directory.Exists(finalTimestampPath))
                finalTimestampPath = JeffTestPath;

            string FilePath = Path.Combine(finalTimestampPath, UpdateTSFileName);

            if (Directory.Exists(finalTimestampPath) && File.Exists(FilePath))
            {
                try
                {
                    using (var sr = new StreamReader(FilePath))
                    {
                        var timestamp = sr.ReadToEnd();
                        return timestamp;
                    }
                }
                catch (Exception e)
                {

                }
            }
            return null;
        }


    }
}