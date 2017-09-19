namespace Ranger
{
    public class Node
    {
        public GeoLocation Point { get; }
        public Node Next { get; private set; }
        public GeoLocation MiddlePoint { get; private set; }

        public Node (GeoLocation point)
        {
            Point = point;
        }

        public void SetNext(Node next, GeoLocation middlePoint)
        {
            Next = next;
            MiddlePoint = middlePoint;
        }

    }
}