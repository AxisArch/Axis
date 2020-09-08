using System;
using static System.Math;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

//using Excel = Microsoft.Office.Interop.Excel;

using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.DocObjects;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;

using Axis.Targets;

using Newtonsoft.Json;

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
        const double SingularityTol = 0.0001;

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
        public static List<List<string>> SplitProgram(List<string> program, int nSize = 5000)
        {
            var list = new List<List<string>>();
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

        /// <summary>
        /// Create a CSV file at a certain file location using a specified delim character.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="delim"></param>
        //public static void CreateCSV(DataTable table, string path, string name, string delim)
        //{
        //    string filePath = string.Concat(path, name, @".csv");
        //    string delimiter = delim;
        //
        //    StringBuilder sb = new StringBuilder();
        //    List<string> row = new List<string>();
        //
        //    // Write headers
        //    foreach (DataColumn c in table.Columns)
        //        row.Add(c.ColumnName.ToString());
        //    sb.AppendLine(string.Join(delimiter, row));
        //
        //    // Write data
        //    foreach (DataRow r in table.Rows)
        //    {
        //        row.Clear();
        //        // Go through each column adding to a list of strings
        //        foreach (DataColumn c in table.Columns)
        //            row.Add(r[c.ColumnName].ToString());
        //        sb.AppendLine(string.Join(delimiter, row));
        //    }
        //    File.WriteAllText(filePath, sb.ToString());
        //}
        //
        /// <summary>
        /// Read an Excel file and return the associated DataTable.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        //public static DataTable ReadExcel(string path)
        //{
        //    DataTable dt = new DataTable();
        //
        //    // Create COM Objects. Create a COM object for everything that is referenced
        //    Excel.Application xlApp = new Excel.Application();
        //    Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(path);
        //    Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
        //    Excel.Range xlRange = xlWorksheet.UsedRange;
        //
        //    int rowCount = xlRange.Rows.Count;
        //    int colCount = xlRange.Columns.Count;
        //
        //    // Iterate over the rows and columns and print to the console as it appears in the file
        //    // NB: Excel is not zero based!!
        //    for (int i = 1; i <= rowCount; i++)
        //    {
        //        if (i == 1)
        //        {
        //            for (int j = 1; j <= colCount; j++)
        //            {
        //                // Write the value to the data tree
        //                if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
        //                {
        //                    dt.Columns.Add(xlRange.Cells[i, j].Value2.ToString());
        //                }
        //            }
        //        }
        //        else
        //        {
        //            string[] vals = new string[colCount];
        //
        //            for (int j = 1; j <= colCount; j++)
        //            {
        //                // Write the value to the data tree
        //                if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
        //                {
        //                    // Skip the first null element due to the zero based difference between GH and Excel
        //                    vals[j - 1] = xlRange.Cells[i, j].Value2.ToString();
        //                }
        //            }
        //
        //            dt.Rows.Add(vals);
        //        }
        //    }
        //
        //    // Cleanup
        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //
        //    // Rule of thumb for releasing com objects:
        //    // never use two dots, all COM objects must be referenced and released individually
        //    // ex: [somthing].[something].[something] is bad
        //
        //    // Release COM objects to fully kill excel process from running in the background
        //    Marshal.ReleaseComObject(xlRange);
        //    Marshal.ReleaseComObject(xlWorksheet);
        //
        //    // Close and release
        //    xlWorkbook.Close();
        //    Marshal.ReleaseComObject(xlWorkbook);
        //
        //    // Quit and release
        //    xlApp.Quit();
        //    Marshal.ReleaseComObject(xlApp);
        //
        //    return dt;
        //}
        //
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
            System.Threading.Timer _timeoutTimer;
            string _caption;
            AutoClosingMessageBox(string text, string caption, int timeout)
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
            void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }
            const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
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
                if (value.CompareTo(inlusiveMaximum) ==  -1 | value.CompareTo(inlusiveMaximum) == 0)
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
        public static T[] ToList<T, Q>(this Grasshopper.Kernel.Data.GH_Structure<Q> gh_struct )  where T : Rhino.Runtime.CommonObject where Q : IGH_Goo
        {
            if (gh_struct == null) return null;
            var list = new List<T>();
            for (int i = 0; i < gh_struct.Branches.Count; ++i)
            {
                for(int j = 0; j< gh_struct[i].Count; ++j) 
                {
                    var data = gh_struct[i][j];
                    //if (data == typeof(T))
                    //{
                        var temp = data as T;
                        list.Add(temp);
                    //}
                    //else if () { }

                    //var data = GH_Convert.ByteArrayToCommonObject<T>(byteArray as byte[]);
                    //list.Add((T)gh_struct[i][j]);
                }
;
            }
            return list.ToArray();
        }
        
    }

    /// <summary>
    /// Axis web communications class.
    /// </summary>
    public static class AxisWebServices
    {
        private static readonly HttpClient Client = new HttpClient();
        public static string SendSmif(List<string> messages, string projectId, string token)
        {
            string json = JsonConvert.SerializeObject(messages);
            try
            {
                string url = $"emptyURL";
                var result = PostJson(url, json, token);
                return result.ReasonPhrase;
            }
            catch (Exception e) { }
            return "Unknown failure.";
        }

        private static HttpResponseMessage PostJson(string url, string json, string token)
        {
            Uri uri = new Uri(url);
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var responseTask = Client.PostAsync(url, content);
            return responseTask.Result;
        }
    }

}

