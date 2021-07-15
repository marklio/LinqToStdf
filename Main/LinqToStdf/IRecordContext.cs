﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {

    /// <summary>
    /// Base interface for things that provide access to an StdfFile.
    /// </summary>
    /// <remarks>
    /// This allows extension methods to exploit the StdfFile to
    /// provide lots of shortcuts.
    /// </remarks>
    public interface IRecordContext {
        /// <summary>
        /// The StdfFile associated with the context.
        /// </summary>
        StdfFile? StdfFile { get; }
    }
}
