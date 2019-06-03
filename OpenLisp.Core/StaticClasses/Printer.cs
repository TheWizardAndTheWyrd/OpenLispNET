using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataStructures;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes;

namespace OpenLisp.Core.StaticClasses
{
    /// <summary>
    /// Printer implementation suitable for most OpenLisp.NET REPLs.
    /// </summary>
    public static class Printer
    {
        /// <summary>
        /// Joins a <see cref="IList{OpenLispVal}"/> with a <see cref="string"/> delimeter.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="delim"></param>
        /// <param name="printReadably">Whether or not to pretty-print the result.</param>
        /// <returns></returns>
        public static string Join(IList<OpenLispVal> values, string delim, bool printReadably)
        {
            return String.Join(delim, values.Select(v => v.ToString(printReadably)).ToArray());
        }

        /// <summary>
        /// Join the specified values, delim and printReadably.
        /// </summary>
        /// <returns>The join.</returns>
        /// <param name="values">Values.</param>
        /// <param name="delim">Delim.</param>
        /// <param name="printReadably">If set to <c>true</c> print readably.</param>
        public static string Join(ConcurrentSkipList<OpenLispVal> values, string delim, bool printReadably)
        {
            var vals = values.ToList().ToArray();           

            return Join(vals, delim, printReadably);
        }

        /// <summary>
        /// Joins a <see cref="IDictionary{T1, T2}"/> with <see cref="string"/> keys and
        /// <see cref="OpenLispVal"/> values using a <see cref="string"/> delimeter.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="delim"></param>
        /// <param name="printReadably">Whether or not to pretty-print the result.</param>
        /// <returns></returns>
        public static string Join(IDictionary<string, OpenLispVal> values, string delim, bool printReadably)
        {
            List<string> strings = new List<string>();

            foreach (KeyValuePair<string, OpenLispVal> v in values)
            {
                if (v.Key.Length > 0 && v.Key[0] == '\u029e')
                {
                    strings.Add(":" + v.Key.Substring(1));
                }
                else if (printReadably)
                {
                    strings.Add("\"" + v.Key + "\"");
                }
                else
                {
                    strings.Add(v.Key);
                }

                strings.Add(v.Value.ToString(printReadably));
            }

            return String.Join(delim, strings.ToArray());
        }

        /// <summary>
        /// pr-str implementation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public static string PrStr(OpenLispVal value, bool printReadably)
        {
            return value.ToString(printReadably);
        }

        /// <summary>
        /// pr-str arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="separator"></param>
        /// <param name="printReadably"></param>
        /// <returns></returns>
        public static string PrStrArgs(OpenLispList args, String separator, bool printReadably)
        {
            return Join(args.Value, separator, printReadably);
        }

        /// <summary>
        /// Escapes a string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string EscapeString(string str)
        {
            return Regex.Escape(str);
        }
    }
}