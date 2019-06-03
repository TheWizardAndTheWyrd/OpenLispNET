using System;
using System.Collections.Generic;
using DataStructures;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes.Errors.Throwable;
using OpenLisp.Core.StaticClasses;
using OpenLisp.Core.Attributes;

namespace OpenLisp.Core.DataTypes.Concurrent
{
    /// <summary>
    /// Open lisp skip list.  On average, all operations should be log(n).
    /// </summary>
    [DocString("OpenLispSkipList implements a thread-safe skip-list that averages O(log(n))... usually.")]
    public class OpenLispSkipList : OpenLispList
    {
        private ConcurrentSkipList<OpenLispVal> _value;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public new ConcurrentSkipList<OpenLispVal> Value
        {
            get
            {
                //if (_value == null) throw new OpenLispException("Value is null.");
                if (_value == null)
                {
                    _value = StaticOpenLispTypes.EmptyConcurrentSkipList;
                }
                return _value;
            }
            set { _value = value; }
        }

        /// <summary>
        /// Is this object a list?
        /// </summary>
        /// <returns><c>true</c>, if q was listed, <c>false</c> otherwise.</returns>
        public override bool ListQ()
        {
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OpenLisp.Core.DataTypes.Concurrent.OpenLispSkipList"/> class.
        /// </summary>
        public OpenLispSkipList()
            :base()
        {
            // TODO: finalize skip list start and end tokens
            //Start = "o(";
            //End = ")o";

            Value = new ConcurrentSkipList<OpenLispVal>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OpenLisp.Core.DataTypes.Concurrent.OpenLispSkipList"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public OpenLispSkipList(List<OpenLispVal> value) 
            :this()
        {
            // TODO: extend ConcurrentSkipList to accept a collection
            foreach (var v in value) {
                Value.Add(v);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OpenLisp.Core.DataTypes.Concurrent.OpenLispSkipList"/> class.
        /// </summary>
        /// <param name="value">Value.</param>
        public OpenLispSkipList(OpenLispList value)
            :this(value.Value) 
        {

        }

        /// <summary>
        /// Constsructor accepting a <see cref="OpenLispVal"/> array for params. 
        /// </summary>
        /// <param name="value"></param>
        public OpenLispSkipList(params OpenLispVal[] value)
            :this()
        {
            Conj(value);
        }

        /// <summary>
        /// Conj operation accepting a <see cref="OpenLispVal"/> array for params.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public new OpenLispSkipList Conj(params OpenLispVal[] values)
        {
            foreach (var v in values)
            {
                Value.Add(v);
            }

            return this;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:OpenLisp.Core.DataTypes.Concurrent.OpenLispSkipList"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:OpenLisp.Core.DataTypes.Concurrent.OpenLispSkipList"/>.</returns>
        public override string ToString()
        {
            return Start + Printer.Join(Value, " ", true) + End;
        }

        /// <summary>
        /// Returns a string representation of the skip list.
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="printReadably">If set to <c>true</c> print readably.</param>
        public override string ToString(bool printReadably)
        {
            return Start + Printer.Join(Value, " ", printReadably) + End;
        }
    }
}
