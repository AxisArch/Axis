﻿using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace Axis.GH_Components
{
    /// <summary>
    /// Define a RAPID-formatted procedure.
    /// </summary>
    public class DefineProcedure_Obsolete : GH_Component
    {
        public DefineProcedure_Obsolete() : base("Define Procedure", "Proc", "Creates a custom RAPID procedure", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name of the RAPID procedure.", GH_ParamAccess.item);
            pManager.AddTextParameter("Variable", "Variable", "(Optional) Name of variable.", GH_ParamAccess.item);
            pManager.AddTextParameter("Commands", "Commands", "A list of RAPID commands as strings.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Custom RAPID-formatted procedure.", GH_ParamAccess.list);
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string procName = null;
            string procVariable = null;
            List<string> strCommands = new List<string>();
            List<string> strProc = new List<string>();

            if (!DA.GetData(0, ref procName)) return;
            DA.GetData(1, ref procVariable);
            if (!DA.GetDataList(2, strCommands)) return;

            // Open procedure and build up list of commands.
            strProc.Add("!");
            strProc.Add("PROC" + " " + procName + "(" + procVariable + ")\n");

            strProc.AddRange(strCommands);

            // Close procedure.
            strProc.Add("ENDPROC");
            strProc.Add("!");

            DA.SetDataList(0, strProc);
        }

        #region Component Settings

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.RAPID;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{1a8abd44-57cf-4edd-a943-fc1b4efe166f}"); }
        }

        #endregion Component Settings
    }
}