/// <summary>
/// This namesspcae proviedes fuctions for
/// the modification of RAPID instructions.
/// </summary>
namespace RAPID
{
    /// <summary>
    /// RAPID program
    /// </summary>
    public class Program : GH_Goo<Program>, IEnumerable<string>
    {
        public List<string> code { get; private set; }
        public bool IsMain { get; private set; }


        private string Name = "ProcName";
        private bool conL_J = false;
        private List<string> comment = new List<string>();
        private List<string> overrides = new List<string>();
        private List<string> ljHeader = new List<string>
                {
                    @"ConfL \Off;",
                    @"ConfJ \Off;",
                    "",
                };
        private List<string> ljFooter = new List<string>
                {
                    " ",
                    @"ConfL \On;",
                    @"ConfJ \On;",
                };


        public Program(List<string> code = null, List<string> overrides = null, string progName = "ProcName", bool LJ = false, List<string> comments = null)
        {
            if (code != null) this.code = code;
            this.Name = progName;
            this.conL_J = LJ;
            if (overrides != null) { this.overrides = overrides; }
            if (progName == "main") { this.IsMain = true; }
            if (comment != null) { this.comment = comment; }
        }
        public void AddOverrides(List<string> overrides)
        {
            this.overrides.AddRange(overrides);
        }

        public List<string> Code()
        {
            var prog = new List<string>();

            prog.AddRange(comment);
            prog.Add($"PROC {Name}()");
            if (this.overrides != null) { prog.AddRange(overrides); }
            prog.AddRange(comment);
            if (conL_J) { prog.AddRange(ljHeader); }
            prog.AddRange(this.code);
            if (conL_J) { prog.AddRange(ljFooter); }

            prog.Add("ENDPROC");

            return prog;
        }
        public List<Program> ToList()
        {
            return new List<Program> { this };
        }

        public void Add(string item)
        {
            code.Add(item);
        }
        public IEnumerator<string> GetEnumerator()
        {
            return code.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string TypeName => "RAPID Proc";
        public override string TypeDescription => "Represents a RAPID procedure that can be combined to a RAPID module";
        public override bool IsValid => true;
        public override string ToString()
        {
            return $"RAPID Proc: {Name}";
        }
        public override IGH_Goo Duplicate()
        {
            return this;
        }
    }

    /// <summary>
    /// RAPID module
    /// </summary>
    public class Module : GH_Goo<Module>
    {
        private bool isValid;

        private string Name;
        private List<string> tag = new List<string>
        {
            "! ABB Robot Code",
            $"! Generated with Axis {Assembly.GetExecutingAssembly().GetName().Version}",
            "! File Created: " + DateTime.Now.ToString(),
            "! Author: " + Environment.UserName.ToString(),
            " ",
        };
        private List<string> declarations = new List<string>
                {
                    "! Declarations",
                    "VAR confdata cData := [0,-1,-1,0];",
                    "VAR extjoint eAxis := [9E9,9E9,9E9,9E9,9E9,9E9];",
                };
        private List<Program> main = new List<Program>();
        private List<Program> progs = new List<Program>();
        private List<string> legacyProgs = new List<string>();

        public List<Program> extraProg = new List<Program>();

