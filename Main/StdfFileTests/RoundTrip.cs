using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LinqToStdf;
using System.IO;
using LinqToStdf.Indexing;
using System.Linq;
using LinqToStdf.Records.V4;
using LinqToStdf.Records;

namespace StdfFileTests
{
    static class HelperExtensions
    {
        public static TRecord GetSingleRecord<TRecord>(this StdfFile file) where TRecord : StdfRecord
        {
            StartOfStreamRecord sos = null;
            TRecord recordOfInterest = null;
            EndOfStreamRecord eos = null;
            foreach (var record in file.GetRecordsEnumerable())
            {
                if (sos == null) sos = (StartOfStreamRecord)record;
                else if (recordOfInterest == null) recordOfInterest = (TRecord)record;
                else if (eos == null) eos = (EndOfStreamRecord)record;
                else
                {
                    Assert.Fail("There were extra records");
                }
            }
            Assert.IsNotNull(sos, "No start of stream");
            Assert.IsNotNull(recordOfInterest, "No record of interest");
            Assert.IsNotNull(eos, "No end of stream");
            //TODO: assert things about sos/eos?
            return recordOfInterest;
        }
    }
    [TestClass]
    public class RoundTrip
    {
        [TestMethod]
        public void TestFar()
        {
            var far = new Far
            {
                Offset = 1234,
                CpuType = 0,
                StdfVersion = 4,
            };
            var readFar = RoundTripRecord(far, Endian.Big, debug: true);
            Assert.AreEqual(far.CpuType, readFar.CpuType, "CpuTypes differ");
            Assert.AreEqual(far.StdfVersion, readFar.StdfVersion, "StdfVersions differ");
            Assert.AreEqual(readFar.Offset, 0, "Offset not 0");
            Assert.IsNotNull(readFar.StdfFile, "StdfFile null");

            far.CpuType = 1;
            readFar = RoundTripRecord(far, Endian.Big, debug: true);
            Assert.AreEqual(far.CpuType, readFar.CpuType, "CpuTypes differ");
            Assert.AreEqual(far.StdfVersion, readFar.StdfVersion, "StdfVersions differ");
            Assert.AreEqual(readFar.Offset, 0, "Offset not 0");
            Assert.IsNotNull(readFar.StdfFile, "StdfFile null");

            far.StdfVersion = 5;
            readFar = RoundTripRecord(far, Endian.Big, debug: true);
            Assert.AreEqual(far.CpuType, readFar.CpuType, "CpuTypes differ");
            Assert.AreEqual(far.StdfVersion, readFar.StdfVersion, "StdfVersions differ");
            Assert.AreEqual(readFar.Offset, 0, "Offset not 0");
            Assert.IsNotNull(readFar.StdfFile, "StdfFile null");

        }

        public TRecord RoundTripRecord<TRecord>(TRecord record, Endian endian, bool debug) where TRecord : StdfRecord
        {
            using (var testStream = new MemoryStream())
            {
                using (var writer = new StdfFileWriter(testStream, endian, debug: true))
                {
                    writer.WriteRecord(record);
                }
                testStream.Seek(0, SeekOrigin.Begin);

                var streamManager = new TestStreamManager(testStream);
                var file = new StdfFile(streamManager, debug) { ThrowOnFormatError = true };
                return file.GetSingleRecord<TRecord>();
            }
        }
    }

    class TestStreamManager : IStdfStreamManager, IStdfStreamScope
    {
        MemoryStream _TestStream;
        public TestStreamManager(MemoryStream testStream)
        {
            _TestStream = testStream;
        }

        public string Name => "TestStream";

        Stream IStdfStreamScope.Stream => _TestStream;

        public IStdfStreamScope GetScope()
        {
            _TestStream.Seek(0, SeekOrigin.Begin);
            return this;
        }

        void IDisposable.Dispose()
        {
            //nop
        }
    }
}
