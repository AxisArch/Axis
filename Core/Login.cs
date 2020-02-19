using System;
using System.Collections.Generic;
using System.Diagnostics;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Auth0.OidcClient;
using static Axis.Properties.Settings;
using System.Windows.Forms;

namespace Axis.Core
{
    public class Login : GH_Component
    {
        // Sticky variables for the options.
        bool m_Logout = false;
        public List<string> log = new List<string>();
        public bool loggedIn = false;
        public bool forceLogout = false;
        public bool clear = false;

        public Login() : base("Login", "Login", "Log in to Axis", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run", "Open a browser window and log in to Axis", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Log", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(0, ref run)) return;

            if (clear)
            {
                ClearToken();
                log.Add("Cleared token at " + System.DateTime.Now.ToShortDateString());
            }

            // Set up our client to handle the login.
            Auth0ClientOptions clientOptions = new Auth0ClientOptions
            {
                Domain = "axisarch.eu.auth0.com",
                ClientId = "bDiJKd5tM8eqHsTX01ovqyFvOSBnC4mE",
                Browser = new WebBrowserBrowser("Authenticating...", 400, 640)
            };

            // Initiate the client
            var client = new Auth0Client(clientOptions);
            clientOptions.PostLogoutRedirectUri = clientOptions.RedirectUri;

            var extra = new Dictionary<string, string>()
            {
                {"response_type", "code"}
            };

            // Handle the logout.
            if ((loggedIn && !run) || forceLogout)
            {
                client.LogoutAsync();
                loggedIn = false;
                this.Message = "Logged Out";
                log.Add("Logged out of Axis at " + System.DateTime.Now.ToShortDateString()); 
            }

            // Handle the login.
            if (!loggedIn && run)
            {
                client.LoginAsync(extra).ContinueWith(t =>
                {
                    if (!t.Result.IsError)
                    {
                        Default.Token = t.Result.AccessToken;
                        log.Clear();
                        log.Add("Logged in to Axis at " + DateTime.Now.ToShortTimeString());
                        DateTime validTo = DateTime.Now.AddDays(2);
                        log.Add("Login valid to: " + validTo.ToLongDateString() + ", " + validTo.ToShortTimeString());
                        this.Message = "OK";
                        loggedIn = true;
                    }
                    else
                    {
                        Debug.WriteLine("Error logging in: " + t.Result.Error);
                        log.Add(t.Result.ToString());
                        log.Add("Error logging in: " + t.Result.Error);
                        loggedIn = false;
                    }
                    t.Dispose();
                });

                // Update our login time.
                Default.LastLoggedIn = DateTime.Now;

                if (loggedIn) this.Message = "Logged In";
                else this.Message = "Error";
            }

            forceLogout = false;
            Axis.Properties.Settings.Default.LoggedIn = loggedIn;
            DA.SetDataList(0, log);
        }

        public void ClearToken()
        {
            Default.LastLoggedIn = new DateTime(2000, 1, 1);
            Default.LoggedIn = false;
            Default.Token = null;
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem fLogout = Menu_AppendItem(menu, "Force Logout", logout_Click);
            fLogout.ToolTipText = "Forceably logout of the Axis domain.";
            ToolStripMenuItem clear = Menu_AppendItem(menu, "Clear Token", clear_Click);
            clear.ToolTipText = "Clear authentification token from the PC.";
        }

        private void logout_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Logout");
            forceLogout = true;
            ExpireSolution(true);
        }

        private void clear_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Clear");
            clear = true;
            ExpireSolution(true);
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
            get { return new Guid("54b2cc2c-688d-4972-a234-2c9976d0a9f8"); }
        }
    }
}