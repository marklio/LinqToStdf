using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LinqToStdf {

    /// <summary>
    /// An interface used by StdfFile to scope the usage of a stream.
    /// Since a stream's lifetime may not match the lifetime of a query,
    /// this interface provides an abstraction from that policy.
    /// </summary>
    /// <remarks>
    /// Dispose is called at the end of each query (when the IEnumerable
    /// is used in a foreach or other proper code construct for IEnuemrable)
    /// </remarks>
    public interface IStdfStreamScope : IDisposable {

        /// <summary>
        /// The stream provided for the current scope (not expected
        /// to be used after dispose is called).
        /// </summary>
        Stream Stream { get; }
    }

    /// <summary>
    /// Interface that provides data in the form of <see cref="Stream"/>
    /// to an <see cref="StdfFile"/>.  Allows an abstraction of stream lifetime
    /// policy via providing an <see cref="IStdfStreamScope"/> for each
    /// "iteration" of a query.
    /// </summary>
    public interface IStdfStreamManager {
        /// <summary>
        /// A name for the source of data, commonly
        /// the original file name for the data.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an <see cref="IStdfStreamScope"/> for a given
        /// iteration of a query.
        /// </summary>
        /// <returns></returns>
        IStdfStreamScope GetScope();
    }

    /// <summary>
    /// Provides an implementation of <see cref="IStdfStreamScope"/>
    /// for an "owned" stream.  That is, it's lifetime is tied directly
    /// to the scope's lifetime and will be disposed with the scope.
    /// </summary>
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

    /// <summary>
    /// <see cref="IStdfStreamManager"/> implementation for a <see cref="FileStream"/>
    /// based on a path.
    /// </summary>
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
