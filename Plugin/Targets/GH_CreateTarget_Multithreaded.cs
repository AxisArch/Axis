using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

//using MoreLinq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Axis.Core;
using Axis.Targets;

namespace Axis
{
    /// <summary>
    /// Define robot end effector plane target positions.
    /// </summary>
    public class GH_CreateTarget_Multithreaded : GH_Component, IGH_VariableParameterComponent
    {
        // Boolean toggle for context menu items.
        bool m_outputCode = false;
        bool m_interpolationTypes = false;

        Manufacturer m_Manufacturer = Manufacturer.ABB;
        List<Target> c_targets = new List<Target>();
        BoundingBox c_bBox = new BoundingBox();

        // External axis presence.
        bool extRotary = false;
        bool extLinear = false;

        public GH_CreateTarget_Multithreaded() : base("Plane Target", "Target", "Create custom robot targets from planes.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Target TCP location as plane.", GH_ParamAccess.list);
            IGH_Param speed = new Axis.Params.SpeedParam();
            pManager.AddParameter(speed, "Speed", "Speed", "List of speed objects per plane.", GH_ParamAccess.list);
            IGH_Param zone = new Axis.Params.ZoneParam();
            pManager.AddParameter(zone, "Zone", "Zone", "Approximation zone per target, in mm.", GH_ParamAccess.list);
            IGH_Param tool = new Axis.Params.ToolParam();
            pManager.AddParameter(tool, "Tool", "Tool", "Tool to use for operation.", GH_ParamAccess.list);
            IGH_Param csystem = new Axis.Params.CSystemParam();
            pManager.AddParameter(csystem, "Wobj", "Wobj", "Wobj to use for operation.", GH_ParamAccess.list);

            for (int i = 1; i < 5; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param target = new Axis.Params.TargetParam();
            pManager.AddParameter(target, "Targets", "Targets", "Robot targets.", GH_ParamAccess.list);
        }
        #endregion

        protected override void BeforeSolveInstance()
        {
            // Subscribe to all event handelers
            this.Params.ParameterSourcesChanged += OnParameterSourcesChanged;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            this.Message = m_Manufacturer.ToString();

            List<string> code = new List<string>();

            List<Plane> planes = new List<Plane>();
            List<Speed> speeds = new List<Speed>();
            List<Zone> zones = new List<Zone>();
            List<Tool> tools = new List<Tool>();
            List<CSystem> wobjs = new List<CSystem>();
            List<int> methods = new List<int>();

            // Store the input lists of external axis values to synchronise with the targets.
            List<double> eRotVals = new List<double>();
            List<double> eLinVals = new List<double>();



            DA.GetDataList(0, planes);
            if (!DA.GetDataList(1, speeds)) speeds.Add(Speed.Default);
            if (!DA.GetDataList(2, zones)) zones.Add(Zone.Default);
            if (!DA.GetDataList(3, tools)) tools.Add(Tool.Default);
            if (!DA.GetDataList(4, wobjs)) wobjs.Add(CSystem.Default);

            // If interpolation types are specified, get them.
            if (m_interpolationTypes) { if (!DA.GetDataList("*Method", methods)) return; }

            // If the inputs are present, get the external axis values.
            if (extRotary) { if (!DA.GetDataList("Rotary", eRotVals)) return; }
            if (extLinear) { if (!DA.GetDataList("Linear", eLinVals)) return; }


            // Compute results on given data
            System.Collections.Concurrent.ConcurrentBag<TargetOrderPair> bag = new System.Collections.Concurrent.ConcurrentBag<TargetOrderPair>();
            //Parallel.For(0, planes.Count, index => ComputeTargets_Unordered(bag, index, planes, m_interpolationTypes, speeds, zones, extRotary, extLinear, tools, wobjs, eRotVals, eLinVals, methods, m_Manufacturer));
            Parallel.For(0, planes.Count, index => ComputeTargets(bag, index, planes, speeds, zones, tools, wobjs, eRotVals, eLinVals, methods, m_Manufacturer));
            c_targets = bag.OrderBy(b => b.Order).Select(b => b.Value).ToList();
            
            //Compute bounding box for visualisation
            c_bBox = new BoundingBox(c_targets.Select(t => t.Plane.Origin).ToList());

            // Set output data
            if (c_targets != null)
            {
                DA.SetDataList("Targets", c_targets);
            }
            if (m_outputCode) 
            {
                code = c_targets.Select(t => t.StrRob).ToList();
                DA.SetDataList("Code", code);
            }
        }

        struct TargetOrderPair
        {
            public Target Value { get; set; }
            public int Order { get; set; }
        }

        private static void ComputeTargets_Unordered(System.Collections.Concurrent.ConcurrentBag<TargetOrderPair> bag, 
            int i, List<Plane> planes, bool m_interpolationTypes, List<Speed> speeds,
            List<Zone> zones, bool extRotary, bool extLinear, List<Tool> tools, List<CSystem> wobjs, List<double> eRotVals,
            List<double> eLinVals, List<int> methods, Manufacturer m_Manufacturer)
        {
            TargetOrderPair result = new TargetOrderPair();

            Speed speed;
            Zone zone;
            Tool tool;
            CSystem wobj;
            int method = 0;
            double extRot = 0;
            double extLin = 0;


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
            result.Value = new Target(planes[i], mType, speed, zone, tool, wobj, extRot, extLin, m_Manufacturer);
            result.Order = i;

            bag.Add(result);
        }

        private static void ComputeTargets(System.Collections.Concurrent.ConcurrentBag<TargetOrderPair> bag, int i, 
            List<Plane> planes, List<Speed> speeds, List<Zone> zones, List<Tool> tools, List<CSystem> wobjs, 
            List<double> eRotVals, List<double> eLinVals, List<int> methods, Manufacturer m_Manufacturer)
        {
            TargetOrderPair result = new TargetOrderPair();

            // Create the robot target.
            result.Value = new Target(planes[i], (MotionType)methods.InfinitElementAt(i), speeds.InfinitElementAt(i), zones.InfinitElementAt(i), 
                tools.InfinitElementAt(i), wobjs.InfinitElementAt(i), eRotVals.InfinitElementAt(i), eLinVals.InfinitElementAt(i), m_Manufacturer);
            result.Order = i;

            bag.Add(result);
        }

        #region UI
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
            ToolStripMenuItem outputCode = Menu_AppendItem(menu, "Output Code", outputCode_Click, true, m_outputCode);
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
            m_outputCode = !m_outputCode;

            if (m_outputCode)
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
        #endregion

        #region Display
        public override BoundingBox ClippingBox => base.ClippingBox;
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);
            foreach (Target target in c_targets) Canvas.Component.DisplayPlane(target.Plane, args);
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {

            base.DrawViewportWires(args);
            foreach (Target target in c_targets) Canvas.Component.DisplayPlane(target.Plane, args);

        }

        public override void ClearData()
        {
            base.ClearData();
            c_targets.Clear();
            c_bBox = BoundingBox.Empty;
        }
        #endregion

        #region Serialization
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
        #endregion

        #region Component Settings
        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Icons.Target;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("C3B49429-C457-4573-939E-0D3BA69DF071"); }
        }
        #endregion
    }

    public static class Extentions
    {
        public static T InfinitElementAt<T>(this IList<T> source, int index) 
        {
            if (source == null) return default(T);
            if (source.Count() == 0) return default(T);
            return ((source.Count() > index) ? source[index] : source[source.Count()-1]); 
        }
    }
}