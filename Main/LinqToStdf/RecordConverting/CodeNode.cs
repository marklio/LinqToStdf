// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.

#nullable enable

namespace LinqToStdf.RecordConverting
{
    abstract class CodeNode
    {
        public abstract CodeNode Accept(CodeNodeVisitor visitor);
    }
}
