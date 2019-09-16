using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using static Axis.Properties.Settings;

namespace Axis.Core
{
    public class AuthTest : GH_Component
    {
        public bool loggedIn = Axis.Properties.Settings.Default.LoggedIn;

        public AuthTest() : base("Test", "Test", "Test", "Axis", "1. Core")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Log", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> log = new List<string>();

            if (Default.LoggedIn)
            {
                log.Add("OK");
                log.Add(Default.LastLoggedIn.ToString());
            }

            if (loggedIn) this.Message = "Logged In";
            else this.Message = "Error";


            DA.SetDataList(0, log);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("10f5cef1-1906-422e-bb81-9f5304fb7902"); }
        }
    }
}