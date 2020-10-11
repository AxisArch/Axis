using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Kernal.UIElements
{
    public sealed class ComponentButton : IComponentUiElement, IButton
    {
        public string Name { get; set ; }

        /* @todo Make EventHandler init after .Net version upgrade
         * @body Actions should only be specifies at initialisation and not change
         */
        public EventHandler LeftClickAction { get; set; } = null;
        public RectangleF Bounds { get; set; }
        public UIElementType Type => UIElementType.ComponentButton;

        public ComponentButton(string name) 
        {
            this.Name = name;
        }
    }
    public sealed class ComponentToggle : IComponentUiElement, IToggle
    {
        public string Name { get; set; }

        /* @todo Make EventHandler init after .Net version upgrade
         * @body Actions should only be specifies at initialisation and not change
         */
        public EventHandler LeftClickAction { get; set; } = null;
        public Tuple<string, string> Toggle { get; set; } = null;
        public bool State { get; set; } = false;
         
        public RectangleF Bounds { get; set; }
        public UIElementType Type => UIElementType.ComponentButton;


        public ComponentToggle(string name)
        {
            this.Name = name;
        }
    }


}
