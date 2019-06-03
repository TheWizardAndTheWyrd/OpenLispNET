using System;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes.Errors.Throwable;
using OpenLisp.Core.StaticClasses;

namespace OpenLisp.Core.DataTypes
{
    /// <summary>
    /// Implementation of a func in OpenLisp.NET
    /// </summary>
    public class OpenLispFunc : OpenLispVal
    {
        private Func<OpenLispList, OpenLispVal> _lambda = null;

        private OpenLispVal _ast = null;

        private Env _env = null;

        private OpenLispList _fparams;

        // TODO: prove our macros work =)
        private bool _macro = false;

        /// <summary>
        /// Constructor accepting a <see cref="Func{T, TResult}"/>.
        /// </summary>
        /// <param name="lambda"></param>
        public OpenLispFunc(Func<OpenLispList, OpenLispVal> lambda)
        {
            Lambda = lambda;
        }

        /// <summary>
        /// Constructor accepting an <see cref="OpenLispVal"/>, an <see cref="Env"/>, an <see cref="OpenLispList"/>,
        /// and a <see cref="Func{T, TResult}"/>.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="env"></param>
        /// <param name="fparams"></param>
        /// <param name="lambda"></param>
        public OpenLispFunc(OpenLispVal ast, Env env, OpenLispList fparams, Func<OpenLispList, OpenLispVal> lambda)
        {
            Ast = ast;
            Env = env;
            FParams = fparams;
            Lambda = lambda;
        }

        /// <summary>
        /// Publicly Get and privately Set the <see cref="Func{T, TResult}"/>
        /// </summary>
        public Func<OpenLispList, OpenLispVal> Lambda
        {
            get
            {
                return _lambda;
            }
            private set { _lambda = value; }
        }

        /// <summary>
        /// Publicly Get and privately Set the <see cref="OpenLispVal"/> representing the AST.
        /// </summary>
        public OpenLispVal Ast
        {
            get
            {
                return _ast;
            }
            private set { _ast = value; }
        }

        /// <summary>
        /// Publicly Get and privately Set the <see cref="Env"/>.
        /// </summary>
        public Env Env
        {
            get
            {
                return _env;
            }
            private set { _env = value; }
        }

        /// <summary>
        /// Generates a new <see cref="Env"/> from an <see cref="OpenLispList"/> parameter.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Env GenEnv(OpenLispList args)
        {
            return new Env(Env, FParams, args);
        }

        /// <summary>
        /// Publicly Get and privately Set the <see cref="OpenLispList"/> parameters
        /// of an <see cref="OpenLispFunc"/>.
        /// </summary>
        public OpenLispList FParams
        {
            get
            {
                return _fparams;
            }
            private set { _fparams = value; }
        }

        /// <summary>
        /// Get or Set whether this <see cref="OpenLispList"/> is a macro.
        /// </summary>
        public bool Macro
        {
            get { return _macro; }
            //private set { _macro = value; }
            set { _macro = value; }
        }

        /// <summary>
        /// Returns a string representation of an OpenLispFunc.
        /// 
        /// TODO: create a parameterized version that can override "builtin_function" per package.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Ast != null
            ? "<fn* " + Printer.PrStr(FParams, true) +
              " " + Printer.PrStr(Ast, true) + ">"
            : "<builtin_function " + Lambda + ">";

        /// <summary>
        /// Apply the <see cref="OpenLispFunc"/> with an <see cref="OpenLispList"/> of parameters.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public OpenLispVal Apply(OpenLispList args)
        {
            return Lambda(args);
        }
    }
}