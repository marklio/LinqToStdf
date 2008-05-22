using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.Attributes {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class StdfDependencyProperty : StdfFieldLayoutAttribute {
        public int DependentOnIndex { get; set; }
    }
}
