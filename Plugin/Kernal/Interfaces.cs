using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Drawing;

namespace Axis.Kernal
{
    internal interface Axis_IDisplayable
    {
        void DrawViewportWires(IGH_PreviewArgs args);

        void DrawViewportMeshes(IGH_PreviewArgs args);
    }
    internal interface Axis_IBakeable
    {
        void BakeGeometry(Rhino.RhinoDoc doc, System.Collections.Generic.List<System.Guid> obj_ids);

        void BakeGeometry(Rhino.RhinoDoc doc, Rhino.DocObjects.ObjectAttributes att, System.Collections.Generic.List<System.Guid> obj_ids);
    }

    public interface IComponentUiElement
    {
        string Name { get; set; }
        RectangleF Bounds { get; set; }
        UIElementType Type { get; }
        EventHandler LeftClickAction { get; }


    }

    public interface IButton 
    {
        string Name { get; set; }
        EventHandler LeftClickAction { get; set; }
    }
    public interface IToggle 
    {
        string Name { get; set; }
        bool State { get; set; }
        Tuple<string, string> Toggle { get; set; }
        EventHandler LeftClickAction { get; set; }
    }
}