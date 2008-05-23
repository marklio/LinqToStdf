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

namespace SilverlightSample {
    public partial class Page : UserControl {
        public Page() {
            InitializeComponent();
        }

        public struct DisplayData {
            public string Field { get; set; }
            public string Value { get; set; }
        }

        private void Load_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK) {
                var file = new StdfFile(dialog.SelectedFile);
                var data = new List<DisplayData>();
                var mir = file.GetMir();
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
                var mrr = file.GetMrr();
                data.Add(new DisplayData() {
                    Field = "Finish Time",
                    Value = mrr.FinishTime == null ? "NA" : mrr.FinishTime.Value.ToString()
                });
                data.Add(new DisplayData() {
                    Field = "Total Record Count",
                    Value = file.GetRecords().Count().ToString()
                });
                Data.ItemsSource = data;
            }
        }
    }
}
