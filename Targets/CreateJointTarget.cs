using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using Axis.Core;

namespace Axis.Targets
{
    public class CreateJointTarget : GH_Component, IGH_VariableParameterComponent
    {
        bool manufacturer = false;
        bool useRotary = false;
        bool useLinear = false;

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Target;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("8854333d-79f7-47e0-9b80-03966486b42b"); }
        }

        public CreateJointTarget() : base("Joint Target", "Joint", "Compose an absolute joint target from a list of axis values.", "Axis", "3. Targets")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("A1", "A1", "Axis value for axis one.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("A2", "A2", "Axis value for axis two.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("A3", "A3", "Axis value for axis three.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("A4", "A4", "Axis value for axis four.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("A5", "A5", "Axis value for axis five.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("A6", "A6", "Axis value for axis six.", GH_ParamAccess.item, 0);
            pManager.AddGenericParameter("Tool", "Tool", "Tool to use for operation.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Speed", "Speed", "Speed to use for the movement.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Zone", "Zone", "Zone to use for the movement.", GH_ParamAccess.item);

            for (int i = 0; i < 9; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Target", "Target", "Robot joint target.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double a1 = 0;
            double a2 = 0;
            double a3 = 0;
            double a4 = 0;
            double a5 = 0;
            double a6 = 0;
            double rot = 0;
            double lin = 0;
            Tool tool = Tool.Default;
            GH_ObjectWrapper speedIn = new GH_ObjectWrapper();
            GH_ObjectWrapper zoneIn = new GH_ObjectWrapper();

            bool hasTool = true;
            bool hasSpeed = true;
            bool hasZone = true;

            if (!DA.GetData(0, ref a1)) a1 = 0;
            if (!DA.GetData(1, ref a2)) a2 = 0;
            if (!DA.GetData(2, ref a3)) a3 = 0;
            if (!DA.GetData(3, ref a4)) a4 = 0;
            if (!DA.GetData(4, ref a5)) a5 = 0;
            if (!DA.GetData(5, ref a6)) a6 = 0;
            if (!DA.GetData(6, ref tool)) hasTool = false;
            if (!DA.GetData(7, ref speedIn)) hasSpeed = false;
            if (!DA.GetData(8, ref zoneIn)) hasZone = false;

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

            List<double> axisVals = new List<double>();
            axisVals.Add(Math.Round(a1, 4));
            axisVals.Add(Math.Round(a2, 4));
            axisVals.Add(Math.Round(a3, 4));
            axisVals.Add(Math.Round(a4, 4));
            axisVals.Add(Math.Round(a5, 4));
            axisVals.Add(Math.Round(a6, 4));

            Target jointTarget = new Target(axisVals, speed, zone, tool, rot, lin, manufacturer);

            DA.SetData(0, jointTarget);
        }

        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[]
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
                AddInput(0);
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
                AddInput(1);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Linear"), true);
            }
            ExpireSolution(true);
        }

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

        // Register the new input parameters to our component.
        private void AddInput(int index)
        {
            IGH_Param parameter = inputParams[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(inputParams, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
}
