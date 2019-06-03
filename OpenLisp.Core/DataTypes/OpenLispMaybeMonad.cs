// This OpenLispMaybeMonad<T> implementation is based heavily upon the tutorial at:
// http://mikhail.io/2016/01/monads-explained-in-csharp/
// Thanks!

using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes;
using OpenLisp.Core.DataTypes.Errors.Throwable;
using OpenLisp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLisp.Core.DataTypes
{
    // TODO: wizard beard meditation...
    public partial class OpenLispMonad<T> : IOpenLispMonad<OpenLispVal> where T : OpenLispVal
    {
        private static readonly MaybeMonad<T> _maybe = new MaybeMonad<T>();

        /// <summary>
        /// Expose the Maybe singleton as a public field.
        /// </summary>
        public static OpenLispMonad<OpenLispVal> Maybe
        {
            get
            {
                return _maybe as OpenLispMonad<OpenLispVal>;
            }
        }

        class MaybeMonad<U> where U : OpenLispVal
        {
            private U _instance;

            public MaybeMonad()
            {
            }

            public MaybeMonad(U instance)
            {
                _instance = instance;
            }
        }
    }
    //public partial class OpenLispMonad<T> : OpenLispVal, IOpenLispMonad<T>
    //{
    //    public class OpenLispMaybeMonad<OpenLispVal> //: OpenLispMonad<T> where T : OpenLispVal
    //    {
    //        private OpenLispVal _value;

    //        private OpenLispMaybeMonad()
    //        {
    //        }

    //        public OpenLispMaybeMonad(OpenLispVal value)
    //        //: base(value)
    //        {
    //            if (value == null)
    //            {
    //                throw new OpenLispException($"{nameof(value)} can not be null.");
    //            }

    //            _value = value;
    //        }

    //        // :(
    //        //public OpenLispMaybeMonad<OpenLispVal> Bind<OpenLispVal>(Func<OpenLispVal, OpenLispMaybeMonad<OpenLispVal>> func)
    //        //{
    //        //    return _value != null ? func((OpenLispVal)_value) : OpenLispMaybeMonad<OpenLispVal>.None();
    //        //}

    //        public static OpenLispMaybeMonad<OpenLispVal> None() => new OpenLispMaybeMonad<OpenLispVal>();
    //    }
    //}
}
