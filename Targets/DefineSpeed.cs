using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Axis.Targets
{
    public class DefineSpeed : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky context menu item values.
        bool m_Rotation = false;
        bool m_Time = false;
        bool m_Name = false;
        bool m_Declaration = false;
        bool m_ExtLin = false;
        bool m_ExtRot = false;

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Speed;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("602c07b3-af08-46c8-8d67-4ab2f351024d"); }
        }

        public DefineSpeed() : base("Speed", "S", "Define a list of robot movement speeds.", AxisInfo.Plugin, AxisInfo.TabToolpath)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Linear", "Linear", "Translational TCP speed in mm/s.", GH_ParamAccess.list, 50);
            Param_Integer param = pManager[0] as Param_Integer;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Speed", "Speed", "List of speed objects.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> linVals = new List<double>();
            List<double> rotVals = new List<double>();
            List<double> timeVals = new List<double>();
            List<string> names = new List<string>();

            if (!DA.GetDataList(0, linVals)) return;

            // Get the optional inputs, if present.
            if (m_Rotation)
            {
                if (!DA.GetDataList("Rotation", rotVals)) return;
            }
            if (m_Time)
            {
                if (!DA.GetDataList("Time", timeVals)) return;
            }
            if (m_Name)
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

            if (m_Time)
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
                        if (m_Rotation)
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

            if (m_Declaration)
            {
                DA.SetDataList("Declaration", declarations);
            }
        }

        // Build a list of optional input and output parameters
        IGH_Param[] inputParams = new IGH_Param[5]
        {
        new Param_Number() { Name = "Rotation", NickName = "Rotation", Description = "Joint rotation limit in degrees / second.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "ExtLin", NickName = "ExtLin", Description = "External linear axis speed in mm / second.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "ExtRot", NickName = "ExtRot", Description = "External rotary axis speed in degrees / second.", Access = GH_ParamAccess.list },
        new Param_Number() { Name = "Time", NickName = "Time", Description = "Override the translation and rotatio speeds by specifying the movement time in seconds.", Access = GH_ParamAccess.list },
        new Param_String() { Name = "Name", NickName = "Name", Description = "Custom speed name for declaration purposes.", Access = GH_ParamAccess.list },
        };

        // Build a list of optional input and output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Declaration", NickName = "Declaration", Description = "Formatted speed declarations as strings.", Access = GH_ParamAccess.list },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem rotOption = Menu_AppendItem(menu, "Specify Rotation Speed", rot_Click, true, m_Rotation);
            rotOption.ToolTipText = "Specify the joint rotation limit in degrees / second.";
            ToolStripMenuItem extLinOpt = Menu_AppendItem(menu, "Specify External Linear Speed", extLin_Click, true, m_ExtLin);
            extLinOpt.ToolTipText = "Specify the external linear axis speed in mm / second.";
            ToolStripMenuItem extRotOpt = Menu_AppendItem(menu, "Specify External Rotary Speed", extRot_Click, true, m_ExtRot);
            extRotOpt.ToolTipText = "Specify the external rotary axis speed in degrees / second.";

            ToolStripSeparator seperator = new ToolStripSeparator();

            ToolStripMenuItem timeOption = Menu_AppendItem(menu, "Define Movement Time", time_Click, true, m_Time);
            timeOption.ToolTipText = "Specify the movement time in seconds and override the other values.";
            ToolStripMenuItem nameOption = Menu_AppendItem(menu, "Add Custom Name", name_Click, true, m_Name);
            nameOption.ToolTipText = "Define a custom name for each target for declaration purposes.";

            ToolStripSeparator seperator2 = new ToolStripSeparator();

            ToolStripMenuItem declarationCheck = Menu_AppendItem(menu, "Output Declarations", declaration_Click, true, m_Declaration);
            declarationCheck.ToolTipText = "Output the formatted speed declaration.";
        }

        private void rot_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("RotationOption");
            m_Rotation = !m_Rotation;
            
            if (m_Rotation)
            {
                AddInput(0);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Rotation"), true);
            }
            ExpireSolution(true);
        }

        private void extLin_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ExtLinOption");
            m_ExtLin = !m_ExtLin;

            if (m_ExtLin)
            {
                AddInput(1);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "ExtLin"), true);
            }
            ExpireSolution(true);
        }

        private void extRot_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ExtRotOption");
            m_ExtRot = !m_ExtRot;

            if (m_ExtRot)
            {
                AddInput(2);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "ExtRot"), true);
            }
            ExpireSolution(true);
        }

        private void time_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("TimeOption");
            m_Time = !m_Time;
            
            if (m_Time)
            {
                AddInput(3);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Time"), true);
            }
            ExpireSolution(true);
        }

        private void name_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("NameOption");
            m_Name = !m_Name;

            if (m_Name)
            {
                AddInput(4);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Name"), true);
            }
            ExpireSolution(true);
        }

        private void declaration_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("DeclOption");
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

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("UseRotation", this.m_Rotation);
            writer.SetBoolean("UseExtLin", this.m_ExtLin);
            writer.SetBoolean("UseExtRot", this.m_ExtRot);
            writer.SetBoolean("UseTime", this.m_Time);
            writer.SetBoolean("UseName", this.m_Name);
            writer.SetBoolean("OutputDec", this.m_Declaration);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.m_Rotation = reader.GetBoolean("UseRotation");
            this.m_ExtLin = reader.GetBoolean("UseExtLin");
            this.m_ExtRot = reader.GetBoolean("UseExtRot");
            this.m_Time = reader.GetBoolean("UseTime");
            this.m_Name = reader.GetBoolean("UseName");
            this.m_Declaration = reader.GetBoolean("OutputDec");
            return base.Read(reader);
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