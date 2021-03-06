﻿using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace Axis.GH_Components
{
    /// <summary>
    /// Get license information for creating
    /// unique user hashes from the PC identifiers.
    /// </summary>
    public class GetLicenseInformation_Obsolete : GH_Component
    {
        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Icons.License;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("c1582c41-3815-4c7b-8965-3272b08450b7"); }
        }

        public GetLicenseInformation_Obsolete() : base("Get License Info", "License", "Generate the information necessary to create a license.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Full Name", "Full Name", "First name followed by surname, capitalized.", GH_ParamAccess.item, "No Name");
            pManager.AddBooleanParameter("Send Mail", "Send Mail", "Boolean toggle to send license information.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Info", "Info", "License information.", GH_ParamAccess.item);
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "No Name";
            bool send = false;

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref send)) return;

            List<string> info = new List<string>();

            info.Add("Full Name: " + name);
            info.Add("CPU: " + GetHardwareId("Win32_Processor", "processorID"));
            info.Add("Serial No: " + GetHardwareId("Win32_BIOS", "SerialNumber"));
            info.Add("Generated: " + System.DateTime.Now.ToString());

            if (send)
            {
                string infoMultiline = string.Join(", ", info.ToArray());
                string subject = "License Request - " + name;
                SendSupportEmail("rhu@axisarch.tech", subject, infoMultiline);
            }

            DA.SetDataList(0, info);
        }

        /// <summary>
        /// Get the requested ID from the key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public string GetHardwareId(string key, string propertyValue)
        {
            var value = string.Empty;
            var searcher = new ManagementObjectSearcher("select * from " + key);

            foreach (ManagementObject share in searcher.Get())
            {
                value = (string)share.GetPropertyValue(propertyValue);
            }

            return value;
        }

        /// <summary>
        /// Automate the sending of the necessary
        /// licensing request.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public void SendSupportEmail(string emailAddress, string subject, string body)
        {
            Process.Start("mailto:" + emailAddress + "?subject=" + subject + "&body="
                         + body);
        }
    }
}