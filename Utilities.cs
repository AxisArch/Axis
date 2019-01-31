using System;
using static System.Math;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;
using System.Drawing;

using Excel = Microsoft.Office.Interop.Excel; // Microsoft Excel 14 object in references-> COM tab

using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.DocObjects;

using Axis.Targets;


namespace Axis
{
    public static class Util
    {
        public const int DefaultSpeed = 200;
        public const int DefaultZone = 1;
        public const int DefaultTime = 5;
        public const double ExAxisTol = 0.00001;
        internal const double SingularityTol = 0.0001;

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

        internal static double ToRadians(this double value)
        {
            return value * (PI / 180);
        }

        internal static double ToDegrees(this double value)
        {
            return value * (180 / PI);
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

        // Conversion based on the Robots plugin (https://github.com/visose/Robots) and Lobster Reloaded by Daniel Piker.
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

        // Based on (https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles)
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

        /// Quaternion interpolation code from the Robots plugin (https://github.com/visose/Robots) based on: http://www.grasshopper3d.com/group/lobster/forum/topics/lobster-reloaded
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

        public static double Remap(double value, double startLow, double startHigh, double targetLow, double targetHigh)
        {
            double remappedValue = targetLow + (value - startLow) / (startHigh - startLow) * (targetHigh - targetLow);
            return remappedValue;
        }

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

        public static string[] SplitCommand(string s, string manufacturer)
        {
            // Use a char array of manufacturer specific delimeters to split the command.
            char[] delimiters = new char[] { };

            if (manufacturer == "ABB")
            {
                delimiters = new char[] { ',', '[', ']', '{', ' ' };
            }
            else
            {
                // Insert KUKA command delimiters.
                delimiters = new char[] { };
            }

            string[] parts = s.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }

        public static List<List<string>> SplitProgram(List<string> program, int nSize = 5000)
        {
            var list = new List<List<string>>();

            for (int i = 0; i < program.Count; i += nSize)
            {
                list.Add(program.GetRange(i, Math.Min(nSize, program.Count - i)));
            }

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
            {
                return -1;
            }
        }

        public static List<double> SnapValues(List<double> values, List<double> snaps)
        {
            List<double> snappedValues = new List<double>();
            double closest = 0;
            for (int i = 0; i < values.Count; i++)
            {
                closest = snaps.Aggregate((x, y) => Math.Abs(x - values[i]) < Math.Abs(y - values[i]) ? x : y);
                snappedValues.Add(closest);
            }
            return snappedValues;
        }

        public static void CreateCSV(DataTable table, string path, string name, string delim)
        {

            string filePath = string.Concat(path, name, @".csv");
            string delimiter = delim;

            StringBuilder sb = new StringBuilder();
            List<string> row = new List<string>();

            // Write headers
            foreach (DataColumn c in table.Columns)
            {
                row.Add(c.ColumnName.ToString());
            }
            sb.AppendLine(string.Join(delimiter, row));

            // Write data
            foreach (DataRow r in table.Rows)
            {
                row.Clear();

                // Go through each column adding to a list of strings
                foreach (DataColumn c in table.Columns)
                {
                    row.Add(r[c.ColumnName].ToString());
                }

                sb.AppendLine(string.Join(delimiter, row));
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        public static DataTable ReadExcel(string path)
        {
            DataTable dt = new DataTable();

            // Create COM Objects. Create a COM object for everything that is referenced
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(path);
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];
            Excel.Range xlRange = xlWorksheet.UsedRange;

            int rowCount = xlRange.Rows.Count;
            int colCount = xlRange.Columns.Count;

            // Iterate over the rows and columns and print to the console as it appears in the file
            // NB: Excel is not zero based!!
            for (int i = 1; i <= rowCount; i++)
            {
                if (i == 1)
                {
                    for (int j = 1; j <= colCount; j++)
                    {
                        // Write the value to the data tree
                        if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                        {
                            dt.Columns.Add(xlRange.Cells[i, j].Value2.ToString());
                        }
                    }
                }
                else
                {
                    string[] vals = new string[colCount];

                    for (int j = 1; j <= colCount; j++)
                    {
                        // Write the value to the data tree
                        if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                        {
                            // Skip the first null element due to the zero based difference between GH and Excel
                            vals[j - 1] = xlRange.Cells[i, j].Value2.ToString();
                        }
                    }

                    dt.Rows.Add(vals);
                }
            }

            // Cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Rule of thumb for releasing com objects:
            // never use two dots, all COM objects must be referenced and released individually
            // ex: [somthing].[something].[something] is bad

            // Release COM objects to fully kill excel process from running in the background
            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);

            // Close and release
            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);

            // Quit and release
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);

            return dt;
        }

        internal sealed class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern bool AllocConsole();

            [DllImport("kernel32.dll")]
            public static extern bool FreeConsole();
        }
    }
}
 