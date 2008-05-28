using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LinqToStdf;
using LinqToStdf.Records;

namespace SilverlightSample {
    public partial class Page : UserControl {
        public Page() {
            InitializeComponent();
        }

        public struct DisplayData {
            public string Field { get; set; }
            public string Value { get; set; }
        }

        public struct ErrorData {
            public long Offset { get; set; }
            public string Type { get; set; }
            public string Info { get; set; }
        }

        private void Load_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK) {
                var file = new StdfFile(new SilverlightStreamManager(dialog.SelectedFile));
                //don't throw
                file.ThrowOnFormatError = false;
                file.AddFilter(BuiltInFilters.V4ContentSpec);
                var data = new List<DisplayData>();
                var mir = file.GetMir();
                if (mir == null) {
                    data.Add(new DisplayData {
                        Field = "Error",
                        Value = "No MIR found"
                    });
                }
                else {
                    data.Add(new DisplayData() {
                        Field = "Lot ID",
                        Value = mir.LotId
                    });
                    data.Add(new DisplayData() {
                        Field = "Mode Code",
                        Value = mir.ModeCode
                    });
                    data.Add(new DisplayData() {
                        Field = "Setup Time",
                        Value = mir.SetupTime == null ? "NA" : mir.SetupTime.Value.ToString()
                    });
                    data.Add(new DisplayData() {
                        Field = "Setup Time",
                        Value = mir.StartTime == null ? "NA" : mir.StartTime.Value.ToString()
                    });
                }
                var mrr = file.GetMrr();
                if (mrr == null) {
                    data.Add(new DisplayData {
                        Field = "Error",
                        Value = "No MRR found"
                    });
                }
                else {
                    data.Add(new DisplayData() {
                        Field = "Finish Time",
                        Value = mrr.FinishTime == null ? "NA" : mrr.FinishTime.Value.ToString()
                    });
                    data.Add(new DisplayData() {
                        Field = "Total Record Count",
                        Value = file.GetRecords().Count().ToString()
                    });
                }
                Data.ItemsSource = data;

                var errors = from r in file.GetRecords()
                             where r is ErrorRecord || r is UnknownRecord
                             select new ErrorData {
                                 Offset = r.Offset,
                                 Type = r.GetType().Name,
                             };
                Errors.ItemsSource = errors.ToList();
            }
        }
    }
}
