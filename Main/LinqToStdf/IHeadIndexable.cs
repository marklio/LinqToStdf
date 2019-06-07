// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf
{

    /// <summary>
    /// An interface that builds on <see cref="IRecordContext"/> to
    /// indicate that an object is indexable by HeadNumber.
    /// That is, it is associated with a particular Head.
    /// </summary>
    public interface IHeadIndexable : IRecordContext
    {
        byte? HeadNumber { get; }
    }
}
