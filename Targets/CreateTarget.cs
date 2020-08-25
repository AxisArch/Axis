using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Axis.Core;
using Axis.Targets;

namespace Axis
{
    public class CreateTarget : GH_Component, IGH_VariableParameterComponent
    {
        // Boolean toggle for context menu items.
        bool m_outputCode = false;
        bool m_interpolationTypes = false;
        bool m_outputTarget = false;

        Manufacturer m_Manufacturer = Manufacturer.ABB;
        List<Target> m_targets = new List<Target>();
        BoundingBox m_bBox = new BoundingBox();

        // External axis presence.
        bool extRotary = false;
        bool extLinear = false;
        
        public CreateTarget() : base("Plane Target", "Target", "Create custom robot targets from planes.", AxisInfo.Plugin, AxisInfo.TabTargets)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Target TCP location as plane.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Speed", "Speed", "List of speed objects per plane.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Zone", "Zone", "Approximation zone per target, in mm.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Tool", "Tool", "Tool to use for operation.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Wobj", "Wobj", "Wobj to use for operation.", GH_ParamAccess.list);

            for (int i = 0; i < 5; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Targets", "Targets", "Robot targets.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            this.Message = m_Manufacturer.ToString();

            List<Plane> planes = new List<Plane>();
            List<GH_ObjectWrapper> speedsIn = new List<GH_ObjectWrapper>();
            List<GH_ObjectWrapper> zonesIn = new List<GH_ObjectWrapper>();
            List<Tool> tools = new List<Tool>();
            List<CSystem> wobjs = new List<CSystem>();
            List<int> methods = new List<int>();

            // Store the input lists of external axis values to synchronise with the targets.
            List<double> eRotVals = new List<double>();
            List<double> eLinVals = new List<double>();

            bool hasSpeed = true;
            bool hasZone = true;

            if (!DA.GetDataList(0, planes)) return;
            if (!DA.GetDataList(1, speedsIn)) hasSpeed = false;
            if (!DA.GetDataList(2, zonesIn)) hasZone = false;
            if (!DA.GetDataList(3, tools)) tools.Add(Tool.Default);
            if (!DA.GetDataList(4, wobjs)) wobjs.Add(CSystem.Default);

            // If interpolation types are specified, get them.
            if (m_interpolationTypes) { if (!DA.GetDataList("*Method", methods)) return; }

            // If the inputs are present, get the external axis values.
            if (extRotary) { if (!DA.GetDataList("Rotary", eRotVals)) return; }
            if (extLinear) { if (!DA.GetDataList("Linear", eLinVals)) return; }

            List<Speed> speeds = new List<Speed>();
            List<Zone> zones = new List<Zone>();

            // Check to see if we have speeds, and if they are custom speed objects, otherwise use values.
            if (hasSpeed)
            {
                // Default speed dictionary.
                Dictionary<double, Speed> defaultSpeeds = Util.ABBSpeeds();
                double speedVal = 0;

                foreach (GH_ObjectWrapper speedIn in speedsIn)
                {
                    GH_ObjectWrapper speedObj = speedIn;
                    Type cType = speedObj.Value.GetType();
                    GH_Convert.ToDouble_Secondary(speedObj.Value, ref speedVal);

                    if (cType.Name == "Speed")
                        speeds.Add(speedObj.Value as Speed);
                    else
                    {
                        if (!defaultSpeeds.ContainsKey(speedVal))
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Supplied speed value is non-standard. Please supply a default value (check the Axis Wiki - Controlling Speed for more info) or create a custom speed using the Speed component.");
                        else
                            speeds.Add(defaultSpeeds[speedVal]);
                    }
                }
            }
            // If we don't have any speed values, use the default speed.
            else
                speeds.Add(Speed.Default);

            // Check to see if we have zones, and if they are custom zones objects, otherwise use values.
            if (hasZone)
            {
                // Default zone dictionary.
                Dictionary<double, Zone> defaultZones = Util.ABBZones();
                double zoneVal = 0;

                foreach (GH_ObjectWrapper zoneIn in zonesIn)
                {
                    GH_ObjectWrapper zoneObj = zoneIn;
                    Type cType = zoneObj.Value.GetType();
                    GH_Convert.ToDouble_Secondary(zoneObj.Value, ref zoneVal);

                    if (cType.Name == "Zone")
                        zones.Add(zoneObj.Value as Zone);
                    else
                    {
                        if (!defaultZones.ContainsKey(zoneVal))
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Supplied zone value is non-standard. Please supply a default value (check the Axis Wiki - Controlling Zone for more info) or create a custom zone using the Zoe component.");
                        else
                            zones.Add(defaultZones[zoneVal]);
                    }
                }
            }
            // If we don't have any zone values, use the default zone.
            else
                zones.Add(Zone.Default);

            List<Target> targets = new List<Target>();
            List<string> code = new List<string>();
            Speed speed = Speed.Default;
            Zone zone = Zone.Default;
            Tool tool = Tool.Default;
            CSystem wobj = CSystem.Default;
            int method = 0;
            
            // External axis placeholders
            double extRot = Util.ExAxisTol;
            double extLin = Util.ExAxisTol;

            for (int i = 0; i < planes.Count; i++)
            {
                if (m_interpolationTypes)
                {
                    // Method
                    if (i < methods.Count) { method = methods[i]; }
                    else if (methods != null && i >= methods.Count) { method = methods[methods.Count - 1]; }
                    else { method = 0; }
                }

                if (speeds.Count > 0)
                {
                    if (i < speeds.Count) { speed = speeds[i]; }
                    else { speed = speeds[speeds.Count - 1]; }
                }
                else { speed = Speed.Default; }

                // Zone
                if (i < zones.Count) { zone = zones[i]; }
                else { zone = zones[zones.Count - 1]; }

                // External rotary axis
                if (extRotary)
                {
                    if (i < eRotVals.Count) { extRot = Math.Round(eRotVals[i], 3); }
                    else { extRot = Math.Round(eRotVals[eRotVals.Count - 1], 3); }
                }

                // External linear axis
                if (extLinear)
                {
                    if (i < eLinVals.Count) { extLin = Math.Round(eLinVals[i], 3); }
                    else { extLin = Math.Round(eLinVals[eLinVals.Count - 1], 3); }
                }

                // Tools
                if (tools.Count > 0)
                {
                    if (i < tools.Count) { tool = tools[i]; }
                    else { tool = tools[tools.Count - 1]; }
                }
                else { tool = Tool.Default; }

                // Wobjs
                if (wobjs.Count > 0)
                {
                    if (i < wobjs.Count) { wobj = wobjs[i]; }
                    else { wobj = wobjs[wobjs.Count - 1]; }
                }
                else { wobj = CSystem.Default; }

                // Methods
                MotionType mType = MotionType.Linear;

                if (method == 1) { mType = MotionType.Joint; }
                else if (method == 2) { mType = MotionType.AbsoluteJoint; }

                // Create the robot target.
                Target robTarg = new Target(planes[i], mType, speed, zone, tool, wobj, extRot, extLin, m_Manufacturer);
                targets.Add(robTarg);

                code.Add(robTarg.StrRob);
            }
            DA.SetDataList(0, targets);

            m_targets = targets;

            List<Point3d> points = new List<Point3d>();
            foreach (Target t in targets) points.Add(t.Position);
            m_bBox = new BoundingBox(points);
            /*
            if (m_outputTarget)
                DA.SetDataList("Code", code);
            */
        }

