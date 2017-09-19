namespace Ranger
{
    /// <summary>
    /// Helper class for results returned by Google APIs
    /// </summary>
    public class TextValuePair
    {
        public string text;
        public int value;

        public TextValuePair()
        {
            text = string.Empty;
            value = -1;
        }
    }
}