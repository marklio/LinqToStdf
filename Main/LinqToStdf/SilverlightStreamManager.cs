using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LinqToStdf {
    public class SilverlightStreamManager : IStdfStreamManager {

        FileDialogFileInfo _FileInfo;

        public SilverlightStreamManager(FileDialogFileInfo fileInfo) {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");
            _FileInfo = fileInfo;
        }

        #region IStdfStreamManager Members

        public string Name {
            get { return _FileInfo.Name; }
        }

        public IStdfStreamScope GetScope() {
            return new OwnedStdfStreamScope(_FileInfo.OpenRead());
        }

        #endregion
    }
}
