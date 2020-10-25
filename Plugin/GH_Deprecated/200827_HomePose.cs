using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;

namespace Axis.Targets
{
    public class HomePose_Obsolete : GH_Component
    {
        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Target;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{a85de75e-48fe-4bd5-93da-0d0e11998c74}"); }
        }

        // Kuka toggle holder
        bool manufacturer = false;

        public HomePose_Obsolete() : base("Home Pose", "Home", "Create an instruction to go to the home pose.", "Axis", "3. Targets")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Speed", "Speed", "Desired robot speed [mm/s].", GH_ParamAccess.item, 300);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Pose", "Pose", "Home pose as joint move / position.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double speed = 300;

            if (!DA.GetData(0, ref speed)) return;

            string homePose = null;

            if (manufacturer)
            {
                homePose = String.Join(
                    Environment.NewLine,
                    "! Home Position",
                    "HOME = {AXIS: A1 -90, A2 -90, A3 90, A4 -90, A5 30.0, A6 0}");
            }
            else
            {
                string strSpeed = "v" + speed.ToString();
                homePose = String.Join(
                    Environment.NewLine,
                    "! Home Position",
                    "MoveAbsJ" + "[[0, 0, 0, 0, 30, 0], eAxis]" + "," + strSpeed + "," + "fine" + "," + "tool0" + ";");
            }

            DA.SetData(0, homePose);
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem kukaTarget = Menu_AppendItem(menu, "Create KUKA pose target.", kuka_Click, true, manufacturer);
            kukaTarget.ToolTipText = "Create robot pose formatted in KRL for KUKA robots.";
        }

        private void kuka_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("KukaTarget");
            manufacturer = !manufacturer;
            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("Kuka", this.manufacturer);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.manufacturer = reader.GetBoolean("Kuka");
            return base.Read(reader);
        }
    }
}