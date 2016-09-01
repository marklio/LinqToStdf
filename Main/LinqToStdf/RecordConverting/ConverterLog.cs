using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.RecordConverting
{
    class ConverterLog
    {
        public static IDisposable CreateLogContext(Action<string> logger)
        {
            if (_Context != null) throw new InvalidOperationException("A log context already exists");
            _Context = new ConverterLog(logger);
            return new Disposer();
        }

        class Disposer : IDisposable
        {
            public void Dispose()
            {
                if (_Context == null) throw new InvalidOperationException("The context has already been disposed");
                _Context = null;
            }
        }

        static ConverterLog _Context;
        public static void Log(string msg)
        {
            _Context?.LogInternal(msg);
        }

        Action<string> _Logger;
        ConverterLog(Action<string> logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _Logger = logger;
        }

        void LogInternal(string msg)
        {
            _Logger(msg);
        }
    }
}
