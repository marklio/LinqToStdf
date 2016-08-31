using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LinqToStdf;
using System.IO;
using LinqToStdf.Indexing;
using System.Linq;
using LinqToStdf.Records.V4;
using LinqToStdf.Records;
using System.Reflection;

namespace StdfFileTests
{
    static class HelperExtensions
    {
        public static TRecord GetSingleRecord<TRecord>(this StdfFile file) where TRecord : StdfRecord
        {
            StartOfStreamRecord sos = null;
            Far far = null;
            TRecord recordOfInterest = null;
            EndOfStreamRecord eos = null;
            foreach (var record in file.GetRecordsEnumerable())
            {
                if (sos == null) sos = (StartOfStreamRecord)record;
                else if (far == null)
                {
                    far = (Far)record;
                    if (typeof(TRecord) == typeof(Far))
                    {
                        recordOfInterest = (TRecord)(object)far;
                    }
                }
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
        public static DateTime TruncateToSeconds(this DateTime dateTime)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
        }
    }

    [TestClass]
    public class RoundTrip
    {
        private object eps;

        [TestMethod]
        public void TestFar()
        {
            var far = new Far
            {
                Offset = 1234,
                CpuType = 0,
                StdfVersion = 4,
            };
            TestRoundTripEquality(far);

            far.CpuType = 1;
            TestRoundTripEquality(far);

            far.CpuType = 2;
            TestRoundTripEquality(far, endian: Endian.Little);

            far.CpuType = 1;
            far.StdfVersion = 5;
            TestRoundTripEquality(far);
        }

        [TestMethod]
        public void TestAtr()
        {
            var atr = new Atr();
            TestRoundTripEquality(atr);
            atr.ModifiedTime = DateTime.Now.TruncateToSeconds();
            TestRoundTripEquality(atr);
            atr.CommandLine = "This is a test";
            TestRoundTripEquality(atr);
            atr.ModifiedTime = null;
            TestRoundTripEquality(atr);
        }

        [TestMethod]
        public void TestMir()
        {
            var mir = new Mir();
            TestRoundTripEquality(mir);
        }

        [TestMethod]
        public void TestMrr()
        {
            var mrr = new Mrr();
            TestRoundTripEquality(mrr);
        }

        [TestMethod]
        public void TestPcr()
        {
            var pcr = new Pcr();
            TestRoundTripEquality(pcr);
        }

        [TestMethod]
        public void TestHbr()
        {
            var hbr = new Hbr();
            TestRoundTripEquality(hbr);
        }

        [TestMethod]
        public void TestSbr()
        {
            var sbr = new Sbr();
            TestRoundTripEquality(sbr);
        }

        [TestMethod]
        public void TestPmr()
        {
            var pmr = new Pmr();
            TestRoundTripEquality(pmr);
        }

        [TestMethod]
        public void TestPgr()
        {
            var pgr = new Pgr();
            TestRoundTripEquality(pgr);
        }

        [TestMethod]
        public void TestPlr()
        {
            var plr = new Plr
            {
                GroupIndexes = new ushort[] { 0 }
            };
            TestRoundTripEquality(plr);
        }

        [TestMethod]
        public void TestRdr()
        {
            var rdr = new Rdr();
            TestRoundTripEquality(rdr);
        }

        [TestMethod]
        public void TestSdr()
        {
            var sdr = new Sdr();
            TestRoundTripEquality(sdr);
        }

        [TestMethod]
        public void TestWir()
        {
            var wir = new Wir();
            TestRoundTripEquality(wir);
        }

        [TestMethod]
        public void TestWrr()
        {
            var wrr = new Wrr();
            TestRoundTripEquality(wrr);
        }

        [TestMethod]
        public void TestWcr()
        {
            var wcr = new Wcr();
            TestRoundTripEquality(wcr);
        }

        [TestMethod]
        public void TestPir()
        {
            var pir = new Pir();
            TestRoundTripEquality(pir);
        }

        [TestMethod]
        public void TestPrr()
        {
            var prr = new Prr();
            TestRoundTripEquality(prr);
        }

        [TestMethod]
        public void TestTsr()
        {
            var tsr = new Tsr();
            TestRoundTripEquality(tsr);
        }

        [TestMethod]
        public void TestPtr()
        {
            var ptr = new Ptr();
            TestRoundTripEquality(ptr);
        }

        [TestMethod]
        public void TestMpr()
        {
            var mpr = new Mpr();
            TestRoundTripEquality(mpr);
            mpr.PinIndexes = new ushort[] { 0, 1 };
            mpr.PinStates = new byte[] { 0, 1 };
            mpr.Results = new float[] { 0.0f, 1.1f };
            TestRoundTripEquality(mpr);
        }

        [TestMethod]
        public void TestFtr()
        {
            var ftr = new Ftr();
            TestRoundTripEquality(ftr);
        }

        [TestMethod]
        public void TestBps()
        {
            var bps = new Bps();
            TestRoundTripEquality(bps);
        }

        [TestMethod]
        public void TestEps()
        {
            var eps = new Eps();
            TestRoundTripEquality(eps);
        }

        [TestMethod]
        public void TestGdr()
        {
            var gdr = new Gdr();
            TestRoundTripEquality(gdr);
        }

        [TestMethod]
        public void TestDtr()
        {
            var dtr = new Dtr();
            TestRoundTripEquality(dtr);
        }

        private void TestRoundTripEquality(object eps)
        {
            throw new NotImplementedException();
        }

        public void TestRoundTripEquality<TRecord>(TRecord record, Endian endian = Endian.Big) where TRecord : StdfRecord
        {
            TestRecordEquality(record, RoundTripRecord(record, endian, debug: true));
        }
        public void TestRecordEquality<TRecord>(TRecord one, TRecord two) where TRecord : StdfRecord
        {
            var props = from prop in typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        where prop.Name != nameof(StdfRecord.StdfFile)
                            && prop.Name != nameof(StdfRecord.Offset)
                        let del = (Func<TRecord, object>)((r) => prop.GetGetMethod().Invoke(r, new object[0]))
                        let test = (Action)(() =>
                        {
                            //TODO: test arrays
                            if (!prop.PropertyType.IsArray)
                            {
                                Assert.AreEqual(del(one), del(two), $"{prop.Name} not equal");
                            }
                        })
                        select test;
            foreach (var t in props) t();
        }


        public TRecord RoundTripRecord<TRecord>(TRecord record, Endian endian, bool debug) where TRecord : StdfRecord
        {
            using (var testStream = new MemoryStream())
            {
                using (var writer = new StdfFileWriter(testStream, endian, debug: true))
                {
                    if (typeof(TRecord) != typeof(Far))
                    {
                        writer.WriteRecord(new Far
                        {
                            CpuType = endian == Endian.Big ? (byte)1 : (byte)2,
                            StdfVersion = 4,
                        });
                    }
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
