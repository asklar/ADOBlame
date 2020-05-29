using System;

namespace ADOCLI
{
    internal partial class ChangeBlameController
    {
        private class ChangeBlameFieldQuery : ChangeBlameQuery
        {
            public string Field
            {
                get;
                set;
            }

            public string Value
            {
                get;
                set;
            }

            public override string ToString()
            {
                return Field + "=" + Value;
            }

            public override string GetFieldName()
            {
                if (Field.Contains(' '))
                {
                    return "System." + Field.Replace(" ", "");
                }
                return Field;
            }

            public override string GetOperator()
            {
                return "=";
            }

            public override bool Matches(string strValue)
            {
                return string.Equals(strValue, Value, StringComparison.OrdinalIgnoreCase);
            }

            public override string GetValue()
            {
                return Value;
            }
        }
    }
}
