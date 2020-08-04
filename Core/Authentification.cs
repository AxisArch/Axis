using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Axis.Properties.Settings;

namespace Axis.Core
{
    /// <summary>
    /// General class to manage the authentification
    /// checking from within Axis itself.
    /// </summary>
    class Auth
    {
        public bool IsValid { get; set; }
        public DateTime Updated { get; set; }

        /// <summary>
        /// Calls the actual update and
        /// updates the timestamp.
        /// </summary>
        public Auth()
        {
            this.IsValid = AuthCheck();
            this.Updated = DateTime.Now;
        }

        /// <summary>
        /// Check to see if the user has
        /// logged into the authentification service
        /// within the past few days.
        /// </summary>
        public bool AuthCheck()
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
    }
}