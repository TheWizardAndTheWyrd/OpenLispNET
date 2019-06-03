using System;
using System.Collections.Generic;
using OpenLisp.Core.DataTypes;
using OpenLisp.Core.AbstractClasses;

namespace OpenLisp.Core.Attributes
{
    /// <summary>
    /// Utility class to read attributes at runtime.
    /// </summary>
    public static class RuntimeAttributeReader
    {
        /// <summary>
        /// Get the DocStrings as an OpenLispList.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static OpenLispList GetDocStrings(object value)
        {
            List<OpenLispVal> docStrings = new List<OpenLispVal>(); 

            // Top level
            foreach (Object attributes in value.GetType().GetCustomAttributes(true))
            {
                var currentObjectType = typeof(object);

                if (currentObjectType == typeof(OpenLispVal))
                {
                    var docString = (DocString)attributes;

                    if (docString != null)
                    {
                        docStrings.Add((OpenLispVal)docString.Text);
                    }
                }
            }

            return new OpenLispList(docStrings);
        }
    }
}
