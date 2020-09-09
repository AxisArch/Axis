using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.IO
{
    /// <summary>
    /// Set the value of a digital output.
    /// </summary>
    public class SetDO_Obsolete : GH_Component, IGH_VariableParameterComponent
    {
        // Context menu data items
        bool m_Sync = false;

        public SetDO_Obsolete() : base("Set Digital Output", "Set DO", "Set the value of a digital output.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Output", "Name of the digital output to set.", GH_ParamAccess.item, "DO10_1");
            pManager.AddIntegerParameter("Status", "Status", "Status of the signal to set. (1 = On, 0 = Off)", GH_ParamAccess.item, 0);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "Command", "Rapid formatted SetDO command for signal control.", GH_ParamAccess.item);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "DO10_1";
            int status = 0;

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref status)) return;

            string strCommand = String.Empty;

            if (m_Sync)
            {
                strCommand = @"SetDO \Sync, " + name + ", " + status.ToString() + ";";
            }
            else
            {
                strCommand = "SetDO " + name + ", " + status.ToString() + ";";
            }

            DA.SetData(0, strCommand);
        }

        #region UI
        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem sync = Menu_AppendItem(menu, "Synchronization", Sync_Click, true, m_Sync);
            sync.ToolTipText = "If this argument is used then the program execution will wait until the signal is physically set to the specified value.";
        }

        private void Sync_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Sync");
            m_Sync = !m_Sync;
            ExpireSolution(true);
        }
        #endregion

        #region Serialization
        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("SyncDO", this.m_Sync);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_Sync = reader.GetBoolean("SyncDO");
            return base.Read(reader);
        }
        #endregion

        #region Component Settings
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override bool Obsolete => true;

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.DigitalOut;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{2230b30e-6aeb-4f55-a54a-b1ec7d8eb410}"); }
        }
        #endregion
    }
}