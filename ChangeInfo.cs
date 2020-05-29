using System;

namespace ADOCLI
{
    internal partial class ChangeBlameController
    {
        internal class ChangeInfo
        {
            public string Author
            {
                get;
                set;
            }

            public DateTime ChangedDate
            {
                get;
                set;
            }

            public override string ToString()
            {
                return $"{Author}\t{ChangedDate.ToLocalTime()}";
            }
        }
    }
}
