using System;
using System.Drawing;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI;
using Grasshopper;

using Rhino.Geometry;
using Axis.Targets;
using Axis.Core;

namespace Axis.Core
{
    // Define a custom robot.
    public class Robot : GH_Component
    {
        public Robot() : base("Robot", "Robot", "Create a kinematic model of a custom robot.", "Axis", "Robot")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconRobot;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{26289bd0-15cc-408f-af2d-5a87ea81cb18}"); }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Manufacturer", "Manufacturer", "Robot manufacturer as string. [used for code generation]", GH_ParamAccess.item, "ABB");
            pManager.AddTextParameter("Model", "Model", "Robot model as string. [used for inverse kinematics]", GH_ParamAccess.item, "IRB.120");
            pManager.AddPointParameter("Axis Points", "Axis Points", "Axis intersection points for kinematics. [4 points]", GH_ParamAccess.list);
            pManager.AddMeshParameter("Robot Mesh", "Robot Mesh", "List of robot mesh geometry. [Base + 6 joint meshes]", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Base Plane", "Base Plane", "Optional custom robot base plane. [Default = World XY]", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddGenericParameter("Tool", "Tool", "The tool to be used for preview purposes.", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }
        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Robot", "Robot", "Custom robot data type.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Meshes", "Meshes", "A list of robot mesh geometry.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string manufacturer = "No Manufacturer Data";
            string model = "No Model Data";
            List<Point3d> inPoints = new List<Point3d>();
            List<Mesh> inMeshes = new List<Mesh>();
            Plane inBase = Plane.WorldXY;
            Tool inTool = null;

            if (!DA.GetData(0, ref manufacturer)) return;
            if (!DA.GetData(1, ref model)) return;
            if (!DA.GetDataList(2, inPoints)) return;
            if (!DA.GetDataList(3, inMeshes)) return;
            if (!DA.GetData(4, ref inBase)) return;
            if (!DA.GetData(5, ref inTool)) return;

            Manipulator robot = new Manipulator(manufacturer, model, inPoints, inMeshes, inBase, inTool);
            List<Mesh> startPose = robot.StartPose();
            List<Plane> axisPlanes = robot.AxisPlanes;

            // Add tool mesh preview by transposing tool to robot flange.
            Transform remap = Transform.ChangeBasis(Plane.WorldXY, robot.AxisPlanes[5]);
            for (int i = 0; i < inTool.Mesh.Count; i++)
            {
                Mesh tempMesh = new Mesh();
                tempMesh = inTool.Mesh[i].DuplicateMesh();
                tempMesh.Transform(remap);
                startPose.Add(tempMesh);
            }

            DA.SetData(0, robot);
            DA.SetDataList(1, startPose);
        }
    }
}