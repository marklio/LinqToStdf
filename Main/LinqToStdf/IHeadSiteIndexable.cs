using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {

    /// <summary>
    /// This interface builds on <see cref="IHeadIndexable"/>
    /// to indicate an object is associated with a particular
    /// site.
    /// </summary>
    public interface IHeadSiteIndexable : IHeadIndexable {
        byte SiteNumber { get; }
    }
}
