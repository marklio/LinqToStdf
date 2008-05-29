// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {
	using Records.V4;

	/// <summary>
	/// Encapsulates the registration records in the V4 stdf spec
	/// </summary>
	static class StdfV4Specification {

		public static void RegisterRecords(RecordConverterFactory converterFactory) {
            if (converterFactory == null) {
                throw new ArgumentNullException("converterFactory");
            }
			converterFactory.RegisterRecordType(new RecordType(0, 10), typeof(Far));
			converterFactory.RegisterRecordType(new RecordType(0, 20), typeof(Atr));
			converterFactory.RegisterRecordType(new RecordType(1, 10), typeof(Mir));
			converterFactory.RegisterRecordType(new RecordType(1, 20), typeof(Mrr));
			converterFactory.RegisterRecordType(new RecordType(1, 30), typeof(Pcr));
			converterFactory.RegisterRecordType(new RecordType(1, 40), typeof(Hbr));
			converterFactory.RegisterRecordType(new RecordType(1, 50), typeof(Sbr));
			converterFactory.RegisterRecordType(new RecordType(1, 60), typeof(Pmr));
			converterFactory.RegisterRecordType(new RecordType(1, 62), typeof(Pgr));
			converterFactory.RegisterRecordConverter(new RecordType(1, 63), Plr.ConvertToPlr);
			converterFactory.RegisterRecordType(new RecordType(1, 70), typeof(Rdr));
			converterFactory.RegisterRecordType(new RecordType(1, 80), typeof(Sdr));
			converterFactory.RegisterRecordType(new RecordType(2, 10), typeof(Wir));
			converterFactory.RegisterRecordType(new RecordType(2, 20), typeof(Wrr));
			converterFactory.RegisterRecordType(new RecordType(2, 30), typeof(Wcr));
			converterFactory.RegisterRecordType(new RecordType(5, 10), typeof(Pir));
			converterFactory.RegisterRecordType(new RecordType(5, 20), typeof(Prr));
			converterFactory.RegisterRecordType(new RecordType(10, 30), typeof(Tsr));
			converterFactory.RegisterRecordType(new RecordType(15, 10), typeof(Ptr));
			converterFactory.RegisterRecordType(new RecordType(15, 15), typeof(Mpr));
			converterFactory.RegisterRecordType(new RecordType(15, 20), typeof(Ftr));
			converterFactory.RegisterRecordType(new RecordType(20, 10), typeof(Bps));
			converterFactory.RegisterRecordType(new RecordType(20, 20), typeof(Eps));
            converterFactory.RegisterRecordConverter(new RecordType(50, 10), Gdr.ConvertToGdr);
            converterFactory.RegisterRecordUnconverter(typeof(Gdr), Gdr.ConvertFromGdr);
            converterFactory.RegisterRecordType(new RecordType(50, 30), typeof(Dtr));
		}
	}
}