        public Module(List<Program> progs = null, List<string> declarations = null, string name = "Submodule")
        {
            if (progs != null)
            {
                foreach (Program prog in progs)
                {
                    if (prog.IsMain)
                    {
                        this.AddMain(prog);
                    }
                    else
                    {
                        if (this.progs == null)
                        {
                            this.progs = new List<Program>();

                        }
                        this.progs.Add(prog);
                    }
                }
            }
            this.Name = name;
            if (declarations != null) { this.declarations = declarations; }
            this.isValid = this.validate();
        }
        public void AddDeclarations(List<string> declaration)
        {
            if (this.declarations == null)
            {
                this.declarations = declaration;
            }
            else
            {
                this.declarations.AddRange(declaration);
            }
        }
        public void AddPrograms(List<Program> progs)
        {
            if (progs == null)
            {
                this.progs = progs;
            }
            else
            {
                this.progs.AddRange(progs);
            }
        }
        public void AddPrograms(List<string> progs)
        {
            legacyProgs.AddRange(progs);
        }
        public void AddMain(Program main)
        {
            if (this.main == null)
            {
                this.main = new List<Program>() { main };
            }
            else
            {
                this.main.Add(main);
            }
            this.isValid = this.validate();
        }
        public void AddOverrides(List<string> overrides)
        {
            foreach (Program prog in this.main)
            {
                prog.AddOverrides(overrides);
            }
        }
        bool ExtraProg(List<Program> extraProg)
        {
            foreach (Program prog in extraProg)
            {
                if (prog.IsMain == true)
                {
                    return false;
                }
            }

            this.extraProg = extraProg;
            return true;
        }

        public List<string> Code()
        {
            List<string> mod = new List<string>();
            mod.Add($"MODULE {Name}");
            mod.AddRange(this.tag);
            mod.AddRange(this.declarations);
            mod.Add("");
            mod.Add("! Main Program");
            foreach (Program prog in main)
            {
                mod.AddRange(prog.Code());
            }
            if (legacyProgs.Count > 0) { mod.AddRange(legacyProgs); }
            if (progs.Count > 0)
            {
                mod.Add("! Additional Programs");
                foreach (Program prog in progs)
                {
                    mod.AddRange(prog.Code());
                }
            }
            mod.Add("ENDMODULE");

            return mod;
        }

        private bool validate()
        {
            bool v = false;
            int c = 0;
            foreach (Program prog in this.main)
            {
                if (prog.IsMain == true) { ++c; }
            }
            if (c == 1) { return true; }
            else { return false; }
        }

        public override string TypeName => "RAPID Module";
        public override string TypeDescription => "Represents a RAPID mudule consisting of RAPID procedures";
        public override bool IsValid => isValid;
        public override string ToString()
        {
            return $"RAPID Module: {Name}";
        }
        public override IGH_Goo Duplicate()
        {
           return this;
        }

    }
}

/// <summary>
/// This namespace provides functions for canvas manipulation in Grasshopper
/// </summary
namespace Canvas
{
    /// <summary>
    /// This class provides functions for components
    /// </summary>
    class Component
    {
        static public void SetValueList(GH_Document doc, GH_Component comp, int InputIndex, List<KeyValuePair<string, string>> valuePairs, string name)
        {
            if (valuePairs.Count == 0) return;
            doc = doc;
            comp = comp;
            GH_DocumentIO docIO = new GH_DocumentIO();
            docIO.Document = new GH_Document();

            if (docIO.Document == null) return;
            doc.MergeDocument(docIO.Document);

            docIO.Document.SelectAll();
            docIO.Document.ExpireSolution();
            docIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = docIO.Document.Objects;
            doc.DeselectAll();
            doc.UndoUtil.RecordAddObjectEvent("Create Accent List", objs);
            doc.MergeDocument(docIO.Document);

            doc.ScheduleSolution(10, chanegValuelist);


            void chanegValuelist(GH_Document document)
            {

                IList<IGH_Param> sources = comp.Params.Input[InputIndex].Sources;
                int inputs = sources.Count;


                // If nothing has been conected create a new component
                if (inputs == 0)
                {
                    //instantiate  new value list and clear it
                    GH_ValueList vl = new GH_ValueList();
                    vl.ListItems.Clear();
                    vl.NickName = name;
                    vl.Name = name;

                    //Create values for list and populate it
                    for (int i = 0; i < valuePairs.Count; ++i)
                    {
                        var item = new GH_ValueListItem(valuePairs[i].Key, valuePairs[i].Value);
                        vl.ListItems.Add(item);
                    }

                    //Add value list to the document
                    document.AddObject(vl, false, 1);

                    //get the pivot of the "accent" param
                    System.Drawing.PointF currPivot = comp.Params.Input[InputIndex].Attributes.Pivot;
                    //set the pivot of the new object
                    vl.Attributes.Pivot = new System.Drawing.PointF(currPivot.X - 210, currPivot.Y - 11);

                    // Connect to input
                    comp.Params.Input[InputIndex].AddSource(vl);
                }

                // If inputs exist replace the existing ones
                else
                {
                    for (int i = 0; i < inputs; ++i)
                    {
                        if (sources[i].Name == "Value List" | sources[i].Name == name)
                        {
                            //instantiate  new value list and clear it
                            GH_ValueList vl = new GH_ValueList();
                            vl.ListItems.Clear();
                            vl.NickName = name;
                            vl.Name = name;

                            //Create values for list and populate it
                            for (int j = 0; j < valuePairs.Count; ++j)
                            {
                                var item = new GH_ValueListItem(valuePairs[j].Key, valuePairs[j].Value);
                                vl.ListItems.Add(item);
                            }

                            document.AddObject(vl, false, 1);
                            //set the pivot of the new object
                            vl.Attributes.Pivot = sources[i].Attributes.Pivot;

                            var currentSource = sources[i];
                            comp.Params.Input[InputIndex].RemoveSource(sources[i]);

                            currentSource.IsolateObject();
                            document.RemoveObject(currentSource, false);

                            //Connect new vl
                            comp.Params.Input[InputIndex].AddSource(vl);
                        }
                        else
                        {
                            //Do nothing if it dosent mach any of the above
                        }
                    }
                }
            }
        }

