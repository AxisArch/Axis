using System;
using System.Drawing;
using System.Collections.Generic;
using static System.Math;

using Rhino.Geometry;
using Axis.Tools;

namespace Axis.Core
{
    public class Manipulator
    {
        public bool Manufacturer { get; set; }
        public List<Point3d> AxisPoints { get; }
        public List<Plane> AxisPlanes { get; }
        public List<Plane> tAxisPlanes { get; }
        public List<Mesh> RobMeshes { get; }
        public Plane RobBasePlane { get; set; }

        public double WristOffset { get; set; }
        public double LowerArmLength { get; set; }
        public double UpperArmLength { get; set; }
        public double AxisFourOffset { get; set; }

        public List<Mesh> IKMeshes { get; }
        public Transform Remap { get; }
        public Transform InverseRemap { get; }

        public List<double> MinAngles { get; set; }
        public List<double> MaxAngles { get; set; }

        public List<int> Indices { get; set; }

        public Manipulator(bool manufacturer, List<Point3d> axisPoints, List<double> minAngles, List<double> maxAngles, List<Mesh> robMeshes, Plane basePlane, List<int> indices)
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
            this.IKMeshes = ikMeshes;
            this.tAxisPlanes = tAxisPlanes;
        }

        public List<Mesh> StartPose()
        {
            List<Mesh> robMeshes = RobMeshes;

            return robMeshes;
        }

        public Manipulator()
        {
        }
    }

    public class Tool
    {
        public string Name { get; set; }
        public Plane TCP { get; set; }
        public double Weight { get; set; }
        public List<Mesh> Geometry { get; set; }
        public string Declaration { get; }
        public Transform FlangeOffset { get; }
        public bool Type { get; }
        public Vector3d relTool { get; }

        public static Tool Default { get; }

        static Tool()
        {
            Default = new Tool("DefaultTool", Plane.WorldXY, 1.5, null, false, Vector3d.Zero);
        }

        public Tool(string name, Plane TCP, double weight, List<Mesh> mesh, bool type, Vector3d relToolOffset)
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
            if (posY < 0.005 && posY> -0.005) { strPosY = "0.00"; }
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


            if (type)
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
            else
            {
                // Compose RAPID-formatted declaration.
                declaration = "PERS tooldata " + toolName + " := [TRUE,[[" + shortenedPosition + "], [" + strQuat + "]], [" + weight.ToString() + "," + COG + "," + userOffset + ";";
            }

            this.Name = toolName;
            this.TCP = TCP;
            this.Weight = weight;
            this.Geometry = mesh;
            this.Declaration = declaration;
            this.FlangeOffset = fOffset;
            this.relTool = tOffset;
        }
    }
}