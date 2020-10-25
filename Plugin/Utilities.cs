using Axis.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Math;

namespace Axis
{
    /// <summary>
    /// Utility class for generic functions.
    /// </summary>
    public static class Util
    {
        // Public constants across the plugin.
        public const int DefaultSpeed = 200;

        public const int DefaultZone = 1;
        public const int DefaultTime = 5;
        public const double ExAxisTol = 0.00001;
        private const double SingularityTol = 0.0001;

        /// <summary>
        /// List of standard ABB zones as a dictionary.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<double, Zone> ABBZones()
        {
            Dictionary<double, Zone> zones = new Dictionary<double, Zone>();

            zones.Add(-1, new Zone(true, 0, 0, 0, 0, 0, 0, "fine"));
            zones.Add(0, new Zone(false, 0.3, 0.3, 0.3, 0.03, 0.3, 0.03, "z0"));
            zones.Add(1, new Zone(false, 1, 1, 1, 0.1, 1, 0.1, "z1"));
            zones.Add(5, new Zone(false, 5, 8, 8, 0.8, 8, 0.8, "z5"));
            zones.Add(10, new Zone(false, 10, 15, 15, 1.5, 15, 1.5, "z10"));
            zones.Add(15, new Zone(false, 15, 23, 23, 2.3, 23, 2.3, "z15"));
            zones.Add(20, new Zone(false, 20, 30, 30, 3.0, 30, 3.0, "z20"));
            zones.Add(30, new Zone(false, 30, 45, 45, 4.5, 45, 4.5, "z30"));
            zones.Add(40, new Zone(false, 40, 60, 60, 6.0, 60, 6.0, "z40"));
            zones.Add(50, new Zone(false, 50, 75, 75, 7.5, 75, 7.5, "z50"));
            zones.Add(60, new Zone(false, 60, 90, 90, 9.0, 90, 9.0, "z60"));
            zones.Add(80, new Zone(false, 80, 120, 120, 12, 80, 12, "z80"));
            zones.Add(100, new Zone(false, 100, 150, 150, 15, 150, 15, "z100"));
            zones.Add(150, new Zone(false, 150, 225, 225, 23, 225, 23, "z150"));
            zones.Add(200, new Zone(false, 200, 300, 300, 30, 300, 30, "z200"));

            return zones;
        }

        /// <summary>
        /// List of standard ABB speeds as a dictionary.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<double, Speed> ABBSpeeds()
        {
            Dictionary<double, Speed> speeds = new Dictionary<double, Speed>();

            speeds.Add(5, new Speed(5, 500, "v5"));
            speeds.Add(10, new Speed(10, 500, "v10"));
            speeds.Add(20, new Speed(20, 500, "v20"));
            speeds.Add(30, new Speed(30, 500, "v30"));
            speeds.Add(40, new Speed(40, 500, "v40"));
            speeds.Add(50, new Speed(50, 500, "v50"));
            speeds.Add(60, new Speed(60, 500, "v60"));
            speeds.Add(80, new Speed(80, 500, "v80"));
            speeds.Add(100, new Speed(100, 500, "v100"));
            speeds.Add(150, new Speed(150, 500, "v150"));
            speeds.Add(200, new Speed(200, 500, "v200"));
            speeds.Add(300, new Speed(300, 500, "v300"));
            speeds.Add(400, new Speed(400, 500, "v400"));
            speeds.Add(500, new Speed(500, 500, "v500"));
            speeds.Add(600, new Speed(600, 500, "v600"));
            speeds.Add(800, new Speed(800, 500, "v800"));
            speeds.Add(1000, new Speed(1000, 500, "v1000"));
            speeds.Add(1500, new Speed(1500, 500, "v1500"));
            speeds.Add(2000, new Speed(2000, 500, "v2000"));
            speeds.Add(2500, new Speed(2500, 500, "v2500"));
            speeds.Add(3000, new Speed(3000, 500, "v3000"));
            speeds.Add(4000, new Speed(4000, 500, "v4000"));
            speeds.Add(5000, new Speed(5000, 500, "v5000"));
            speeds.Add(6000, new Speed(6000, 500, "v6000"));
            speeds.Add(7000, new Speed(7000, 500, "v7000"));

            return speeds;
        }

        // Simple math conversion functions.
        internal static double ToRadians(this double value)
        {
            return value * (PI / 180);
        }

        internal static double ToDegrees(this double value)
        {
            return value * (180 / PI);
        }

