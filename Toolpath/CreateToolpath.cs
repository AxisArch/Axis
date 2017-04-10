using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Axis.Targets;
using Axis.Tools;

namespace Axis.Core
{
    public class CreateToolpath : GH_Component
    {
        public CreateToolpath() : base("Toolpath", "Toolpath", "Create a toolpath from targets as well as speed, zone, tool and workobject data..", "Axis", "Toolpath")
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.iconHome;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{f28e1be6-8694-4049-a161-432aa30b291b}"); }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Commands", "Commands", "A collection of robot commands to create the toolpath from.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Toolpath", "Toolpath", "Axis robot toolpath.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> commands = new List<object>();

            if (!DA.GetDataList(0, commands)) return;

            List<string> commandsList = new List<string>();
            
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i] is GH_String)
                {
                    commandsList.Add(commands[i].ToString());
                }
                else
                {
                    Target robTarg = commands[i] as Target;
                    commandsList.Add(robTarg.StrABB);
                }
            }
            
            DA.SetDataList(0, commandsList);
        }
    }
}