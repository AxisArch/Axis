using System;
using System.Collections.Generic;

using Rhino.Geometry;

using Grasshopper.Kernel;

using Axis.Robot;
using Axis.Tools;
using Axis.Targets;

namespace Axis.Core
{
    public class InverseKinematics : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.IK;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{9079ef8a-31c0-4c5a-8f04-775b9aa21047}"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        public InverseKinematics() : base("Inverse Kinematics", "Kinematics", "Inverse and forward kinematics for a 6 degree of freedom robotic arm, based on Lobster Reloaded by Daniel Piker.","Axis", "1. Core")
        {
        }

        // Inverse kinematics for a six-axis industrial robot, based on Lobster Reloaded by Daniel Piker
        /*
         * WIP: Create compound transformation for end effector, only transform meshes once & integrate toggle to simulate entire program without displaying meshes.
         * */

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Target", "Target", "Robotic target for inverse kinematics. Use the simulation component to select a specific target from a toolpath for preview of the kinematic solution.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Meshes", "Meshes", "Transformed robot geometry as list.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Flange", "Flange", "Robot flange position.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angles", "Angles", "Axis angles for forward kinematics.", GH_ParamAccess.list);
            pManager.AddColourParameter("Colour", "Colour", "Preview indication colours.", GH_ParamAccess.list);
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Tool", "Tool", "Tool mesh as list.", GH_ParamAccess.list);
        }

        public double[] currPos = { 0, -90, 90, 0, 0, 0 };
        protected List<double[]> motionPlan = new List<double[]>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize variables to store the incoming data
            Target robTarg = null;
            Manipulator robot = null;
            List<int> indices = new List<int>();
            List<double> angles = new List<double>();
            int singularityTol = 5;

            // Get the data.
            if (!DA.GetData(0, ref robot)) return;
            DA.GetData(1, ref robTarg);

            // Get the list of kinematic indices from the robot definition.
            indices = robot.Indices;
            Plane target = Plane.WorldXY;
            bool isValid = true;

            List<double> anglesOut = new List<double>();
            Plane flangeOut = new Plane();
            List<Mesh> meshesOut = new List<Mesh>();
            List<string> colorsOut = new List<string>();
            List<string> logOut = new List<string>();
            List<object> debugOut = new List<object>();
            List<Plane> planesOut = new List<Plane>();


            if (indices.Count != 6)
            {
                indices = new List<int>() { 2, 2, 2, 2, 2, 2 };
            }

            List<string> colors = new List<string>();
            List<string> log = new List<string>();
            List<Plane> aPlns = new List<Plane>();

            double[] radAngles = new double[6];

            List<double> selectedAngles = new List<double>();

            if (robTarg.Method == MotionType.AbsoluteJoint)
            {
                colors.Clear();
                for (int i = 0; i < 5; i++)
                {
                    angles = robTarg.JointAngles;

                    // Check if the solution value is inside the manufacturer permitted range
                    if (angles[i] < robot.MaxAngles[i] && angles[i] > robot.MinAngles[i])
                    {
                        colors.Add("210, 210, 210");
                    }
                    else
                    {
                        colors.Add("202, 47, 24");
                        log.Add("Axis " + i.ToString() + " is out of rotation domain.");
                        isValid = false;
                    }
                    selectedAngles.Add(angles[i]);
                }

                // For KUKA, adjust the angles to conform with the home position.
                if (robot.Manufacturer != true)
                {
                    // Adjust the first three axis positions.
                    radAngles[0] = -angles[0].ToRadians();
                    radAngles[1] = (angles[1] - 90).ToRadians();
                    radAngles[2] = (angles[2] + 90).ToRadians();

                    for (int i = 3; i < 6; i++)
                    {
                        radAngles[i] = angles[i].ToRadians();
                    }
                }
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        radAngles[i] = angles[i].ToRadians();
                    }
                }
            }

            else
            {
                target = robTarg.Plane;

                // Transform the robot target from the base plane to the XY plane.
                target.Transform(robot.InverseRemap);

                Transform xForm = Transform.PlaneToPlane(Plane.WorldXY, target);

                Plane tempTarg = Plane.WorldXY;

                tempTarg.Transform(robTarg.Tool.FlangeOffset);
                tempTarg.Transform(xForm);

                target = tempTarg;

                // Get axis points from custom robot class.
                Point3d P1 = robot.AxisPoints[0];
                Point3d P2 = robot.AxisPoints[1];
                Point3d P3 = robot.AxisPoints[2];
                Point3d P4 = robot.AxisPoints[3];

                List<double> a1list = new List<double>();
                List<double> a2list = new List<double>();
                List<double> a3list = new List<double>();
                List<double> a4list = new List<double>();
                List<double> a5list = new List<double>();
                List<double> a6list = new List<double>();

                // Find the wrist position by moving back along the robot flange the distance of the wrist link
                // defined in the DH / robot parameters
                Point3d WristLocation = new Point3d(target.PointAt(0, 0, -robot.WristOffset));

                double Axis1Angle = -1 * Math.Atan2(WristLocation.Y, WristLocation.X);

                // Check for overhead singularity
                if (WristLocation.Y < singularityTol && WristLocation.Y > -singularityTol && WristLocation.X < singularityTol && WristLocation.X > -singularityTol)
                {
                    log.Add("Overhead singularity detected.");
                }

                if (Axis1Angle > Math.PI)
                {
                    Axis1Angle -= 2 * Math.PI;
                }

                for (int j = 0; j < 4; j++)
                {
                    a1list.Add(Axis1Angle);
                }

                Axis1Angle += Math.PI;

                if (Axis1Angle > Math.PI)
                {
                    Axis1Angle -= 2 * Math.PI;
                }

                for (int j = 0; j < 4; j++)
                {
                    a1list.Add(1 * Axis1Angle);
                }

                // Generate four sets of values for each option of axis one
                for (int j = 0; j < 2; j++)
                {
                    Axis1Angle = a1list[j * 4];

                    Transform Rotation = Transform.Rotation(-1 * Axis1Angle, Point3d.Origin);

                    Point3d P1A = new Point3d(P1);
                    Point3d P2A = new Point3d(P2);
                    Point3d P3A = new Point3d(P3);

                    // Transform each of our new points with our new transformation.
                    P1A.Transform(Rotation);
                    P2A.Transform(Rotation);
                    P3A.Transform(Rotation);

                    Vector3d ElbowDir = new Vector3d(1, 0, 0);
                    ElbowDir.Transform(Rotation);
                    Plane ElbowPlane = new Plane(P1A, ElbowDir, Plane.WorldXY.ZAxis);

                    Sphere Sphere1 = new Sphere(P1A, robot.LowerArmLength);
                    Sphere Sphere2 = new Sphere(WristLocation, robot.UpperArmLength);
                    Circle Circ = new Circle();

                    double Par1 = new double();
                    double Par2 = new double();

                    Rhino.Geometry.Intersect.Intersection.SphereSphere(Sphere1, Sphere2, out Circ);
                    Rhino.Geometry.Intersect.Intersection.PlaneCircle(ElbowPlane, Circ, out Par1, out Par2);

                    Point3d IntersectPt1 = Circ.PointAt(Par1);
                    Point3d IntersectPt2 = Circ.PointAt(Par2);

                    for (int k = 0; k < 2; k++)
                    {
                        Point3d ElbowPt = new Point3d();

                        if (k == 0)
                        { ElbowPt = IntersectPt1; }
                        else
                        { ElbowPt = IntersectPt2; }

                        double elbowx, elbowy, wristx, wristy; // Parameters in the elbow plane.

                        ElbowPlane.ClosestParameter(ElbowPt, out elbowx, out elbowy);
                        ElbowPlane.ClosestParameter(WristLocation, out wristx, out wristy);

                        double Axis2Angle = Math.Atan2(elbowy, elbowx);
                        double Axis3Angle = Math.PI - Axis2Angle + Math.Atan2(wristy - elbowy, wristx - elbowx) - robot.AxisFourOffset;

                        for (int n = 0; n < 2; n++)
                        {
                            a2list.Add(-Axis2Angle);

                            double Axis3AngleWrapped = -Axis3Angle + Math.PI;
                            while (Axis3AngleWrapped >= Math.PI) Axis3AngleWrapped -= 2 * Math.PI;
                            while (Axis3AngleWrapped < -Math.PI) Axis3AngleWrapped += 2 * Math.PI;
                            a3list.Add(Axis3AngleWrapped);
                        }

                        for (int n = 0; n < 2; n++)
                        {
                            Vector3d Axis4 = new Vector3d(WristLocation - ElbowPt);
                            Axis4.Rotate(-robot.AxisFourOffset, ElbowPlane.ZAxis);
                            Vector3d LowerArm = new Vector3d(ElbowPt - P1A);
                            Plane TempPlane = ElbowPlane;
                            TempPlane.Rotate(Axis2Angle + Axis3Angle, TempPlane.ZAxis);

                            Plane Axis4Plane = new Rhino.Geometry.Plane(WristLocation, TempPlane.ZAxis, -1.0 * TempPlane.YAxis);

                            double axis6x, axis6y;
                            Axis4Plane.ClosestParameter(target.Origin, out axis6x, out axis6y);

                            double Axis4Angle = Math.Atan2(axis6y, axis6x);
                            if (n == 1)
                            {
                                Axis4Angle += Math.PI;
                                if (Axis4Angle > Math.PI)
                                {
                                    Axis4Angle -= 2 * Math.PI;
                                }
                            }

                            double Axis4AngleWrapped = Axis4Angle + Math.PI / 2;
                            while (Axis4AngleWrapped >= Math.PI) Axis4AngleWrapped -= 2 * Math.PI;
                            while (Axis4AngleWrapped < -Math.PI) Axis4AngleWrapped += 2 * Math.PI;
                            a4list.Add(Axis4AngleWrapped);

                            Plane ikAxis5Plane = new Rhino.Geometry.Plane(Axis4Plane);
                            ikAxis5Plane.Rotate(Axis4Angle, Axis4Plane.ZAxis);
                            ikAxis5Plane = new Rhino.Geometry.Plane(WristLocation, -ikAxis5Plane.ZAxis, ikAxis5Plane.XAxis);

                            ikAxis5Plane.ClosestParameter(target.Origin, out axis6x, out axis6y);
                            double Axis5Angle = Math.Atan2(axis6y, axis6x);
                            a5list.Add(Axis5Angle);

                            Plane ikAxis6Plane = new Rhino.Geometry.Plane(ikAxis5Plane);
                            ikAxis6Plane.Rotate(Axis5Angle, ikAxis5Plane.ZAxis);
                            ikAxis6Plane = new Rhino.Geometry.Plane(WristLocation, -ikAxis6Plane.YAxis, ikAxis6Plane.ZAxis);

                            double endx, endy;
                            ikAxis6Plane.ClosestParameter(target.PointAt(1, 0), out endx, out endy);

                            double Axis6Angle = (Math.Atan2(endy, endx));
                            a6list.Add(Axis6Angle);
                        }
                    }
                }

                for (int k = 0; k < 8; k++)
                {
                    a1list[k] = Util.ToDegrees(a1list[k]);
                    a2list[k] = Util.ToDegrees(a2list[k]);
                    a3list[k] = Util.ToDegrees(a3list[k]);
                    a4list[k] = Util.ToDegrees(a4list[k]);
                    a5list[k] = Util.ToDegrees(a5list[k]);
                    a6list[k] = Util.ToDegrees(a6list[k]);
                }

                double A1 = a1list[indices[0]];
                selectedAngles.Add(A1);
                radAngles[0] = A1.ToRadians();
                double A2 = a2list[indices[1]] + 90;
                selectedAngles.Add(A2);
                radAngles[1] = (A2 - 90).ToRadians();
                double A3 = a3list[indices[2]] - 90;
                selectedAngles.Add(A3);
                radAngles[2] = (A3 + 90).ToRadians();
                double A4 = a4list[indices[3]];
                selectedAngles.Add(A4);
                radAngles[3] = A4.ToRadians();
                double A5 = a5list[indices[4]];
                selectedAngles.Add(A5);
                radAngles[4] = A5.ToRadians();
                double A6 = a6list[indices[5]];
                selectedAngles.Add(A6);
                radAngles[5] = A6.ToRadians();

                // Add the base colour as standard.
                colors.Add("210, 210, 210");

                for (int i = 0; i < 6; i++)
                {
                    // Check if the solution value is inside the manufacturer permitted range.
                    if (selectedAngles[i] < robot.MaxAngles[i] && selectedAngles[i] > robot.MinAngles[i])
                    {
                        colors.Add("210, 210, 210"); // Near white.
                    }
                    else
                    {
                        colors.Add("202, 47, 24"); // Red.
                        log.Add("Axis " + i + " is out of rotation domain.");
                        isValid = false;
                    }

                    // Check for singularity and replace the preview color.
                    if (selectedAngles[4] > -singularityTol && selectedAngles[4] < singularityTol)
                    {
                        colors[5] = ("59, 162, 117");
                        log.Add("Close to singularity.");
                    }
                }
            }

            /*
            double largestDiff = 0;
            double angularStep = Math.Round(Util.ToRadians(2),4);

            if (currPos != radAngles)
            {
                for (int n = 0; n < radAngles.Length; n++)
                {
                    double diff = Math.Abs(radAngles[n] - currPos[n]);
                    if (diff > largestDiff) { largestDiff = diff; }
                }

                double lerpCount = Math.Ceiling((double)largestDiff / (double)angularStep);
                if (lerpCount < 1) { lerpCount = 1; }

                double[] step = new double[6];

                for (int g = 0; g < lerpCount; g++)
                {
                    for (int h = 0; h < radAngles.Length; h++)
                    {
                        double max = Math.Abs(radAngles[h] - currPos[h]);
                        double increment = max / lerpCount;

                        step[h] = currPos[h] + increment;
                        increment = increment * (h + 1);
                    }
                    motionPlan.Add(step);
                }
            }
            */

            // Check the validity of the current target and update the current position if ok.
            if (isValid)
            {
                currPos = radAngles;
            }         
            else // Otherwise, revert to the last valid pose.
            {
                radAngles = currPos;
            }

            // Start updating all of the meshes based on the selected angles list.
            List<Mesh> meshes = robot.IKMeshes;
            List<Plane> aPlanes = robot.tAxisPlanes;
            Plane robBase = robot.RobBasePlane;

            meshesOut.Add(meshes[0]);

            Transform Rot1 = Transform.Rotation(-1 * radAngles[0], robBase.ZAxis, robBase.Origin);
            List<Mesh> Meshes1 = new List<Mesh>();
            List<Plane> Planes1 = new List<Plane>();
            for (int i = 1; i < meshes.Count; i++)
            {
                Mesh temp = meshes[i].DuplicateMesh();
                temp.Transform(Rot1);
                Meshes1.Add(temp);
            }
            for (int i = 0; i < 6; i++)
            {
                Plane temp = new Plane(aPlanes[i]);
                temp.Transform(Rot1);
                Planes1.Add(temp);
            }

            meshesOut.Add(Meshes1[0]);

            Transform Rot2 = Transform.Rotation(radAngles[1] + Math.PI / 2, Planes1[0].ZAxis, Planes1[0].Origin);
            List<Mesh> Meshes2 = new List<Mesh>();
            List<Plane> Planes2 = new List<Plane>();
            for (int i = 1; i < Meshes1.Count; i++)
            {
                Mesh temp = Meshes1[i].DuplicateMesh();
                temp.Transform(Rot2);
                Meshes2.Add(temp);
            }
            for (int i = 0; i < Planes1.Count; i++)
            {
                Plane temp = new Plane(Planes1[i]);
                temp.Transform(Rot2);
                Planes2.Add(temp);
            }

            aPlns.Add(Planes1[0]);
            meshesOut.Add(Meshes2[0]);

            Transform Rot3 = Transform.Rotation(radAngles[2] - Math.PI / 2, Planes2[1].ZAxis, Planes2[1].Origin);
            List<Mesh> Meshes3 = new List<Mesh>();
            List<Plane> Planes3 = new List<Plane>();
            for (int i = 1; i < Meshes2.Count; i++)
            {
                Mesh temp = Meshes2[i].DuplicateMesh();
                temp.Transform(Rot3);
                Meshes3.Add(temp);
            }
            for (int i = 0; i < Planes2.Count; i++)
            {
                Plane temp = new Plane(Planes2[i]);
                temp.Transform(Rot3);
                Planes3.Add(temp);
            }

            aPlns.Add(Planes2[1]);
            meshesOut.Add(Meshes3[0]);

            Transform Rot4 = Transform.Rotation(radAngles[3] * -1.0, Planes3[2].ZAxis, Planes3[2].Origin);
            List<Mesh> Meshes4 = new List<Mesh>();
            List<Plane> Planes4 = new List<Plane>();
            for (int i = 1; i < Meshes3.Count; i++)
            {
                Mesh temp = Meshes3[i].DuplicateMesh();
                temp.Transform(Rot4);
                Meshes4.Add(temp);
            }
            for (int i = 0; i < Planes3.Count; i++)
            {
                Plane temp = new Plane(Planes3[i]);
                temp.Transform(Rot4);
                Planes4.Add(temp);
            }

            aPlns.Add(Planes3[2]);
            meshesOut.Add(Meshes4[0]);

            Transform Rot5 = Transform.Rotation(radAngles[4], Planes4[3].ZAxis, Planes4[3].Origin);
            List<Mesh> Meshes5 = new List<Mesh>();
            List<Plane> Planes5 = new List<Plane>();
            for (int i = 1; i < Meshes4.Count; i++)
            {
                Mesh temp = Meshes4[i].DuplicateMesh();
                temp.Transform(Rot5);
                Meshes5.Add(temp);
            }
            for (int i = 0; i < Planes4.Count; i++)
            {
                Plane temp = new Plane(Planes4[i]);
                temp.Transform(Rot5);
                Planes5.Add(temp);
            }

            aPlns.Add(Planes4[3]);
            meshesOut.Add(Meshes5[0]);

            Transform Rot6 = Transform.Rotation(-1.0 * radAngles[5], Planes5[4].ZAxis, Planes5[4].Origin);
            List<Mesh> Meshes6 = new List<Mesh>();
            List<Plane> Planes6 = new List<Plane>();
            for (int i = 1; i < Meshes5.Count; i++)
            {
                Mesh temp = Meshes5[i].DuplicateMesh();
                temp.Transform(Rot6);
                Meshes6.Add(temp);
            }
            for (int i = 0; i < Planes5.Count; i++)
            {
                Plane temp = new Plane(Planes5[i]);
                temp.Transform(Rot6);
                Planes6.Add(temp);
            }

            aPlns.Add(Planes5[4]);
            meshesOut.Add(Meshes6[0]);

            flangeOut = Planes6[Planes6.Count - 1];
            anglesOut = selectedAngles;
            planesOut = aPlns;

            // Transform tool per target to robot flange.
            List<Mesh> toolMeshes = new List<Mesh>();
            Transform orientFlange = Transform.PlaneToPlane(Plane.WorldXY, flangeOut);
            foreach (Mesh m in robTarg.Tool.Geometry)
            {
                Mesh tool = m.DuplicateMesh();
                tool.Transform(orientFlange);
                toolMeshes.Add(tool);
            }
                        
            colorsOut = colors;
            logOut = log;

            // Output data
            DA.SetDataList(0, meshesOut);
            DA.SetData(1, flangeOut);
            DA.SetDataList(2, anglesOut);
            DA.SetDataList(3, colorsOut);
            DA.SetDataList(4, logOut);
            DA.SetDataList(5, toolMeshes);
        }
    }
}
 