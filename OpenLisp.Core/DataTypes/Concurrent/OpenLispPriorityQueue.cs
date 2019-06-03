using OpenLisp.Core.AbstractClasses;
using OpenLisp.Core.Attributes;
using System;

namespace OpenLisp.Core.DataTypes.Concurrent
{
    /// <summary>
    /// Type to implement a thread-safe priority queue.
    /// </summary>
    [DocString("OpenLispPriorityQueue implements a thread-safe priority queue.")]
    public class OpenLispPriorityQueue : OpenLispVal
    {
        /// <summary>
        /// Default contstructor.
        /// </summary>
        public OpenLispPriorityQueue()
        {
        }
    }
}
