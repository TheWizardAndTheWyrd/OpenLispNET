using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes;

namespace OpenLisp.Core.StaticClasses
{
    /// <summary>
    /// Extension methods useful to all types in OpenLisp.NET
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Wrap any <see cref="OpenLispVal"/> in a first class function.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static OpenLispFunc ToFunc(this OpenLispVal v)
        {
            return new OpenLispFunc(x => v);
        }
    }
}
