using System;
using DeepEqual.Syntax;
using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.DataTypes;
using System.Collections.Generic;
using DataStructures;

namespace OpenLisp.Core.StaticClasses
{
    /// <summary>
    /// OpenLisp.NET native static types.  Keeping these static means a smaller memory footprint.
    /// </summary>
    public static class StaticOpenLispTypes
    {
        /// <summary>
        /// Compares equality between two instances of <see cref="object"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool OpenLispEqualB(object a, object b)
        {
            Type typeA = a.GetType();
            Type typeB = b.GetType();

            if (!(typeA == typeB) && !(a.IsDeepEqual(b)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Compares equality between two instances of <see cref="OpenLispVal"/>.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool OpenLispEqualQ(OpenLispVal a, OpenLispVal b)
        {
            Type typeA = a.GetType();
            Type typeB = b.GetType();


            if ((typeA != typeB) && (!(a is OpenLispList) || !(b is OpenLispList))) return false;

            if (a is OpenLispInt)
            {
                return ((OpenLispInt) a).Value ==
                       ((OpenLispInt) b).Value;
            }

            if (a is OpenLispSymbol)
            {
                return ((OpenLispSymbol) a).Value ==
                       ((OpenLispSymbol) b).Value;
            }

            if (a is OpenLispString)
            {
                return ((OpenLispString) a).Value ==
                       ((OpenLispString) b).Value;
            }

            if (!(a is OpenLispList)) return a == b;

            if (((OpenLispList) a).Size != ((OpenLispList) b).Size) return false;

            for (int i = 0; i < ((OpenLispList) a).Size; i++)
            {
                if (OpenLispEqualQ(((OpenLispList) a)[i], ((OpenLispList) b)[i]))
                    continue;

                return false;
            }

            return true;
            //return (typeA == typeB) || (a is OpenLispList && b is OpenLispList);
        }

        /// <summary>
        /// Returns the instance <see cref="OpenLispConstant"/> representing nil.
        /// </summary>
        public static OpenLispConstant Nil = new OpenLispConstant("nil");

        /// <summary>
        /// Returns the instance of <see cref="OpenLispConstant"/> representing true.
        /// </summary>
        public static OpenLispConstant True = new OpenLispConstant("true");

        /// <summary>
        /// Returns the instance of <see cref="OpenLispConstant"/> representing false.
        /// </summary>
        public static OpenLispConstant False = new OpenLispConstant("false");

        /// <summary>
        /// Returns a map of only Nil values.
        /// </summary>
        //public static OpenLispHashMap NilMap = new OpenLispHashMap(new OpenLispList(new List<OpenLispVal>() {Nil, Nil}));

        /// <summary>
        /// Returns and empty dictionary of <see cref="Dictionary{TKey, TValue}"/>
        /// where TKey is <see cref="String"/> and TValue is <see cref="OpenLispVal"/>.
        /// </summary>
        public static Dictionary<string, OpenLispVal> EmptyDictionary = new Dictionary<string, OpenLispVal>();

        /// <summary>
        /// Returns an empty secondary dictionary of <see cref="Dictionary{TKey, TValue}"/>
        /// where TKey is <see cref="OpenLispString"/> and TValue is <see cref="OpenLispVal"/>.
        /// </summary>
        public static Dictionary<OpenLispString, OpenLispVal> EmptySecondaryDictionary = new Dictionary<OpenLispString, OpenLispVal>();

        /// <summary>
        /// Returns an empty <see cref="ConcurrentSkipList{T}"/> where T is <see cref="OpenLispVal"/>.
        /// </summary>
        public static ConcurrentSkipList<OpenLispVal> EmptyConcurrentSkipList = new ConcurrentSkipList<OpenLispVal>();

        /// <summary>
        /// Returns an empty <see cref="List{T}"/> where T is <see cref="OpenLispVal"/>.
        /// </summary>
        public static List<OpenLispVal> EmpyListOpenLispVal = new List<OpenLispVal>();
    }
}