using GalaSoft.MvvmLight;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProceduralTrackGeneration.ViewModel
{
    /// <summary>
    /// Race track model with procedural generation.
    /// </summary>
    public class TrackViewModel : ViewModelBase
    {
        private Random rnd = new Random();
        private double trackWidth, trackHeight;
        private int trackPoints;

        /// <summary>
        /// Track constructor.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="points"></param>
        public TrackViewModel(double width, double height, int points)
        {
            this.trackWidth = width;
            this.trackHeight = height;
            this.trackPoints = points;
        }

        static int GetMaxSegmentIndex(IEnumerable<Point> Curve)
        {
            int segmentIndex = 0;
            double segmentDistance = 0;
            int index = 0;
            Point prevPoint = new Point();

            foreach (var point in Curve)
            {
                if (index != 0)
                {
                    var curDistance = (point - prevPoint).Length;
                    if (curDistance > segmentDistance)
                    {
                        segmentIndex = index - 1;
                        segmentDistance = curDistance;
                    }
                }
                index++;
                prevPoint = point;
            }
            return segmentIndex;
        }

        /// <summary>
        /// Stun track generated using Simulated Annealing algorithm to solve traveling salesman problem.
        /// Resulting curve smoothed using cubic spline.
        /// </summary>
        public void GenerateTrack(Random rnd = null)
        {
            if (rnd == null)
                rnd = new Random();
            this.rnd = rnd;
            
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                var points = GenerateRandomTrack(trackWidth, trackHeight, trackPoints);

                int startIndex = GetMaxSegmentIndex(points);
                startIndex = (int)((startIndex) * 2000.0 / (trackPoints + 1));
                var curve = Spline(points, 2000).ToList();

                var p1 = curve[startIndex];
                var p2 = p1 - curve[startIndex + 1];

                StartPosition = p1;
                StartOrientation = -Math.Atan2(p2.X, p2.Y) * 180 / Math.PI;
                Length = MeasureLength(curve);
                Curve = curve;
            });
        }

        private double startOrientation = 0;
        public double StartOrientation
        {
            get => startOrientation;
            set => Set(ref startOrientation, value);
        }

        private Point startPosition = new Point();
        public Point StartPosition
        {
            get => startPosition;
            set => Set(ref startPosition, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private IEnumerable<Point> GenerateRandomTrack(double width, double height, int points)
        {
            IEnumerable<Point> best = null;
            var list = Enumerable.Range(0, points - 1).Select(p => new Point(rnd.NextDouble() * width, rnd.NextDouble() * height));
            list = list.Concat(list.Take(1));
            int cnt = list.Count() - 1;
            double bestLen = double.PositiveInfinity;

            /// Iteration for the Simulated Annealing algorithm.
            /// Using Simulated Annealing algorithm to solve traveling salesman problem.
            for (int i = 0; i < 2000; i++)
            {
                double len = MeasureLength(list);
                int id1, id2;

                id1 = rnd.Next(cnt);
                do id2 = rnd.Next(cnt); while (id2 == id1);

                if (id2 < id1)
                    Swap(ref id1, ref id2);

                var newPoints = ReversePoints(list, id1, id2);
                double newLen = MeasureLength(newPoints);

                double k = 100;
                double T = ((1.0 + k) / (i + 1 + k)) / 2;
                double p = Math.Exp(-(newLen - len) / (T * 100)) / 2;

                if (newLen > len && rnd.NextDouble() > p)
                {
                }
                else
                {
                    list = newPoints.ToList(); // We have to convert it to list to get enumeration faster
                    if (newLen < bestLen)
                    {
                        bestLen = newLen;
                        best = list;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Swap two values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        /// <summary>
        /// Returns points list with reversed points order from start to stop index.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        private static IEnumerable<Point> ReversePoints(IEnumerable<Point> points, int start, int stop)
        {
            var count = points.Count();
            var l1 = points.Take(start);
            var l2 = points.Skip(start).Take(stop - start + 1).Reverse();
            var l3 = points.Skip(stop + 1);
            var np = l1.Concat(l2).Concat(l3);
            // Copy first point to the end
            return np.Take(count - 1).Concat(np.Take(1));
        }

        /// <summary>
        /// Spline the specified curve.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<Point> Spline(IEnumerable<Point> points, int count)
        {
            var points1 = new List<Point>(points);
            points1.Add(points1[1]);
            points1.Insert(0, points1[points1.Count - 3]);

            var distances = new List<double>();
            double distance = 0;
            for (int i = 0; i < points1.Count; i++)
            {
                if (i > 0)
                    distance += Distance(points1[i], points1[i - 1]);
                distances.Add(distance);
            }

            var splineX = Interpolate.CubicSplineRobust(distances, points1.Select(v => v.X));
            var splineY = Interpolate.CubicSplineRobust(distances, points1.Select(v => v.Y));

            // Distances without first and last points
            var dst = Enumerable.Range(0, count).Select(v => distances[1] + (distances[distances.Count - 2] - distances[1]) * v / (count - 1));
            return dst.Select(v => new Point(splineX.Interpolate(v), splineY.Interpolate(v)));
        }

        private IList<Point> curve = new List<Point>();
        public IList<Point> Curve
        {
            get => curve;
            private set
            {
                Set(ref curve, value);
                RaisePropertyChanged(nameof(Points));
            }
        }

        public PointCollection Points
        {
            get => new PointCollection(curve);
        }

        private static double Distance(Point p1, Point p2)
        {
            return Point.Subtract(p1, p2).Length;
        }

        public static double MeasureLength(IEnumerable<Point> Points)
        {
            return Points.Zip(Points.Skip(1), Distance).Sum() + Distance(Points.First(), Points.Last());
        }

        public double length = 0;
        public double Length
        {
            get { return length; }
            private set { Set(ref length, value); }
        }

        public double roadWidth = 8;
        public double RoadWidth
        {
            get { return roadWidth; }
            private set { Set(ref roadWidth, value); }
        }

    }
}
