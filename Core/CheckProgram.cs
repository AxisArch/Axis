using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;
using Axis.Robot;
using Axis.Targets;
using Axis.Core;

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

        public CheckProgram() : base("Check Program", "Check", "Check an entire program for kinematic errors.", "Axis", "1. Core")
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

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize variables to store the incoming data.
            List<GH_ObjectWrapper> program = new List<GH_ObjectWrapper>();
            Plane target = Plane.WorldXY;
            Manipulator robot = null;
            List<double> angles = new List<double>();
            List<Plane> planes = new List<Plane>();

            int singularityTol = 5;
            bool run = false;
            int counter = 0;

            // Get the data.
            if (!DA.GetDataList(0, program)) return;
            if (!DA.GetData(1, ref robot)) return;
            DA.GetData(2, ref run);

            List<string> log = new List<string>();
            Point3d errorPos = new Point3d();

            // Get the list of kinematic indices from the robot definition, otherwise use default values.
            List<int> indices = new List<int>() { 2, 2, 2, 2, 2, 2 };
            if (robot.Indices.Count == 6) indices = robot.Indices;

            List<Point3d> errorPositions = new List<Point3d>();

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
                    target.Transform(robot.InverseRemap);
                    errorPos = target.Origin;

                    Transform xForm = Transform.PlaneToPlane(Plane.WorldXY, target);

                    Plane tempTarg = Plane.WorldXY;
                    tempTarg.Transform(targ.Tool.FlangeOffset);
                    tempTarg.Transform(xForm);
                    target = tempTarg;

                    // Check the movement type.
                    mType = targ.Method;

                    List<string> msg = new List<string>();
                    if (mType != MotionType.NoMovement)
                    {
                        if (mType == MotionType.Linear || mType == MotionType.Joint)
                        {
                            List<string> info = new List<string>();
                            List<List<double>> results = InverseKinematics.TargetInverseKinematics(robot, target, out info);
                            foreach (string s in info)
                                msg.Add("[" + counter.ToString() + "]" + s);
                        }
                        else if (mType == MotionType.AbsoluteJoint)
                        {
                            // Grab the joint angles from the joint target instead of using IK.
                            angles = targ.JointAngles;

                            List<string> info = new List<string>();
                            for (int i = 0; i < 6; i++)
                            {
                                // Check if the solution value is inside the manufacturer permitted range
                                if (angles[i] > robot.MaxAngles[i] || angles[i] < robot.MinAngles[i])
                                {
                                    int axis = i + 1;
                                    info.Add("Axis " + axis.ToString() + " is out of rotation domain");
                                    break;
                                }
                            }
                            foreach (string s in info)
                                msg.Add("[" + counter.ToString() + "]" + s);
                        }
                        counter++; // Update the position in the program.
                    }
                    log.AddRange(msg);
                }
            }
            else
            {
                this.Message = "Disabled";
                log.Add("Program check not active.");
            }

            if (log.Count == 0)
            {
                log.Add("No kinematic errors were found in program.");
                this.Message = "No Errors";
            }
            else { this.Message = "Check Log"; }

            DA.SetDataList(0, log);
            DA.SetDataList(1, errorPositions);
        }
    }
}