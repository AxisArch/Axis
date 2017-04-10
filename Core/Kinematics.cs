using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Axis.Targets;
using Axis.Tools;

namespace Axis.Core
{
    public class Kinematics : GH_Component
    {
        // Inverse and forward kinematics for a six-axis ABB robot.

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconCore;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{1d458edc-6470-4373-9851-439ea99f4a1f}"); }
        }

        public Kinematics() : base("Kinematics", "Kinematics", "Inverse and forward kinematics solutions for six-axes.", "Axis", "Core")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "Toolpath", "Robotic toolpath for inverse kinematics.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Robot", "Robot", "Robot to use for inverse kinematics.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Mode", "Mode", "Kinematic mode selection. [True = Preview, False = Code Export]", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Preview", "Preview", "Slider from 0 - 1 indicating position along program to preview.", GH_ParamAccess.item, 0.000);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Robot", "Robot", "Transformed robot mesh geometry as list.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Axis Angles", "Axis Angles", "Matrix of axis angle arrays.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Program", "Program", "Robot program as list of RAPID commands.", GH_ParamAccess.item);
            pManager.AddGenericParameter("IKStuff", "IKStuff", "Lists of generic IK stuff.", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Target> toolpath = new List<Target>();
            Manipulator robot = null;
            bool previewMode = true;
            double previewSlider = 0.000;

            if (!DA.GetDataList(0, toolpath)) return;
            if (!DA.GetData(1, ref robot)) return;
            if (!DA.GetData(2, ref previewMode)) return;
            if (!DA.GetData(3, ref previewSlider)) return;

            // Get tool from robot definition.
            Tool tool = robot.RobTool;

            // Get axis points from custom robot class.
            Point3d P1 = robot.AxisPoints[0];
            Point3d P2 = robot.AxisPoints[1];
            Point3d P3 = robot.AxisPoints[2];
            Point3d P4 = robot.AxisPoints[3];

            // Remap preview slider value to selection index based on toolpath length.
            double toolpathLength = toolpath.Count;
            double previewVal = previewSlider;
            double remappedValue = Util.Remap(previewVal, 0, 1, 0, toolpathLength);
            int previewIndex = Convert.ToInt32(remappedValue - 1);

            if (previewIndex == -1)
            {
                previewIndex = 0;
            }
            else if (previewIndex == toolpathLength)
            {
                previewIndex -= 1;
            }

            if ((previewMode) && (toolpath.Count > 0))
            {

                List<double> Axis1Angles = new List<double>();
                List<double> Axis2Angles = new List<double>();
                List<double> Axis3Angles = new List<double>();
                List<double> Axis4Angles = new List<double>();
                List<double> Axis5Angles = new List<double>();
                List<double> Axis6Angles = new List<double>();

                List<double> selectedAngles = new List<double>();

                Plane targPlane = toolpath[previewIndex].TargetPlane;

                // Transform robot target based on tool TCP offset.
                Transform flangeOffset = Transform.PlaneToPlane(tool.TCP, Plane.WorldXY);
                targPlane.Transform(flangeOffset);
                Point3d wristLocation = new Point3d(targPlane.PointAt(0, 0, -robot.WristOffset));

                // DEBUG //
                List<object> ikStuff = new List<object>();
                ikStuff.Add(targPlane);
                ikStuff.Add(wristLocation);

                DA.SetDataList(3, ikStuff);
                /*
                // If the selected target has a linear movement method specification, then perform inverse kinematics.
                if (toolpath[previewIndex].Method <= 1)
                {
                    Point3d ikWrist = new Point3d(wristLocation);
                    ikWrist.Transform(robot.InverseRemap);

                    double Axis1Angle = -1 * Math.Atan2(ikWrist.Y, ikWrist.X);

                    if (Axis1Angle > Math.PI)
                    {
                        Axis1Angle -= 2 * Math.PI;
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        Axis1Angles.Add(Axis1Angle);
                    }

                    Axis1Angle += Math.PI;

                    if (Axis1Angle > Math.PI)
                    {
                        Axis1Angle -= 2 * Math.PI;
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        Axis1Angles.Add(1 * Axis1Angle);
                    }

                    // Generate four sets of values for each option of axis one
                    for (int j = 0; j < 2; j++)
                    {
                        Axis1Angle = Axis1Angles[j * 4];

                        Transform Rotation = Transform.Rotation((-1 * Axis1Angle) + Math.PI, robot.RobBasePlane.Origin);

                        Point3d P1A = new Point3d(P1);
                        Point3d P2A = new Point3d(P2);
                        Point3d P3A = new Point3d(P3);

                        Vector3d ElbowDir = robot.RobBasePlane.XAxis;
                        Plane ElbowPlane = new Plane(P1A, ElbowDir, robot.RobBasePlane.ZAxis);
                        ElbowPlane.Transform(Rotation);
                        ikStuff.Add(ElbowPlane);

                        // Transform each of our new points with our new transformation.
                        P1A.Transform(Rotation);
                        P2A.Transform(Rotation);
                        P3A.Transform(Rotation);

                        Sphere Sphere1 = new Sphere(P1A, robot.LowerArmLength);
                        Sphere Sphere2 = new Sphere(wristLocation, robot.UpperArmLength);
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

                            // Remap the elbow and wrist points from the robot coordinate system to the world XY system.
                            Point3d ikElbowPt = new Point3d(ElbowPt);
                            ikElbowPt.Transform(robot.InverseRemap);

                            Plane ikElbowPlane = new Plane(ElbowPlane);
                            ikElbowPlane.Transform(robot.InverseRemap);


                            Point3d ikWristLocation = new Point3d(wristLocation);
                            ikWristLocation.Transform(robot.InverseRemap);

                            double elbowx, elbowy, wristx, wristy; // Parameters in the elbow plane.

                            ikElbowPlane.ClosestParameter(ikElbowPt, out elbowx, out elbowy);
                            ikElbowPlane.ClosestParameter(ikWristLocation, out wristx, out wristy);

                            double Axis2Angle = Math.Atan2(elbowy, elbowx);
                            double Axis3Angle = Math.PI - Axis2Angle + Math.Atan2(wristy - elbowy, wristx - elbowx) - robot.AxisFourOffset;

                            for (int n = 0; n < 2; n++)
                            {
                                Axis2Angles.Add(-Axis2Angle);

                                double Axis3AngleWrapped = -Axis3Angle + Math.PI;
                                while (Axis3AngleWrapped >= Math.PI) Axis3AngleWrapped -= 2 * Math.PI;
                                while (Axis3AngleWrapped < -Math.PI) Axis3AngleWrapped += 2 * Math.PI;
                                Axis3Angles.Add(Axis3AngleWrapped);
                            }

                            for (int n = 0; n < 2; n++)
                            {
                                Vector3d Axis4 = new Vector3d(ikWristLocation - ikElbowPt);
                                Axis4.Rotate(-robot.AxisFourOffset, ikElbowPlane.ZAxis);
                                Vector3d LowerArm = new Vector3d(ikElbowPt - P1A);
                                Plane TempPlane = ikElbowPlane;
                                TempPlane.Rotate(Axis2Angle + Axis3Angle, TempPlane.ZAxis);

                                Plane ikAxis4Plane = new Rhino.Geometry.Plane(ikWristLocation, TempPlane.ZAxis, -1.0 * TempPlane.YAxis);

                                Plane ikEndPlane = new Plane(endPlane.Origin, endPlane.XAxis, endPlane.YAxis);
                                ikEndPlane.Transform(robot.InverseRemap);


                                double axis6x, axis6y;
                                ikAxis4Plane.ClosestParameter(ikEndPlane.Origin, out axis6x, out axis6y);

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
                                Axis4Angles.Add(Axis4AngleWrapped);

                                Plane ikAxis5Plane = new Rhino.Geometry.Plane(ikAxis4Plane);
                                ikAxis5Plane.Rotate(Axis4Angle, ikAxis4Plane.ZAxis);
                                ikAxis5Plane = new Rhino.Geometry.Plane(wristLocation, -ikAxis5Plane.ZAxis, ikAxis5Plane.XAxis);

                                ikAxis5Plane.ClosestParameter(ikEndPlane.Origin, out axis6x, out axis6y);
                                double Axis5Angle = Math.Atan2(axis6y, axis6x);
                                Axis5Angles.Add(Axis5Angle);

                                Plane ikAxis6Plane = new Rhino.Geometry.Plane(ikAxis5Plane);
                                ikAxis6Plane.Rotate(Axis5Angle, ikAxis5Plane.ZAxis);
                                ikAxis6Plane = new Rhino.Geometry.Plane(wristLocation, -ikAxis6Plane.YAxis, ikAxis6Plane.ZAxis);

                                double endx, endy;
                                ikAxis6Plane.ClosestParameter(ikEndPlane.PointAt(1, 0), out endx, out endy);

                                double Axis6Angle = (Math.Atan2(endy, endx));
                                Axis6Angles.Add(Axis6Angle);
                            }
                        }
                    }
                    double pi = Math.PI;
                    for (int k = 0; k < 8; k++)
                    {
                        Axis1Angles[k] = Axis1Angles[k] * 180 / pi;
                        Axis2Angles[k] = (Axis2Angles[k] * 180 / pi) + 90;
                        Axis4Angles[k] = (Axis4Angles[k] * 180 / pi);
                        Axis5Angles[k] = Axis5Angles[k] * 180 / pi;
                        Axis6Angles[k] = Axis6Angles[k] * 180 / pi;
                    }

                    List<object> axisAngles = new List<object>();

                    axisAngles.Add(Axis1Angles);
                    axisAngles.Add(Axis2Angles);
                    axisAngles.Add(Axis3Angles);
                    axisAngles.Add(Axis4Angles);
                    axisAngles.Add(Axis5Angles);
                    axisAngles.Add(Axis6Angles);

                    // Select inverse kinematic solution
                    double A1 = Axis1Angles[0];
                    selectedAngles.Add(A1);
                    double A1Rad = A1.ToRadians();
                    double A2 = Axis2Angles[6];
                    selectedAngles.Add(A2);
                    double A2Rad = A2.ToRadians();
                    double A3 = Axis3Angles[2];
                    selectedAngles.Add(A3);
                    double A3Rad = A3.ToRadians();
                    double A4 = Axis4Angles[1];
                    selectedAngles.Add(A4);
                    double A4Rad = A4.ToRadians();
                    double A5 = Axis5Angles[0];
                    selectedAngles.Add(A5);
                    double A5Rad = A5.ToRadians();
                    double A6 = Axis6Angles[1];
                    selectedAngles.Add(A6);
                    double A6Rad = A6.ToRadians();

                    List<Plane> axisPlanes = new List<Plane>();
                    for (int i = 0; i < robot.AxisPlanes.Count; i++)
                    {
                        Plane tempPlane = new Plane(robot.AxisPlanes[i].Origin, robot.AxisPlanes[i].XAxis, robot.AxisPlanes[i].YAxis);
                        axisPlanes.Add(tempPlane);
                    }

                    // Reference our input meshes, and create a new list of manipulatable meshes that will act as our output meshes.
                    List<Mesh> robotMeshes = new List<Mesh>();

                    for (int i = 0; i < robot.IKMeshes.Count; i++)
                    {
                        Mesh tempMesh = new Mesh();
                        tempMesh = robot.IKMeshes[i].DuplicateMesh();
                        robotMeshes.Add(tempMesh);
                    }
                    */
                /*
                Transform remap = Transform.ChangeBasis(Plane.WorldXY, robot.AxisPlanes[5]);
                for (int i = 0; i < robot.RobTool.Mesh.Count; i++)
                {
                    Mesh tempToolMesh = new Mesh();
                    Mesh toolMesh = robot.RobTool.Mesh[i];
                    tempToolMesh = toolMesh.DuplicateMesh();
                    robotMeshes.Add(tempToolMesh);
                }
                */
                /*
                // Rotation set one.
                Transform Rotation1 = Transform.Rotation(-1 * A1Rad, robot.RobBasePlane.ZAxis, robot.RobBasePlane.Origin);
                for (int j = 1; j < robotMeshes.Count; j++)
                {
                    robotMeshes[j].Transform(Rotation1);
                }

                for (int j = 0; j < axisPlanes.Count; j++)
                {
                    //Plane tempPlane = axisPlanes[j];
                    Plane tempPlane = new Plane(axisPlanes[j].Origin, axisPlanes[j].XAxis, axisPlanes[j].YAxis);
                    tempPlane.Transform(Rotation1);
                    axisPlanes[j] = tempPlane;
                }

                // Rotation set two.
                Transform Rotation2 = Transform.Rotation(A2Rad, axisPlanes[0].ZAxis, axisPlanes[0].Origin);
                for (int j = 2; j < robotMeshes.Count; j++)
                {
                    robotMeshes[j].Transform(Rotation2);
                }

                for (int j = 1; j < axisPlanes.Count; j++)
                {
                    Plane tempPlane = new Plane(axisPlanes[j].Origin, axisPlanes[j].XAxis, axisPlanes[j].YAxis);
                    tempPlane.Transform(Rotation2);
                    axisPlanes[j] = tempPlane;
                }

                // Rotation set three.
                Transform Rotation3 = Transform.Rotation(A3Rad, axisPlanes[1].ZAxis, axisPlanes[1].Origin);
                for (int j = 3; j < robotMeshes.Count; j++)
                {
                    robotMeshes[j].Transform(Rotation3);
                }
                for (int j = 2; j < axisPlanes.Count; j++)
                {
                    Plane tempPlane = new Plane(axisPlanes[j].Origin, axisPlanes[j].XAxis, axisPlanes[j].YAxis);
                    tempPlane.Transform(Rotation3);
                    axisPlanes[j] = tempPlane;
                }

                // Rotation set four.
                Transform Rotation4 = Transform.Rotation(A4Rad * -1.0, axisPlanes[2].ZAxis, axisPlanes[2].Origin);
                for (int j = 4; j < robotMeshes.Count; j++)
                {
                    robotMeshes[j].Transform(Rotation4);
                }
                for (int j = 3; j < axisPlanes.Count; j++)
                {
                    Plane tempPlane = new Plane(axisPlanes[j].Origin, axisPlanes[j].XAxis, axisPlanes[j].YAxis);
                    tempPlane.Transform(Rotation4);
                    axisPlanes[j] = tempPlane;
                }

                // Rotation set five.
                Transform Rotation5 = Transform.Rotation(A5Rad, axisPlanes[3].ZAxis, axisPlanes[3].Origin);
                for (int j = 5; j < robotMeshes.Count; j++)
                {
                    robotMeshes[j].Transform(Rotation5);
                }
                for (int j = 4; j < axisPlanes.Count; j++)
                {
                    Plane tempPlane = new Plane(axisPlanes[j].Origin, axisPlanes[j].XAxis, axisPlanes[j].YAxis);
                    tempPlane.Transform(Rotation5);
                    axisPlanes[j] = tempPlane;
                }

                // Rotation set six.
                Transform Rotation6 = Transform.Rotation(-1.0 * A6Rad, axisPlanes[4].ZAxis, axisPlanes[4].Origin);
                for (int j = 6; j < robotMeshes.Count; j++)
                {
                    robotMeshes[j].Transform(Rotation6);
                }
                for (int j = 5; j < axisPlanes.Count; j++)
                {
                    Plane tempPlane = new Plane(axisPlanes[j].Origin, axisPlanes[j].XAxis, axisPlanes[j].YAxis);
                    tempPlane.Transform(Rotation6);
                    axisPlanes[j] = tempPlane;
                }

                // Output robot mesh preview geometry, as well as a list of the resultant joint angles.
                DA.SetDataList(0, robotMeshes);
                DA.SetDataList(1, axisAngles);
                DA.SetDataList(3, ikStuff);
            }
        }
    }
}
*/
                /*
                        int[] confAxes = new int[] { 0, 3, 5 };

                        foreach (int axis in confAxes)
                        {
                            if (selectedAngles[axis] >= 0 && selectedAngles[axis] < 90)
                            {
                                if (axis == 0)
                                {
                                    axisOne = 0;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = 0;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = 0;
                                }
                            }
                            else if (selectedAngles[axis] >= 90 && selectedAngles[axis] < 180)
                            {
                                if (axis == 0)
                                {
                                    axisOne = 1;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = 1;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = 1;
                                }
                            }
                            else if (selectedAngles[axis] >= 180 && selectedAngles[axis] < 270)
                            {
                                if (axis == 0)
                                {
                                    axisOne = 2;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = 2;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = 2;
                                }
                            }
                            else if (selectedAngles[axis] >= 270 && selectedAngles[axis] < 360)
                            {
                                if (axis == 0)
                                {
                                    axisOne = 3;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = 3;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = 3;
                                }
                            }
                            if (selectedAngles[axis] < 0 && selectedAngles[axis] > -90)
                            {
                                if (axis == 0)
                                {
                                    axisOne = -1;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = -1;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = -1;
                                }
                            }
                            else if (selectedAngles[axis] < -90 && selectedAngles[axis] > -180)
                            {
                                if (axis == 0)
                                {
                                    axisOne = -2;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = -2;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = -2;
                                }
                            }
                            else if (selectedAngles[axis] < -180 && selectedAngles[axis] > -270)
                            {
                                if (axis == 0)
                                {
                                    axisOne = -3;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = -3;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = -3;
                                }
                            }
                            else if (selectedAngles[axis] < -270 && selectedAngles[axis] > -360)
                            {
                                if (axis == 0)
                                {
                                    axisOne = -4;
                                }
                                else if (axis == 3)
                                {
                                    axisFour = -4;
                                }
                                else if (axis == 5)
                                {
                                    axisSix = -4;
                                }

                                }
                                        // Replace conf data in original program.
                                        int[] confData = { axisOne, axisFour, axisSix, 0 };

                                        toolpath.Targets[targIndex].ConfData = confData;
                                        // toolpath.ConfData[targIndex] = confData;
                                        DA.SetData(3, confData);

                                    }
                                }

                                else
                                {
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid robot targets were supplied.");
                                    return;
                                }
                            }
                            DA.SetData(2, program);

                            /*
                            // Create empty lists to store data.
                            List<string> log = new List<string>();
                            List<double> angles = new List<double>();
                            List<Color> colors = new List<Color>();

                            // Define Preview Materials.
                            Color normalState = Color.FromArgb(150, 150, 150);
                            Color outOfRotState = Color.FromArgb(160, 0, 0);
                            Color singularityState = Color.FromArgb(0, 0, 160);

                            // Set singularity start state.
                            bool singularity = false;

                                // Start angle check - reference manufacturer angles.
                                if (selectedAngles[0] < 165 && selectedAngles[0] > -165)
                                {
                                    colors.Add(normalState); // If inside rotation, color = gray.
                                }
                                else
                                {
                                    colors.Add(outOfRotState); // If inside rotation, color = red, print error to log.
                                    log.Add("Robot axis 1 is out of rotation domain.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Robot axis 1 is out of rotation domain.");
                                    return;
                                }

                                if (selectedAngles[1] < 110 && selectedAngles[1] > -110)
                                {
                                    colors.Add(normalState);
                                }
                                else
                                {
                                    colors.Add(outOfRotState);
                                    log.Add("Robot axis 2 is out of rotation domain.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Robot axis 2 is out of rotation domain.");
                                    return;
                                }

                                if (selectedAngles[2] < 70 && selectedAngles[2] > -165)
                                {
                                    colors.Add(normalState);
                                }
                                else
                                {
                                    colors.Add(outOfRotState);
                                    log.Add("Robot axis 3 is out of rotation domain.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Robot axis 3 is out of rotation domain.");
                                    return;
                                }

                                if (selectedAngles[3] < 160 && selectedAngles[3] > -160)
                                {
                                    colors.Add(normalState);
                                }
                                else
                                {
                                    colors.Add(outOfRotState);
                                    log.Add("Robot axis 4 is out of rotation domain.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Robot axis 4 is out of rotation domain.");
                                    return;
                                }

                                // Note additional checks on axis 5.
                                if (selectedAngles[4] < 120 && selectedAngles[4] > -120)
                                {
                                    colors.Add(normalState);
                                }
                                else if (selectedAngles[5] < 2 && selectedAngles[5] > -2) // Singularity check.
                                {
                                    singularity = true;
                                    colors.Add(singularityState);
                                    log.Add("The robot is close to a singularity.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The robot is close to a singularity.");
                                    return;
                                }
                                else if (selectedAngles[4] < -2 && selectedAngles[4] > -120)
                                {
                                    colors.Add(normalState);
                                }
                                else
                                {
                                    colors.Add(outOfRotState);
                                    log.Add("Robot axis 5 is out of rotation domain.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Robot axis 5 is out of rotation domain.");
                                    return;
                                }

                                if (selectedAngles[5] < 400 && selectedAngles[5] > -400)
                                {
                                    colors.Add(normalState);
                                }
                                else
                                {
                                    colors.Add(outOfRotState);
                                    log.Add("Robot axis 6 is out of rotation domain.");
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Robot axis 6 is out of rotation domain.");
                                    return;
                                }

                                // Error log.
                                if (log.Count > 0)
                                {
                                    DA.SetData(2, log);
                                }
                                else
                                {
                                    log.Clear();
                                    log.Add("There are no errors to display.");
                                    DA.SetData(2, log);
                                }

                                // Colors log.
                                if (singularity == true)
                                {
                                    colors.Clear();
                                    colors.Add(singularityState);
                                    DA.SetData(3, colors);
                                }
                                else
                                {
                                    DA.SetData(3, colors);
                                }
                                */
            }
        }
    }
}
