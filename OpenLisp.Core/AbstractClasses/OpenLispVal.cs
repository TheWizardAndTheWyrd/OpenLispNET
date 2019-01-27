﻿using System;
using DeepEqual.Syntax;
using OpenLisp.Core.DataTypes;

namespace OpenLisp.Core.AbstractClasses
{
    /// <summary>
    /// Base class used inherited by all valid language contructs and primitives in OpenLisp.NET
    /// </summary>
    public abstract class OpenLispVal : IComparable
    {
        private OpenLispVal _meta;

        /// <summary>
        /// Get and Set the meta <see cref="OpenLispVal"/>.
        /// </summary>
        public OpenLispVal Meta
        {
            get { return _meta; }
            set { _meta = value; }
        }

        /// <summary>
        /// Performs a memberwise clone of an <see cref="OpenLispVal"/> instance.
        /// </summary>
        /// <returns></returns>
        public virtual OpenLispVal Copy()
        {
            return (OpenLispVal)MemberwiseClone();
        }

        /// <summary>
        /// Gets the string representation of an <see cref="OpenLispVal"/> instance.
        /// </summary>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public virtual string ToString(bool printReadably)
        {
            return ToString();
        }

        /// <summary>
        /// By default, an <see cref="OpenLispVal"/> is not a List or collection of any kind.
        /// </summary>
        /// <returns></returns>
        public virtual bool ListQ()
        {
            return false;
        }

        /// <summary>
        /// Compares one object to another.
        /// </summary>
        /// <returns>The to.</returns>
        /// <param name="obj">Object.</param>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (this.IsDeepEqual(obj)) return 0;

            return -1;
        }

        /// <summary>
        /// An explicit conversion from OpenLispVal to an OpenLispString.
        /// </summary>
        /// <param name="v"></param>
        public static explicit operator OpenLispVal(string v)
        {
            //throw new NotImplementedException();
            return new OpenLispString(v);
        }

        /// <summary>
        /// Returns a new <see cref="OpenLispInt"/>.
        /// </summary>
        /// <param name="v"></param>
        public static explicit operator OpenLispVal(int v)
        {
            return new OpenLispInt(v);
        }

        public static explicit operator long(OpenLispVal v)
        {
            return ((OpenLispInt)v).Value;
        }
    }
}