using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.RecordConverting
{
    public class ConverterLog
    {
        public static event Action<string> MessageLogged;

        public static void Log(string msg)
        {
            MessageLogged?.Invoke(msg);
        }

        public static bool IsLogging => MessageLogged != null;
    }
}