        /// <summary>
        /// Create a plane based on a quaternion rotation and a location in space.
        /// Gets the rotation of the quaternion as a plane and centres is at the point.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="quaternion"></param>
        /// <returns></returns>
        public static Plane QuaternionToPlane(Point3d point, Quaternion quaternion)
        {
            Plane plane;
            quaternion.GetRotation(out plane);
            plane.Origin = point;
            return plane;
        }

        /// <summary>
        /// Create a plane based on a quaternion rotation and a location in space.
        /// Overloaded method that accepts double values instead of points.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <param name="q3"></param>
        /// <param name="q4"></param>
        /// <returns></returns>
        public static Plane QuaternionToPlane(double x, double y, double z, double q1, double q2, double q3, double q4)
        {
            var point = new Point3d(x, y, z);
            var quaternion = new Quaternion(q1, q2, q3, q4);
            return QuaternionToPlane(point, quaternion);
        }

        /// <summary>
        /// Get the Quaternion description of the rotation of a plane target.
        /// Conversion based on the Robots plugin (https://github.com/visose/Robots).
        /// </summary>
        /// <param name="inPlane"></param>
        /// <returns></returns>
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

            // Using these frame vectors, calculate the quaternion. Calculation based on information from euclideanspace:
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

        /// <summary>
        /// Convert the Quaternion representation of a rotation to Euler angles.
        /// Based on (https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Linear interpolate between two planes.
        /// Quaternion interpolation code from the Robots plugin (https://github.com/visose/Robots)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Plane Lerp(Plane a, Plane b, double t, double min, double max)
        {
            t = (t - min) / (max - min);
            if (double.IsNaN(t)) t = 0;
            var newOrigin = a.Origin * (1 - t) + b.Origin * t;

            Quaternion q = Quaternion.Rotation(a, b);
            double angle;
            Vector3d axis;
            q.GetRotation(out angle, out axis);
            angle = (angle > PI) ? angle - 2 * PI : angle;
            a.Rotate(t * angle, axis, a.Origin);

            a.Origin = newOrigin;
            return a;
        }

        /// <summary>
        /// Remap a value from one domain to another.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startLow"></param>
        /// <param name="startHigh"></param>
        /// <param name="targetLow"></param>
        /// <param name="targetHigh"></param>
        /// <returns></returns>
        public static double Remap(double value, double startLow, double startHigh, double targetLow, double targetHigh)
        {
            double remappedValue = targetLow + (value - startLow) / (startHigh - startLow) * (targetHigh - targetLow);
            return remappedValue;
        }

        /// <summary>
        /// Compute the standard deviation for a set of values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double StandardDev(List<double> values)
        {
            double sd = 0;
            double avg = 0;
            double variance = 0;
            double total = 0;
            double totalSqrDiff = 0;

            foreach (double v in values) { total = total + v; }
            avg = total / values.Count;
            foreach (double v in values)
            {
                double sqrDiff = Math.Pow((v - avg), 2);
                totalSqrDiff = totalSqrDiff + sqrDiff;
            }
            variance = totalSqrDiff / values.Count;
            sd = Math.Round(Math.Sqrt(variance), 2);
            return sd;
        }

        /// <summary>
        /// Split a command into it's component parts based on manufacturer conventions.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="manufacturer"></param>
        /// <returns></returns>
        public static string[] SplitCommand(string s, string manufacturer)
        {
            // Use a char array of manufacturer specific delimeters to split the command.
            char[] delimiters = new char[] { };

            if (manufacturer == "ABB")
                delimiters = new char[] { ',', '[', ']', '{', ' ' };
            else // Not implemented
                delimiters = new char[] { }; // Insert KUKA command delimiters.

            string[] parts = s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }

        /// <summary>
        /// Split a program into seperate chunks to fit robot controller memory.
        /// </summary>
        /// <param name="program"></param>
        /// <param name="nSize"></param>
        /// <returns></returns>
        public static List<List<T>> SplitProgram<T>(List<T> program, int nSize = 5000)
        {
            var list = new List<List<T>>();
            for (int i = 0; i < program.Count; i += nSize)
                list.Add(program.GetRange(i, Math.Min(nSize, program.Count - i)));

            return list;
        }

        public static int TypeCheck(Rhino.DocObjects.RhinoObject obj)
        {
            // Check the object type and store it as a variable.
            ObjectType oType = obj.ObjectType;

            if (oType == ObjectType.Brep) { return 0; }
            else if (oType == ObjectType.Extrusion) { return 1; }
            else if (oType == ObjectType.Surface) { return 2; }
            else if (oType == ObjectType.Mesh) { return 3; }
            else if (oType == ObjectType.Curve) { return 4; }
            else if (oType == ObjectType.Point) { return 5; }
            else // If the geoemtry type is not found, return -1.
                return -1;
        }

