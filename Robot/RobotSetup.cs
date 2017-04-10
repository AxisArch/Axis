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
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public List<Point3d> AxisPoints { get; set; }
        public List<Plane> AxisPlanes { get; set; }
        public List<Mesh> RobMeshes { get; set; }
        public Plane RobBasePlane { get; set; }
        public Tool RobTool { get; set; }

        public double WristOffset { get; set; }
        public double LowerArmLength { get; set; }
        public double UpperArmLength { get; set; }
        public double AxisFourOffset { get; set; }

        public List<Mesh> IKMeshes { get; set; }
        public Transform Remap { get; }
        public Transform InverseRemap { get; }

        public Manipulator(string manufacturer, string model, List<Point3d> axisPoints, List<Mesh> robMeshes, Plane basePlane, Tool robTool)
        {
            List<Point3d> tAxisPoints = new List<Point3d>();
            List<Plane> tAxisPlanes = new List<Plane>();
            List<Mesh> tRobMeshes = new List<Mesh>();

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
            
            // Create axis planes in relation to robot joint points.
            List<Plane> axisPlanes = new List<Plane>();

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
            AP5.Rotate(Math.PI / 2, AP5.ZAxis);
            axisPlanes.Add(AP5);
            Plane AP6 = Plane.WorldYZ;
            AP6.Origin = p4;
            AP6.Rotate(Math.PI / 2, AP6.ZAxis);
            axisPlanes.Add(AP6);

            // Consolidate robot and tool meshes into a single list for IK transformation later.
            List<Mesh> robotMeshes = new List<Mesh>();
            for (int i = 0; i < robMeshes.Count; i++)
            {
                robotMeshes.Add(robMeshes[i]);
            }

            for (int i = 0; i < robTool.Mesh.Count; i++)
            {
                Plane toolFlange = new Plane(axisPlanes[5].Origin, -axisPlanes[5].XAxis, -axisPlanes[5].YAxis);
                Transform toolToFlange = Transform.PlaneToPlane(Plane.WorldXY, toolFlange);
                Mesh tMesh = robTool.Mesh[i].DuplicateMesh();
                tMesh.Transform(toolToFlange);
                robotMeshes.Add(tMesh);
            }

            // Robot base plane transformations.
            Transform remap = Transform.PlaneToPlane(Plane.WorldXY, basePlane);
            Transform inverseRemap = Transform.PlaneToPlane(basePlane, Plane.WorldXY);
            this.Remap = remap;
            this.InverseRemap = inverseRemap;
            
            for (int i = 0; i < axisPlanes.Count; i++)
            {
                Plane tPlane = new Plane(axisPlanes[i]);
                tPlane.Transform(remap);
                tAxisPlanes.Add(tPlane);
            }

            // Add the new transformed points to the list based on the axis planes.
            tAxisPoints.Add(tAxisPlanes[0].Origin);
            tAxisPoints.Add(tAxisPlanes[1].Origin);
            tAxisPoints.Add(tAxisPlanes[2].Origin);
            tAxisPoints.Add(tAxisPlanes[5].Origin);

            // Then transform all of these meshes based on the input base plane.
            List<Mesh> ikMeshes = new List<Mesh>();
            for (int i = 0; i < robotMeshes.Count; i++)
            {
                robotMeshes[i].Transform(remap);
                tRobMeshes.Add(robotMeshes[i]);

                // Duplicate and add the temporary mesh to the inverse kinematics mesh list.
                Mesh tempMesh = new Mesh();
                tempMesh = robotMeshes[i].DuplicateMesh();
                ikMeshes.Add(tempMesh);
            }

            this.Manufacturer = manufacturer;
            this.Model = model;
            this.AxisPoints = tAxisPoints;
            this.AxisPlanes = tAxisPlanes;
            this.RobMeshes = tRobMeshes;
            this.RobBasePlane = basePlane;
            this.RobTool = robTool;

            // Indepent lists of information for the inverse kinematics.
            this.IKMeshes = ikMeshes;
        }

        public List<Mesh> StartPose()
        {
            Plane basePlane = RobBasePlane;
            List<Mesh> robMeshes = RobMeshes;

            return robMeshes;
        }
    }

    public class Tool
    {
        public string Name { get; set; }
        public Plane TCP { get; set; }
        public double Weight { get; set; }
        public List<Mesh> Mesh { get; set; }
        public string Declaration { get; }
        public Transform FlangeOffset { get; }
        
        public Tool(string name, Plane tcp, double weight, List<Mesh> mesh)
        {
            string toolName = "t_" + name;
            string COG = "[0.0,0.0,10.0]";
            string userOffset = "[1,0,0,0],0,0,0]]";

            string strPosX, strPosY, strPosZ;

            // Round each position component to two decimal places.
            double posX = tcp.Origin.X;
            if (posX < 0.005)
            {
                strPosX = "0.00";
            }
            else
            {
                strPosX = posX.ToString("#.##");
            }
            double posY = tcp.Origin.Y;
            if (posY < 0.005)
            {
                strPosY = "0.00";
            }
            else
            {
                strPosY = posY.ToString("#.##");
            }
            double posZ = tcp.Origin.Z;
            if (posZ < 0.005)
            {
                strPosZ = "0.00";
            }
            else
            {
                strPosZ = posZ.ToString("#.##");
            }

            // Recompose component parts.
            string shortenedPosition = strPosX + "," + strPosY + "," + strPosZ;

            Quaternion quat = Util.QuaternionFromPlane(tcp);

            double A = quat.A, B = quat.B, C = quat.C, D = quat.D;
            double w = Math.Round(A, 6);
            double x = Math.Round(B, 6);
            double y = Math.Round(C, 6);
            double z = Math.Round(D, 6);

            string strQuat = w.ToString() + "," + x.ToString() + "," + y.ToString() + "," + z.ToString();

            // Compose RAPID-formatted declaration.
            string declaration = "PERS tooldata " + toolName + " := [TRUE,[[" + shortenedPosition + "], [" + strQuat + "]], [" + weight.ToString() + "," + COG + "," + userOffset + ";";

            this.Name = toolName;
            this.TCP = tcp;
            this.Weight = weight;
            this.Mesh = mesh;
            this.Declaration = declaration;
        }
    }
}