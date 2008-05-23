// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

//we only need this for silverlight
#if SILVERLIGHT
namespace System.Collections {
    public class BitArray {

        byte[] _Data;
        int _Length;

        public BitArray(byte[] data) {
            _Data = data;
        }

        public BitArray(int length) {
            _Data = new byte[(length + 7) / 8];
        }

        public int Length {
            get { return _Length; }
            set {
                if (value > _Length) {
                    var newData = new byte[(value + 7) / 8];
                    _Data.CopyTo(newData, 0);
                    _Data = newData;
                    _Length = value;
                }
            }
        }

        public void CopyTo(byte[] _Buffer, int start) {
            _Data.CopyTo(_Buffer, start);
        }
    }
}
#endif
