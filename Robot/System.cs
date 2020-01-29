using System;
using System.Drawing;
using System.Collections.Generic;
using static System.Math;

using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace Axis.Core
{
    public class Manipulator : GH_Goo<Manipulator>
    {
        public Manufacturer Manufacturer { get; set; }
        public string Name { get; set; }
        public List<Point3d> AxisPoints { get; }
        public List<Plane> AxisPlanes { get; }
        public List<Plane> tAxisPlanes { get; }
        public List<Mesh> RobMeshes { get; }
        public Plane RobBasePlane { get; set; }

        public double WristOffset { get; set; }
        public double LowerArmLength { get; set; }
        public double UpperArmLength { get; set; }
        public double AxisFourOffset { get; set; }

        public List<Mesh> ikMeshes { get; private set; }
        public List<double> ikAngles { get; private set; }
        public List<Plane> ikPlanes { get; private set; }
        public Plane ikFlange { get; private set; }
        private List<Color> colors;
        public bool overHeadSig { get; private set; }
        public bool outOfReach { get; private set; }
        public bool wristSing { get; private set; }
        public bool outOfRoation { get; private set; }

        public int singularityTol = 5;

        
        public Transform Remap { get; }
        public Transform InverseRemap { get; }

        public List<double> MinAngles { get; set; }
        public List<double> MaxAngles { get; set; }
        public List<int> Indices { get; set; }


        public Manipulator(Manufacturer manufacturer, List<Point3d> axisPoints, List<double> minAngles, List<double> maxAngles, List<Mesh> robMeshes, Plane basePlane, List<int> indices)
        {
            List<Mesh> tRobMeshes = new List<Mesh>();

            this.AxisPoints = axisPoints;
            this.Indices = indices;

            // Shorter notation of axis points.
            Point3d p1 = axisPoints[0];
            Point3d p2 = axisPoints[1];
            Point3d p3 = axisPoints[2];
            Point3d p4 = axisPoints[3];

            // Figure out kinematic distances for use in the IK.
            double wristOffset = p4.X - p3.X;
            double lowerArmLength = p1.DistanceTo(p2);
            double upperArmLength = p2.DistanceTo(p3);
            double axisFourOffsetAngle = Math.Atan2(p3.Z - p2.Z, p3.X - p2.X);

            this.WristOffset = wristOffset;
            this.LowerArmLength = lowerArmLength;
            this.UpperArmLength = upperArmLength;
            this.AxisFourOffset = axisFourOffsetAngle;
            this.MinAngles = minAngles;
            this.MaxAngles = maxAngles;

            // Create axis planes in relation to robot joint points.
            List<Plane> axisPlanes = new List<Plane>();
            List<Plane> tAxisPlanes = new List<Plane>();

            Plane AP1 = Plane.WorldZX;
            AP1.Origin = p1;
            axisPlanes.Add(AP1);
            Plane AP2 = Plane.WorldZX;
            AP2.Origin = p2;
            axisPlanes.Add(AP2);
            Plane AP3 = Plane.WorldYZ;
            AP3.Origin = p3;
            axisPlanes.Add(AP3);
            Plane AP4 = Plane.WorldZX;
            AP4.Origin = p3;
            axisPlanes.Add(AP4);
            Plane AP5 = Plane.WorldYZ;
            AP5.Origin = p3;
            axisPlanes.Add(AP5);
            Plane AP6 = new Plane(Plane.WorldXY.Origin, -Plane.WorldXY.ZAxis, Plane.WorldXY.YAxis);
            AP6.Origin = p4;
            axisPlanes.Add(AP6);

            // Robot base plane transformations.
            Transform remap = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            Transform inverseRemap = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            this.Remap = remap;
            this.InverseRemap = inverseRemap;

            // Transform the axis planes to the new robot location.
            for (int i = 0; i < axisPlanes.Count; i++)
            {
                Plane tempPlane = new Plane(axisPlanes[i]);
                tempPlane.Transform(remap);
                tAxisPlanes.Add(tempPlane);
            }

            // Then transform all of these meshes based on the input base plane.
            List<Mesh> ikMeshes = new List<Mesh>();
            for (int i = 0; i < robMeshes.Count; i++)
            {
                Mesh mesh = new Mesh();
                mesh = robMeshes[i].DuplicateMesh();
                mesh.Transform(remap);
                tRobMeshes.Add(mesh);

                // Duplicate and add the temporary mesh to the inverse kinematics mesh list.
                Mesh ikMesh = new Mesh();
                ikMesh = mesh.DuplicateMesh();
                ikMeshes.Add(ikMesh);
            }

            this.Manufacturer = manufacturer;
            this.RobMeshes = tRobMeshes;
            this.RobBasePlane = basePlane;

            // Indepent lists of information for the inverse kinematics.
            this.ikMeshes = ikMeshes;
            this.tAxisPlanes = tAxisPlanes;
        }

        public List<Mesh> StartPose()
        {
            List<Mesh> robMeshes = RobMeshes;

            return robMeshes;
        }
        public void SetPose(Targets.Target target) 
        {
            //Signals
            bool overHeadSig = false;
            bool outOfReach = false;
            bool wristSing = false;
            bool outOfRoation = false;

            //Geometry and location
            List<Plane> planes = new List<Plane>();
            Plane flange = new Plane();
            List<Mesh> mesches = new List<Mesh>();
            List<Color> colors = new List<Color>();
            List<double> angles = new List<double>();

            //Invers and farward kinematics devided by manufacturar
            double[] radAngles = new double[6];
            if (this.Manufacturer == Manufacturer.ABB)
            {
                if (target.Method == Targets.MotionType.Linear | target.Method == Targets.MotionType.Joint)
                {
                    var anglesSet = TargetInverseKinematics(target.Plane, out overHeadSig, out outOfReach);

                    for (int i = 0; i < angles.Count; i++)
                    {
                        double sel = anglesSet[i][this.Indices[i]];
                        radAngles[i] = sel.ToRadians();

                        // Correction for setup
                        if (i == 1) sel += 90;
                        if (i == 2) sel -= 90;

                        angles.Add(sel);
                    }

                    colors = CheckJointAngles(angles, out wristSing, out outOfRoation);
                }
                else if (target.Method == Targets.MotionType.AbsoluteJoint)
                {
                    

                    for (int i = 0; i < 6; i++)
                    {
                        angles = target.JointAngles;

                        // Check if the solution value is inside the manufacturer permitted range
                        if (angles[i] < this.MaxAngles[i] && angles[i] > this.MinAngles[i])
                            colors.Add(Styles.DarkGrey);
                        else
                        {
                            colors.Add(Styles.Pink);
                            outOfRoation = true;
                        }
                        angles.Add(angles[i]);
                    }
                     //ABB Spesific
                        radAngles[0] = angles[0].ToRadians();
                        radAngles[1] = (angles[1] - 90).ToRadians();
                        radAngles[2] = (angles[2] + 90).ToRadians();
                        radAngles[3] = -angles[3].ToRadians();
                        radAngles[4] = angles[4].ToRadians();
                        radAngles[5] = -angles[5].ToRadians();
                    
                }
                
            }
            else if (this.Manufacturer == Manufacturer.Kuka)
            {
                if (target.Method == Targets.MotionType.AbsoluteJoint) 
                {
                    for (int i = 0; i < 6; i++)
                    {
                        angles = target.JointAngles;

                        // Check if the solution value is inside the manufacturer permitted range
                        if (angles[i] < this.MaxAngles[i] && angles[i] > this.MinAngles[i])
                            colors.Add(Styles.DarkGrey);
                        else
                        {
                            colors.Add(Styles.Pink);
                            outOfRoation = true;
                        }
                        angles.Add(angles[i]);
                    }
                        for (int i = 0; i < 6; i++)
                            radAngles[i] = angles[i].ToRadians();
                }
            }
            //Space for other manufacturars
            else throw new Exception($"This movment: {target.Method.ToString()} has not jet been implemented for {this.Manufacturer.ToString()}");
            mesches = UpdateRobotMeshes(radAngles, out planes, out flange);

            //Setting the signal
            this.overHeadSig =  overHeadSig;
            this.outOfReach = outOfReach;
            this.wristSing = wristSing;
            this.outOfRoation = outOfRoation;

            //Setting Geometry
            this.ikFlange = flange;
            this.ikMeshes = mesches;
            this.ikPlanes = planes;
        }
        public Manipulator()
        {
        }
        public List<Color> ikColors 
        { 
            get 
            {
                if (this.colors == null) return new List<Color>() { Axis.Styles.DarkGrey };
                else return this.colors;
            } 
            set 
            {
                if (value != null | value.Count != 0)
                    this.colors = value;
            } 
        }
        public override string TypeName => "Manipulator";
        public override string TypeDescription => "Robot movment system";
        public override string ToString()
        {
            return $"Robot {this.Manufacturer.ToString()}";
        }
        public override bool IsValid => true;
        public override int GetHashCode()
        {
            var val = Manufacturer.GetHashCode() + AxisPoints.GetHashCode() + RobBasePlane.GetHashCode() + MinAngles.GetHashCode() + MaxAngles.GetHashCode() + Indices.GetHashCode();
            return base.GetHashCode();
        }
        public override IGH_Goo Duplicate()
        {
            return new Manipulator(this.Manufacturer, this.AxisPoints, this.MinAngles, this.MaxAngles, this.RobMeshes, this.RobBasePlane, this. Indices);
        }
        public BoundingBox GetBoundingBox()
        {
            if (this.ikMeshes == null) return BoundingBox.Empty;

            Mesh joinedMesh = new Mesh();
            foreach (Mesh m in this.ikMeshes)
            {
                joinedMesh.Append(m);
            }
            return joinedMesh.GetBoundingBox(false);
        }


        /// <summary>
        /// Closed form inverse kinematics for a 6 DOF industrial robot. Returns flags for validity and error types.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="overheadSing"></param>
        /// <param name="outOfReach"></param>
        /// <returns></returns>
        private List<List<double>> TargetInverseKinematics(Plane target, out bool overheadSing, out bool outOfReach)
        {
            // Validity checks
            bool unreachable = true;
            bool singularity = false;

            // Get axis points from custom robot class.
            Point3d[] RP = new Point3d[] { this.AxisPoints[0], this.AxisPoints[1], this.AxisPoints[2], this.AxisPoints[3] };

            // Lists of doubles to hold our axis values and our output log.
            List<double> a1list = new List<double>(),
                a2list = new List<double>(),
                a3list = new List<double>(),
                a4list = new List<double>(),
                a5list = new List<double>(),
                a6list = new List<double>();
            List<string> info = new List<string>();

            // Find the wrist position by moving back along the robot flange the distance of the wrist link.
            Point3d WristLocation = new Point3d(target.PointAt(0, 0, -this.WristOffset));

            double angle1 = Math.Atan2(WristLocation.Y, WristLocation.X);

            // Check for overhead singularity and add message to log if needed
            if (WristLocation.Y < this.singularityTol && WristLocation.Y > -this.singularityTol &&
                WristLocation.X < this.singularityTol && WristLocation.X > -this.singularityTol)
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
                Transform Rotation = Transform.Rotation(angle1, Point3d.Origin);

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
                Sphere Sphere1 = new Sphere(P1A, this.LowerArmLength);
                Sphere Sphere2 = new Sphere(WristLocation, this.UpperArmLength);
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
                    double angle3 = Math.PI - angle2 + Math.Atan2(wristy - elbowy, wristx - elbowx) - this.AxisFourOffset;

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
                        Axis4.Rotate(-this.AxisFourOffset, ElbowPlane.ZAxis);
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
        private List<System.Drawing.Color> CheckJointAngles(List<double> selectedAngles, out bool wristSing, out bool outOfRotation)
        {
            // Colours
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            colors.Add(Styles.DarkGrey); // Add robot base.

            bool rotationError = false; bool singularity = false;

            for (int i = 0; i < 6; i++)
            {
                // Check if the solution value is inside the manufacturer permitted range.
                if (selectedAngles[i] < this.MaxAngles[i] && selectedAngles[i] > this.MinAngles[i])
                    colors.Add(Styles.DarkGrey);
                else
                {
                    colors.Add(Styles.Pink);
                    rotationError = true;
                }
                try
                {
                    // Check for singularity and replace the preview color.
                    if (selectedAngles[4] > -this.singularityTol && selectedAngles[4] < this.singularityTol)
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
        private List<Mesh> UpdateRobotMeshes(double[] radAngles, out List<Plane> planesOut, out Plane flange)
        {
            List<Mesh> meshes = this.RobMeshes;
            List<Plane> aPlanes = this.tAxisPlanes;
            List<Plane> robPlanes = new List<Plane>();
            Plane robBase = this.RobBasePlane;

            List<Mesh> meshesOut = new List<Mesh>();
            meshesOut.Add(meshes[0]);

            Transform Rot1 = Transform.Rotation(radAngles[0], robBase.ZAxis, robBase.Origin);
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

    public class Tool: GH_Goo<Tool>
    {
        public string Name { get; set; }
        public Plane TCP { get; set; }
        public double Weight { get; set; }
        public List<Mesh> Geometry { get; }
        public List<Mesh> ikGeometry { get; private set; }
        public Plane ikTCP { get; private set; }
        public Plane ikBase { get; private set; }
        private List<Color> colors;
        public string Declaration { get; }
        public Transform FlangeOffset { get; }
        public Manufacturer Manufacturer { get; }
        public Vector3d relTool { get; }

        public static Tool Default { get; }

        static Tool()
        {
            Default = new Tool("DefaultTool", Plane.WorldXY, 1.5, null, Manufacturer.ABB, Vector3d.Zero);
        }

        public Tool(string name, Plane TCP, double weight, List<Mesh> mesh, Manufacturer type, Vector3d relToolOffset)
        {
            string toolName = name;
            string COG = "[0.0,0.0,10.0]";
            string userOffset = "[1,0,0,0],0,0,0]]";
            bool relT = false;
            Vector3d tOffset = relToolOffset;

            string strPosX, strPosY, strPosZ;

            // Round each position component to two decimal places.
            double posX = TCP.Origin.X;
            if (posX < 0.005 && posX > -0.005) { strPosX = "0.00"; }
            else { strPosX = posX.ToString("#.##"); }

            double posY = TCP.Origin.Y;
            if (posY < 0.005 && posY > -0.005) { strPosY = "0.00"; }
            else { strPosY = posY.ToString("#.##"); }

            double posZ = TCP.Origin.Z;
            if (posZ < 0.005 && posZ > -0.005) { strPosZ = "0.00"; }
            else { strPosZ = posZ.ToString("#.##"); }

            // Recompose component parts.
            string shortenedPosition = strPosX + "," + strPosY + "," + strPosZ;

            Quaternion quat = Util.QuaternionFromPlane(TCP);

            double A = quat.A, B = quat.B, C = quat.C, D = quat.D;
            double w = Math.Round(A, 6);
            double x = Math.Round(B, 6);
            double y = Math.Round(C, 6);
            double z = Math.Round(D, 6);

            string strQuat = w.ToString() + "," + x.ToString() + "," + y.ToString() + "," + z.ToString();

            // Compute the tool TCP offset from the flange.
            Transform fOffset = Transform.PlaneToPlane(TCP, Plane.WorldXY);

            string declaration;


            if (type == Manufacturer.Kuka)
            {
                /*
                List<double> eulers = new List<double>();
                eulers = Util.QuaternionToEuler(quat);

                double eA = eulers[0];
                double eB = eulers[1];
                double eC = eulers[2];

                // Compose KRL-formatted declaration.
                declaration = "$TOOL = {X " + strPosX + ", Y " + strPosY + ", Z " + strPosZ + ", A " + eA.ToString() + ", B " + eB.ToString() + ", C " + eC.ToString() + "}";
                */
                declaration = "KUKA TOOL DECLARATION";
            }
            else if (type == Manufacturer.ABB)
            {
                // Compose RAPID-formatted declaration.
                declaration = "PERS tooldata " + toolName + " := [TRUE,[[" + shortenedPosition + "], [" + strQuat + "]], [" + weight.ToString() + "," + COG + "," + userOffset + ";";
            }
            else throw new Exception("Manufacturer not yet implemented");

            this.Name = toolName;
            this.TCP = TCP;
            this.Weight = weight;
            this.Geometry = mesh;
            this.Declaration = declaration;
            this.FlangeOffset = fOffset;
            this.relTool = tOffset;
            this.Manufacturer = type;
        }

        public List<Color> ikColors
        {
            get
            {
                if (this.colors == null) return new List<Color>() { Axis.Styles.DarkGrey };
                else return this.colors;
            }
            set
            {
                if (value != null | value.Count != 0)
                    this.colors = value;
            }
        }

        public override string TypeName => "Tool";
        public override string TypeDescription => "Robot end effector";
        public override string ToString()
        {
            return $"Tool: {this.Name}";
        }
        public override bool IsValid => true;
        public override int GetHashCode()
        {
            var val = Name.GetHashCode() + TCP.GetHashCode() + Weight.GetHashCode() + FlangeOffset.GetHashCode();
            return base.GetHashCode();
        }
        public override IGH_Goo Duplicate()
        {
            return new Tool(this.Name, this.TCP, this.Weight, this.Geometry, this.Manufacturer, this.relTool);
        }
        public BoundingBox GetBoundingBox()
        {
            if (this.ikGeometry == null) return BoundingBox.Empty;

            Mesh joinedMesh = new Mesh();
            foreach (Mesh m in this.ikGeometry)
            {
                joinedMesh.Append(m);
            }
            return joinedMesh.GetBoundingBox(false);
        }

        public void SetPose(Plane flange) 
        {
            List<Mesh> toolMeshes = new List<Mesh>();
            Transform orientFlange = Transform.PlaneToPlane(Plane.WorldXY, flange);

            foreach (Mesh m in this.Geometry)
            {
                Mesh tool = m.DuplicateMesh();
                tool.Transform(orientFlange);
                toolMeshes.Add(tool);
            }

            this.ikBase = flange;

            Plane tcp = new Plane(this.TCP);
            tcp.Transform(orientFlange);
            this.ikTCP = tcp;

            this.ikGeometry = toolMeshes;
        }
    }
    
    public enum Manufacturer
    {
        ABB = 0,
        Kuka = 1,
        Universal = 2
    }
}