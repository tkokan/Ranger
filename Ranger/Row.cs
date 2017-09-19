namespace Ranger
{
    /// <summary>
    /// Row returned by Google APIs
    /// </summary>
    public class Row
    {
        public Element[] elements;

        /// <summary>
        /// Ctor
        /// </summary>
        public Row()
        {
            elements = null;
        }
    }
}
