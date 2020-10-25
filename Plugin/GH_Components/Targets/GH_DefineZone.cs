using Axis;
using Axis.Kernal;
using Axis.Types;
using Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Axis.GH_Components
{
    /// <summary>
    /// Define a custom zone object.
    /// </summary>
    public class GH_DefineZone : Axis_Component, IGH_VariableParameterComponent
    {

        public GH_DefineZone() : base("Zone", "Z", "Define a list of robot interpolation zones.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
            stopPoint = new ToolStripMenuItem("Specify a Stop Point", null, stop_Click)
            {
                ToolTipText = "Specify the tool reorientation zone in mm.",
            };
            orientOpt = new ToolStripMenuItem("Specify Reorientation Zone", null, rot_Click) 
            {
                ToolTipText = "Specify the tool reorientation zone in mm.",
            };
            extAxOpt = new ToolStripMenuItem("Specify External Linear Speed", null, extAxis_Click) 
            {
                ToolTipText = "Specify the external rotary axis zone in mm.",
            };
            toolOriOpt = new ToolStripMenuItem("Specify Reorientation Degrees", null, toolDeg_Click) 
            {
                ToolTipText = "Specify the tool reorientation zone in degrees.",
            };
            linExtOpt = new ToolStripMenuItem("Specify Linear External Axis Zone", null, linExt_Click) 
            {
                ToolTipText = "Specify the linear external axis zone in mm.",
            };
            rotExtOpt = new ToolStripMenuItem("Specify Rotary External Axis Zone", null, rotExt_Click) 
            {
                ToolTipText = "Specify the rotary external axis zone in degrees.",
            };
            declarationCheck = new ToolStripMenuItem("Output Declarations", null, declaration_Click) 
            {
                ToolTipText = "Output the formatted zone declaration.",
            };

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                stopPoint,
                orientOpt,
                extAxOpt,
                toolOriOpt,
                linExtOpt,
                rotExtOpt,
                declarationCheck,
            };
        }
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
            if (orientOpt.Checked)
            {
                snapDefault = false;
                if (!DA.GetDataList("Reorientation", orientTCP)) return;
            }
            if (extAxOpt.Checked)
            {
                snapDefault = false;
                if (!DA.GetDataList("External", extTCP)) return;
            }
            if (toolOriOpt.Checked)
            {
                snapDefault = false;
                if (!DA.GetDataList("Degrees", orientation)) return;
            }
            if (linExtOpt.Checked)
            {
                snapDefault = false;
                if (!DA.GetDataList("Linear", linExt)) return;
            }
            if (rotExtOpt.Checked)
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
                if (stopPoint.Checked)
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
                            if (orientOpt.Checked)
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
                            if (extAxOpt.Checked)
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
                            if (toolOriOpt.Checked)
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
                            if (linExtOpt.Checked)
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
                            if (rotExtOpt.Checked)
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

            if (declarationCheck.Checked)
            {
                DA.SetDataList("Declaration", declarations);
            }
        }

        #region Variables
        ToolStripMenuItem stopPoint;
        ToolStripMenuItem orientOpt;
        ToolStripMenuItem extAxOpt; 
        ToolStripMenuItem toolOriOpt;
        ToolStripMenuItem linExtOpt;
        ToolStripMenuItem rotExtOpt;
        ToolStripMenuItem declarationCheck;

        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("*TCP", "TCP", "TCP path radius of the zone.", GH_ParamAccess.list, 5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param zone = new Axis.GH_Params.ZoneParam();
            pManager.AddParameter(zone, "Zone", "Zone", "List of zone objects.", GH_ParamAccess.list);
        }

        #endregion IO


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
        private IGH_Param[] inputParams = new IGH_Param[5]
        {
        new Param_Number() { Name = "Reorientation", NickName = "Reorientation", Description = "Joint reorientation zone in mm.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "External", NickName = "External", Description = "External axis zone in mm.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Degrees", NickName = "Degrees", Description = "Tool reorientation zone in degrees.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Linear", NickName = "Linear", Description = "External linear axis zone in mm.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Rotary", NickName = "Rotary", Description = "External rotary axis zone in degrees.", Access = GH_ParamAccess.list },
        };

        // Build a list of optional input and output parameters
        private IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Declaration", NickName = "Declaration", Description = "Formatted zone declarations as strings.", Access = GH_ParamAccess.list },
        };


        private void stop_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("StopPoint");
            button.Checked = !button.Checked;
            ExpireSolution(true);
        }

        private void rot_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("Reorientation");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(0, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Reorientation"), true);
            }
            ExpireSolution(true);
        }

        private void extAxis_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("External");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(1, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "External"), true);
            }
            ExpireSolution(true);
        }

        private void toolDeg_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("ToolReorientation");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(2, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Degrees"), true);
            }
            ExpireSolution(true);
        }

        private void linExt_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("LinExtZone");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(3, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Linear"), true);
            }
            ExpireSolution(true);
        }

        private void rotExt_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("RotExtZone");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(4, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Rotary"), true);
            }
            ExpireSolution(true);
        }

        private void declaration_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;

            RecordUndoEvent("Declarations");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddOutput(0, outputParams);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Declaration"), true);
            }
            ExpireSolution(true);
        }

        #endregion UI

        #region Serialization

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("StopPoint", this.stopPoint.Checked);
            writer.SetBoolean("UseReorientation", this.orientOpt.Checked);
            writer.SetBoolean("UseExternal", this.extAxOpt.Checked);
            writer.SetBoolean("UseDegrees", this.toolOriOpt.Checked);
            writer.SetBoolean("UseLinear", this.linExtOpt.Checked);
            writer.SetBoolean("UseRotary", this.rotExtOpt.Checked);
            writer.SetBoolean("OutputDec", this.declarationCheck.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if(reader.ItemExists("StopPoint")) this.stopPoint.Checked = reader.GetBoolean("StopPoint");
            if(reader.ItemExists("UseReorientation")) this.orientOpt.Checked = reader.GetBoolean("UseReorientation");
            if(reader.ItemExists("UseExternal")) this.extAxOpt.Checked = reader.GetBoolean("UseExternal");
            if(reader.ItemExists("UseDegrees")) this.toolOriOpt.Checked = reader.GetBoolean("UseDegrees");
            if(reader.ItemExists("UseLinear")) this.linExtOpt.Checked = reader.GetBoolean("UseLinear");
            if(reader.ItemExists("UseRotary")) this.rotExtOpt.Checked = reader.GetBoolean("UseRotary");
            if(reader.ItemExists("OutputDec")) this.declarationCheck.Checked = reader.GetBoolean("OutputDec");
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

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Icons.Zone;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8314b5fd-f75c-46db-922d-ee506338b161"); }
        }

        #endregion Component Settings
    }
}