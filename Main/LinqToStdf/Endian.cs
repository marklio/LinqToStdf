// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf
{

    /// <summary>
    /// Used to indicate endian-ness
    /// </summary>
    public enum Endian
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Big endian
        /// </summary>
		Big,
        /// <summary>
        /// Little Endian
        /// </summary>
		Little,
    }
}
