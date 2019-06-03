using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.StaticClasses;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Default str implementation for OpenLisp.NET
    /// </summary>
    public class OpenLispString : OpenLispVal
    {
        private string _value;

        /// <summary>
        /// Publicly Get or privately Set the non-null, non-empty, non-whitespace string.
        /// </summary>
        new public string Value
        {
            get
            {
                //if (string.IsNullOrWhiteSpace(_value)) throw new OpenLispException("Value is null, empty, or white-space.");
                if (string.IsNullOrWhiteSpace(_value))
                {
                    _value = StaticOpenLispTypes.Nil.ToString();
                }
                return _value;
            }
            private set { _value = value; }
        }

        /// <summary>
        /// Constructor accepting a <see cref="string"/>.
        /// </summary>
        /// <param name="value"></param>
        public OpenLispString(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Constructor accepting a <see cref="OpenLispVal"/>
        /// </summary>
        /// <param name="value"></param>
        public OpenLispString(OpenLispVal value)
        {
            Value = value.ToString();
        }

        /// <summary>
        /// Copy this instance by returning this instance.
        /// </summary>
        /// <returns></returns>
        public new OpenLispString Copy()
        {
            return this;
        }

        /// <summary>
        /// Pretty-print an <see cref="OpenLispString"/> surrounding by quotation marks.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => "\"" + Value + "\"";

        /// <summary>
        /// Gets a string representation of a <see cref="OpenLispString"/> with optional pretty-print.
        /// </summary>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public override string ToString(bool printReadably)
        {
            if (Value.Length > 0 && Value[0] == '\u029e')
            {   
                return ":" + Value.Substring(1);
            }
            if (printReadably)
            {
                return "\"" + Value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n") + "\"";
            }
            return Value;
        }

        /// <summary>
        /// Return a new OpenLispInt from a OpenLispString.
        /// </summary>
        /// <param name="v"></param>
        public static explicit operator OpenLispInt(OpenLispString v)
        {
            return new OpenLispInt(long.Parse(v.ToString(false)));
        }
    }
}