        public static void ChangeObjects(IEnumerable<IGH_Param> items, IGH_Param newObject)
        {
            foreach (IGH_Param item in items)
            {
                //get the input it is connected to
                if (item.Recipients.Count == 0) return;
                var parrent = item.Recipients[0];

                GH_DocumentIO docIO = new GH_DocumentIO();
                docIO.Document = new GH_Document();

                //get active GH doc
                GH_Document doc = item.OnPingDocument();
                if (doc == null) return;
                if (docIO.Document == null) return;

                Component.AddObject(docIO, newObject, parrent, item.Attributes.Pivot);
                Component.MergeDocuments(docIO, doc, $"Create {newObject.Name}");

                doc.RemoveObject(item, false);
                parrent.AddSource(newObject);
            }
        }

        public static GH_ValueList CreateValueList(string name, Dictionary<string, string> valuePairs)
        {
            //initialize object
            Grasshopper.Kernel.Special.GH_ValueList vl = new Grasshopper.Kernel.Special.GH_ValueList();
            //clear default contents
            vl.ListItems.Clear();

            //set component nickname
            vl.NickName = name;
            vl.Name = name;

            foreach (KeyValuePair<string, string> entety in valuePairs)
            {
                GH_ValueListItem vi = new GH_ValueListItem(entety.Key, entety.Value);
                vl.ListItems.Add(vi);
            }

            return vl;
        }
        public static GH_NumberSlider CreateNumbersilder(string name, decimal min, decimal max, int precision = 0, int length = 174)
        {
            var nS = new GH_NumberSlider();
            nS.ClearData();

            //Naming
            nS.Name = name;
            nS.NickName = name;

            nS.Slider.Minimum = min;
            nS.Slider.Maximum = max;

            nS.Slider.DecimalPlaces = Axis.Util.LimitToRange(precision, 0,12);

            if (precision == 0)
                nS.Slider.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Integer;
            else
                nS.Slider.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Float;

            nS.CreateAttributes();
            var bounds = nS.Attributes.Bounds;
            bounds.Width = length;
            nS.Attributes.Bounds = bounds;

            nS.SetSliderValue(min);
            return nS;
        }

        // private methods to magee the placement of ne objects
        static void AddObject(GH_DocumentIO docIO, IGH_Param Object, IGH_Param param, PointF location = new PointF())
        {
            // place the object
            docIO.Document.AddObject(Object, false, 1);

            //get the pivot of the "accent" param
            System.Drawing.PointF currPivot = param.Attributes.Pivot;

            if (location == new PointF()) Object.Attributes.Pivot = new System.Drawing.PointF(currPivot.X - 120, currPivot.Y - 11);
            //set the pivot of the new object
            else Object.Attributes.Pivot = location;
        }
        static void MergeDocuments(GH_DocumentIO docIO, GH_Document doc, string name = "Merge")
        {
            docIO.Document.SelectAll();
            docIO.Document.ExpireSolution();
            docIO.Document.MutateAllIds();
            IEnumerable<IGH_DocumentObject> objs = docIO.Document.Objects;
            doc.DeselectAll();
            doc.UndoUtil.RecordAddObjectEvent(name, objs);
            doc.MergeDocument(docIO.Document);
            //doc.ScheduleSolution(10);
        }

