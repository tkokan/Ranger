namespace Ranger
{
    /// <summary>
    /// Four cardinal directions
    /// </summary>
    public enum DirectionEnum
    {
        North = 0,
        West = 1,
        South = 2,
        East = 3
    }

    /// <summary>
    /// Helper class for DirectionEnum
    /// </summary>
    public static class DirectionEnumExtensionMethods
    {
        /// <summary>
        /// Positive - counterclockwise
        /// Negative - clockwise
        /// </summary>
        public static DirectionEnum Rotate(this DirectionEnum direction, int step)
        {
            // make sure step is positive, because of the way % works
            while (step < 0)
            {
                step += 4;
            }

            return (DirectionEnum)(((int)direction + step) % 4);
        }

        /// <summary>
        /// Change in the X coordinate as we move in this direction by 1.
        /// </summary>
        public static int MultX(this DirectionEnum direction)
        {
            switch (direction)
            {
                case DirectionEnum.East:
                    return 1;
                case DirectionEnum.West:
                    return -1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Change in the Y coordinate as we move in this direction by 1.
        /// </summary>
        public static int MultY(this DirectionEnum direction)
        {
            switch (direction)
            {
                case DirectionEnum.North:
                    return 1;
                case DirectionEnum.South:
                    return -1;
                default:
                    return 0;
            }
        }

        public static int Heading(this DirectionEnum direction)
        {
            return (360 - (int)direction * 90) % 360;
        }
    }
}