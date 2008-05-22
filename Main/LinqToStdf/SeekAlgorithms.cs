using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf {

    /// <summary>
    /// This is the signature used as part of the rewind and seek implementation.
    /// It it provided to a <see cref="SeekAlgorithm"/> to allow it to indicate how many bytes should be backed up
    /// to put the current position on a valid record.
    /// </summary>
    public delegate void BackUpCallback(int bytes);

    /// <summary>
    /// <para>
    /// Defines the signature for a "seek algorithm". That is, an algorithm that, given a stream of bytes,
    /// can identify a valid record in order to get the parser "back on" the sequence of records.
    /// When <see cref="StdfFile.RewindAndSeek()"/> is called, the parser will enter seek mode,
    /// invoking any registered seek algorithms to help locate a valid record.
    /// </para>
    /// <para>
    /// A seek algorithm is implemented by consuming a sequence of bytes (<paramref name="bytes"/>)
    /// and implementing some sort of state machine that can identify a record. The endianness of the
    /// stream is provided (<paramref name="endian"/>) so that the algorithm can properly decode numbers.
    /// Since it is prohibitively complex to implement a lookahead mechanism with such a pluggable API,
    /// the algorithm is provided with a callback (<paramref name="backupCallback"/>) to indicate how many
    /// of the consumed bytes should "backed up" (not to be confused with the initial rewind operation) to
    /// put the position at the first byte of the found record.
    /// </para>
    /// <para>
    /// An implementation should look something like the following example, which looks for PIR records:
    /// </para>
    /// <example>
    /// <code lang="cs">
    /// IEnumerable&lt;byte&gt; LookForPirsImpl(IEnumerable&lt;byte&gt; bytes, Endian endian, BackUpCallback backupCallback) {
    ///     var pirHeader = new byte[] { 0, 0, 5, 10 };
    ///     pirHeader[endian == Endian.Little ? 0 : 1] = 2;
    ///     int testIndex = 0;
    ///     foreach (var b in bytes) {
    ///         if (b == pirHeader[testIndex]) {
    ///             testIndex++;
    ///         }
    ///         else {
    ///             if (b == pirHeader[0]) {
    ///                 testIndex = 1;
    ///             }
    ///             else {
    ///                 testIndex = 0;
    ///             }
    ///         }
    ///         if (testIndex >= pirHeader.Length) {
    ///             //we're back on!
    ///             backupCallback(4); //tell seek mode that 4 of the bytes that were consumed are actually part of the record
    ///             yield break;
    ///         }
    ///         yield return b;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public delegate IEnumerable<byte> SeekAlgorithm(IEnumerable<byte> bytes, Endian endian, BackUpCallback backupCallback);

    /// <summary>
    /// Provides some "built-in" <see cref="SeekAlgorithm"/>s
    /// </summary>
    public static class SeekAlgorithms {

        #region LookForPirs implementation

        static IEnumerable<byte> LookForPirsImpl(IEnumerable<byte> bytes, Endian endian, BackUpCallback backupCallback) {
            //the state machine can be simplified since the searching sequence
            //doesn't contain the same byte twice.
            var pirHeader = new byte[] { 0, 0, 5, 10 };
            //TODO: did I get this right?
            pirHeader[endian == Endian.Little ? 0 : 1] = 2;
            int testIndex = 0;
            foreach (var b in bytes) {
                if (b == pirHeader[testIndex]) {
                    testIndex++;
                }
                else {
                    if (b == pirHeader[0]) {
                        testIndex = 1;
                    }
                    else {
                        testIndex = 0;
                    }
                }
                if (testIndex >= pirHeader.Length) {
                    //we're back on!
                    backupCallback(4);
                    yield break;
                }
                yield return b;
            }
        }

        #endregion

        /// <summary>
        /// finds PIR records in the stream of bytes
        /// </summary>
        public static SeekAlgorithm LookForPirs {
            get {
                return LookForPirsImpl;
            }
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        public static SeekAlgorithm Identity {
            get { return (a, endian, callback) => a; }
        }
    }
}
