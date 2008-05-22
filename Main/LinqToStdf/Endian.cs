using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {

	/// <summary>
	/// Used to indicate endian-ness
	/// </summary>
	public enum Endian {
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
