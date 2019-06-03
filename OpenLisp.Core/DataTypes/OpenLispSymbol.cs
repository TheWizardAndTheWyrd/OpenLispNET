using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes.Errors.Throwable;
using OpenLisp.Core.StaticClasses;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Default symbol implementation for OpenLisp.NET
    /// </summary>
    public class OpenLispSymbol : OpenLispVal
    {
        private string _value;

        /// <summary>
        /// Publicly Get or privately Set the <see cref="string"/> value of an <see cref="OpenLispSymbol"/>.
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
        /// Constructor accepting a <see cref="string"/> value.
        /// </summary>
        /// <param name="value"></param>
        public OpenLispSymbol(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Constructor accepting an <see cref="OpenLispString"/> parameter.
        /// </summary>
        /// <param name="value"></param>
        public OpenLispSymbol(OpenLispString value)
        {
            Value = value.Value;
        }

        /// <summary>
        /// Returns this instance when asked to copy an <see cref="OpenLispSymbol"/>.
        /// </summary>
        /// <returns></returns>
        public new OpenLispSymbol Copy()
        {
            return this;
        }

        /// <summary>
        /// Return the underlying value when printing a string representation of an <see cref="OpenLispSymbol"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Return the underlying value when printing a string representation of an <see cref="OpenLispSymbol"/>.
        /// </summary>
        /// <param name="printReadably">Ignored and present for internal API compatibility.</param>
        /// <returns></returns>
        public override string ToString(bool printReadably)
        {
            return Value;
        }
    }
}