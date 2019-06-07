﻿using System;
using LinqToStdf;
using System.IO;
using System.Linq;
using LinqToStdf.Records.V4;
using LinqToStdf.Records;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

#nullable enable

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
                    Assert.True(false, "There were extra records");
                }
            }
            Assert.NotNull(sos);
            Assert.NotNull(recordOfInterest);
            Assert.NotNull(eos);
            //TODO: assert things about sos/eos?
            return recordOfInterest;
        }
        public static DateTime TruncateToSeconds(this DateTime dateTime)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
        }
    }

    public class RoundTrip : IDisposable
    {
        public RoundTrip()
        {
            LinqToStdf.RecordConverting.ConverterLog.MessageLogged += Log;
        }

        void Log(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

        void IDisposable.Dispose()
        {
            LinqToStdf.RecordConverting.ConverterLog.MessageLogged -= Log;
        }

        [Fact]
        public void TestFar()
        {
            var far = new Far();
            TestRoundTripEquality(far);

            far.CpuType = 1;
            TestRoundTripEquality(far);

            far.CpuType = 2;
            TestRoundTripEquality(far, endian: Endian.Little);

            far.CpuType = 1;
            far.StdfVersion = 5;
            TestRoundTripEquality(far);
        }

        [Fact]
        public void TestAtr()
        {
            var atr = new Atr();
            TestRoundTripEquality(atr);
            atr.CommandLine = "This is a test";
            TestRoundTripEquality(atr);
            atr.ModifiedTime = DateTime.Now.TruncateToSeconds();
            atr.CommandLine = null;
            TestRoundTripEquality(atr);
            atr.ModifiedTime = null;
            TestRoundTripEquality(atr);
        }

        [Fact]
        public void TestMir()
        {
            var mir = new Mir();
            TestRoundTripEquality(mir);
            mir.SupervisorName = "Mark";
            TestRoundTripEquality(mir);
        }

        [Fact]
        public void TestMrr()
        {
            var mrr = new Mrr();
            TestRoundTripEquality(mrr);
            mrr.ExecDescription = "Super Cool";
            TestRoundTripEquality(mrr);
        }

        [Fact]
        public void TestPcr()
        {
            var pcr = new Pcr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(pcr);
            pcr.FunctionalCount = 1;
            TestRoundTripEquality(pcr);
        }

        [Fact]
        public void TestHbr()
        {
            var hbr = new Hbr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(hbr);
            hbr.BinName = "Fred";
            TestRoundTripEquality(hbr);
        }

        [Fact]
        public void TestSbr()
        {
            var sbr = new Sbr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(sbr);
            sbr.BinName = "Bob";
            TestRoundTripEquality(sbr);
        }

        [Fact]
        public void TestPmr()
        {
            var pmr = new Pmr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(pmr);
            pmr.SiteNumber = 0;
            TestRoundTripEquality(pmr);
        }

        [Fact]
        public void TestPgr()
        {
            var pgr = new Pgr();
            TestRoundTripEquality(pgr);
            pgr.PinIndexes = new ushort[] { 0, 1 };
            TestRoundTripEquality(pgr);
        }

        [Fact]
        public void TestPlr()
        {
            var plr = new Plr
            {
                GroupIndexes = new ushort[] { 0 }
            };
            TestRoundTripEquality(plr);
        }

        [Fact]
        public void TestRdr()
        {
            var rdr = new Rdr();
            TestRoundTripEquality(rdr);
            rdr.RetestBins = new ushort[] { 1, 2, 3, 4 };
            TestRoundTripEquality(rdr);
        }

        [Fact]
        public void TestSdr()
        {
            var sdr = new Sdr
            {
                HeadNumber = 1,
            };
            TestRoundTripEquality(sdr);
            sdr.ExtraId = "Professor Snape";
            TestRoundTripEquality(sdr);
        }

        [Fact]
        public void TestWir()
        {
            var wir = new Wir
            {
                HeadNumber = 1,
            };
            TestRoundTripEquality(wir);
            wir.WaferId = "Wolverine";
            TestRoundTripEquality(wir);
        }

        [Fact]
        public void TestWrr()
        {
            var wrr = new Wrr
            {
                HeadNumber = 1,
            };
            TestRoundTripEquality(wrr);
            wrr.ExecDescription = "It looks good";
            TestRoundTripEquality(wrr);
        }

        [Fact]
        public void TestWcr()
        {
            var wcr = new Wcr();
            TestRoundTripEquality(wcr);
            wcr.PositiveY = "U";
            TestRoundTripEquality(wcr);
        }

        [Fact]
        public void TestPir()
        {
            var pir = new Pir();
            //we must skip head number since we persist the missing value 1
            TestRoundTripEquality(pir, skipProps: new[] { "HeadNumber" });
            pir.SiteNumber = 1;
            TestRoundTripEquality(pir, skipProps: new[] { "HeadNumber" });
            pir.HeadNumber = 1;
            TestRoundTripEquality(pir);
        }

        [Fact]
        public void TestPrr()
        {
            var prr = new Prr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(prr);
            prr.PartFix = new byte[] { 1, 2, 3 };
            TestRoundTripEquality(prr);
        }

        [Fact]
        public void TestTsr()
        {
            var tsr = new Tsr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(tsr);
            tsr.TestSumOfSquares = 47.001f;
            TestRoundTripEquality(tsr);
        }

        [Fact]
        public void TestPtr()
        {
            var ptr = new Ptr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(ptr);
            ptr.TestFlags = 0xff;
            ptr.OptionalFlags = 0xf7;
            ptr.HighSpecLimit = 40.01f;
            TestRoundTripEquality(ptr);
        }

        [Fact]
        public void TestMpr()
        {
            var mpr = new Mpr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(mpr);
            mpr.PinIndexes = new ushort[] { 0, 1 };
            mpr.PinStates = new byte[] { 0, 1 };
            mpr.Results = new float[] { 0.0f, 1.1f };
            TestRoundTripEquality(mpr);
        }

        [Fact]
        public void TestFtr()
        {
            var ftr = new Ftr
            {
                HeadNumber = 1,
                SiteNumber = 1,
            };
            TestRoundTripEquality(ftr);
        }

        [Fact]
        public void TestBps()
        {
            var bps = new Bps();
            TestRoundTripEquality(bps);
        }

        [Fact]
        public void TestEps()
        {
            var eps = new Eps();
            TestRoundTripEquality(eps);
        }

        [Fact]
        public void TestGdr()
        {
            var gdr = new Gdr();
            TestRoundTripEquality(gdr);
        }

        [Fact]
        public void TestDtr()
        {
            var dtr = new Dtr();
            TestRoundTripEquality(dtr);
        }

        void TestRoundTripEquality<TRecord>(TRecord record, Endian endian = Endian.Big, IEnumerable<string>? skipProps = null) where TRecord : StdfRecord
        {
            TestRecordEquality(record, RoundTripRecord(record, endian, debug: true), skipProps);
        }
        void TestRecordEquality<TRecord>(TRecord one, TRecord two, IEnumerable<string>? skipProps = null) where TRecord : StdfRecord
        {
            var props = from prop in typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        where prop.Name != nameof(StdfRecord.StdfFile)
                            && prop.Name != nameof(StdfRecord.Offset)
                            && !(skipProps?.Contains(prop.Name) ?? false)
                        let del = (Func<TRecord, object>)((r) => prop.GetGetMethod().Invoke(r, new object[0]))
                        let test = (Action)(() =>
                        {
                            //TODO: test arrays
                            if (prop.PropertyType.IsArray)
                            {
                                var listOne = (System.Collections.IList)del(one);
                                var listTwo = (System.Collections.IList)del(two);
                                Assert.Equal(listOne?.Count, listTwo?.Count);
                                for (int i = 0; i < (listOne?.Count ?? 0); i++)
                                {
                                    Assert.Equal(listOne[i], listTwo[i]);
                                }
                            }
                            else
                            {
                                Assert.Equal(del(one), del(two));
                            }
                        })
                        select test;
            foreach (var t in props) t();
        }


        public TRecord RoundTripRecord<TRecord>(TRecord record, Endian endian, bool debug) where TRecord : StdfRecord
        {
            using var testStream = new MemoryStream();
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

            using var streamManager = new TestStreamManager(testStream);
            var file = new StdfFile(streamManager, debug) { ThrowOnFormatError = true };
            return file.GetSingleRecord<TRecord>();
        }
    }

    class TestStreamManager : IStdfStreamManager, IStdfStreamScope
    {
        readonly MemoryStream _TestStream;
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
