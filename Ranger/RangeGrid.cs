using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ranger
{
    /// <summary>
    /// Main grid class.
    /// </summary>
    public class RangeGrid
    {
        public Origin Home { get; }

        private readonly int gridSize;
        private readonly int rangeMins;

        private readonly DistanceMatrixApi distanceApi;
        private readonly RangerDataContext dbContext;

        private double deltaLat;
        private double deltaLon;

        private HashSet<LatticePoint> insidePoints;
        private HashSet<LatticePoint> outsidePoints;

        List<LatticePoint> borderPoints;

        /// <summary>
        /// Ctor
        /// </summary>
        public RangeGrid(string originName, int rangeMins, int gridSize)
        {
            var connectionStringPath = Path.Combine(Properties.Settings.Default.RangerFolder, "connectionString.txt");
            var connectionString = File.ReadAllText(connectionStringPath);

            Home = Origin.Load(connectionString, originName);

            if (Home == null)
            {
                throw new Exception("Could not load Origin from DB.");
            }

            this.rangeMins = rangeMins;
            this.gridSize = gridSize;

            // make sure an exception is thrown if we accidentaly use deltas before setting them
            deltaLat = double.NaN;
            deltaLon = double.NaN;

            var apiKeyPath = Path.Combine(Properties.Settings.Default.RangerFolder, "apiKey.txt");
            distanceApi = new DistanceMatrixApi(apiKeyPath);
            
            dbContext = new RangerDataContext(connectionString);
        }

        /// <summary>
        /// Initializes the grid by loading or creating cardinal direction points.
        /// </summary>
        public void Init()
        {
            var directionPoints = new Dictionary<DirectionEnum, IGeoLocation>();

            // load existing cardinal direction points from DB
            foreach (var directionPoint in Home.CardinalDirectionPoints.Where(x => x.RangeMins == rangeMins))
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

                directionPoints[direction] = FindIntersection(direction);

                var newPoint = new CardinalDirectionPoint()
                {
                    OriginId = Home.Id,
                    RangeMins = rangeMins,
                    Direction = direction,
                    Latitude = directionPoints[direction].Latitude,
                    Longitude = directionPoints[direction].Longitude
                };

                dbContext.CardinalDirectionPoints.InsertOnSubmit(newPoint);
            }

            // save any possible new points
            dbContext.SubmitChanges();

            // set deltas
            // we're dividing by 2.0 because we only look at even points - odd points are reserved for the border
            deltaLat = (directionPoints[DirectionEnum.North].Latitude - directionPoints[DirectionEnum.South].Latitude) / (2.0 * gridSize);
            deltaLon = (directionPoints[DirectionEnum.East].Longitude - directionPoints[DirectionEnum.West].Longitude) / (2.0 * gridSize);
        }

        /// <summary>
        /// Determines the border.
        /// </summary>
        public void Process()
        {
            LoadGridNodes();

            // make sure we have two nodes to start with
            if (insidePoints.Count == 0 && outsidePoints.Count == 0)
            {
                FirstTwoNodes();
            }

            var stack = new Stack<LatticePoint>();
            var pushed = new HashSet<LatticePoint>();

            // initialize the stack
            FindUnprocessedBorder(insidePoints, outsidePoints, stack, pushed);

            // process the stack
            while (stack.Count > 0)
            {
                var currPoint = stack.Pop();
                Debug.WriteLine($"\tProcessing: {currPoint}");

                // already processed?
                if (insidePoints.Contains(currPoint) || outsidePoints.Contains(currPoint))
                {
                    continue;
                }

                var inside = Inside(GeoPointFromLatticePoint(currPoint));

                // save this point
                AddNode(currPoint.X, currPoint.Y, inside);

                var otherSet = inside ? outsidePoints : insidePoints;

                // possibly add some of this point's neigbors to the stack
                foreach (var firstNei in Neighbors(currPoint))
                {
                    if (pushed.Contains(firstNei))
                    {
                        continue;
                    }

                    foreach (var secondNei in Neighbors(firstNei))
                    {
                        if (otherSet.Contains(secondNei) && !pushed.Contains(firstNei))
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
        public void CreateBorder()
        {
            var startingCenter = GetStartingBorderPoint();

            var direction = DirectionEnum.West;

            borderPoints = new List<LatticePoint>();

            var currCenter = startingCenter;

            do
            {
                borderPoints.Add(currCenter);
                MoveOnBorder(ref currCenter, ref direction);
            } while (currCenter != startingCenter);
        }

        /// <summary>
        /// Creats a dynamic Google map.
        /// </summary>
        public void CreateDynamicMap(int smoothPct = 0)
        {
            // convert lattice points to geo locations
            var nodes = borderPoints.Select(p => GeoPointFromLatticePoint(p)).ToArray();

            var n = nodes.Length;

            var smoothed = new GeoLocation[n];

            // apply smoothing
            for (var i = 0; i < n; i++)
            {
                var next = (i + 1) % n;
                var prev = (i - 1 + n) % n;

                smoothed[i] = new GeoLocation()
                {
                    Latitude = Smoothed(nodes[prev].Latitude, nodes[i].Latitude, nodes[next].Latitude, smoothPct),
                    Longitude = Smoothed(nodes[prev].Longitude, nodes[i].Longitude, nodes[next].Longitude, smoothPct)
                };
            }

            var apiKeyPath = Path.Combine(Properties.Settings.Default.RangerFolder, "apiKey.txt");
            var geometryApi = new GeometryApi(apiKeyPath);

            // build a filename with origin name, range, grid size and smooth percentage
            var fileName = string.Format(
                "{0}-Rng{1}-Grd{2}-Pct{3}.html",
                Home.Name.Replace(" ", ""),
                rangeMins.ToString("000"),
                gridSize.ToString("000"),
                smoothPct.ToString("000"));

            var filePath = Path.Combine(Properties.Settings.Default.RangerFolder, "Maps", fileName);

            // generate map
            geometryApi.GenerateDynamicMap(smoothed, Home, filePath);
        }

        private static double Smoothed(double a, double b, double c, int smoothPct)
        {
            var smooth = smoothPct / 100.0;
            return b * (1.0 - smooth) + smooth * (a + c) / 2.0;
        }

        private void MoveOnBorder(ref LatticePoint currCenter, ref DirectionEnum direction)
        {
            // move in the given direction
            currCenter = currCenter.Move(direction, 2);

            var oneForward = currCenter.Move(direction, 1);
            var forwardLeft = oneForward.Move(direction.Rotate(1));

            // decide where to go next
            if (!insidePoints.Contains(forwardLeft))
            {
                direction = direction.Rotate(1);
            }
            else
            {
                var directionRight = direction.Rotate(-1);
                var forwardRight = oneForward.Move(directionRight);

                if (insidePoints.Contains(forwardRight))
                {
                    direction = directionRight;
                }
            }
        }

        private LatticePoint GetStartingBorderPoint()
        {
            foreach (var insidePoint in insidePoints)
            {
                // east
                if (!insidePoints.Contains(insidePoint.Move(DirectionEnum.East, 2)))
                {
                    continue;
                }

                // north
                if (!outsidePoints.Contains(insidePoint.Move(DirectionEnum.North, 2)))
                {
                    continue;
                }

                // north-east
                if (!outsidePoints.Contains(insidePoint.Move(DirectionEnum.East, 2).Move(DirectionEnum.North, 2)))
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
                .Where(x => x.OriginId == Home.Id)
                .ToList()
                .Single(x => x.Direction == DirectionEnum.North);

            var north = (int)Math.Floor((northernPoint.Latitude - Home.Latitude) / deltaLat);

            // north needs to be even
            north = (north / 2) * 2;

            // this point should be inside, but we're going to check that
            var initiallyInside = true;

            // bring the point inside
            while (!Inside(GeoPointFromLatticePoint(0, north)))
            {
                north -= 2;
                initiallyInside = false;
            }

            // if we were intially inside, we may want to go further north
            // otherwise, we're done
            if (initiallyInside)
            {
                while (Inside(GeoPointFromLatticePoint(0, north + 2)))
                {
                    north += 2;
                }
            }

            // north: the last even point inside
            AddNode(0, north, true);

            // north + 2: the first even point outside
            AddNode(0, north + 2, false);
        }

        /// <summary>
        /// Finds all unprocessed points that have an inside neighbor and an outside neighbor
        /// </summary>
        private void FindUnprocessedBorder(HashSet<LatticePoint> smallerSet, HashSet<LatticePoint> biggerSet, Stack<LatticePoint> stack, HashSet<LatticePoint> pushedOnStack)
        {
            if (smallerSet.Count > biggerSet.Count)
            {
                FindUnprocessedBorder(biggerSet, smallerSet, stack, pushedOnStack);
                return;
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
                X = x,
                Y = y,
                Inside = inside
            };

            // add to corresponding hash set
            (inside ? insidePoints : outsidePoints).Add(new LatticePoint(gridNode));

            // save
            dbContext.GridNodes.InsertOnSubmit(gridNode);
            dbContext.SubmitChanges();
        }

        private void AddNode(LatticePoint latticePoint, bool inside)
        {
            AddNode(latticePoint.X, latticePoint.Y, inside);
        }

        private void LoadGridNodes()
        {
            insidePoints = new HashSet<LatticePoint>();
            outsidePoints = new HashSet<LatticePoint>();

            // ToDo: Make sure this will have newly added nodes, or find a way to refresh.
            foreach (var gridNode in Home.GridNodes)
            {
                // add to the corresponding hash set
                (gridNode.Inside ? insidePoints : outsidePoints).Add(new LatticePoint(gridNode));
            }
        }

        /// <summary>
        /// Finds first intersection of the ray given by "startingPoint + (multX, multY) * t" (t > 0) and the border.
        /// </summary>
        private IGeoLocation FindIntersection(DirectionEnum direction)
        {
            var geoLine = new DirectionGeoLine(Home, direction);

            var low = 0.0;
            var high = Properties.Settings.Default.StartingDelta;

            while (Inside(geoLine, high))
            {
                low = high;
                high *= 2;
            }

            var maxAerialDistance = Properties.Settings.Default.AerialDistanceLimit;

            var curr = double.NaN;
            double aerialDistance;

            // binary search until low and high points are close enough
            do
            {
                curr = (low + high) / 2.0;

                if (Inside(geoLine, curr))
                {
                    low = curr;
                }
                else
                {
                    high = curr;
                }

                aerialDistance = AerialDistance(geoLine, high, low);
            } while (aerialDistance > maxAerialDistance);

            return geoLine.PointOnLine(curr);
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