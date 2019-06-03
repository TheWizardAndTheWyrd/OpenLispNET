using System;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.StaticClasses;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Default int implementation of OpenLisp.NET
    /// </summary>
    public class OpenLispInt : OpenLispVal
    {
        private Int64 _value;

        /// <summary>
        /// All <see cref="OpenLispInt"/> Values box <see cref="Int64"/> instances.
        /// </summary>
        new public Int64 Value
        {
            get { return _value; }
            private set { _value = value; }
        }

        /// <summary>
        /// Constructor accepting an <see cref="Int64"/> parameter.
        /// </summary>
        /// <param name="value"></param>
        public OpenLispInt(Int64 value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OpenLisp.Core.DataTypes.OpenLispInt"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public OpenLispInt(OpenLispVal value)
        {
            Value = ((OpenLispInt)value).Value;
        }

        /// <summary>
        /// Returns a "copy" of an <see cref="OpenLispInt"/>.
        /// </summary>
        /// <returns></returns>
        public new OpenLispInt Copy()
        {
            return this;
        }

        /// <summary>
        /// Pretty-print an <see cref="OpenLispInt"/> as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Prints an <see cref="OpenLispInt"/> as a string with optional pretty-printing.
        /// </summary>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public override string ToString(bool printReadably)
        {
            return Value.ToString();
        }

        /// <summary>
        /// Override the less than operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispConstant operator <(OpenLispInt a, OpenLispInt b)
        {
            return a.Value < b.Value ? StaticOpenLispTypes.True : StaticOpenLispTypes.False;
        }

        /// <summary>
        /// Override the less than or equal to operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispConstant operator <=(OpenLispInt a, OpenLispInt b)
        {
            return a.Value <= b.Value ? StaticOpenLispTypes.True : StaticOpenLispTypes.False;
        }

        /// <summary>
        /// Override the greater than operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispConstant operator >(OpenLispInt a, OpenLispInt b)
        {
            return a.Value > b.Value ? StaticOpenLispTypes.True : StaticOpenLispTypes.False;
        }

        /// <summary>
        /// Override the greater than or equal to operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispConstant operator >=(OpenLispInt a, OpenLispInt b)
        {
            return a.Value >= b.Value ? StaticOpenLispTypes.True : StaticOpenLispTypes.False;
        }

        /// <summary>
        /// Override the addition operator for <see cref="OpenLispInt"/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispInt operator +(OpenLispInt a, OpenLispInt b)
        {
            return new OpenLispInt(a.Value + b.Value);
        }

        /// <summary>
        /// Override the substraction operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispInt operator -(OpenLispInt a, OpenLispInt b)
        {
            return new OpenLispInt(a.Value - b.Value);
        }

        /// <summary>
        /// Override the mutiplication operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispInt operator *(OpenLispInt a, OpenLispInt b)
        {
            return new OpenLispInt(a.Value * b.Value);
        }

        /// <summary>
        /// Override the division operator for <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static OpenLispInt operator /(OpenLispInt a, OpenLispInt b)
        {
            return new OpenLispInt(a.Value / b.Value);
        }

        /// <summary>
        /// Returns a new <see cref="OpenLispString"/>.
        /// </summary>
        /// <param name="v"></param>
        public static explicit operator OpenLispString(OpenLispInt v)
        {
            return new OpenLispString(v.ToString(false));
        }

        /// <summary>
        /// Return a new OpenLispInt.
        /// </summary>
        /// <param name="v"></param>
        public static explicit operator OpenLispInt(OpenLispString v)
        {
            return new OpenLispInt(long.Parse(v.ToString(false)));
        }

        /// <summary>
        /// Returns a new string without pretty printing.
        /// </summary>
        /// <param name="v"></param>
        public static explicit operator String(OpenLispInt v)
        {
            return (String)v.ToString(false);
        }
    }
}