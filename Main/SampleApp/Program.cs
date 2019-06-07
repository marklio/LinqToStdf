// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToStdf;
using LinqToStdf.Records.V4;

#nullable enable

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if ((args.Length < 1) || (args.Length > 2))
            {
                Usage();
                return;
            }

            var stdf = new StdfFile(args[0]);

            //silly query
            var query = from r in stdf.GetRecords()
                        select r;

            Console.WriteLine("There were {0} records!", query.Count());

            var testTimes = from prr in stdf.GetRecords().OfExactType<Prr>()
                            let testTime = prr.TestTime
                            where testTime != null
                            select testTime.Value / 1000.0;

            Console.WriteLine("Average Test Time: {0}", testTimes.Average());

            //Get PTR results from failing parts grouped by test number
            var results = from prr in stdf.GetRecords().OfExactType<Prr>()
                          where prr.Failed ?? false
                          from ptr in prr.GetChildRecords().OfExactType<Ptr>()
                          let result = ptr.Result
                          where result != null
                          group result.Value by ptr.TestNumber into g
                          select new { TestNumber = g.Key, Results = g };

            foreach (var test in results)
            {
                Console.WriteLine("Test {0}:", test.TestNumber);
                foreach (var result in test.Results)
                {
                    Console.WriteLine("\t{0}", result);
                }
            }

            // Output STDF to a new file
            if (args.Length == 2)
            {
                Console.WriteLine("Writing to {0}...", args[1]);
                using (StdfFileWriter writer = new StdfFileWriter(args[1]))
                {
                    writer.WriteRecords(stdf.GetRecords());
                }
            }
        }

        private static void Usage()
        {
            Console.WriteLine(@"SampleApp <path to STDF> <optional path to STDF output>");
        }
    }
}
