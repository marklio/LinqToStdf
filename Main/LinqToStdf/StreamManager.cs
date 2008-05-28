using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LinqToStdf {
    public interface IStdfStreamScope : IDisposable {
        Stream Stream { get; }
    }

    public interface IStdfStreamManager {
        string Name { get; }
        IStdfStreamScope GetScope();
    }

    public class OwnedStdfStreamScope : IStdfStreamScope {

        Stream _Stream;
        public OwnedStdfStreamScope(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }
            _Stream = stream;
        }

        #region IStdfStreamScope Members

        public Stream Stream {
            get { return _Stream; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            _Stream.Dispose();
        }

        #endregion
    }

    public class StdfFileStreamManager : IStdfStreamManager {

        string _Path;
        public StdfFileStreamManager(string path) {
            if (path == null) throw new ArgumentNullException("path");
            _Path = path;
        }

        #region IStdfStreamManager Members

        public string Name {
            get { return Path.GetFileName(_Path); }
        }

        public IStdfStreamScope GetScope() {
            return new OwnedStdfStreamScope(new FileStream(_Path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        #endregion
    }
}
