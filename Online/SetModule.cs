using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;


using Rhino.Geometry;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.Messaging;
using ABB.Robotics.Controllers.IOSystemDomain;

using Axis.Targets;

namespace Axis.Online
{
    public class SetModule : GH_Component
    {
        public string ControllerID { get; set; }
        public List<string> Status { get; set; }
        public Controller controller = null;
        private Task[] tasks = null;
        public IpcQueue RobotQueue { get; set; }

        //Copied
        private bool scan;
        private bool kill;
        private bool connect;
        private int controllerIndex;
        private bool start;
        private bool stream;
        private string command;

        //New 
        private bool logOption;


        NetworkScanner scanner = new NetworkScanner();
        ControllerInfo[] controllers = null;


        // Create a list of string to store a log of the connection status.
        private List<string> log = new List<string>();


        /// <summary>
        /// Initializes a new instance of the WriteModuleToControler class.
        /// </summary>
        public SetModule()
          : base("Set Module", "Set Mod",
              "Set the main module on the robot controller",
              "Axis", "9. Online")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Controller", "Controller", "Recives the output from a controller module", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Send", "Send", "Send to module", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Moduel", "Module", "Module to be wtritten to the controller.", GH_ParamAccess.list);

            for (int i = 0; i < 2; i++)
            {
                pManager[i].Optional = true;
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool activate = false;

            if (!DA.GetData("Controller", ref activate)) ;
            if (!DA.GetData("Send", ref activate)) ;
            if (!DA.GetData("Moduel", ref activate)) ;


            if (activate)
            {
                if (logOption)
                {
                    Status = log;
                    DA.SetDataList("Log", log);
                }

                ExpireSolution(true);
            }
            
        }
        


        /// <summary>
        /// Additional Input and Output parameters for the component
        /// </summary>
        // Build a list of optional input parameters
        IGH_Param[] inputParams = new IGH_Param[0]
        {
            //new Param_Integer() { Name = "Method", NickName = "Method", Description = "A list of target interpolation types [0 = Linear, 1 = Joint]. If one value is supplied it will be applied to all targets.", Access = GH_ParamAccess.list },
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
                AddOutput(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Log"), true);
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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.Online;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("676a28f1-9320-4a02-a9bc-59c617dd04d0"); }
        }
    }
}