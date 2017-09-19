using System;

namespace Ranger
{
    /// <summary>
    /// Point with integer coordinates in Cartesian coordinate system
    /// </summary>
    public struct LatticePoint : IEquatable<LatticePoint>
    {
        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        public LatticePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public LatticePoint(GridNode node) : this(node.X, node.Y) { }

        public override bool Equals(object obj)
        {
            return obj is LatticePoint && this == (LatticePoint)obj;
        }

        public bool Equals(LatticePoint other)
        {
            return other.X == X && other.Y == Y;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public static bool operator ==(LatticePoint first, LatticePoint second) => first.X == second.X && first.Y == second.Y;

        public static bool operator !=(LatticePoint first, LatticePoint second) => !(first == second);

        /// <summary>
        /// Move the point in the given cardinal direction
        /// </summary>
        public LatticePoint Move(DirectionEnum direction, int step = 1)
        {
            return new LatticePoint(X + step * direction.MultX(), Y + step * direction.MultY());
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
