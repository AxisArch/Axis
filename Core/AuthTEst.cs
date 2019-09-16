﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using static Axis.Properties.Settings;

namespace Axis.Core
{
    public class AuthTest : GH_Component
    {
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
                log.Add(Default.LastLogin.ToString());
            }
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

            Properties.Settings.Default.Acsess = loggedIn;

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