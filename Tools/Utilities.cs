using System;
using static System.Math;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using System.Reflection;
using System.IO;

namespace Axis.Tools
{
    static class Util
    {
        public const double DistanceTol = 0.001;
        public const double AngleTol = 0.001;
        public const double TimeTol = 0.00001;
        public const double UnitTol = 0.000001;
        internal const double SingularityTol = 0.0001;

        internal const string ResourcesFolder = @"D:\Robotic Control\Axis\Resources";

        internal static Transform ToTransform(this double[,] matrix)
        {
            var transform = new Transform();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    transform[i, j] = matrix[i, j];

            return transform;
        }

        internal static Plane ToPlane(this Transform transform)
        {
            Plane plane = Plane.WorldXY;
            // Plane plane = new Plane(Point3d.Origin, -Vector3d.XAxis, -Vector3d.YAxis);
            plane.Transform(transform);
            return plane;
        }

        internal static Transform ToTransform(this Plane plane)
        {
            return Transform.PlaneToPlane(Plane.WorldXY, plane);
        }

        internal static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        internal static double ToRadians(this double value)
        {
            return value * (PI / 180);
        }

        internal static double ToDegrees(this double value)
        {
            return value * (180 / PI);
        }

        public static T[] Subset<T>(this T[] array, int[] indices)
        {
            if (array == null) return null;

            T[] subset = new T[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                subset[i] = array[indices[i]];
            }
            return subset;
        }

        public static T[] RangeSubset<T>(this T[] array, int startIndex, int length)
        {
            if (array == null) return null;

            T[] subset = new T[length];
            Array.Copy(array, startIndex, subset, 0, length);
            return subset;
        }

        public static Plane QuaternionToPlane(Point3d point, Quaternion quaternion)
        {
            Plane plane;
            quaternion.GetRotation(out plane);
            plane.Origin = point;
            return plane;
        }

        public static Plane QuaternionToPlane(double x, double y, double z, double q1, double q2, double q3, double q4)
        {
            var point = new Point3d(x, y, z);
            var quaternion = new Quaternion(q1, q2, q3, q4);
            return QuaternionToPlane(point, quaternion);
        }

        public static IEnumerable<List<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var enumerators = source.Select(e => e.GetEnumerator()).ToArray();
            try
            {
                while (enumerators.All(e => e.MoveNext()))
                {
                    yield return enumerators.Select(e => e.Current).ToList();
                }
            }
            finally
            {
                foreach (var enumerator in enumerators)
                    enumerator.Dispose();
            }
        }

        public static Quaternion QuaternionFromPlane(Plane inPlane)
        {
            // Initialize the vectors which will store each axis of our plane frame.
            Vector3d xV, yV, zV;

            // Set these frame vectors and unitize them.
            xV = inPlane.XAxis;
            xV.Unitize();
            yV = inPlane.YAxis;
            yV.Unitize();
            zV = inPlane.ZAxis;
            zV.Unitize();

            // Using these frame vectors, calculate the quaternion.  Calclation based on information from euclideanspace:
            // http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/
            double trace, s, w, x, y, z;

            trace = xV[0] + yV[1] + zV[2];
            if (trace > 0.0)
            {
                s = 0.5 / Math.Sqrt(trace + 1.0);
                w = 0.25 / s;
                x = (yV[2] - zV[1]) * s;
                y = (zV[0] - xV[2]) * s;
                z = (xV[1] - yV[0]) * s;
            }
            else
            {
                if (xV[0] > yV[1] && xV[0] > zV[2])
                {
                    s = 2.0 * Math.Sqrt(1.0 + xV[0] - yV[1] - zV[2]);
                    w = (yV[2] - zV[1]) / s;
                    x = 0.25 * s;
                    y = (yV[0] + xV[1]) / s;
                    z = (zV[0] + xV[2]) / s;
                }
                else if (yV[1] > zV[2])
                {
                    s = 2.0 * Math.Sqrt(1.0 + yV[1] - xV[0] - zV[2]);
                    w = (zV[0] - xV[2]) / s;
                    x = (yV[0] + xV[1]) / s;
                    y = 0.25 * s;
                    z = (zV[1] + yV[2]) / s;
                }
                else
                {
                    s = 2.0 * Math.Sqrt(1.0 + zV[2] - xV[0] - yV[1]);
                    w = (xV[1] - yV[0]) / s;
                    x = (zV[0] + xV[2]) / s;
                    y = (zV[1] + yV[2]) / s;
                    z = 0.25 * s;
                }
            }

            // Normalize the found quaternion.
            double qLength;
            qLength = 1.0 / Math.Sqrt(w * w + x * x + y * y + z * z);
            w *= qLength;
            x *= qLength;
            y *= qLength;
            z *= qLength;
            
            Quaternion quat = new Quaternion(w, x, y, z);
            return quat;
        }

        public static List<double> QuaternionToEuler(Quaternion q)
        {
            List<double> eulers = new List<double>();
            double Csqr = q.C * q.C;

            // Roll
            double t0 = +2.0 * (q.A * q.B + q.C * q.D);
            double t1 = +1.0 - 2.0 * (q.B * q.B + Csqr);
            double roll = Math.Atan2(t0, t1);

            // Pitch
            double t2 = +2.0 * (q.A * q.C - q.D * q.B);
            t2 = t2 > 1.0 ? 1.0 : t2;
            t2 = t2 < -1.0 ? -1.0 : t2;
            double pitch = Math.Asin(t2);

            // Yaw
            double t3 = +2.0 * (q.A * q.D + q.B * q.C);
            double t4 = +1.0 - 2.0 * (Csqr + q.D * q.D);
            double yaw = Math.Atan2(t3, t4);

            eulers.Add(roll);
            eulers.Add(pitch);
            eulers.Add(yaw);

            return eulers;
        }

        public static double Remap(double value, double startLow, double startHigh, double targetLow, double targetHigh)
        {
            // low2 + (value - low1)/(high1 - low1) * (high2 - low2)
            double remappedValue = targetLow + (value - startLow) / (startHigh - startLow) * (targetHigh - targetLow);
            return remappedValue;
        }
    }
}
 