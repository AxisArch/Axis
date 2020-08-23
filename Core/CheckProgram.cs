using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;
using Axis.Robot;
using Axis.Targets;
using Axis.Core;
using static Axis.Properties.Settings;

namespace Axis.Core
{
    public class CheckProgram : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.CheckProgram;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("73f9ed66-9a0d-48aa-afdc-9a7a8902031c"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        public CheckProgram() : base("Check Program", "Check", "Check an entire program for kinematic errors.", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Program", "Program", "Robot program as list of commands.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run a check of the entire program.", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
            pManager.AddPointParameter("Error", "Errors", "List of program error points.", GH_ParamAccess.list);
        }

        // License token variable.
        bool validToken = false;

        protected override void BeforeSolveInstance()
        {
            Auth auth = new Auth();
            validToken = auth.IsValid;

            if (!validToken)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please log in to Axis.");
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize variables to store the incoming data.
            List<GH_ObjectWrapper> program = new List<GH_ObjectWrapper>();
            Plane target = Plane.WorldXY;
            Manipulator robot = null;

            //List<double> angles = new List<double>();
            //List<Plane> planes = new List<Plane>();

            //int singularityTol = 5;
            bool run = false;
            int counter = 0;

            // Get the data.
            if (!DA.GetDataList(0, program)) return;
            if (!DA.GetData(1, ref robot)) return;
            DA.GetData(2, ref run);

            List<string> log = new List<string>();
            Point3d errorPos = new Point3d();

            // Get the list of kinematic indices from the robot definition, otherwise use default values.
            List<int> indices = new List<int>() { 0, 0, 0, 0, 0, 0 };
            if (robot.Indices.Count == 6) indices = robot.Indices;

            List<Point3d> errorPositions = new List<Point3d>();

            // Exit if we don't have a valid login token.
            if (!validToken) { return; }

            if (run)
            {
                foreach (GH_ObjectWrapper command in program)
                {
                    Target targ = null;
                    MotionType mType = MotionType.Linear;

                    // Retrieve the current target and its type from the program.
                    GH_ObjectWrapper currTarg = command;
                    Type cType = currTarg.Value.GetType();

                    if (cType.Name == "GH_String")
                        continue;
                    else
                        targ = currTarg.Value as Target; // Cast back to target type

                    // Transform the robot target from the base plane to the XY plane.
                    target = new Plane(targ.Plane);
                    errorPos = target.Origin;
                    


                    // Kinematics
                    bool overheadSing = false; bool outOfReach = false; bool wristSing = false; bool outOfRotation = false;
                    List<System.Drawing.Color> colors = new List<System.Drawing.Color>();

                    if (targ.Method != MotionType.NoMovement)
                    {
                        if (targ.Method == MotionType.Linear || targ.Method == MotionType.Joint)
                        {
                            Axis.Core.Manipulator.ManipulatorPose pose = new Axis.Core.Manipulator.ManipulatorPose(robot, targ);

                            outOfReach = pose.OutOfReach; overheadSing = pose.OverHeadSig; wristSing = pose.WristSing; outOfRotation = pose.OutOfRoation;

                            if (overheadSing) log.Add(counter.ToString() + ": Singularity");
                            if (outOfReach) log.Add(counter.ToString() + ": Unreachable");
                            if (wristSing) log.Add(counter.ToString() + ": Wrist Singularity");
                            if (outOfRotation) log.Add(counter.ToString() + ": Joint Error");
                            if (overheadSing || outOfReach || wristSing || outOfRotation) errorPositions.Add(errorPos);
                        }
                        else if (targ.Method == MotionType.AbsoluteJoint)
                        {
                            Axis.Core.Manipulator.ManipulatorPose pose = new Axis.Core.Manipulator.ManipulatorPose(robot, targ);
                            outOfReach = pose.OutOfReach; overheadSing = pose.OverHeadSig; wristSing = pose.WristSing; outOfRotation = pose.OutOfRoation;

                            if (wristSing) log.Add(counter.ToString() + ": Wrist Singularity");
                            if (outOfRotation) log.Add(counter.ToString() + ": Joint Error");
                        }
                        counter++; // Update the position in the program.
                    }
                }
            }
            else this.Message = "Disabled";

            // Component messages
            if (log.Count == 0) this.Message = "No Errors";
            else this.Message = "Errors";

            DA.SetDataList(0, log);
            DA.SetDataList(1, errorPositions);
        }
    }
}