        #region Display Methods
        
        static public void DisplayPlane(Plane plane, IGH_PreviewArgs args, double sizeLine = 70, double sizeArrow = 30, int thickness = 3)
        {
            args.Display.DrawLineArrow(
                new Line(plane.Origin, plane.XAxis, sizeLine),
                Axis.Styles.Pink,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(plane.Origin, plane.YAxis, sizeLine),
                Axis.Styles.LightBlue,
                thickness,
                sizeArrow);
            args.Display.DrawLineArrow(new Line(plane.Origin, plane.ZAxis, sizeLine),
                Axis.Styles.LightGrey,
                thickness,
                sizeArrow);
        }
        static public void DisplayRobotLines(Axis.Core.Manipulator robot, IGH_PreviewArgs args, int thickness = 3)
        {
            //List<Point3d> points = new List<Point3d>();
            //foreach (Plane p in robot.ikPlanes) { points.Add(p.Origin); }
            //
            //Polyline pLine = new Polyline(points);
            //
            //Line[] lines = pLine.GetSegments();
            //
            //// Draw lines
            //for (int i = 0; i < lines.Length; ++i)
            //{
            //    int cID = i;
            //    if (i >= lines.Length) cID = robot.ikColors.Count - 1;
            //    args.Display.DrawLine(lines[i], robot.ikColors[cID], thickness);
            //}
            //
            ////Draw Sphers
            //
            ////Draw Plane
            //DisplayPlane(robot.ikPlanes[0], args);
        }
        static public void DisplayTool(Axis.Core.Tool tool, IGH_PreviewArgs args)
        {
            //int cC = tool.ikColors.Count;
            //int tC = tool.ikGeometry.Count;
            //
            //for (int i = 0; i < tC; ++i)
            //{
            //    int cID = i;
            //
            //    if (i >= cC) cID = cC - 1;
            //    args.Display.DrawMeshShaded(tool.ikGeometry[i], new DisplayMaterial(tool.ikColors[cID]));
            //}
        }
        static public void DisplayToolLines(Axis.Core.Tool tool, IGH_PreviewArgs args, int thickness = 3)
        {
            //Line line = new Line(tool.ikBase.Origin, tool.ikTCP.Origin);
            //args.Display.DrawLine(line, tool.ikColors[0], thickness);
            //
            ////Draw Plane
            //DisplayPlane(tool.ikTCP, args);
        }
        static public void DisplayToolPath(Axis.Targets.Toolpath toolpath, IGH_PreviewArgs args, int thickness = 3, double radius = 2 )
        {
            List<Line> lines = new List<Line>();
            List<Color> colors = new List<Color>();

            Dictionary<Line, Color> pairs = lines.Zip(colors, (k, v) => new { k, v })
              .ToDictionary(x => x.k, x => x.v);

            //Draw line segments
            foreach (KeyValuePair<Line, Color> pair in pairs)
            {
                args.Display.DrawLine(pair.Key, pair.Value, thickness);
            }

            List<Point3d> points = new List<Point3d>();
            List<Sphere> spheres = points.Select(p => new Sphere(p, radius)).ToList();

            //Draw Commants
            foreach (Sphere sphere in spheres)
            {
                //IF bitmats are to be used
                //args.Display.DrawSprites();
                args.Display.DrawSphere(sphere, Axis.Styles.LightOrange);
            }

        }
        
        #endregion
    }
    class Menu
    {
        /// <summary>
        /// Uncheck other dropdown menu items
        /// </summary>
        /// <param name="selectedMenuItem"></param>
        static public void UncheckOtherMenuItems(ToolStripMenuItem selectedMenuItem)
        {
            selectedMenuItem.Checked = true;

            // Select the other MenuItens from the ParentMenu(OwnerItens) and unchecked this,
            // The current Linq Expression verify if the item is a real ToolStripMenuItem
            // and if the item is a another ToolStripMenuItem to uncheck this.
            foreach (var ltoolStripMenuItem in (from object
                                                    item in selectedMenuItem.Owner.Items
                                                let ltoolStripMenuItem = item as ToolStripMenuItem
                                                where ltoolStripMenuItem != null
                                                where !item.Equals(selectedMenuItem)
                                                select ltoolStripMenuItem))
                (ltoolStripMenuItem).Checked = false;

            // This line is optional, for show the mainMenu after click
            //selectedMenuItem.Owner.Show();
        }
    }
}
