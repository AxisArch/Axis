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
    /// Define a custom speed object.
    /// </summary>
    public class GH_DefineSpeed : Axis_Component, IGH_VariableParameterComponent
    {

        public GH_DefineSpeed() : base("Speed", "S", "Define a list of robot movement speeds.", AxisInfo.Plugin, AxisInfo.TabConfiguration)
        {
            rotOption = new ToolStripMenuItem("Specify Rotation Speed", null, rot_Click) 
            {
                ToolTipText = "Specify the joint rotation limit in degrees / second.",
            };
            extLinOpt = new ToolStripMenuItem("Specify External Linear Speed", null, extLin_Click) 
            {
                ToolTipText = "Specify the external linear axis speed in mm / second.",
            };
            extRotOpt = new ToolStripMenuItem("Specify External Rotary Speed", null, extRot_Click) 
            {
                ToolTipText = "Specify the external rotary axis speed in degrees / second.",
            };
            timeOption = new ToolStripMenuItem("Define Movement Time", null, time_Click) 
            {
                ToolTipText = "Specify the movement time in seconds and override the other values.",
            };
            nameOption = new ToolStripMenuItem("Add Custom Name", null, name_Click) 
            {
                ToolTipText = "Define a custom name for each target for declaration purposes.",
            };
            declarationCheck = new ToolStripMenuItem("Output Declarations", null, declaration_Click) 
            {
                ToolTipText = "Output the formatted speed declaration.",
            };

            RegularToolStripItems = new ToolStripMenuItem[]
            {
                rotOption,
                extLinOpt,
                extRotOpt,
                timeOption,
                nameOption,
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
            List<double> linVals = new List<double>();
            List<double> rotVals = new List<double>();
            List<double> timeVals = new List<double>();
            List<string> names = new List<string>();

            if (!DA.GetDataList(0, linVals)) return;

            // Get the optional inputs, if present.
            if (rotOption.Checked)
            {
                if (!DA.GetDataList("Rotation", rotVals)) return;
            }
            if (timeOption.Checked)
            {
                if (!DA.GetDataList("Time", timeVals)) return;
            }
            if (nameOption.Checked)
            {
                if (!DA.GetDataList("Name", names)) return;
            }

            // Always try to check for default dictionary zones.
            bool snapDefault = true;

            List<Speed> speeds = new List<Speed>();
            List<string> declarations = new List<string>();

            // Default and current speed dictionaries.
            Dictionary<double, Speed> defaultSpeeds = Util.ABBSpeeds();
            Dictionary<double, Speed> presentSpeeds = new Dictionary<double, Speed>();

            if (timeOption.Checked)
            {
                for (int i = 0; i < timeVals.Count; i++)
                {
                    if (i < names.Count)
                    {
                        Speed speed = new Speed(100, 30, names[i], timeVals[i]);
                        speeds.Add(speed);
                    }
                    else
                    {
                        Speed speed = new Speed(100, 30, "time_" + timeVals[i].ToString(), timeVals[i]);
                        speeds.Add(speed);
                    }
                }
            }
            else
            {
                // Default rotation value.
                double rotVal = 30;

                for (int i = 0; i < linVals.Count; i++)
                {
                    Speed speed = new Speed(100, 60, "Speed100");

                    if (snapDefault && defaultSpeeds.ContainsKey(linVals[i]))
                    {
                        speed = defaultSpeeds[linVals[i]];
                    }
                    else
                    {
                        if (rotOption.Checked)
                        {
                            if (i < rotVals.Count)
                            {
                                rotVal = rotVals[i];
                            }
                            else { rotVal = rotVals[rotVals.Count - 1]; }
                        }

                        if (i < names.Count)
                        {
                            speed = new Speed(linVals[i], rotVal, names[i], 0);
                        }
                        else
                        {
                            string name = linVals[i].ToString().Replace('.', '_');
                            speed = new Speed(linVals[i], rotVal, "v" + name, 0);
                        }
                    }

                    // Check to see if the dictionary contains the current speed, if not, add it.
                    if (presentSpeeds.ContainsKey(linVals[i]))
                    {
                        speeds.Add(speed);
                    }
                    else
                    {
                        speeds.Add(speed);
                        presentSpeeds.Add(speed.TranslationSpeed, speed);
                    }
                }
            }

            List<Speed> speedList = presentSpeeds.Values.ToList();
            for (int i = 0; i < speedList.Count; i++)
            {
                if (!defaultSpeeds.ContainsKey(speedList[i].TranslationSpeed))
                {
                    Speed sp = speedList[i];
                    string dec = "VAR speeddata " + sp.Name + " := [ " + Math.Round(sp.TranslationSpeed, 2).ToString() + ", " + Math.Round(sp.RotationSpeed, 2).ToString() + ", 200, 30 ];";
                    declarations.Add(dec);
                }
            }

            DA.SetDataList(0, speeds);

            if (declarationCheck.Checked)
            {
                DA.SetDataList("Declaration", declarations);
            }
        }

        #region Variables
        // Sticky context menu item values.
        ToolStripMenuItem rotOption;
        ToolStripMenuItem extLinOpt;
        ToolStripMenuItem extRotOpt;
        ToolStripMenuItem timeOption;
        ToolStripMenuItem nameOption;
        ToolStripMenuItem declarationCheck;
        #endregion Variables

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("*Linear", "Linear", "Translational TCP speed in mm/s.", GH_ParamAccess.list, 50);
            Param_Integer param = pManager[0] as Param_Integer;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            IGH_Param speed = new Axis.GH_Params.SpeedParam();
            pManager.AddParameter(speed, "Speed", "Speed", "List of speed objects.", GH_ParamAccess.list);
        }

        #endregion IO


        #region UI

        /// <summary>
        ///  Replace a value list with one that has been pre-populated with possible speeds.
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
            foreach (KeyValuePair<double, Speed> entity in Util.ABBSpeeds())
            {
                options.Add(entity.Value.Name, entity.Key.ToString());
            }
            Grasshopper.Kernel.Special.GH_ValueList gH_ValueList = Canvas.Component.CreateValueList("Speeds", options);

            //The magic
            Canvas.Component.ChangeObjects(extractedItems, gH_ValueList);
        }

        // Build a list of optional input and output parameters
        private IGH_Param[] inputParams = new IGH_Param[5]
        {
        new Param_Number() { Name = "Rotation", NickName = "Rotation", Description = "Joint rotation limit in degrees / second.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "ExtLin", NickName = "ExtLin", Description = "External linear axis speed in mm / second.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "ExtRot", NickName = "ExtRot", Description = "External rotary axis speed in degrees / second.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Time", NickName = "Time", Description = "Override the translation and rotatio speeds by specifying the movement time in seconds.", Access = GH_ParamAccess.list },
        new Param_String() { Name = "Name", NickName = "Name", Description = "Custom speed name for declaration purposes.", Access = GH_ParamAccess.list },
        };

        // Build a list of optional input and output parameters
        private IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Declaration", NickName = "Declaration", Description = "Formatted speed declarations as strings.", Access = GH_ParamAccess.list },
        };


        private void rot_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("RotationOption");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(0, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Rotation"), true);
            }
            ExpireSolution(true);
        }

        private void extLin_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("ExtLinOption");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(1, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "ExtLin"), true);
            }
            ExpireSolution(true);
        }

        private void extRot_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("ExtRotOption");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(2, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "ExtRot"), true);
            }
            ExpireSolution(true);
        }

        private void time_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("TimeOption");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(3, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Time"), true);
            }
            ExpireSolution(true);
        }

        private void name_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("NameOption");
            button.Checked = !button.Checked;

            if (button.Checked)
            {
                this.AddInput(4, inputParams);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Name"), true);
            }
            ExpireSolution(true);
        }

        private void declaration_Click(object sender, EventArgs e)
        {
            var button = (ToolStripMenuItem)sender;
            RecordUndoEvent("DeclOption");
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
            writer.SetBoolean("UseRotation", this.rotOption.Checked);
            writer.SetBoolean("UseExtLin", this.extLinOpt.Checked);
            writer.SetBoolean("UseExtRot", this.extRotOpt.Checked);
            writer.SetBoolean("UseTime", this.timeOption.Checked);
            writer.SetBoolean("UseName", this.nameOption.Checked);
            writer.SetBoolean("OutputDec", this.declarationCheck.Checked);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if(reader.ItemExists("UseRotation")) this.rotOption.Checked = reader.GetBoolean("UseRotation");
            if(reader.ItemExists("UseExtLin")) this.extLinOpt.Checked = reader.GetBoolean("UseExtLin");
            if(reader.ItemExists("UseExtRot")) this.extRotOpt.Checked = reader.GetBoolean("UseExtRot");
            if(reader.ItemExists("UseTime")) this.timeOption.Checked = reader.GetBoolean("UseTime");
            if(reader.ItemExists("UseName")) this.nameOption.Checked = reader.GetBoolean("UseName");
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
                return Axis.Properties.Icons.Speed;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("602c07b3-af08-46c8-8d67-4ab2f351024d"); }
        }

        #endregion Component Settings
    }
}