using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Axis.Properties.Settings;

namespace Axis
{
    /// <summary>
    /// General class to manage the authentification
    /// checking from within Axis itself.
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// Check to see if the user has
        /// logged into the authentification service
        /// within the past few days.
        /// </summary>
        static public bool AuthCheck()
        {
            if (Default.LoggedIn)
            {
                // Check that its still valid.
                DateTime validTo = Default.LastLoggedIn.AddDays(2);
                int valid = DateTime.Compare(System.DateTime.Now, validTo);
                if (valid < 0) { return true; }
                else return false;
            }
            else
            {
                return false;
            }
        }

        public static EventHandler<LoginEnventArgs> OnLoginStateChange;

        public static void RaiseEvent(bool r)
        {
            EventHandler<LoginEnventArgs> modifyEvent = OnLoginStateChange;
            var t = modifyEvent.GetInvocationList();
            modifyEvent?.Invoke(OnLoginStateChange.Target, new LoginEnventArgs(r));
        }

        public class LoginEnventArgs : EventArgs
        {
            public bool LogedIn { get; private set; }
            public LoginEnventArgs(bool result) { LogedIn = result; }
        }
    }


    public abstract class AxisLogin_Component : GH_Component
    {
        bool IsTokenValid { get; set; }
        string WarningMessage = "Please log in to Axis.";

        public AxisLogin_Component(string name, string nickname, string discription, string plugin, string tab) : base(name, nickname, discription, plugin, tab) 
        {
            //IsTokenValid = Auth.AuthCheck();
        }

        protected void UpdateToken(object sender, Axis.Auth.LoginEnventArgs e)
        {

            var component = this;

            //component.IsTokenValid = e.LogedIn;

            var doc = component.OnPingDocument();
            if (doc != null) doc.ScheduleSolution(10, ExpireComponent);
                

            void ExpireComponent(GH_Document document)
            {
                component.ExpireSolution(false);
            }
        }


        protected override  void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();

            Auth.OnLoginStateChange += UpdateToken;

            if (!Properties.Settings.Default.LoggedIn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, WarningMessage);
            }
            else 
            {
                ClearRuntimeMessages();
            }
        }

        protected override sealed void SolveInstance(IGH_DataAccess da)
        {
            if (!Properties.Settings.Default.LoggedIn)
                return;

            SolveInternal(da);
        }
        protected abstract void SolveInternal(IGH_DataAccess da);


        public override void ClearData()
        {
            Auth.OnLoginStateChange -= UpdateToken;
            base.ClearData();
        }

    }

}
