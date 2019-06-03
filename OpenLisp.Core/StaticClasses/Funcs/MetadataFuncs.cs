using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.Attributes;
using OpenLisp.Core.DataTypes;

namespace OpenLisp.Core.StaticClasses.Funcs
{
    /// <summary>
    /// Funcs used to manipulate OpenLisp.NET metadata.
    /// </summary>
    public class MetadataFuncs
    {
        /// <summary>
        /// Returns an <see cref="OpenLispFunc"/> containing the meta of an instance of <see cref="OpenLispVal"/>.
        /// </summary>
        public static OpenLispFunc Meta = new OpenLispFunc(x => x[0].Meta);

        /// <summary>
        /// Returns an <see cref="OpenLispFunc"/> as a new instance of an <see cref="OpenLispFunc"/> that
        /// has had the first parameter invoke <see cref="OpenLispVal.Copy"/>, retrieve that new instance's
        /// <see cref="OpenLispVal.Meta"/>, and assign the value of that meta to the <see cref="OpenLispVal"/>
        /// instance that is the second parameter of the invocation.
        /// </summary>
        public static OpenLispFunc WithMeta = new OpenLispFunc(x => x[0].Copy().Meta = x[1]);

        /// <summary>
        /// Get the DocString and stick it in the Meta
        /// </summary>
        public static OpenLispFunc ReadDocString = new OpenLispFunc(x => x.Meta = RuntimeAttributeReader.GetDocStrings((OpenLispVal)x[0]));
    }
}