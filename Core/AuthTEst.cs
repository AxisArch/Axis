using System;
using System.Collections.Generic;
using System.Diagnostics;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Auth0.OidcClient;

namespace Axis.Core
{
    public class AuthTest : GH_Component
    {
        public List<string> log = new List<string>();
        public bool loggedIn = false;

        public AuthTest() : base("Login", "Login", "Log in to Axis", "Axis", "Core")
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

            // Make sure we are logged in to Spacemaker
            if (!loggedIn && run)
            {
                Login();
                if (loggedIn) this.Message = "Logged In";
                else this.Message = "Error";
            }

            DA.SetDataList(0, log);

            /*
            // If we have data and and ID, send the data and update the UI
            Debug.WriteLine("Sending messages...");
            Debug.WriteLine("Token" + Properties.Settings.Default.Token);
            string msg = SpacemakerServices.SendSmif(messages, projectID, Properties.Settings.Default.Token);
            Debug.WriteLine(msg);

            if (msg == "OK")
                this.Message = "Live";
            else
            {
                this.Message = "Error";
                log.Add("[" + DateTime.Now.TimeOfDay.ToString().Split('.')[0] + "] " + msg);
            }

            if (m_outputLog)
                DA.SetDataList(0, log);
            */
        }

        public void Login()
        {
            Auth0ClientOptions clientOptions = new Auth0ClientOptions
            {
                Domain = "axisarch.eu.auth0.com",
                ClientId = "bDiJKd5tM8eqHsTX01ovqyFvOSBnC4mE",
                Browser = new WebBrowserBrowser("Authenticating...", 400, 400)
            };

            var client = new Auth0Client(clientOptions);
            clientOptions.PostLogoutRedirectUri = clientOptions.RedirectUri;
            /*
            var extra = new Dictionary<string, string>()
            {
                {"response_type", "code"}
            };
            */

            //client.LoginAsync(extra).ContinueWith(t =>
            client.LoginAsync();
            /*
            client.LoginAsync().ContinueWith(t =>
            {
                if (!t.Result.IsError)
                {
                    Properties.Settings.Default.Token = t.Result.AccessToken;
                    Debug.WriteLine("Logged in with token... " + t.Result.AccessToken);
                    log.Clear();
                    log.Add("[" + DateTime.Now.TimeOfDay.ToString().Split('.')[0] + "] ");
                    log.Add("Logged in to Axis.");
                    loggedIn = true;
                }
                t.Dispose();
                //else
                //{
                //    Debug.WriteLine("Error logging in: " + t.Result.Error);
                //    log.Add("Error logging in: " + t.Result.Error);
                //    loggedIn = false;
                //}
                
            });
            */
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