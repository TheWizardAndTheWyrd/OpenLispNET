using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes.Errors.Throwable;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Implementation of constants in OpenLisp.NET
    /// </summary>
    public class OpenLispConstant : OpenLispVal
    {
        private string _value;

        /// <summary>
        /// Get or Set the Value of this <see cref="OpenLispConstant"/>.
        /// </summary>
        new public string Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_value)) throw new OpenLispException("Value is null, empty, or white-space.");
                return _value;
            }
            private set { _value = value; }
        }

        /// <summary>
        /// Constructor accepting a <see cref="string"/>.
        /// </summary>
        /// <param name="name"></param>
        public OpenLispConstant(string name)
        {
            Value = name;
        }

        /// <summary>
        /// Returns a "copy" of this constant.  Since it is a constant, just return this instance.
        /// </summary>
        /// <returns></returns>
        public new OpenLispConstant Copy()
        {
            return this;
        }

        /// <summary>
        /// Represent a <see cref="OpenLispConstant"/> as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Represent a <see cref="OpenLispConstant"/> as a string.
        /// Here for completeness.
        /// </summary>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public override string ToString(bool printReadably)
        {
            return Value;
        }
    }
}