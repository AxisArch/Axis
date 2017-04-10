using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Axis.Tools;
using Axis.Core;
using Axis.Targets;

namespace Axis
{
    public class RobTarget : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconHome;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{c8ae5262-f447-4807-b1ff-10b29b37c984}"); }
        }

        public RobTarget() : base("Target", "Target", "Create custom robot targets.", "Axis", "Targets")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Target TCP location as plane.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Method", "Method", "Movement method. [0 = Linear, 1 = Joint]", GH_ParamAccess.list, 0);
            pManager.AddIntegerParameter("Speed", "Speed", "Speed of robot movement per target, in mm/sec.", GH_ParamAccess.list, 50);
            pManager.AddIntegerParameter("Zone", "Zone", "Approximation zone per target, in mm.", GH_ParamAccess.list, 5);
            pManager.AddTextParameter("Tool", "Tool", "Tool to use for operation.", GH_ParamAccess.item, "tool0");
            pManager.AddTextParameter("Wobj", "Wobj", "Wobj to use for operation.", GH_ParamAccess.item, "wobj0");
            pManager.AddIntegerParameter("Type", "Type", "Robot brand to use for target creation. [0 = ABB, 1 = KUKA]", GH_ParamAccess.item, 0);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Target", "Target", "Robot target.", GH_ParamAccess.list);
            pManager.AddTextParameter("Code", "Code", "Code representation of target.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Plane> planes = new List<Plane>();
            List<int> methods = new List<int>();
            List<int> speeds = new List<int>();
            List<int> zones = new List<int>();
            string tool = "tool0";
            string wobj = "wobj0";
            int type = 0;

            if (!DA.GetDataList(0, planes)) return;
            if (!DA.GetDataList(1, methods)) return;
            if (!DA.GetDataList(2, speeds)) return;
            if (!DA.GetDataList(3, zones)) return;
            if (!DA.GetData(4, ref tool)) return;
            if (!DA.GetData(5, ref wobj)) return;
            if (!DA.GetData(6, ref type)) return;

            //List<Target> targets = new List<Target>();
            List<string> code = new List<string>();
            int method = 0;
            int speed = 50;
            int zone = 5;

            for (int i = 0; i < planes.Count; i++)
            {
                // Method
                if (i < methods.Count)
                {
                    method = methods[i];
                }
                else
                {
                    method = methods[methods.Count - 1];
                }

                // Speed
                if (i < speeds.Count)
                {
                    speed = speeds[i];
                }
                else
                {
                    speed = speeds[speeds.Count - 1];
                }

                // Zone
                if (i < zones.Count)
                {
                    zone = zones[i];
                }
                else
                {
                    zone = zones[zones.Count - 1];
                }
                
                Target robTarg = new Target(planes[i], method, speed, zone, tool, wobj, type);
                if (type == 0)
                {
                    code.Add(robTarg.StrABB);
                }
                else
                {
                    code.Add(robTarg.StrKUKA);
                }                
            }

            //DA.SetDataList(0, targets);
            DA.SetDataList(0, code);
        }
    }
}