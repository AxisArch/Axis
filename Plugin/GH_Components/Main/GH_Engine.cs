using Auth0.OidcClient;
using Axis.Kernal;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using static Axis.Properties.Settings;

namespace Axis.GH_Components
{
    /// <summary>
    /// Core engine class that handles
    /// login, system settings etc.
    /// </summary>
    public class GH_Engine : Axis_Component
    {

        // Set up our client to handle the login.
        private Auth0ClientOptions clientOptions = new Auth0ClientOptions
        {
            Domain = "axisarch.eu.auth0.com",
            ClientId = System.Environment.GetEnvironmentVariable("AUTHID"),
            Browser = new WebBrowserBrowser("Authenticating...", 400, 640)
        };

        public GH_Engine() : base("Axis", "Axis", "Manage the Axis application.", AxisInfo.Plugin, AxisInfo.TabMain)
        {
            var attr = this.Attributes as AxisComponentAttributes;

            this.UI_Elements = new IComponentUiElement[]
            {
                new Kernal.UIElements.ComponentButton("Login"){ LeftClickAction = Login }, //Start
                //new Kernal.UIElements.ComponentButton("Settings"){ LeftClickAction = ShowSettings },
            };

            attr.Update(UI_Elements);

            RegularToolStripItems = new ToolStripItem[]
            {
                new ToolStripMenuItem("Force Logout", null, logout_Click)
            {
                ToolTipText = "Forceably logout of the Axis domain."
            },
                new ToolStripMenuItem("Clear Token", null, clear_Click)
            {
                ToolTipText = "Clear authentification token from the PC."
            },
            };
        }
        protected override void BeforeSolveInstance()
        {
            // Initiate the client
            client = new Auth0Client(clientOptions);
            clientOptions.PostLogoutRedirectUri = clientOptions.RedirectUri;
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetDataList(0, log);
        }
        public void ClearToken()
        {
            Default.LastLoggedIn = new DateTime(2000, 1, 1);
            Default.LoggedIn = false;
            Default.Token = String.Empty;
        }

        #region Variables
        public List<string> log = new List<string>();
        public Auth0Client client = null;
        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Log", GH_ParamAccess.list);
        }

        #endregion IO

        #region UI


        private void logout_Click(object sender, EventArgs e)
        {

            RecordUndoEvent("Logout");

            client.LogoutAsync();

            //logged_In = false;
            Axis.Properties.Settings.Default.LoggedIn = false;

            Auth.RaiseEvent(false);

            this.Message = "Logged Out";
            log.Add("Logged out of Axis at " + System.DateTime.Now.ToShortDateString());

            // Porbmen seems to be that this does not expire the specific components
            this.ExpireSolution(true);
        }

        private void clear_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Clear");
            log.Add("Clearing token at " + System.DateTime.Now.ToShortDateString());
            ClearToken();
            ExpireSolution(true);
        }

        #endregion UI

        /// <summary>
        /// Handle authentification and login in
        /// response to a component UI click.
        /// </summary>
        protected void Login(object sender, object e)
        {
            // If it's not valid, offer a login.
            if (!Auth.AuthCheck())
            {
                Dictionary<string, string> extra = new Dictionary<string, string>()
                {
                    {"response_type", "code"}
                };

                ClearToken();
                client.LoginAsync(extra).ContinueWith(t =>
                {
                    if (!t.Result.IsError)
                    {
                        Default.Token = t.Result.AccessToken;
                        log.Clear();
                        log.Add("Logged in to Axis at " + DateTime.Now.ToShortTimeString());
                        DateTime validTo = DateTime.Now.AddDays(2);
                        log.Add("Login valid to: " + validTo.ToLongDateString() + ", " + validTo.ToShortTimeString());
                        Message = "OK";
                        Axis.Properties.Settings.Default.LoggedIn = true;
                        Axis.Auth.RaiseEvent(true);

                        var doc = OnPingDocument();
                        if (doc != null) doc.ScheduleSolution(10, LocalExpire);

                        void LocalExpire(GH_Document document) => ExpireSolution(false);
                    }
                    else
                    {
                        Debug.WriteLine("Error logging in: " + t.Result.Error);
                        log.Add(t.Result.ToString());
                        log.Add("Error logging in: " + t.Result.Error);
                        Axis.Properties.Settings.Default.LoggedIn = false;
                        Axis.Auth.RaiseEvent(false);

                        var doc = OnPingDocument();
                        if (doc != null) doc.ScheduleSolution(10, LocalExpire);

                        void LocalExpire(GH_Document document) => ExpireSolution(false);
                    }
                    t.Dispose();
                });

                // Update our login time.
                Default.LastLoggedIn = DateTime.Now;
            }
            else
            {
                log.Add("Already logged in.");
                ExpireSolution(true);
            }

            if (Axis.Properties.Settings.Default.LoggedIn) Message = "Logged In";
            else Message = "Error";
        }

        /// <summary>
        /// Show a general settings dialog.
        /// </summary>
        private static void ShowSettings(object sender, object e)
        {
            // Options dialog.
            Eto.Forms.Form dialog = new Eto.Forms.Form();
            dialog.Size = new Eto.Drawing.Size(300, 300);

            StackLayout buttonStack = new StackLayout();

            Eto.Forms.Button b0 = new Eto.Forms.Button();
            buttonStack.Items.Add(b0);

            // Set the main content and options.
            dialog.Content = buttonStack;

            dialog.BackgroundColor = Colors.SlateGray;
            dialog.Maximizable = false;
            dialog.Minimizable = false;
            dialog.Topmost = true;

            dialog.Show();
        }

        #region Component Settings

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.LogIn;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("54b2cc2c-688d-4972-a234-2c9976d0a9f8"); }
        }

        #endregion Component Settings
    }
}