using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Ranger.Properties.Settings;

namespace Ranger
{
    /// <summary>
    /// Main grid class.
    /// </summary>
    public class RangeGrid
    {
        public Origin Home { get; }

        private readonly int rangeMins;
        private readonly int unitDistance;

        private readonly DistanceMatrixApi distanceApi;
        private readonly RangerDataContext dbContext;

        private readonly Dictionary<bool, HashSet<LatticePoint>> queriedPoints;

        private double deltaLat;
        private double deltaLon;

        /// <summary>
        /// Ctor
        /// </summary>
        public RangeGrid(string originName, int rangeMins, int unitDistance)
        {
            var connectionStringPath = Path.Combine(Default.RangerFolder, "connectionString.txt");
            var connectionString = File.ReadAllText(connectionStringPath);

            Home = Origin.Load(connectionString, originName);

            if (Home == null)
            {
                throw new Exception("Could not load Origin from DB.");
            }

            queriedPoints = new Dictionary<bool, HashSet<LatticePoint>>()
            {
                //inside points
                [true] = new HashSet<LatticePoint>(),

                // outside points
                [false] = new HashSet<LatticePoint>()
            };

            this.rangeMins = rangeMins;
            this.unitDistance = unitDistance;

            // make sure an exception is thrown if we accidentaly use deltas before setting them
            deltaLat = double.NaN;
            deltaLon = double.NaN;

            var apiKeyPath = Path.Combine(Default.RangerFolder, "apiKey.txt");
            distanceApi = new DistanceMatrixApi(apiKeyPath);

            dbContext = new RangerDataContext(connectionString);
        }

        /// <summary>
        /// Initializes the grid by loading or creating cardinal direction points.
        /// </summary>
        private void Init()
        {
            var directionPoints = new Dictionary<DirectionEnum, IGeoLocation>();

            // load existing cardinal direction points from DB
            foreach (var directionPoint in Home.CardinalDirectionPoints.Where(x => x.UnitDistance == unitDistance))
            {
                directionPoints[directionPoint.Direction] = directionPoint;
            }

            // add any missing direction points
            foreach (DirectionEnum direction in Enum.GetValues(typeof(DirectionEnum)))
            {
                if (directionPoints.ContainsKey(direction))
                {
                    continue;
                }

                directionPoints[direction] = ComputeOffset(direction);

                var newPoint = new CardinalDirectionPoint()
                {
                    OriginId = Home.Id,
                    UnitDistance = unitDistance,
                    Direction = direction,
                    Latitude = directionPoints[direction].Latitude,
                    Longitude = directionPoints[direction].Longitude
                };

                dbContext.CardinalDirectionPoints.InsertOnSubmit(newPoint);
            }

            // save any possible new points
            dbContext.SubmitChanges();

            // set deltas
            deltaLat = (directionPoints[DirectionEnum.North].Latitude - directionPoints[DirectionEnum.South].Latitude) / 2.0;
            deltaLon = (directionPoints[DirectionEnum.East].Longitude - directionPoints[DirectionEnum.West].Longitude) / 2.0;
        }

        private IGeoLocation ComputeOffset(DirectionEnum direction)
        {
            //public static IGeoLocation ComputeOffset(IGeoLocation start, int distance, double bearing, string apiKey)
            var apiKeyPath = Path.Combine(Default.RangerFolder, "apiKey.txt");
            var apiKey = File.ReadAllText(apiKeyPath);
            return GeometryApi.ComputeOffset(Home, unitDistance, direction.Heading(), apiKey);
        }

        /// <summary>
        /// Determines the border.
        /// </summary>
        public void Process()
        {
            Init();

            LoadGridNodes();

            // make sure we have two nodes to start with
            if (queriedPoints.Any(x => x.Value.Count == 0))
            {
                FirstTwoNodes();
            }

            var stack = new Stack<LatticePoint>();
            var pushed = new HashSet<LatticePoint>();

            // initialize the stack
            FindUnprocessedBorder(stack, pushed);

            // process the stack
            ProcessStatck(stack, pushed);
        }

