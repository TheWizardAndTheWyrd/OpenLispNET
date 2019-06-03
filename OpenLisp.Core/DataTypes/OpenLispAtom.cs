using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes.Errors.Throwable;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Implementation of an atom in OpenLisp.NET
    /// </summary>
    public class OpenLispAtom : OpenLispVal
    {
        private OpenLispVal _value;

        /// <summary>
        /// Get or Set the Value of this <see cref="OpenLispAtom"/>.
        /// </summary>
        new public OpenLispVal Value
        {
            get
            {
                if (_value == null) throw new OpenLispException("Value is null.");
                return _value;
            }
            //private set { _value = value; }
            set { _value = value; }
        }

        /// <summary>
        /// Constructor accepting a <see cref="OpenLispVal"/>.
        /// </summary>
        /// <param name="value"></param>
        public OpenLispAtom(OpenLispVal value)
        {
            Value = value;
        }

        /// <summary>
        /// Print a string representation of this <see cref="OpenLispAtom"/> in a printer friendly form.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "(atom " + StaticClasses.Printer.PrStr(Value, true) + ")";
        }

        /// <summary>
        /// Print a representation of this <see cref="OpenLispAtom"/>.
        /// </summary>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public override string ToString(bool printReadably)
        {
            return "(atom " + StaticClasses.Printer.PrStr(Value, printReadably) + ")";
        }
    }
}