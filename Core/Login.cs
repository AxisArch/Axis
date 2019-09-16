using System;
using System.Collections.Generic;
using System.Diagnostics;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Auth0.OidcClient;

namespace Axis.Core
{
    public class Login : GH_Component
    {
        public List<string> log = new List<string>();
        public bool loggedIn = false;

        public Login() : base("Login", "Login", "Log in to Axis", "Axis", "Core")
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

            // Set up our client to handle the login.
            Auth0ClientOptions clientOptions = new Auth0ClientOptions
            {
                Domain = "axisarch.eu.auth0.com",
                ClientId = "bDiJKd5tM8eqHsTX01ovqyFvOSBnC4mE",
                Browser = new WebBrowserBrowser("Authenticating...", 400, 600)
            };

            // Initiate the client
            var client = new Auth0Client(clientOptions);
            clientOptions.PostLogoutRedirectUri = clientOptions.RedirectUri;

            var extra = new Dictionary<string, string>()
            {
                {"response_type", "code"}
            };

            // *********
            // Run 'Get License Information'- send CPU ID etc. as a attrib of the token that can later be checked?

            // Handle the logout.
            if (loggedIn && !run)
            {
                client.LogoutAsync();
                loggedIn = false;
                this.Message = "Logged Out";
            }

            // Handle the login.
            if (!loggedIn && run)
            {
                // Async login as a task, can grab a lot of details from t.[] if needed. (User, permissions etc.).
                // Thinking that this can be used to limit functionality based on pricing.
                client.LoginAsync(extra).ContinueWith(t =>
                {
                    if (!t.Result.IsError)
                    {
                        Properties.Settings.Default.Token = t.Result.AccessToken;
                        Debug.WriteLine("Logged in with token... " + t.Result.AccessToken);
                        log.Clear();
                        log.Add("[" + DateTime.Now.TimeOfDay.ToString().Split('.')[0] + "] ");
                        log.Add("Logged in to Axis.");
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

                if (loggedIn) this.Message = "Logged In";
                else this.Message = "Error";
            }
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
            get { return new Guid("54b2cc2c-688d-4972-a234-2c9976d0a9f8"); }
        }
    }
}