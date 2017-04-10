using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Axis.Tools;
using Axis.Core;

namespace Axis.Targets
{
    public class Target
    {
        public Point3d Position { get; set; }
        public Plane TargetPlane { get; set; }
        public Quaternion Quaternion { get; set; }
        public string Tool { get; }
        public string WorkObject { get; }
        public string StrABB { get; }
        public string StrKUKA { get; }
        public int Method { get; }

        public Target(Plane target, int method, int speed, int zone, string tool, string wobj, int robot)
        {
            Point3d position = target.Origin;
            double posX = Math.Round(position.X, 3);
            double posY = Math.Round(position.Y, 3);
            double posZ = Math.Round(position.Z, 3);

            // Adjust plane to comply with robot programming convetions.
            // Plane plane = new Plane(target.Origin, target.XAxis, target.YAxis);
            Plane plane = new Plane(target);
            Quaternion quat = Util.QuaternionFromPlane(plane);
            
            string movement = null;
            string strABB = null;
            string strKUKA = null;

            // ABB
            if (robot == 0)
            {
                string ABBposition = posX.ToString() + ", " + posY.ToString() + ", " + posZ.ToString();

                double A = quat.A, B = quat.B, C = quat.C, D = quat.D;
                double w = Math.Round(A, 6);
                double x = Math.Round(B, 6);
                double y = Math.Round(C, 6);
                double z = Math.Round(D, 6);

                string strQuat = w.ToString() + ", " + x.ToString() + ", " + y.ToString() + ", " + z.ToString();

                // If the method is linear or joint..
                if (method == 0)
                {
                    movement = "MoveL";
                }
                else if (method == 1)
                {
                    movement = "MoveJ";
                }

                string workObject = @"\Wobj:=" + wobj;

                strABB = movement + @" [[" + ABBposition + "],[" + strQuat + "]," + "cData," + "eAxis]" + ",v" + speed.ToString() + ",z" + zone.ToString() + ",t_" + tool + workObject + ";";
            }

            // KUKA
            else
            {
                string KUKAposition = "X " + posX.ToString() + ", Y " + posY.ToString() + ", Z " + posZ.ToString();

                List<double> eulers = Util.QuaternionToEuler(quat);

                double E1 = eulers[0] * 180 / Math.PI;
                E1 = Math.Round(E1, 3);
                double E2 = eulers[1] * 180 / Math.PI;
                E2 = Math.Round(E2, 3);
                double E3 = eulers[2] * 180 / Math.PI;
                E3 = Math.Round(E3, 3);

                string strEuler = "A " + E1.ToString() + ", B " + E2.ToString() + ", C " + E3.ToString();
                string strExtAxis = "E1 0, E2 0, E3 0, E4 0";
                string approx = "";

                // If the method is linear or joint..
                if (method == 0)
                {
                    movement = "LIN";
                    approx = "C_VEL";
                }
                else if (method == 1)
                {
                    movement = "PTP";
                    approx = "C_PTP";
                }
                strKUKA = movement + " {E6POS: " + KUKAposition + ", " + strEuler + ", " + strExtAxis + "} " + approx;
            }

            this.Position = position;
            this.TargetPlane = plane;
            this.Quaternion = quat;
            this.WorkObject = wobj;
            this.StrABB = strABB;
            this.StrKUKA = strKUKA;
            this.Method = method;
            this.Tool = tool;
        }
    }
}