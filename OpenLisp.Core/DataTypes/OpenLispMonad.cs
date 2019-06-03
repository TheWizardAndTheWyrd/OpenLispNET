using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes;
using OpenLisp.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Currently, we can wrap a <see cref="OpenLispMonad{OpenLispVal}"/> 
    /// inside the OpenLisp.Net runtime <see cref="Env"/>
    /// 
    /// Changes to an instance of <seealso cref="Core.Env"/> should actually
    /// be thread safe and should be transactional.  Instead of modifying the
    /// Env instance, instantiate a new instance and clone from the input.
    /// Previous instances of Env should be stored on a stack and popped off
    /// if an exception is caught so that we return the unmodified environment
    /// just as we received it.
    /// 
    /// TODO: consider creating an Env state diff utility that actually performs 
    /// a diff on two JSON payloads.
    /// 
    /// TODO: introduce memoization so that we can lazily evaluate Monadic 
    /// functions while avoiding premature performance optimizations outside the
    /// scope of Monadic function memoization.
    /// </summary>
    public partial class OpenLispMonad<T> : IOpenLispMonad<OpenLispVal> where T : OpenLispVal
    {
        /// <summary>
        /// Public default constructor
        /// </summary>
        public OpenLispMonad() 
        {
        }

        private Core.Env _env;
        private OpenLispVal _instance;

        /// <summary>
        /// Get the <see cref="Env"/>
        /// </summary>
        public  Env Env => _env;

        /// <summary>
        /// Access the instance
        /// </summary>
        public OpenLispVal Instance => _instance;

        /// <summary>
        /// Get a new <see cref="OpenLispMonad{OpenLispVal}"/> instance
        /// </summary>
        public static OpenLispMonad<OpenLispVal> Monad => new OpenLispMonad<OpenLispVal>();

        /// <summary>
        /// Default public constructor.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="env"></param>
        public OpenLispMonad(OpenLispVal instance, Env env)
        {
            _env = env;
            _instance = instance;
        }

        /// <summary>
        /// Public constructor with an <see cref="OpenLispVal"/>
        /// instance parameter.
        /// </summary>
        /// <param name="instance"></param>
        public OpenLispMonad(OpenLispVal instance)
        {
            _instance = instance;
        }

        /// <summary>
        /// Monadic bind function.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public virtual IOpenLispMonad<U> Bind<U>(Func<IOpenLispMonad<OpenLispVal>, IOpenLispMonad<U>> func)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="openLispFunc"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public virtual IOpenLispMonad<OpenLispVal> TypeConstructor(OpenLispFunc openLispFunc, Env environment)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public virtual IOpenLispMonad<OpenLispVal> UnitFunction(OpenLispVal dataType)
        {
            throw new NotImplementedException();
        }
    }
}
