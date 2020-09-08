using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

using Rhino.Geometry;

namespace Axis.Geometry
{
    /// <summary>
    /// Get the quaternion description of a plane rotation.
    /// </summary>
    public class PlaneToQuat_Obsolete : GH_Component, IGH_VariableParameterComponent
    {
        bool originOut = false;

        public PlaneToQuat_Obsolete() : base("Plane To Quaternion", "P-Q", "Convert a geometric plane to a quaternion and a point.", AxisInfo.Plugin, AxisInfo.TabGeometry)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "Plane", "Output plane.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Quaternion", "Quat", "Rotation as four-component quaternion string.", GH_ParamAccess.list);
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane inPlane = new Plane(Plane.WorldXY);

            if (!DA.GetData(0, ref inPlane)) return;

            Point3d origin = new Point3d();
            List<double> quatComps = new List<double>();

            Quaternion quat = Util.QuaternionFromPlane(inPlane);

            // Add each of the four components of the quaternion to a list.
            quatComps.Add(quat.A);
            quatComps.Add(quat.B);
            quatComps.Add(quat.C);
            quatComps.Add(quat.D);

            DA.SetDataList(0, quatComps);

            if (originOut)
                DA.SetData("Origin", origin);
        }

        #region UI
        IGH_Param[] parameters = new IGH_Param[1]
        {
        new Param_Point() { Name = "Origin", NickName = "Origin", Description = "Location of the plane origin as a point." },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Output Origin", originClick, true, originOut);
        }

        private void originClick(object sender, EventArgs e)
        {
            RecordUndoEvent("OriginOut");
            originOut = !originOut;

            // If incremental naming is enabled, add a reset input parameter.
            if (originOut)
            {
                AddParam(0);
            }
            else
            {
                Params.UnregisterOutputParameter(Params.Output.FirstOrDefault(x => x.Name == "Origin"), true);
            }

            ExpireSolution(true);
        }

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("OriginOut", this.originOut);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.originOut = reader.GetBoolean("OriginOut");
            return base.Read(reader);
        }

        // Register the new output parameters to our component.
        private void AddParam(int index)
        {
            IGH_Param parameter = parameters[index];

            if (Params.Output.Any(x => x.Name == parameter.Name))
                Params.UnregisterOutputParameter(Params.Output.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Output.Count;
                for (int i = 0; i < Params.Output.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Output[i].Name);
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

        #region Component Settings
        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        public override bool Obsolete => true;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.Robot;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("194c9305-457d-47b1-9a60-8cf3b31ea6a5"); }
        }
        #endregion
    }
}