using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Axis.Targets
{
    /// <summary>
    /// Define a custom zone object.
    /// </summary>
    public class DefineZone : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky context menu item values.
        bool m_Stop = false;
        bool m_Reorient = false;
        bool m_ToolReorient = false;
        bool m_ExtAxis = false;
        bool m_Declaration = false;
        bool m_LinExt = false;
        bool m_RotExt = false;

        public DefineZone() : base("Zone", "Z", "Define a list of robot interpolation zones.", AxisInfo.Plugin, AxisInfo.TabToolpath)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("*TCP", "TCP", "TCP path radius of the zone.", GH_ParamAccess.list, 5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Zone", "Zone", "List of zone objects.", GH_ParamAccess.list);
        }
        #endregion

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();

            //Subscribe to all event handelers
            this.Params.ParameterSourcesChanged += OnParameterSourcesChanged;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> pathTCP = new List<double>();
            List<double> orientTCP = new List<double>();
            List<double> extTCP = new List<double>();
            List<double> orientation = new List<double>();
            List<double> linExt = new List<double>();
            List<double> rotExt = new List<double>();

            if (!DA.GetDataList(0, pathTCP)) return;

            // Always try to check for default dictionary zones.
            bool snapDefault = true;

            // Get the optional inputs, if present.
            if (m_Reorient)
            {
                snapDefault = false;
                if (!DA.GetDataList("Reorientation", orientTCP)) return;
            }
            if (m_ExtAxis)
            {
                snapDefault = false;
                if (!DA.GetDataList("External", extTCP)) return;
            }
            if (m_ToolReorient)
            {
                snapDefault = false;
                if (!DA.GetDataList("Degrees", orientation)) return;
            }
            if (m_LinExt)
            {
                snapDefault = false;
                if (!DA.GetDataList("Linear", linExt)) return;
            }
            if (m_RotExt)
            {
                snapDefault = false;
                if (!DA.GetDataList("Rotary", rotExt)) return;
            }

            List<Zone> zones = new List<Zone>();
            List<string> declarations = new List<string>();

            // Default and current zone dictionaries.
            Dictionary<double, Zone> defaultZones = Util.ABBZones();
            Dictionary<double, Zone> presentZones = new Dictionary<double, Zone>();

            for (int i = 0; i < pathTCP.Count; i++)
            {
                Zone zone = new Zone(false, 5, 7.5, 7.5, 0.5, 7.5, 0.5, "EmptyZone");

                // If we want to program stop points, do so and skip the rest.
                if (m_Stop)
                {
                    zone = defaultZones[-1];
                    zones.Add(zone);
                }
                else
                {
                    if (snapDefault && defaultZones.ContainsKey(pathTCP[i]))
                    {
                        zone = defaultZones[pathTCP[i]];
                        zones.Add(zone);
                    }
                    else
                    {
                        // Set the finepoint if our radius is -1, and ignore the rest.
                        if (pathTCP[i] == -1)
                        {
                            zone.StopPoint = true;
                            zones.Add(zone);
                        }
                        else
                        {
                            zone.StopPoint = false;
                            zone.PathRadius = pathTCP[i];

                            // Path zone reorientation
                            if (m_Reorient)
                            {
                                if (i < orientTCP.Count)
                                {
                                    zone.PathOrient = orientTCP[i];
                                }
                                else { zone.PathOrient = orientTCP[orientTCP.Count - 1]; }
                            }
                            else
                            {
                                // Use the default convention of a value of 1.5 times the TCP path val.
                                zone.PathOrient = pathTCP[i] * 1.5;
                            }

                            // External zone 
                            if (m_ExtAxis)
                            {
                                if (i < extTCP.Count)
                                {
                                    zone.PathExternal = extTCP[i];
                                }
                                else { zone.PathExternal = extTCP[extTCP.Count - 1]; }
                            }
                            else
                            {
                                // Use the default convention of a value of 1.5 times the TCP path val.
                                zone.PathExternal = pathTCP[i] * 1.5;
                            }

                            // Tool reorientation degrees
                            if (m_ToolReorient)
                            {
                                if (i < orientation.Count)
                                {
                                    zone.Orientation = orientation[i];
                                }
                                else { zone.Orientation = orientation[orientation.Count - 1]; }
                            }
                            else
                            {
                                // Use the default convention of a value of 1.5 times the TCP path val divided by 10.
                                zone.Orientation = (pathTCP[i] * 1.5) / 10;
                            }

                            // Linear external axis zone
                            if (m_LinExt)
                            {
                                if (i < linExt.Count)
                                {
                                    zone.LinearExternal = linExt[i];
                                }
                                else { zone.LinearExternal = linExt[linExt.Count - 1]; }
                            }
                            else
                            {
                                // Use the default convention of a value of 1.5 times the TCP path val.
                                zone.LinearExternal = (pathTCP[i] * 1.5);
                            }

                            // Linear external axis zone
                            if (m_RotExt)
                            {
                                if (i < rotExt.Count)
                                {
                                    zone.RotaryExternal = rotExt[i];
                                }
                                else { zone.RotaryExternal = rotExt[rotExt.Count - 1]; }
                            }
                            else
                            {
                                // Use the default convention of a value of 1.5 times the TCP path val divided by 10
                                zone.RotaryExternal = (pathTCP[i] * 1.5) / 10;
                            }

                            zone.Name = "Zone" + zone.PathRadius.ToString();

                            // Check to see if the dictionary contains the current zone, if not, add it.
                            if (presentZones.ContainsKey(pathTCP[i]))
                            {
                                zones.Add(zone);
                            }
                            else
                            {
                                zones.Add(zone);
                                presentZones.Add(zone.PathRadius, zone);
                            }
                        }
                    }
                }                
            }
            DA.SetDataList(0, zones);

            List<Zone> zoneList = presentZones.Values.ToList();
            for (int i = 0; i < zoneList.Count; i++)
            {
                Zone z = zoneList[i];
                string dec = "VAR zonedata " + z.Name + " := [" + z.StopPoint.ToString() + ", " + Math.Round(z.PathRadius, 1).ToString() + ", " + Math.Round(z.PathOrient, 1).ToString() + ", " + Math.Round(z.PathExternal, 1).ToString() + ", " + Math.Round(z.Orientation, 1).ToString() + ", " + Math.Round(z.LinearExternal, 1).ToString() + ", " + Math.Round(z.RotaryExternal, 1).ToString() + " ];";
                declarations.Add(dec);
            }

            if (m_Declaration)
            {
                DA.SetDataList("Declaration", declarations);
            }
        }

        #region UI
        /// <summary>
        ///  Replace a value list with one that has been pre-populated with possible zones.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnParameterSourcesChanged(Object sender, GH_ParamServerEventArgs e)
        {
            int index = e.ParameterIndex;
            IGH_Param param = e.Parameter;

            //Only add value list to the first input
            if (index != 0) return;

            //Only change value lists
            var extractedItems = param.Sources.Where(p => p.Name == "Value List");

            //Set up value list
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (KeyValuePair<double, Zone> entity in Util.ABBZones())
            {
                options.Add(entity.Value.Name, entity.Key.ToString());
            }
            Grasshopper.Kernel.Special.GH_ValueList gH_ValueList = Canvas.Component.CreateValueList("Zones", options);

            //The magic
            Canvas.Component.ChangeObjects(extractedItems, gH_ValueList);
        }

        // Build a list of optional input and output parameters
        IGH_Param[] inputParams = new IGH_Param[5]
        {
        new Param_Number() { Name = "Reorientation", NickName = "Reorientation", Description = "Joint reorientation zone in mm.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "External", NickName = "External", Description = "External axis zone in mm.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Degrees", NickName = "Degrees", Description = "Tool reorientation zone in degrees.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Linear", NickName = "Linear", Description = "External linear axis zone in mm.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Rotary", NickName = "Rotary", Description = "External rotary axis zone in degrees.", Access = GH_ParamAccess.list },
        };

        // Build a list of optional input and output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Declaration", NickName = "Declaration", Description = "Formatted zone declarations as strings.", Access = GH_ParamAccess.list },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem stopPoint = Menu_AppendItem(menu, "Specify a Stop Point", stop_Click, true, m_Stop);
            stopPoint.ToolTipText = "Specify the tool reorientation zone in mm.";

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);

            ToolStripMenuItem orientOpt = Menu_AppendItem(menu, "Specify Reorientation Zone", rot_Click, true, m_Reorient);
            orientOpt.ToolTipText = "Specify the tool reorientation zone in mm.";
            ToolStripMenuItem extAxOpt = Menu_AppendItem(menu, "Specify External Linear Speed", extAxis_Click, true, m_ExtAxis);
            extAxOpt.ToolTipText = "Specify the external rotary axis zone in mm.";

            ToolStripSeparator seperator2 = Menu_AppendSeparator(menu);

            ToolStripMenuItem toolOriOpt = Menu_AppendItem(menu, "Specify Reorientation Degrees", toolDeg_Click, true, m_ToolReorient);
            toolOriOpt.ToolTipText = "Specify the tool reorientation zone in degrees.";
            ToolStripMenuItem linExtOpt = Menu_AppendItem(menu, "Specify Linear External Axis Zone", linExt_Click, true, m_LinExt);
            linExtOpt.ToolTipText = "Specify the linear external axis zone in mm.";
            ToolStripMenuItem rotExtOpt = Menu_AppendItem(menu, "Specify Rotary External Axis Zone", rotExt_Click, true, m_RotExt);
            rotExtOpt.ToolTipText = "Specify the rotary external axis zone in degrees.";

            ToolStripSeparator seperator3 = Menu_AppendSeparator(menu);

            ToolStripMenuItem declarationCheck = Menu_AppendItem(menu, "Output Declarations", declaration_Click, true, m_Declaration);
            declarationCheck.ToolTipText = "Output the formatted zone declaration.";
        }

        private void stop_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("StopPoint");
            m_Stop = !m_Stop;
            ExpireSolution(true);
        }

        private void rot_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Reorientation");
            m_Reorient = !m_Reorient;

            if (m_Reorient)
            {
                AddInput(0);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Reorientation"), true);
            }
            ExpireSolution(true);
        }

        private void extAxis_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("External");
            m_ExtAxis = !m_ExtAxis;

            if (m_ExtAxis)
            {
                AddInput(1);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "External"), true);
            }
            ExpireSolution(true);
        }

        private void toolDeg_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ToolReorientation");
            m_ToolReorient = !m_ToolReorient;

            if (m_ToolReorient)
            {
                AddInput(2);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Degrees"), true);
            }
            ExpireSolution(true);
        }

        private void linExt_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("LinExtZone");
            m_LinExt = !m_LinExt;

            if (m_LinExt)
            {
                AddInput(3);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Linear"), true);
            }
            ExpireSolution(true);
        }

        private void rotExt_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("RotExtZone");
            m_RotExt = !m_RotExt;

            if (m_RotExt)
            {
                AddInput(4);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Rotary"), true);
            }
            ExpireSolution(true);
        }

        private void declaration_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Declarations");
            m_Declaration = !m_Declaration;

            if (m_Declaration)
            {
                AddOutput(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Declaration"), true);
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

        #region Serialization
        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("UseReorientation", this.m_Reorient);
            writer.SetBoolean("UseExternal", this.m_ExtAxis);
            writer.SetBoolean("UseDegrees", this.m_ToolReorient);
            writer.SetBoolean("UseLinear", this.m_LinExt);
            writer.SetBoolean("UseRotary", this.m_RotExt);
            writer.SetBoolean("OutputDec", this.m_Declaration);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_Reorient = reader.GetBoolean("UseReorientation");
            this.m_ExtAxis = reader.GetBoolean("UseExternal");
            this.m_ToolReorient = reader.GetBoolean("UseDegrees");
            this.m_LinExt = reader.GetBoolean("UseLinear");
            this.m_RotExt = reader.GetBoolean("UseRotary");
            this.m_Declaration = reader.GetBoolean("OutputDec");
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

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Zone;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("8314b5fd-f75c-46db-922d-ee506338b161"); }
        }
        #endregion
    }
}