using System;

namespace OpenLisp.Core.Attributes
{
    /// <summary>
    /// Provide doc strings to any Class.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class,
        AllowMultiple = true)]
    public class DocString : System.Attribute
    {
        private string text;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DocString(string text)
        {
            this.Text = text;
        }

        /// <summary>
        /// DocString Text
        /// </summary>
        public string Text { get => text; set => text = value; }
    }
}
