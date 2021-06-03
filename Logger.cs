using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DefinitelyNotMinecraft
{
    public enum LogType
    {
        Info,
        Error,
        Debug
    }
    public static class Logger
    {
        public static void Init()
        {
            if (Directory.Exists("Log") && File.Exists("Log\\latest.log"))
                File.Delete("Log\\latest.log");
        }
        public static void Log(string str, LogType type)
        {
            if (!Directory.Exists("Log"))
                Directory.CreateDirectory("Log");
            File.AppendAllText("Log\\latest.log", DateTime.Now.ToString("HH:mm:ss") + " (" + type.ToString() + "): " + str + '\n');
        }
        public static void Log(Exception e)
        {
            Log(e.Message + '\n' + e.StackTrace, LogType.Error);
        }
        public static void Log(string str)
        {
            Log(str, LogType.Info);
        }
    }
}
