using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;

using ABB.Robotics;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;

namespace Axis.Online
{
    public class SetModule : GH_Component, IGH_VariableParameterComponent
    {
        public List<string> Status { get; set; }
        //public Controller controller = null;
        private Task[] tasks = null;

        private bool send = false;
        private bool logOption = false;
        private bool logOptionOut = false;
        private bool sending = false;

        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();

        public SetModule() : base("Set Module", "Set Mod", "Set the main module on the robot controller", "Axis", "9. Online")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Robot controller to send to. Use the controller component to find network controllers.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Send", "Send", "Send the module.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Module", "Module", "Module to be written to the controller.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper controller = new GH_ObjectWrapper();
            List<string> modFile = new List<string>();
            Controller abbController = null;
            bool clear = false;

            if (!DA.GetData("Controller", ref controller)) ;
            if (!DA.GetData("Send", ref send)) ;
            if (!DA.GetDataList("Module", modFile)) { return; }
            if (logOption)
            {
                if (!DA.GetData("Clear", ref clear)) ;
            }

            //Check for valid input, else top execuion
            AxisController myAxisController = controller.Value as AxisController;
            if ((myAxisController != null) && (myAxisController.axisControllerState == true))
            {
                abbController = myAxisController;
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No active controller connected."); return; }

            if ((abbController != null) && send)
            {

                var filename = "MyModule";
                var tempFile = Path.GetTempPath() + @"\" + filename + ".mod";

                using (StreamWriter writer = new StreamWriter(tempFile, false))
                {
                    for (int i = 0; i < modFile.Count; i++)
                    {
                        writer.WriteLine(modFile[i]);
                    }
                }

                //Not working perfectly yet
                if (sending == false)
                {
                    sending = true;
                    log.Add("Sending module to controller.");
                    try
                    {
                        using (Mastership m = Mastership.Request(abbController.Rapid))
                        {
                            if (abbController.IsVirtual)
                            {
                                // Load program to virtual controller
                                tasks = abbController.Rapid.GetTasks();
                                tasks[0].LoadModuleFromFile(tempFile, RapidLoadMode.Replace);

                                if (File.Exists(tempFile)) { File.Delete(tempFile); }
                                log.Add("Program loaded to virtual controller.");
                                sending = false;
                            }
                            else
                            {
                                // Load program to physical controller
                                tasks = abbController.Rapid.GetTasks();

                                // Missing Check if file and directory exist
                                if (abbController.FileSystem.DirectoryExists(@"Axis"))
                                {
                                    if (abbController.FileSystem.FileExists(@"Axis/AxisModule.mod"))
                                    {
                                        abbController.FileSystem.RemoveFile(@"Axis/AxisModule.mod");
                                    }
                                }
                                else { abbController.FileSystem.CreateDirectory(@"Axis"); }

                                // Delete all previouse tasks
                                for (int i = 0; i < tasks.Length; ++i) { tasks[i].DeleteProgram(); }

                                // Load module 
                                abbController.FileSystem.PutFile(tempFile, @"Axis/AxisModule.mod");
                                var success = tasks[0].LoadModuleFromFile(@"Axis/AxisModule.mod", RapidLoadMode.Replace);

                                if (success) { log.Add("Program loaded to robot controller."); }
                                else { log.Add("The program contains errors and cannot be loaded."); }

                                if (File.Exists(tempFile)) { File.Delete(tempFile); }
                                sending = false;
                            }
                        }
                    }
                    catch (Exception e) // If we run into any problems writing to the controller.
                    {
                        log.Add("Can't write to controller.");
                        log.Add(e.ToString());
                        sending = false;
                        if (File.Exists(tempFile)) { File.Delete(tempFile); }; return;
                    }
                }
            }

            if (clear)
            {
                log.Clear();
                log.Add("Log cleared.");
            }

            if (logOptionOut)
            {
                Status = log;
                DA.SetDataList("Log", log);
            }
        }

        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[1]
        {
            new Param_Boolean() { Name = "Clear", NickName = "Clear", Description = "Clear the communication log.", Access = GH_ParamAccess.item, Optional = true},
        };

        // Build a list of optional output parameters
        IGH_Param[] outputParams = new IGH_Param[1]
        {
        new Param_String() { Name = "Log", NickName = "Log", Description = "Log checking the connection status"},
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem log = Menu_AppendItem(menu, "Log", log_Click, true, logOption);
            log.ToolTipText = "Activate the log output";

            //ToolStripSeparator seperator = Menu_AppendSeparator(menu);
        }
        private void log_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Log");
            logOption = !logOption;

            if (logOption)
            {
                AddInput(0);
                AddOutput(0);
                logOptionOut = true;
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Clear"), true);
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Log"), true);
                logOptionOut = false;
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
            writer.SetBoolean("LogOptionSetModule", this.logOption);
            writer.SetBoolean("LogOptionSetOutModule", this.logOptionOut);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.logOption = reader.GetBoolean("LogOptionSetModule");
            this.logOptionOut = reader.GetBoolean("LogOptionSetOutModule");
            return base.Read(reader);
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Set_Module;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("676a28f1-9320-4a02-a9bc-59c617dd04d0"); }
        }
    }
}