using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;
using Axis.Robot;
using Axis.Tools;
using Axis.Targets;

namespace Axis.Core
{
    public class CheckProgram : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.CheckProgram;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("73f9ed66-9a0d-48aa-afdc-9a7a8902031c"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        public CheckProgram() : base("Check Program", "Check", "Check an entire program for kinematic errors.", "Axis", "1. Core")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Program", "Program", "Robot program as list of commands.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run a check of the entire program.", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
            pManager.AddPointParameter("Error", "Errors", "List of program error points.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize variables to store the incoming data.
            List<GH_ObjectWrapper> program = new List<GH_ObjectWrapper>();
            Plane target = Plane.WorldXY;
            Manipulator robot = null;
            List<int> indices = new List<int>();
            List<double> angles = new List<double>();
            List<Plane> planes = new List<Plane>();

            List<Point3d> errorPositions = new List<Point3d>();

            int singularityTol = 5;
            bool run = false;

            // Get the data.
            if (!DA.GetDataList(0, program)) return;
            if (!DA.GetData(1, ref robot)) return;
            DA.GetData(2, ref run);

            List<string> log = new List<string>();
            Point3d errorPos = new Point3d();

            if (robot.Indices != null)
            {
                if (robot.Indices.Count != 6) // Check to see if we have indices already.
                {
                    indices = new List<int>() { 2, 2, 2, 2, 2, 2 };
                }
                else { indices = robot.Indices; }
            }
            else { indices = new List<int>() { 2, 2, 2, 2, 2, 2 }; } // Otherwise assign indices from the robot object.

            int counter = 0;
            
            Target targ = null;
            MotionType mType = MotionType.Linear;

            if (run)
            {
                foreach (GH_ObjectWrapper command in program)
                {
                    // Retrieve the current target from the program.
                    GH_ObjectWrapper currTarg = command;

                    Type cType = currTarg.Value.GetType();

                    if (cType.Name == "GH_String")
                    {
                        continue;
                    }
                    else
                    {
                        targ = currTarg.Value as Target; // Cast back to target type
                    }

                    target = targ.Plane;

                    // Transform the robot target from the base plane to the XY plane.
                    target.Transform(robot.InverseRemap);
                    errorPos = target.Origin;

                    Transform xForm = Transform.PlaneToPlane(Plane.WorldXY, target);

                    Plane tempTarg = Plane.WorldXY;

                    tempTarg.Transform(targ.Tool.FlangeOffset);
                    tempTarg.Transform(xForm);

                    target = tempTarg;

                    mType = targ.Method;
                    
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

                    List<double> selectedAngles = new List<double>();

                    Point3d WristLocation = new Point3d();
                    double Axis1Angle = 0;

                    if (mType != MotionType.NoMovement)
                    {
                        if (mType == MotionType.Linear || mType == MotionType.Joint)
                        {
                            // Adjust plane to comply with robot programming convetions.
                            //Plane plane = new Plane(target.Origin, -target.XAxis, target.YAxis);

                            // Transform the robot target from the base plane to the XY plane.
                            target.Transform(robot.InverseRemap);

                            // Find the wrist position by moving back along the robot flange the distance of the wrist link
                            // defined in the DH / robot parameters
                            WristLocation = new Point3d(target.PointAt(0, 0, -robot.WristOffset));

                            Axis1Angle = -1 * Math.Atan2(WristLocation.Y, WristLocation.X);

                            // Check for overhead singularity
                            if (WristLocation.Y < singularityTol && WristLocation.Y > -singularityTol && WristLocation.X < singularityTol && WristLocation.X > -singularityTol)
                            {
                                log.Add("Close to overhead singularity found at command " + counter.ToString() + ".");
                                continue;
                            }
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

                        // Check the movement type, if it is a linear or joint interpolation, continue..
                        if (mType == MotionType.Linear || mType == MotionType.Joint)
                        {
                            double A1 = a1list[indices[0]];
                            selectedAngles.Add(A1);
                            double A2 = a2list[indices[1]] + 90;
                            selectedAngles.Add(A2);
                            double A3 = a3list[indices[2]] - 90;
                            selectedAngles.Add(A3);
                            double A4 = a4list[indices[3]];
                            selectedAngles.Add(A4);
                            double A5 = a5list[indices[4]];
                            selectedAngles.Add(A5);
                            double A6 = a6list[indices[5]];
                            selectedAngles.Add(A6);

                            for (int i = 0; i < 6; i++)
                            {
                                // Check if the solution value is inside the manufacturer permitted range.
                                if (selectedAngles[i] > robot.MaxAngles[i] || selectedAngles[i] < robot.MinAngles[i])
                                {
                                    int axis = i + 1;
                                    log.Add("Axis " + axis.ToString() + " is out of rotation domain at command " + counter.ToString() + ".");

                                    errorPositions.Add(errorPos);

                                    break;
                                }

                                // Check for singularity and replace the preview color.
                                if (selectedAngles[4] > -singularityTol && selectedAngles[4] < singularityTol)
                                {
                                    log.Add("Close to wrist singularity at command " + counter.ToString() + ".");
                                    break;
                                }
                            }
                        }

                        // Absolute target checks
                        else if (mType == MotionType.AbsoluteJoint)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                // Check if the solution value is inside the manufacturer permitted range
                                if (angles[i] > robot.MaxAngles[i] || angles[i] < robot.MinAngles[i])
                                {
                                    int axis = i + 1;
                                    log.Add("Axis " + axis.ToString() + " is out of rotation domain at command " + counter.ToString() + ".");
                                    break;
                                }
                            }
                            // Check for wrist singularity, clear the list of preview colors and override
                            if (angles[4] < singularityTol && angles[4] > -singularityTol)
                            {
                                log.Add("Wrist singularity at command " + counter.ToString() + ".");
                                break;
                            }
                        }

                        counter++;
                    }
                }
            }

            else
            {
                this.Message = "Disabled";
                log.Add("Program check not active.");
            }

            if (log.Count == 0)
            {
                log.Add("No kinematic errors were found in program.");
                this.Message = "No Errors";
            }
            else { this.Message = "Check Log"; }

            DA.SetDataList(0, log);
            DA.SetDataList(1, errorPositions);
        }
    }
}