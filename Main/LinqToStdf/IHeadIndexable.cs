using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {

    /// <summary>
    /// An interface that builds on <see cref="IRecordContext"/> to
    /// indicate that an object is indexable by HeadNumber.
    /// That is, it is associated with a particular Head.
    /// </summary>
    public interface IHeadIndexable : IRecordContext {
        byte HeadNumber { get; }
    }
}