        protected override void BeforeSolveInstance()
        {
            // Subscribe to all event handelers
            this.Params.ParameterSourcesChanged += OnParameterSourcesChanged;
        }

        /// <summary>
        ///  Replace a value list with one that has been pre-populated with possible methonds.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnParameterSourcesChanged(Object sender, GH_ParamServerEventArgs e)
        {
            int index = e.ParameterIndex;
            IGH_Param param = e.Parameter;

            //Only add value list to the first input
            if (param.Name != "*Method") return;

            //Only change value lists
            var extractedItems = param.Sources.Where(p => p.Name == "Value List");

            //Set up value list
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (int entity in typeof(MotionType).GetEnumValues())
            {
                MotionType m = (MotionType)entity;

                options.Add(m.ToString(), entity.ToString());
            }
            Grasshopper.Kernel.Special.GH_ValueList gH_ValueList = Canvas.Component.CreateValueList("Mothods", options);

            //The magic
            Canvas.Component.ChangeObjects(extractedItems, gH_ValueList);
        }

        public override BoundingBox ClippingBox => base.ClippingBox;
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);
            foreach (Target target in m_targets) Canvas.Component.DisplayPlane(target.Plane, args);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {

            base.DrawViewportWires(args);
            foreach (Target target in m_targets) Canvas.Component.DisplayPlane(target.Plane, args);

        }
        public override void ClearData()
        {
            base.ClearData();
            m_targets.Clear();
            m_bBox = BoundingBox.Empty;
        }

        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[3]
        {
        new Param_Integer() { Name = "*Method", NickName = "Method", Description = "A list of target interpolation types [0 = Linear, 1 = Joint]. If one value is supplied it will be applied to all targets.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Rotary", NickName = "Rotary", Description = "A list of external rotary axis positions in degrees. If one value is supplied it will be applied to all targets.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Linear", NickName = "Linear", Description = "A list of external linear axis positions in degrees. If one value is supplied it will be applied to all targets.", Access = GH_ParamAccess.list },
        };

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Code", NickName = "Code", Description = "Robot targets formatted as strings." }
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem outputCode = Menu_AppendItem(menu, "Output Code", outputCode_Click, true, m_outputTarget);
            outputCode.ToolTipText = "Output a string representation of the robot targets.";

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);

            ToolStripMenuItem robotManufacturers = Menu_AppendItem(menu, "Manufacturer");
            robotManufacturers.ToolTipText = "Select the robot manufacturer";
            foreach (string name in typeof(Manufacturer).GetEnumNames())
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, manufacturer_Click);

                if (name == this.m_Manufacturer.ToString()) item.Checked = true;
                robotManufacturers.DropDownItems.Add(item);
            }

            ToolStripSeparator seperator2 = Menu_AppendSeparator(menu);

            ToolStripMenuItem extRotAxisCheck = Menu_AppendItem(menu, "Use Rotary Axis", rotAxis_Click, true, extRotary);
            extRotAxisCheck.ToolTipText = "Add an input for external rotary axis position values.";
            ToolStripMenuItem extLinAxisCheck = Menu_AppendItem(menu, "Use Linear Axis", linAxis_Click, true, extLinear);
            extLinAxisCheck.ToolTipText = "Add an input for external linear axis position values.";

            ToolStripSeparator seperator3 = Menu_AppendSeparator(menu);

            ToolStripMenuItem interpolation = Menu_AppendItem(menu, "Specify Interpolation Types", interpolation_Click, true, m_interpolationTypes);
            interpolation.ToolTipText = "Specify the interpolation type per target. [0 = Linear, 1 = Joint]";
        }
        private void manufacturer_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Manufacturer");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.m_Manufacturer = (Manufacturer)currentItem.Owner.Items.IndexOf(currentItem);
            ExpireSolution(true);
        }

        private void outputCode_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("OutputCode");
            m_outputTarget = !m_outputTarget;

            if (m_outputTarget)
            {
                AddOutput(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Code"), true);
            }
            ExpireSolution(true);
        }

        private void interpolation_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("InterpolationTypes");
            m_interpolationTypes = !m_interpolationTypes;

            if (m_interpolationTypes)
            {
                AddInput(0);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "*Method"), true);
            }
            ExpireSolution(true);
        }

        private void rotAxis_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("LinAxisClick");
            extRotary = !extRotary;

            if (extRotary)
            {
                AddInput(1);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Rotary"), true);
            }
            ExpireSolution(true);
        }

        private void linAxis_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("RotAxisClick");
            extLinear = !extLinear;

            if (extLinear)
            {
                AddInput(2);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Linear"), true);
            }
            ExpireSolution(true);
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

        // Register the new output parameters to our component.
        private void AddOutput(int index)
        {
            IGH_Param parameter = outputParams[index];

            if (Params.Output.Any(x => x.Name == parameter.Name))
                Params.UnregisterOutputParameter(Params.Output.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Output.Count;
                for (int i = 0; i < Params.Output.Count; i++)
                {
                    int otherIndex = Array.FindIndex(outputParams, x => x.Name == Params.Output[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                Params.RegisterOutputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("OutputCode", this.m_outputCode);
            writer.SetInt32("Manufacturer", (int)this.m_Manufacturer);
            writer.SetBoolean("RotAxis", this.extRotary);
            writer.SetBoolean("LinAxis", this.extLinear);
            writer.SetBoolean("Method", this.m_interpolationTypes);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_outputCode = reader.GetBoolean("OutputCode");
            this.m_Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
            this.extRotary = reader.GetBoolean("RotAxis");
            this.extLinear = reader.GetBoolean("LinAxis");
            this.m_interpolationTypes = reader.GetBoolean("Method");
            return base.Read(reader);
        }

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
            get { return new Guid("{c8ae5262-f447-4807-b1ff-10b29b37c984}"); }
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