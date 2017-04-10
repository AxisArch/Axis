using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Axis.Core;

namespace Axis.Robot
{
    // Define a custom tool.
    public class CreateTool : GH_Component
    {
        public CreateTool() : base("Tool", "Tool", "Define a custom robot tool.", "Axis", "Robot")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconRobot;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{ae134b08-ee29-444e-b689-a218ff73379d}"); }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Tool name.", GH_ParamAccess.item, "AxisTool");
            pManager.AddPlaneParameter("TCP", "TCP", "Tool Centre Point plane, at end of tool.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Weight", "Weight", "Tool weight in [kg], as double.", GH_ParamAccess.item, 2.5);
            pManager.AddMeshParameter("Mesh", "Mesh", "Tool mesh geometry for kinematic preview.", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tool", "Tool", "Custom tool data type.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string inName = "AxisTool";
            Plane inTCP = Plane.WorldXY;
            double inWeight = 2.5;
            List<Mesh> inMesh = new List<Mesh>();

            if (!DA.GetData(0, ref inName)) return;
            if (!DA.GetData(1, ref inTCP)) return;
            if (!DA.GetData(2, ref inWeight)) return;
            if (!DA.GetDataList(3, inMesh)) return;

            Tool tool = new Tool(inName, inTCP, inWeight, inMesh);

            DA.SetData(0, tool);
        }
    }
}