        /// <summary>
        /// Snap values to a list of snap values.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="snaps"></param>
        /// <returns></returns>
        public static List<double> SnapValues(List<double> values, List<double> snaps)
        {
            List<double> snappedValues = new List<double>();
            for (int i = 0; i < values.Count; i++)
                snappedValues.Add(snaps.Aggregate((x, y) => Math.Abs(x - values[i]) < Math.Abs(y - values[i]) ? x : y));
            return snappedValues;
        }

        internal sealed class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern bool AllocConsole();

            [DllImport("kernel32.dll")]
            public static extern bool FreeConsole();
        }

        /// <summary>
        /// Logic for the automatically closing message box "popup".
        /// </summary>
        public class AutoClosingMessageBox
        {
            private System.Threading.Timer _timeoutTimer;
            private string _caption;

            private AutoClosingMessageBox(string text, string caption, int timeout)
            {
                _caption = caption;
                _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                    null, timeout, System.Threading.Timeout.Infinite);
                using (_timeoutTimer)
                    MessageBox.Show(text, caption);
            }

            public static void Show(string text, string caption, int timeout)
            {
                new AutoClosingMessageBox(text, caption, timeout);
            }

            private void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }

            private const int WM_CLOSE = 0x0010;

            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }

        /// <summary>
        /// Colour a list of meshes.
        /// </summary>
        /// <param name="meshes"></param>
        /// <param name="colors"></param>
        /// <returns></returns>
        public static List<Mesh> ColorMeshes(List<Mesh> meshes, List<Color> colors)
        {
            List<Mesh> meshOut = new List<Mesh>();
            for (int i = 0; i < meshes.Count; i++)
                meshOut[i].VertexColors.CreateMonotoneMesh(colors[i]);
            return meshOut;
        }

        /// <summary>
        /// Limit a value to a range
        /// </summary>
        /// <param name="value">Value to limit</param>
        /// <param name="inclusiveMinimum">Minimum value</param>
        /// <param name="inlusiveMaximum">Maximum value</param>
        /// <returns>Limited Value</returns>
        public static T LimitToRange<T>(IComparable<T> value, T inclusiveMinimum, T inlusiveMaximum)
        {
            if (value.CompareTo(inclusiveMinimum) == 0 | value.CompareTo(inclusiveMinimum) == 1)
            {
                if (value.CompareTo(inlusiveMaximum) == -1 | value.CompareTo(inlusiveMaximum) == 0)
                {
                    return (T)value;
                }

                return inlusiveMaximum;
            }

            return inclusiveMinimum;
        }

        /// <summary>
        /// Convert List to GH_Structure - Extention method
        /// </summary>
        /// <typeparam name="T">Target type, has to inherit from IGH_Goo</typeparam>
        /// <typeparam name="Q">Source type, can be IGH_Goo or Rhino CommonObject</typeparam>
        /// <param name="list">List to be converted</param>
        /// <returns>GH_Structure containing list</returns>
        public static Grasshopper.Kernel.Data.GH_Structure<T> ToGHStructure<T, Q>(this IEnumerable<Q> list) where T : IGH_Goo
        {
            Grasshopper.Kernel.Data.GH_Structure<T> gh_Struc = new Grasshopper.Kernel.Data.GH_Structure<T>();

            if (list == null) return null;

            if (typeof(Rhino.Runtime.CommonObject).IsAssignableFrom(typeof(Q)))
            {
                foreach (object item in list)
                {
                    var g_Obj = GH_Convert.ToGoo(item);
                    gh_Struc.Append((T)g_Obj);
                }
            }
            else if (typeof(IGH_Goo).IsAssignableFrom(typeof(Q)))
            {
                foreach (object obj in list)
                {
                    var g_Obj = obj as IGH_Goo;
                    gh_Struc.Append((T)g_Obj);
                }
            }
            else return null;

            return gh_Struc;
        }

        /// <summary>
        /// Convert a GH_Structure back to a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
        /// <param name="gh_struct"></param>
        /// <returns></returns>
        public static List<T> ToList<T, Q>(this Grasshopper.Kernel.Data.GH_Structure<Q> gh_struct) where T : Rhino.Runtime.CommonObject where Q : IGH_Goo
        {
            if (gh_struct == null) return null;
            var list = new List<T>();
            for (int i = 0; i < gh_struct.Branches.Count; ++i)
            {
                for (int j = 0; j < gh_struct[i].Count; ++j)
                {
                    var data = gh_struct[i][j];
                    list.Add(data as T);
                }
            }
            return list;
        } //<--- This is buggy
    }
}