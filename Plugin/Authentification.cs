using System;
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
            modifyEvent?.Invoke(OnLoginStateChange.Target, new LoginEnventArgs(r));
        }

        public class LoginEnventArgs : EventArgs
        {
            public bool LogedIn { get; private set; }

            public LoginEnventArgs(bool result)
            {
                LogedIn = result;
            }
        }
    }


}