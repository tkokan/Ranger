namespace Ranger
{
    /// <summary>
    /// Holds info about the four points (one for each cardinal direction) on the range border.
    /// </summary>
    public partial class CardinalDirectionPoint : IGeoLocation
    {
        /// <summary>
        /// One of the four cardinal directions
        /// </summary>
        public DirectionEnum Direction
        {
            // Direction is stored as an integer in the db.

            get => (DirectionEnum)direction;

            set => direction = (int)value;
        }
    }
}
