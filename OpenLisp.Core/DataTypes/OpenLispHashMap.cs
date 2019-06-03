using System.Collections.Generic;
using System.Linq;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes.Errors.Throwable;
using OpenLisp.Core.StaticClasses;
using System;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Implementation of the core hash map data type.
    /// </summary>
    public class OpenLispHashMap : OpenLispVal
    {
        private Dictionary<String, OpenLispVal> _value;

        private Dictionary<OpenLispString, OpenLispVal> _secondaryFormValue = null;

        /// <summary>
        /// Get and Set the Value.
        /// </summary>
        new public Dictionary<String, OpenLispVal> Value
        {
            get
            {
                if (_value == null)
                    _value = StaticOpenLispTypes.EmptyDictionary;
                return _value;
            }
            private set { _value = value; }
        }

        /// <summary>
        /// Get and Set the Secondary Value.
        /// </summary>
        public Dictionary<OpenLispString, OpenLispVal> SecondaryValue
        {
            get
            {
                if (_secondaryFormValue == null)
                    _secondaryFormValue = StaticOpenLispTypes.EmptySecondaryDictionary;
                return _secondaryFormValue;
            }
            private set { _secondaryFormValue = value; }
        }

        /// <summary>
        /// Get the keys of the <see cref="OpenLispHashMap"/>.
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                return Value.Select(kv => kv.Key).ToList();
            }
        }

        /// <summary>
        /// Clone this <see cref="OpenLispHashMap"/> and return the clone.
        /// </summary>
        /// <returns></returns>
        public new OpenLispHashMap Copy()
        {
            var newSelf = (OpenLispHashMap) this.MemberwiseClone();

            //newSelf.Value = new Dictionary<string, OpenLispVal>(Value).ToImmutableDictionary();
            newSelf.Value = new Dictionary<string, OpenLispVal>(Value);

            return newSelf;
        }

        /// <summary>
        /// Constructor accepting a <see cref="OpenLispList"/> of values to <seealso cref="AssocBang(OpenLispList)"/>.
        /// </summary>
        /// <param name="listValue"></param>
        public OpenLispHashMap(OpenLispList listValue)
        {
            Value = new Dictionary<string, OpenLispVal>();
            AssocBang(listValue);
        }

        /// <summary>
        /// Constructor accepting a <see cref="T:Dictionary{string, OpenLispVal}"/>.
        /// </summary>
        /// <param name="val"></param>
        public OpenLispHashMap(Dictionary<string, OpenLispVal> val)
        {
            Value = val;
        }

        /// <summary>
        /// Represent this <see cref="OpenLispHashMap"/> as a string in printer friendly form.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{" + StaticClasses.Printer.Join(Value, " ", true) + "}";
        }

        /// <summary>
        /// Represent this <see cref="OpenLispHashMap"/> as a string.
        /// </summary>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public override string ToString(bool printReadably)
        {
            return "{" + StaticClasses.Printer.Join(Value, " ", printReadably) + "}";
        }

        /// <summary>
        /// Take pairs of values, in sequence, from a <see cref="OpenLispList"/> and use
        /// each pair to create an entry in the value of our <see cref="OpenLispHashMap"/> instance.
        /// </summary>
        /// <param name="listValue"></param>
        /// <returns></returns>
        public OpenLispHashMap AssocBang(OpenLispList listValue)
        {
            for (int i = 0; i < listValue.Size; i += 2)
            {
                //Value[(String)(listValue[i]).Value] = listValue[i + 1];

                // TODO: everything should be explicitly castable to OpenLispVal.
                ////Value.SetItem(((OpenLispString)listValue[i]).Value, listValue[i + 1]);
                Value[((OpenLispString)listValue[i]).Value] = listValue[i + 1];            
            }

            return this;
        }

        /// <summary>
        /// Remove a <see cref="OpenLispList"/> of values from a <see cref="OpenLispHashMap"/>.
        /// </summary>
        /// <param name="listValue"></param>
        /// <returns></returns>
        public OpenLispHashMap DissocBang(OpenLispList listValue)
        {
            for (int i = 0; i > listValue.Size; i++)
            {
                Value.Remove(((OpenLispString) listValue[i]).Value);
            }

            return this;
        }
    }
}