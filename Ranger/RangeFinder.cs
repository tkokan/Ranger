using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ranger
{
    public class RangeFinder
    {
        private const double Limit = 0.00001;
        private const int N = 80;

        public GeoLocation Origin { get; }
        public int RangeSeconds { get; }

        private DistanceMatrixAPI distanceAPI;
        private StaticMapsAPI staticMapsAPI;

        private Node borderHead;
        private int numNodes;

        public RangeFinder(GeoLocation origin, int rangeMins)
        {
            Origin = origin;
            RangeSeconds = 60 * rangeMins;

            distanceAPI = new DistanceMatrixAPI(Origin);
            staticMapsAPI = new StaticMapsAPI(Origin);
        }

        public void Init()
        {
            //const double startingMultipier = 1000.0;

            //var east = FindIntersection(Origin, 1.0, 0.0, 0);
            //var north = FindIntersection(Origin, 0.0, 1.0, 0);
            //var west = FindIntersection(Origin, -1.0, 0.0, 0);
            //var south = FindIntersection(Origin, 0.0, -1.0, 0);

            var east = new GeoLocation(43.505586, 17.985984);
            var north = new GeoLocation(44.251940, 16.535788);
            var west = new GeoLocation(43.563329, 15.945273);
            var south = new GeoLocation(43.132685, 16.465615);

            var directions = new DirectionEnum[]
            {
                DirectionEnum.East,
                DirectionEnum.North,
                DirectionEnum.West,
                DirectionEnum.South
            };

            borderHead = new Node(east);

            var northNode = new Node(north);
            var westNode = new Node(west);
            var southNode = new Node(south);

            var startingPoints = new GeoLocation[]
           {
                east,
                north,
                west,
                south
           };

            staticMapsAPI.GenerateMap(startingPoints, "..\\..\\Maps\\map1.png");

            borderHead.SetNext(northNode, GetMiddlePoint(east, north));
            northNode.SetNext(westNode, GetMiddlePoint(north, west));
            westNode.SetNext(southNode, GetMiddlePoint(west, south));
            southNode.SetNext(borderHead, GetMiddlePoint(south, east));

            staticMapsAPI.GenerateMap(GetAllPoints(), "..\\..\\Maps\\map2.png");
        }

        private IEnumerable<GeoLocation> GetAllPoints()
        {
            var points = new List<GeoLocation>();

            var curr = borderHead;

            do
            {
                points.Add(curr.Point);
                points.Add(curr.MiddlePoint);
                curr = curr.Next;
            } while (curr != borderHead);

            return points;
        }

        private GeoLocation GetMiddlePoint(GeoLocation firstPoint, GeoLocation secondPoint)
        {
            var x = (firstPoint.Latitude + secondPoint.Latitude) / 2.0;
            var y = (firstPoint.Longitude + secondPoint.Longitude) / 2.0;
            var startingPoint = new GeoLocation(x, y);

            double multX = secondPoint.Latitude - firstPoint.Latitude;
            double multY = firstPoint.Longitude - secondPoint.Longitude;

            double fact = Math.Abs(multX) + Math.Abs(multY);

            return FindIntersection(startingPoint, multX / fact, multY / fact);
        }

        public void GenerateMap()
        {
            throw new NotImplementedException();
        }

        private GeoLocation FindIntersection(GeoLocation startingPoint, double multX, double multY)
        {
            var startingPointDistance = distanceAPI.Query(startingPoint);
            return FindIntersection(startingPoint, multX, multY, startingPointDistance);
        }

        /// <summary>
        /// Finds first intersection of the ray given by "startingPoint + (multX, multY) * t"
        /// and the border.
        /// If startingPoints belongs to the area, it searches from inside out (t > 0),
        /// otherwise it searches from outside in (t less then 0).
        /// </summary>
        private GeoLocation FindIntersection(GeoLocation startingPoint, double multX, double multY, int startingPointDistance)
        {
            var belongs = startingPointDistance <= RangeSeconds;
            var sgn = belongs ? 1.0 : -1.0;

            double low = 0.5;
            int lowDistance = Distance(startingPoint, multX, multY, sgn, low);

            while (lowDistance > RangeSeconds || lowDistance == -1)
            {
                low /= 2.0;
                lowDistance = Distance(startingPoint, multX, multY, sgn, low);
                Debug.WriteLine("Decreasing low");
            }

            double high = 1.0;
            int highDistance = Distance(startingPoint, multX, multY, sgn, high);

            while (highDistance != -1 && highDistance < RangeSeconds)
            {
                high *= 2;
                highDistance = Distance(startingPoint, multX, multY, sgn, high);
            }

            int cnt = 1;

            var curr = double.NaN;
            int currDistance = -1;

            do
            {
                curr = (low + high) / 2.0;
                currDistance = Distance(startingPoint, multX, multY, sgn, curr);
                Debug.WriteLine("[{0}] :: {1:0.000000}, {2:0.000000}, {3:0.000000} -> {4}", cnt, low, high, curr, currDistance);

                cnt++;

                if (currDistance < RangeSeconds && currDistance != -1)
                    low = curr;
                else if (currDistance > RangeSeconds || currDistance == -1)
                    high = curr;
            } while (currDistance != RangeSeconds && high - low > Limit);

            return new GeoLocation(startingPoint.Latitude + multX * sgn * curr, startingPoint.Longitude + multY * sgn * curr);
        }

        private int Distance(GeoLocation startingPoint, double multX, double multY, double sgn, double t)
        {
            var queryPoint = new GeoLocation(startingPoint.Latitude + multX * sgn * t, startingPoint.Longitude + multY * sgn * t);
            return distanceAPI.Query(queryPoint);
        }

        private bool DurationOK(int v, bool belongs) => belongs ? v <= RangeSeconds : v > RangeSeconds;

        private Line LinearRegression(IEnumerable<Tuple<double, int>> points)
        {
            int n = points.Count();

            var xAvg = points.Average(x => x.Item1);
            var yAvg = points.Average(x => x.Item2);

            var aNum = points.Sum(i => (i.Item1 - xAvg) * (i.Item2 - yAvg));
            var aDenom = points.Sum(i => Math.Pow(i.Item1 - xAvg, 2.0));
            var a = aNum / aDenom;

            var b = yAvg - a * xAvg;

            return new Line(a, b);
        }
    }
}
