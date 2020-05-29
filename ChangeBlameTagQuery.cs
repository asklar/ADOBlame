using System;
using System.Linq;

namespace ADOCLI
{
    internal partial class ChangeBlameController
    {
        private class ChangeBlameTagQuery : ChangeBlameQuery
        {
            public string Tag
            {
                get;
                set;
            }

            public override string GetFieldName()
            {
                return "System.Tags";
            }

            public override string ToString()
            {
                return "Tags: " + Tag;
            }

            public override string GetOperator()
            {
                return "CONTAINS";
            }

            public override bool Matches(string strValue)
            {
                return strValue.Contains(Tag, StringComparison.OrdinalIgnoreCase);
            }

            public override string GetValue()
            {
                return Tag;
            }
        }
    }
}
