using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

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
