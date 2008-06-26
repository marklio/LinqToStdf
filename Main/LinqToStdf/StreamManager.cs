using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

#if !SILVERLIGHT
using System.IO.Compression;
#endif

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
	/// <see cref="IStdfStreamManager"/> implementation for an uncompressed STDF <see cref="FileStream"/>
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

	/// <summary>
	/// The default <see cref="IStdfStreamManager"/> implementation for a <see cref="FileStream"/>
	/// based on a path.
	/// </summary>
	/// <remarks>This manager will auto-select the appropriate manager based on the path</remarks>
	public class DefaultFileStreamManager : IStdfStreamManager {

		IStdfStreamManager _InnerManager;

		public DefaultFileStreamManager(string path) {
			if (path == null) throw new ArgumentNullException("path");

			_InnerManager = GetAppropriateManager(path);

			// TODO: If there's an unrecognized path, should we return null from GetAppropriateManager and throw here?  Assume uncompressed STDF?
		}

		private IStdfStreamManager GetAppropriateManager(string path) {
			if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".gzip", StringComparison.OrdinalIgnoreCase)) {
#if SILVERLIGHT
				throw new NotSupportedException("GZip compression not supported in Silverlight");
#else
				return new GZipStdfFileStreamManager(path);
#endif
			}

			if (path.EndsWith(".stdf", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".std", StringComparison.OrdinalIgnoreCase)) {
				return new StdfFileStreamManager(path);
			}

			throw new ArgumentException("Could not recognize the file stream type from the path provided.", "path");
		}

		#region IStdfStreamManager Members

		public string Name {
			get { return _InnerManager.Name; }
		}

		public IStdfStreamScope GetScope() {
			return _InnerManager.GetScope();
		}

		#endregion
	}

#if !SILVERLIGHT
	/// <summary>
	/// <see cref="IStdfStreamManager"/> implementation for a GZip compressed STDF <see cref="FileStream"/>
	/// based on a path.
	/// </summary>
	public class GZipStdfFileStreamManager : IStdfStreamManager {

        string _Path;
		public GZipStdfFileStreamManager(string path) {
            if (path == null) throw new ArgumentNullException("path");
            _Path = path;
        }

		#region IStdfStreamManager Members

        public string Name {
            get { return Path.GetFileName(_Path); }
        }

        public IStdfStreamScope GetScope() {
			Stream stream = new FileStream(_Path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return new OwnedStdfStreamScope(new PositionTrackingStream(new GZipStream(stream, CompressionMode.Decompress), true));
        }

		#endregion
	}
#endif

	internal class PositionTrackingStream : Stream {

        public PositionTrackingStream(Stream stream, bool ownStream) {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }
            _Stream = stream;
            _OwnStream = ownStream;
        }

        Stream _Stream;
        bool _OwnStream;

        public override bool CanRead {
            get { return _Stream.CanRead; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return false; }
        }

        public override void Flush() {
            _Stream.Flush();
        }

        public override long Length {
            get { return _Stream.Length; }
        }

        long _Position = 0;
        public override long Position {
            get { return _Position; }
            set {
                throw new NotSupportedException("Setting position not supported for this stream instance.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            var bytesRead = _Stream.Read(buffer, offset, count);
            _Position += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("Seek not supported for this stream instance.");
        }

        public override void SetLength(long value) {
            _Stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("Write not supported for this stream instance.");
        }

        protected override void Dispose(bool disposing) {
            if (_OwnStream) _Stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
