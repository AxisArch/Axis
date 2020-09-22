using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Define robot end effector joint target positions.
    /// </summary>
    public class GH_CreateJointTarget_Obsolete : GH_Component, IGH_VariableParameterComponent
    {
        private bool manufacturer = false;
        private bool useRotary = false;
        private bool useLinear = false;

        public GH_CreateJointTarget_Obsolete() : base("Joint Target", "Joint", "Compose an absolute joint target from a list of axis values.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Angles", "Angles", "Axis values for each axis as a list.", GH_ParamAccess.list, new List<double> { 0, 0, 0, 0, 0, 0 });
            IGH_Param tool = new Axis.GH_Params.ToolParam();
            pManager.AddParameter(tool, "Tool", "Tool", "Tool to use for operation.", GH_ParamAccess.item);
            IGH_Param speed = new Axis.GH_Params.SpeedParam();
            pManager.AddParameter(speed, "Speed", "Speed", "Speed to use for the movement.", GH_ParamAccess.item);
            IGH_Param zone = new Axis.GH_Params.ZoneParam();
            pManager.AddParameter(zone, "Zone", "Zone", "Zone to use for the movement.", GH_ParamAccess.item);
            for (int i = 0; i < 4; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param target = new Axis.GH_Params.TargetParam();
            pManager.AddParameter(target, "Target", "Target", "Robot joint target.", GH_ParamAccess.item);
        }

        #endregion IO

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> angles = new List<double>();
            double lin = 0;
            double rot = 0;
            Tool tool = Tool.Default;
            GH_ObjectWrapper speedIn = new GH_ObjectWrapper();
            GH_ObjectWrapper zoneIn = new GH_ObjectWrapper();

            bool hasTool = true;
            bool hasSpeed = true;
            bool hasZone = true;

            if (!DA.GetDataList(0, angles)) angles = new List<double> { 0, 0, 0, 0, 0, 0 };
            if (!DA.GetData(1, ref tool)) hasTool = false;
            if (!DA.GetData(2, ref speedIn)) hasSpeed = false;
            if (!DA.GetData(3, ref zoneIn)) hasZone = false;

            Speed speed = Speed.Default;
            Zone zone = Zone.Default;

            // Check to see if we have speeds, and if they are custom speed objects, otherwise use values.
            if (hasSpeed)
            {
                // Default speed dictionary.
                Dictionary<double, Speed> defaultSpeeds = Util.ABBSpeeds();
                double speedVal = 0;

                GH_ObjectWrapper speedObj = speedIn;
                Type cType = speedObj.Value.GetType();
                GH_Convert.ToDouble_Secondary(speedObj.Value, ref speedVal);

                if (cType.Name == "Speed")
                    speed = speedObj.Value as Speed;
                else
                {
                    if (!defaultSpeeds.ContainsKey(speedVal))
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Supplied speed value is non-standard. Please supply a default value (check the Axis Wiki - Controlling Speed for more info) or create a custom speed using the Speed component.");
                    else
                        speed = defaultSpeeds[speedVal];
                }
            }
            // If we don't have any speed values, use the default speed.
            else
                speed = Speed.Default;

            // Check to see if we have zones, and if they are custom zones objects, otherwise use values.
            if (hasZone)
            {
                // Default zone dictionary.
                Dictionary<double, Zone> defaultZones = Util.ABBZones();
                double zoneVal = 0;

                GH_ObjectWrapper zoneObj = zoneIn;
                Type cType = zoneObj.Value.GetType();
                GH_Convert.ToDouble_Secondary(zoneObj.Value, ref zoneVal);

                if (cType.Name == "Zone")
                    zone = zoneObj.Value as Zone;
                else
                {
                    if (!defaultZones.ContainsKey(zoneVal))
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Supplied zone value is non-standard. Please supply a default value (check the Axis Wiki - Controlling Zone for more info) or create a custom zone using the Zoe component.");
                    else
                        zone = defaultZones[zoneVal];
                }
            }
            // If we don't have any zone values, use the default zone.
            else
                zone = Zone.Default;

            if (useRotary) { if (!DA.GetData("Rotary", ref rot)) return; }
            if (useLinear) { if (!DA.GetData("Linear", ref lin)) return; }

            //Poor mans temporary fix
            var rType = Manufacturer.ABB;
            if (manufacturer)
            {
                rType = Manufacturer.Kuka;
            }

            Target jointTarget = new Target(angles, speed, zone, tool, rot, lin, rType);

            DA.SetData(0, jointTarget);
        }

        #region UI

        // Build a list of optional input parameters
        private IGH_Param[] inputParams = new IGH_Param[]
        {
        new Param_Number() { Name = "Rotary", NickName = "Rotary", Description = "An external rotary axis position in degrees.", Access = GH_ParamAccess.item },
        new Param_Number() { Name = "Linear", NickName = "Linear", Description = "An external linear axis position in degrees.", Access = GH_ParamAccess.item },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem kukaOption = Menu_AppendItem(menu, "KUKA Tool", KUKA_Click, true, manufacturer);
            kukaOption.ToolTipText = "Create a KUKA-formatted joint command.";

            Menu_AppendSeparator(menu);

            ToolStripMenuItem mRotary = Menu_AppendItem(menu, "Use Rotary Axis", rot_Click, true, useRotary);
            mRotary.ToolTipText = "Add an input for external rotary axis position values.";

            ToolStripMenuItem mLinear = Menu_AppendItem(menu, "Use Linear Axis", lin_Click, true, useLinear);
            mLinear.ToolTipText = "Add an input for external linear axis position values.";
        }

        private void KUKA_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("KukaJointMovement");
            manufacturer = !manufacturer;
        }

        private void rot_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("RotaryClick");
            useRotary = !useRotary;

            if (useRotary)
            {
                this.AddInput(0, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Rotary"), true);
            }
            ExpireSolution(true);
        }

        private void lin_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("LinearClick");
            useLinear = !useLinear;

            if (useLinear)
            {
                this.AddInput(1, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Linear"), true);
            }
            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("KukaJointMovement", this.manufacturer);
            writer.SetBoolean("JointRotary", this.useRotary);
            writer.SetBoolean("JointLinear", this.useLinear);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.manufacturer = reader.GetBoolean("KukaJointMovement");
            this.useRotary = reader.GetBoolean("JointRotary");
            this.useLinear = reader.GetBoolean("JointLinear");
            return base.Read(reader);
        }

        #endregion Serialization

        #region Component Settings

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.Target;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8854333d-79f7-47e0-9b80-03966486b42b"); }
        }

        #endregion Component Settings
    }
}