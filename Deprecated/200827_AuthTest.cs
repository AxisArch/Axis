using System;
using System.Reflection;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using static Axis.Properties.Settings;

namespace Axis.Core
{
    /// <summary>
    /// Test class to verify the auth pipeline.
    /// </summary>
    public class AuthTest : GH_Component
    {
        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public AuthTest() : base("Auth Test", "Auth", "Test", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Log", GH_ParamAccess.list);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> log = new List<string>();
            bool loggedIn = Default.LoggedIn;
            System.DateTime lastLogin = Default.LastLoggedIn;

            log.Add("Axis Version Number: " + Assembly.GetExecutingAssembly().GetName().Version);

            // Check the validity of the login token. (Refactored method)
            DateTime t0 = DateTime.Now;
            Auth auth = new Auth();
            bool isValid = auth.IsValid;
            if (isValid)
            {
                log.Add("Valid token.");
            }
            log.Add(DateTime.Now.Subtract(t0).TotalMilliseconds.ToString());

            if (Default.LoggedIn)
            {
                log.Add("Logged in.");
                log.Add("LLI: " + lastLogin.ToLongDateString() + ", " + lastLogin.ToShortTimeString());
                DateTime validTo = lastLogin.AddDays(2);
                int valid = DateTime.Compare(System.DateTime.Now, validTo);
                if (valid < 0)
                {
                    log.Add("Login token valid.");
                    log.Add("Valid to: " + validTo.ToLongDateString() + ", " + validTo.ToShortTimeString());
                }
                Default.ValidTo = validTo;
            }

            if (loggedIn) this.Message = "Logged In";
            else this.Message = "Error";

            DA.SetDataList(0, log);
        }

        #region Component Settings
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
        #endregion
    }
}