        private void ProcessStatck(Stack<LatticePoint> stack, HashSet<LatticePoint> pushed)
        {
            while (stack.Count > 0)
            {
                var currPoint = stack.Pop();
                Debug.WriteLine($"\tProcessing: {currPoint}");

                // already processed?
                if (queriedPoints.Any(x => x.Value.Contains(currPoint)))
                {
                    continue;
                }

                var inside = Inside(GeoPointFromLatticePoint(currPoint));

                // save this point
                AddNode(currPoint.X, currPoint.Y, inside);

                // possibly add some of this point's neigbors to the stack
                foreach (var firstNei in Neighbors(currPoint))
                {
                    if (pushed.Contains(firstNei))
                    {
                        continue;
                    }

                    foreach (var secondNei in Neighbors(firstNei))
                    {
                        if (queriedPoints[!inside].Contains(secondNei) && !pushed.Contains(firstNei))
                        {
                            stack.Push(firstNei);
                            pushed.Add(firstNei);
                            Debug.WriteLine($"\t\tPushing to stack: {firstNei}");
                            break;
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Creates border that goes between inside points and outside points.
        /// </summary>
        public GeoLocation[] GetBorder(int smoothPct = 0)
        {
            var unprocessedNodes = new HashSet<LatticePoint>(queriedPoints[true].Where(x => HasOutsideCloseNeighbor(x)));

            var borderParts = new List<List<LatticePoint>>();

            while (unprocessedNodes.Count > 0)
            {
                var borderPoints = new List<LatticePoint>();
                var startingCenterAndDirection = StartBorder(unprocessedNodes);
                var startingCenter = startingCenterAndDirection.Item1;
                var direction = startingCenterAndDirection.Item2;
                var currCenter = startingCenter;

                do
                {
                    borderPoints.Add(currCenter);
                    unprocessedNodes.Remove(currCenter.Move(direction).Move(direction.Rotate(1)));
                    currCenter = currCenter.Move(direction, 2);
                    direction = MoveOnBorder(currCenter, direction);
                } while (currCenter != startingCenter);

                borderParts.Add(borderPoints);
            }

            var joinedBorderPoints = JoinBorderParts(borderParts);

            // convert lattice points to geo locations
            var geoBorderPoints = joinedBorderPoints.Select(p => GeoPointFromLatticePoint(p)).ToArray();

            return Smooth(geoBorderPoints, smoothPct);
        }

        private List<LatticePoint> JoinBorderParts(List<List<LatticePoint>> borderParts)
        {
            // first one should be the longest
            var sortedParts = borderParts.OrderByDescending(x => x.Count);
            var biggestPart = sortedParts.First();
            var otherParts = sortedParts.Skip(1).ToList();

            if(otherParts.Count == 0)
            {
                return biggestPart;
            }

            var borderPoints = new List<LatticePoint>();

            foreach(var borderPoint in biggestPart)
            {
                var otherPart = otherParts.SingleOrDefault(x => x.Contains(borderPoint));

                if(otherPart != null)
                {
                    // add all points from the other part
                    var n = otherPart.Count;

                    var first = Enumerable.Range(0, n).Single(x => otherPart[x] == borderPoint);

                    for(var i = 0; i < n; i++)
                    {
                        borderPoints.Add(otherPart[(i + first) % n]);
                    }

                    // this part is done
                    otherParts.Remove(otherPart);
                }

                borderPoints.Add(borderPoint);
            }

            return borderPoints;
        }

        private bool HasOutsideCloseNeighbor(LatticePoint insidePoint)
        {
            return Enum
                .GetValues(typeof(DirectionEnum))
                .Cast<DirectionEnum>()
                .Any(x => queriedPoints[false].Contains(insidePoint.Move(x, 2)));
        }

        private Tuple<LatticePoint, DirectionEnum> StartBorder(HashSet<LatticePoint> unprocessedNodes)
        {
            var firstBorderPoint = unprocessedNodes.OrderByDescending(p => p.Y).ThenBy(p => p.X).First();
            var left = firstBorderPoint.Move(DirectionEnum.West, 2);
            var center = firstBorderPoint.Move(DirectionEnum.West).Move(DirectionEnum.North);

            if (queriedPoints[true].Contains(left))
            {
                return Tuple.Create(center, DirectionEnum.West);
            }
            else
            {
                return Tuple.Create(center, DirectionEnum.South);
            }
        }

        private GeoLocation[] Smooth(IGeoLocation[] geoBorderPoints, int smoothPct)
        {
            var n = geoBorderPoints.Length;

            var smoothed = new GeoLocation[n];

            // apply smoothing
            for (var i = 0; i < n; i++)
            {
                var next = (i + 1) % n;
                var prev = (i - 1 + n) % n;

                smoothed[i] = new GeoLocation()
                {
                    Latitude = Smooth(geoBorderPoints[prev].Latitude, geoBorderPoints[i].Latitude, geoBorderPoints[next].Latitude, smoothPct),
                    Longitude = Smooth(geoBorderPoints[prev].Longitude, geoBorderPoints[i].Longitude, geoBorderPoints[next].Longitude, smoothPct)
                };
            }

            return smoothed;
        }

        private static double Smooth(double a, double b, double c, int smoothPct)
        {
            var smooth = smoothPct / 100.0;
            return b * (1.0 - smooth) + smooth * (a + c) / 2.0;
        }

        private DirectionEnum MoveOnBorder(LatticePoint currCenter, DirectionEnum direction)
        {
            var oneForward = currCenter.Move(direction, 1);

            var directionLeft = direction.Rotate(1);
            var forwardLeft = oneForward.Move(directionLeft);

            if (!queriedPoints[true].Contains(forwardLeft))
            {
                return directionLeft;
            }

            var directionRight = direction.Rotate(-1);
            var forwardRight = oneForward.Move(directionRight);

            if (queriedPoints[true].Contains(forwardRight))
            {
                return directionRight;
            }

            return direction;
        }

        // ToDo: Remove
        private LatticePoint GetStartingBorderPoint()
        {
            var insidePoints = queriedPoints[true];

            foreach (var insidePoint in insidePoints)
            {
                // east
                if (!insidePoints.Contains(insidePoint.Move(DirectionEnum.East, 2)))
                {
                    continue;
                }

                // north
                if (insidePoints.Contains(insidePoint.Move(DirectionEnum.North, 2)))
                {
                    continue;
                }

                // north-east
                if (insidePoints.Contains(insidePoint.Move(DirectionEnum.East, 2).Move(DirectionEnum.North, 2)))
                {
                    continue;
                }

                return insidePoint.Move(DirectionEnum.East, 1).Move(DirectionEnum.North, 1);
            }

            throw new NotImplementedException("No suitable starting border point found.");
        }

        /// <summary>
        /// Finds two neighboring points - one inside, one outside - and stores them to DB.
        /// </summary>
        private void FirstTwoNodes()
        {
            // doing this in two parts because DirectionEnum is not in the DB
            var northernPoint = dbContext
                .CardinalDirectionPoints
                .Where(x => x.OriginId == Home.Id && x.UnitDistance == unitDistance)
                .ToList()
                .Single(x => x.Direction == DirectionEnum.North);

            var low = 0;
            var high = 2;

            while (Inside(GeoPointFromLatticePoint(0, high)))
            {
                high *= 2;
            }

            while (high - low > 2)
            {
                // mid must be even
                var mid = ((high + low) / 4) * 2;

                if (Inside(GeoPointFromLatticePoint(0, mid)))
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            // low: the last even point inside
            AddNode(0, low, true);

            // high: the first even point outside
            AddNode(0, high, false);
        }

        /// <summary>
        /// Finds all unprocessed points that have an inside neighbor and an outside neighbor
        /// </summary>
        private void FindUnprocessedBorder(Stack<LatticePoint> stack, HashSet<LatticePoint> pushedOnStack)
        {
            HashSet<LatticePoint> smallerSet;
            HashSet<LatticePoint> biggerSet;

            if (queriedPoints[true].Count > queriedPoints[false].Count)
            {
                smallerSet = queriedPoints[false];
                biggerSet = queriedPoints[true];
            }
            else
            {
                smallerSet = queriedPoints[true];
                biggerSet = queriedPoints[false];
            }

            foreach (var gridNode in smallerSet)
            {
                foreach (var firstNei in Neighbors(gridNode))
                {
                    if (smallerSet.Contains(firstNei) || biggerSet.Contains(firstNei))
                    {
                        continue;
                    }

                    foreach (var secondNei in Neighbors(firstNei))
                    {
                        if (biggerSet.Contains(secondNei) && !pushedOnStack.Contains(firstNei))
                        {
                            stack.Push(firstNei);
                            pushedOnStack.Add(firstNei);
                            Debug.WriteLine($"\t\tPushing to stack: {firstNei}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns list of all neigbors of a lattice point.
        /// Step is 2 because we only look at points whose both coordinates are even numbers.
        /// </summary>
        private IList<LatticePoint> Neighbors(LatticePoint point)
        {
            var neighbors = new List<LatticePoint>();

            for (var i = -2; i <= 2; i += 2)
            {
                for (var j = -2; j <= 2; j += 2)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }

                    neighbors.Add(new LatticePoint(point.X + i, point.Y + j));
                }
            }

            return neighbors;
        }

        private IGeoLocation GeoPointFromLatticePoint(int x, int y)
        {
            return new GeoLocation()
            {
                Latitude = Home.Latitude + y * deltaLat,
                Longitude = Home.Longitude + x * deltaLon
            };
        }

        private IGeoLocation GeoPointFromLatticePoint(LatticePoint latticePoint)
        {
            return GeoPointFromLatticePoint(latticePoint.X, latticePoint.Y);
        }

        private void AddNode(int x, int y, bool inside)
        {
            var gridNode = new GridNode()
            {
                OriginId = Home.Id,
                RangeMins = rangeMins,
                UnitDistance = unitDistance,
                X = x,
                Y = y,
                Inside = inside
            };

            // add to corresponding hash set
            queriedPoints[inside].Add(new LatticePoint(gridNode));

            // save
            dbContext.GridNodes.InsertOnSubmit(gridNode);
            dbContext.SubmitChanges();
        }

        private void LoadGridNodes()
        {
            foreach (var gridNode in Home.GridNodes.Where(x => x.RangeMins == rangeMins && x.UnitDistance == unitDistance))
            {
                // add to the corresponding hash set
                queriedPoints[gridNode.Inside].Add(new LatticePoint(gridNode));
            }
        }

        private double AerialDistance(DirectionGeoLine geoLine, double high, double low)
        {
            var firstPoint = geoLine.PointOnLine(low);
            var secondPoint = geoLine.PointOnLine(high);

            return Geo.AerialDistance(firstPoint, secondPoint);
        }

        private bool Inside(DirectionGeoLine geoLine, double t)
        {
            return Inside(geoLine.PointOnLine(t));
        }

        private bool Inside(IGeoLocation geoPoint)
        {
            var distance = distanceApi.Query(Home, geoPoint);

            if (distance == -1)
            {
                throw new Exception("Couldn't determine distance.");
            }

            // distance in seconds
            var inside = distance <= rangeMins * 60;

            Debug.WriteLine("\t\t[{0}] {1}", inside ? "+" : "-", geoPoint);

            return inside;
        }
    }
}