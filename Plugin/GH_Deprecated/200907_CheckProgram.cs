using Axis.Types;
using Axis.Kernal;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Axis.GH_Components
{
    /// <summary>
    /// Method to check an entire program for issues
    /// without running full geometry for the IK solutions.
    /// </summary>
    public class CheckProgram_Obsolete : Kernal.AxisLogin_Component
    {
        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public CheckProgram_Obsolete() : base("Check Program", "Check", "Check an entire program for kinematic errors.", AxisInfo.Plugin, AxisInfo.TabDepricated)
        {
        }

        #region IO

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Program", "Program", "Robot program as list of commands.", GH_ParamAccess.list);
            IGH_Param robot = new Axis.GH_Params.RobotParam();
            pManager.AddParameter(robot, "Robot", "Robot", "Robot object to use for inverse kinematics. You can define this using the robot creator tool.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run a check of the entire program.", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Message log.", GH_ParamAccess.list);
            pManager.AddPointParameter("Error", "Errors", "List of program error points.", GH_ParamAccess.list);
        }

        #endregion IO

        protected override void SolveInternal(IGH_DataAccess DA)
        {
            // Initialize variables to store the incoming data.
            List<GH_ObjectWrapper> program = new List<GH_ObjectWrapper>();
            Plane target = Plane.WorldXY;
            Robot robot = null;

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

            if (run)
            {
                foreach (GH_ObjectWrapper command in program)
                {
                    Target targ = null;

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

                    // Compute kinematic solutions.
                    bool overheadSing = false; bool outOfReach = false; bool wristSing = false; bool outOfRotation = false;
                    List<System.Drawing.Color> colors = new List<System.Drawing.Color>();

                    if (targ.Method != MotionType.NoMovement)
                    {
                        if (targ.Method == MotionType.Linear || targ.Method == MotionType.Joint)
                        {
                            Axis.Types.Abb6DOFRobot.ManipulatorPose pose = new Axis.Types.Abb6DOFRobot.ManipulatorPose(robot, targ);

                            outOfReach = pose.OutOfReach; overheadSing = pose.OverHeadSig; wristSing = pose.WristSing; outOfRotation = pose.OutOfRoation;

                            if (overheadSing) log.Add(counter.ToString() + ": Singularity");
                            if (outOfReach) log.Add(counter.ToString() + ": Unreachable");
                            if (wristSing) log.Add(counter.ToString() + ": Wrist Singularity");
                            if (outOfRotation) log.Add(counter.ToString() + ": Joint Error");
                            if (overheadSing || outOfReach || wristSing || outOfRotation) errorPositions.Add(errorPos);
                        }
                        else if (targ.Method == MotionType.AbsoluteJoint)
                        {
                            Abb6DOFRobot.ManipulatorPose pose = new Abb6DOFRobot.ManipulatorPose(robot, targ);
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

        #region Component Settings

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Icons.CheckProgram;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("73f9ed66-9a0d-48aa-afdc-9a7a8902031c"); }
        }

        #endregion Component Settings

    }
}