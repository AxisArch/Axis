using System;
using System.Collections.Generic;

using Rhino.Geometry;

using Grasshopper.Kernel;

using Axis.Robot;
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

        public InverseKinematics() : base("Inverse Kinematics", "Kinematics", "Inverse and forward kinematics for a 6 degree of freedom robotic arm, based on Lobster Reloaded by Daniel Piker.", "Axis", "1. Core")
        {
        }

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

        // Sticky variables.
        public double[] currPos = { 0, -90, 90, 0, 0, 0 };
        public static int singularityTol = 5;
        protected List<double[]> motionPlan = new List<double[]>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Target robTarg = null;
            Manipulator robot = null;

            if (!DA.GetData(0, ref robot)) return;
            if (!DA.GetData(1, ref robTarg)) return;

            List<string> log = new List<string>();

            // Get the list of kinematic indices from the robot definition, otherwise use default values.
            List<int> indices = new List<int>() { 2, 2, 2, 2, 2, 2 };
            if (robot.Indices.Count == 6) indices = robot.Indices;

            // Kinematics error flags
            bool isValid = true; bool overheadSing = false; bool wristSing = false; bool outOfReach = false; bool outOfRotation = false;

            List<double> angles = new List<double>();
            List<double> selectedAngles = new List<double>();
            double[] radAngles = new double[6];

            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();



            if (robTarg.Method == MotionType.AbsoluteJoint) // Forward kinematics
            {
                colors.Clear();
                for (int i = 0; i < 6; i++)
                {
                    angles = robTarg.JointAngles;

                    // Check if the solution value is inside the manufacturer permitted range
                    if (angles[i] < robot.MaxAngles[i] && angles[i] > robot.MinAngles[i])
                        colors.Add(Styles.DarkGrey);
                    else
                    {
                        colors.Add(Styles.Pink);
                        outOfRotation = true;
                    }
                    selectedAngles.Add(angles[i]);
                }
                if (robot.Manufacturer != true) // Adjust for KUKA home position being different to ABB.
                {
                    radAngles[0] = -angles[0].ToRadians();
                    radAngles[1] = (angles[1] - 90).ToRadians();
                    radAngles[2] = (angles[2] + 90).ToRadians();
                    for (int i = 3; i < 6; i++)
                        radAngles[i] = angles[i].ToRadians();
                }
                else
                    for (int i = 0; i < 6; i++)
                        radAngles[i] = angles[i].ToRadians();
            }
            else // Inverse kinematics
            {
                // Transform the robot target from the robot base plane to the XY plane.
                Plane target = robTarg.Plane;
                target.Transform(robot.InverseRemap);

                Transform xForm = Transform.PlaneToPlane(Plane.WorldXY, target);
                Plane tempTarg = Plane.WorldXY;

                tempTarg.Transform(robTarg.Tool.FlangeOffset);
                tempTarg.Transform(xForm);
                target = tempTarg;

                // Inverse kinematics
                List<List<double>> ikAngles = TargetInverseKinematics(robot, target, out overheadSing, out outOfReach);

                // Select an angle from each list and make the radian version.
                for (int i = 0; i < ikAngles.Count; i++)
                {
                    double sel = ikAngles[i][indices[i]];
                    radAngles[i] = sel.ToRadians();

                    // Correction for setup
                    if (i == 1) sel += 90;
                    if (i == 2) sel -= 90;

                    selectedAngles.Add(sel);
                }

                // Check the joint angles.
                colors = CheckJointAngles(robot, selectedAngles, out wristSing, out outOfRotation);
            }

            #region Commented out
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
            #endregion

            // Handle errors
            if (overheadSing || wristSing || outOfReach || outOfRotation) isValid = false;
            if (overheadSing) log.Add("Close to overhead singularity.");
            if (wristSing) log.Add("Close to wrist singularity.");
            if (outOfReach) log.Add("Target out of range.");
            if (outOfRotation) log.Add("Joint out of range.");

            // Check the validity of the current target and update the current position if ok, otherwise use last valid.
            double[] robPos = currPos;
            if (isValid)
            {
                robPos = radAngles; currPos = radAngles;
            }
            else robPos = currPos;

            //  Update the position of our robot geometry and store the outputs.
            List<Plane> planesOut = new List<Plane>();
            Plane flange = new Plane();
            List<Mesh> robMesh = UpdateRobotMeshes(robot, robPos, out planesOut, out flange);

            // Transform tool per target to robot flange.
            List<Mesh> toolMeshes = new List<Mesh>();
            Transform orientFlange = Transform.PlaneToPlane(Plane.WorldXY, flange);
            try
            {
                foreach (Mesh m in robTarg.Tool.Geometry)
                {
                    Mesh tool = m.DuplicateMesh();
                    tool.Transform(orientFlange);
                    toolMeshes.Add(tool);
                }
            }
            // Add warning if no mesh is present
            catch { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No tool mesh present."); }

            // ******** Check that this works
            // Colour mesh
            //robMesh = Util.ColorMeshes(robMesh, colors);

            // Output data
            DA.SetDataList(0, robMesh);
            DA.SetData(1, flange);
            DA.SetDataList(2, selectedAngles);
            DA.SetDataList(3, colors);
            DA.SetDataList(4, log);
            DA.SetDataList(5, toolMeshes);
        }

        /// <summary>
        /// Closed form inverse kinematics for a 6 DOF industrial robot. Returns flags for validity and error types.
        /// </summary>
        /// <param name="robot"></param>
        /// <param name="target"></param>
        /// <param name="overheadSing"></param>
        /// <param name="outOfReach"></param>
        /// <returns></returns>
        public static List<List<double>> TargetInverseKinematics(Manipulator robot, Plane target, out bool overheadSing, out bool outOfReach) //Check why outOfTeach flag is beeing set
        {
            // Validity checks
            bool unreachable = true;
            bool singularity = false;

            // Get axis points from custom robot class.
            Point3d[] RP = new Point3d[] { robot.AxisPoints[0], robot.AxisPoints[1], robot.AxisPoints[2], robot.AxisPoints[3] };

            // Lists of doubles to hold our axis values and our output log.
            List<double> a1list = new List<double>(), 
                a2list = new List<double>(), 
                a3list = new List<double>(), 
                a4list = new List<double>(), 
                a5list = new List<double>(), 
                a6list = new List<double>();
            List<string> info = new List<string>();

            // Find the wrist position by moving back along the robot flange the distance of the wrist link.
            Point3d WristLocation = new Point3d(target.PointAt(0, 0, -robot.WristOffset));

            double angle1 = -1 * Math.Atan2(WristLocation.Y, WristLocation.X);

            // Check for overhead singularity and add message to log if needed
            if (WristLocation.Y < singularityTol && WristLocation.Y > -singularityTol &&
                WristLocation.X < singularityTol && WristLocation.X > -singularityTol)
                singularity = true;

            // Standard cases for axis one.
            if (angle1 > Math.PI) angle1 -= 2 * Math.PI;
            for (int j = 0; j < 4; j++)
                a1list.Add(angle1);

            // Other cases for axis one.
            angle1 += Math.PI;
            if (angle1 > Math.PI) angle1 -= 2 * Math.PI;
            for (int j = 0; j < 4; j++)
                a1list.Add(1 * angle1);

            // Generate four sets of values for each option of axis one
            for (int j = 0; j < 2; j++)
            {
                angle1 = a1list[j * 4];

                // Rotate all of our points based on axis one.
                Transform Rotation = Transform.Rotation(-1 * angle1, Point3d.Origin);

                Point3d P1A = new Point3d(RP[0]);
                Point3d P2A = new Point3d(RP[1]);
                Point3d P3A = new Point3d(RP[2]);

                P1A.Transform(Rotation);
                P2A.Transform(Rotation);
                P3A.Transform(Rotation);

                // Define the elbow direction and create a plane there.
                Vector3d ElbowDir = new Vector3d(1, 0, 0);
                ElbowDir.Transform(Rotation);
                Plane ElbowPlane = new Plane(P1A, ElbowDir, Plane.WorldXY.ZAxis);

                // Create our spheres for doing the intersections.
                Sphere Sphere1 = new Sphere(P1A, robot.LowerArmLength);
                Sphere Sphere2 = new Sphere(WristLocation, robot.UpperArmLength);
                Circle Circ = new Circle();

                double Par1 = new double(), Par2 = new double();

                // Do the intersections and store them as pars.
                Rhino.Geometry.Intersect.Intersection.SphereSphere(Sphere1, Sphere2, out Circ);
                Rhino.Geometry.Intersect.Intersection.PlaneCircle(ElbowPlane, Circ, out Par1, out Par2);

                // Logic to check if the target is unreachable.
                if (unreachable)
                    if (Par1 != double.NaN || Par2 != double.NaN)
                        unreachable = false;

                // Get the points.
                Point3d IntersectPt1 = Circ.PointAt(Par1), IntersectPt2 = Circ.PointAt(Par2);

                // ******** Check that this works
                // Solve IK for the remaining axes using these points.
                for (int k = 0; k < 2; k++)
                {
                    Point3d ElbowPt = new Point3d();

                    if (k == 0) ElbowPt = IntersectPt1;
                    else ElbowPt = IntersectPt2;

                    // Parameters in the elbow plane.
                    double elbowx, elbowy, wristx, wristy;

                    ElbowPlane.ClosestParameter(ElbowPt, out elbowx, out elbowy);
                    ElbowPlane.ClosestParameter(WristLocation, out wristx, out wristy);
                    
                    double angle2 = Math.Atan2(elbowy, elbowx);
                    double angle3 = Math.PI - angle2 + Math.Atan2(wristy - elbowy, wristx - elbowx) - robot.AxisFourOffset;

                    for (int n = 0; n < 2; n++)
                    {
                        a2list.Add(-angle2);
                        double axis3Wrapped = -angle3 + Math.PI;
                        while (axis3Wrapped >= Math.PI) axis3Wrapped -= 2 * Math.PI;
                        while (axis3Wrapped < -Math.PI) axis3Wrapped += 2 * Math.PI;
                        a3list.Add(axis3Wrapped);
                    }

                    for (int n = 0; n < 2; n++)
                    {
                        Vector3d Axis4 = new Vector3d(WristLocation - ElbowPt);
                        Axis4.Rotate(-robot.AxisFourOffset, ElbowPlane.ZAxis);
                        Vector3d LowerArm = new Vector3d(ElbowPt - P1A);
                        Plane TempPlane = ElbowPlane;
                        TempPlane.Rotate(angle2 + angle3, TempPlane.ZAxis);

                        Plane Axis4Plane = new Rhino.Geometry.Plane(WristLocation, TempPlane.ZAxis, -1.0 * TempPlane.YAxis);

                        double axis6x, axis6y;
                        Axis4Plane.ClosestParameter(target.Origin, out axis6x, out axis6y);

                        double angle4 = Math.Atan2(axis6y, axis6x);
                        if (n == 1)
                        {
                            angle4 += Math.PI;
                            if (angle4 > Math.PI)
                                angle4 -= 2 * Math.PI;
                        }

                        double Axis4AngleWrapped = angle4 + Math.PI / 2;
                        while (Axis4AngleWrapped >= Math.PI) Axis4AngleWrapped -= 2 * Math.PI;
                        while (Axis4AngleWrapped < -Math.PI) Axis4AngleWrapped += 2 * Math.PI;
                        a4list.Add(Axis4AngleWrapped);

                        Plane ikAxis5Plane = new Rhino.Geometry.Plane(Axis4Plane);
                        ikAxis5Plane.Rotate(angle4, Axis4Plane.ZAxis);
                        ikAxis5Plane = new Rhino.Geometry.Plane(WristLocation, -ikAxis5Plane.ZAxis, ikAxis5Plane.XAxis);

                        ikAxis5Plane.ClosestParameter(target.Origin, out axis6x, out axis6y);
                        double angle5 = Math.Atan2(axis6y, axis6x);
                        a5list.Add(angle5);

                        Plane ikAxis6Plane = new Rhino.Geometry.Plane(ikAxis5Plane);
                        ikAxis6Plane.Rotate(angle5, ikAxis5Plane.ZAxis);
                        ikAxis6Plane = new Rhino.Geometry.Plane(WristLocation, -ikAxis6Plane.YAxis, ikAxis6Plane.ZAxis);

                        double endx, endy;
                        ikAxis6Plane.ClosestParameter(target.PointAt(1, 0), out endx, out endy);

                        double angle6 = (Math.Atan2(endy, endx));
                        a6list.Add(angle6);
                    }
                }
            }

            // Compile our list of all axis angle value lists.
            List<List<double>> angles = new List<List<double>>();
            angles.Add(a1list); angles.Add(a2list); angles.Add(a3list); angles.Add(a4list); angles.Add(a5list); angles.Add(a6list);

            // Convert all angles to degrees.
            foreach (List<double> aList in angles)
                for (int k = 0; k < 8; k++)
                    aList[k] = Util.ToDegrees(aList[k]);

            // Update validity based on flags
            outOfReach = unreachable;
            overheadSing = singularity;

            return angles; // Return the angles.
        }

        /// <summary>
        /// Check joint angle values against the robot model data. Outputs flags for joint error and wrist singularity. Returns a list of colors.
        /// </summary>
        /// <param name="robot"></param>
        /// <param name="selectedAngles"></param>
        /// <param name="wristSing"></param>
        /// <param name="outOfRotation"></param>
        /// <returns></returns>
        public static List<System.Drawing.Color> CheckJointAngles(Manipulator robot, List<double> selectedAngles, out bool wristSing, out bool outOfRotation)
        {
            // Colours
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            colors.Add(Styles.DarkGrey); // Add robot base.

            bool rotationError = false; bool singularity = false;

            for (int i = 0; i < 6; i++)
            {
                // Check if the solution value is inside the manufacturer permitted range.
                if (selectedAngles[i] < robot.MaxAngles[i] && selectedAngles[i] > robot.MinAngles[i])
                    colors.Add(Styles.DarkGrey);
                else
                {
                    colors.Add(Styles.Pink);
                    rotationError = true;
                }
                try
                {
                    // Check for singularity and replace the preview color.
                    if (selectedAngles[4] > -singularityTol && selectedAngles[4] < singularityTol)
                    {
                        colors[5] = (Styles.Blue);
                        singularity = true;
                    }
                }
                catch { }
            }
            colors.Add(Styles.DarkGrey); // Hardcoded tool color

            outOfRotation = rotationError;
            wristSing = singularity;

            return colors;
        }

        /// <summary>
        /// Update robot mesh geometry based on axis values. Returns a list of mesh geometry.
        /// </summary>
        /// <param name="robot"></param>
        /// <param name="radAngles"></param>
        /// <param name="robPlanes"></param>
        /// <param name="flange"></param>
        /// <returns></returns>
        public List<Mesh> UpdateRobotMeshes(Manipulator robot, double[] radAngles, out List<Plane> planesOut, out Plane flange)
        {
            List<Mesh> meshes = robot.IKMeshes;
            List<Plane> aPlanes = robot.tAxisPlanes;
            List<Plane> robPlanes = new List<Plane>();
            Plane robBase = robot.RobBasePlane;

            List<Mesh> meshesOut = new List<Mesh>();
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
            robPlanes.Add(Planes1[0]);
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
            robPlanes.Add(Planes2[1]);
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
            robPlanes.Add(Planes3[2]);
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
            robPlanes.Add(Planes4[3]);
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
            robPlanes.Add(Planes5[4]);
            meshesOut.Add(Meshes6[0]);

            flange = Planes6[Planes6.Count - 1];
            planesOut = robPlanes;

            return meshesOut;
        }
    }
}