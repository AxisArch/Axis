using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.Drawing;

namespace Axis.Kernal
{
    /// <summary>
    /// Interface ensuring types have the right propperies to be displayed inside the other componets
    /// This should help enforce consistency throughout the plugin.
    /// </summary>
    internal interface Axis_Displayable<T> : IGH_GeometricGoo where T : Rhino.Runtime.CommonObject
    {
        T[] Geometries { get; }
        Color[] Colors { get; }
    }

    internal interface Axis_Displayable
    {
        void DrawViewportWires(IGH_PreviewArgs args);

        void DrawViewportMeshes(IGH_PreviewArgs args);
    }
}