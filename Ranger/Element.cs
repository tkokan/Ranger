namespace Ranger
{
    /// <summary>
    /// Element of a row returned by Google APIs
    /// </summary>
    public class Element
    {
        public TextValuePair distance;
        public TextValuePair duration;
        public StatusEnum status;

        /// <summary>
        /// Ctor
        /// </summary>
        public Element()
        {
            distance = null;
            duration = null; ;
            status = StatusEnum.NA;
        }
    }
}