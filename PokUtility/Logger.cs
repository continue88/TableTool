using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokUtility
{
    public class Logger
    {
        public delegate void LogProc(String text);

        static LogProc GlobalLogger = null;
        public static void SetGlobalLogger(LogProc proc) { GlobalLogger = proc; }

        public static void Log(String text)
        {
            var info = DateTime.Now.ToShortTimeString() + ": " + text;
            if (GlobalLogger != null)
                GlobalLogger(info);

            Console.WriteLine(info);
        }

        public static void LogError(String text)
        {
            Console.WriteLine(text);
        }
    }
}
