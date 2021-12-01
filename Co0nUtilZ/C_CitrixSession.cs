using System;
using System.Globalization;

namespace Co0nUtilZ
{
    /// <summary>
    /// Holds information about a Citrix-Session
    /// </summary>
    public class C_CitrixSession //: IComparable
    {
        private string _username, _clientname;
        protected DateTime _userlogontime, _appstatechangetime;
        protected string _citrixdateformat = "yyyy,MM,dd,H,mm,ss"; //Citrix-SessionLogon-Datenformat -> example: 2021,10,21,13,41,23

        /// <summary>
        /// Creates a new instance of an object which provides information about a citrix-session
        /// </summary>
        /// <param name="UserName">Username</param>
        /// <param name="ClientName">name of the client-computer</param>
        /// <param name="UserLogonTime">logon time in the citrix-format ('yyyy,MM,dd,h,m,s') </param>
        public C_CitrixSession(string UserName, string ClientName, string UserLogonTime, string AppStateChangeTime)
        {
            this._username = UserName;
            this._clientname = ClientName;
            this._userlogontime = this.parselogontime(UserLogonTime);
            this._appstatechangetime = this.parselogontime(AppStateChangeTime);
        }


        /// <summary>
        /// Tries to parse a string with a citrix-timestamp to a datetime-object.
        /// </summary>
        /// <param name="citrixtimestamp">the citrix-formatted ('yyyy,MM,dd,h,m,s') timestamp </param>
        /// <returns>On Success, a Datetime-object representing an the citrix-timestamp. On Failure the current timestamp.</returns>
        protected DateTime parselogontime(string citrixtimestamp)
        {

            DateTime converted = DateTime.ParseExact(
                DateTime.Now.ToString(this._citrixdateformat), this._citrixdateformat, new CultureInfo("de-DE")
                );
            try
            {
                converted = DateTime.ParseExact(citrixtimestamp, this._citrixdateformat, new CultureInfo("de-DE"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(citrixtimestamp + " => " + ex.ToString());
            }

            return converted;

        }

        /*
        public int CompareTo(object obj)
        {
            return UserLogonTime.CompareTo(obj);
        }
        */

        public string UserName
        {
            get
            {
                return this._username;
            }
        }

        public string ClientName
        {
            get
            {
                return this._clientname;
            }
        }

        public DateTime UserLogonTime
        {
            get
            {
                return this._userlogontime;
            }
        }

        public DateTime AppStateChangeTime
        {
            get
            {
                return this._appstatechangetime;
            }
        }

        public override string ToString()
        {
            return this._username + ", " + this._clientname + ", " + this._userlogontime.ToString() + ", " + this._appstatechangetime;
        }

        /*
        // https://docs.microsoft.com/en-us/troubleshoot/dotnet/csharp/use-icomparable-icomparer
        //Implement IComparable CompareTo method - provide default sort order.
        int IComparable.CompareTo(object obj)  //Compares to UserLogonTime
        {
            C_CitrixSession s = (C_CitrixSession)obj;

            int result = DateTime.Compare(this.UserLogonTime, s.UserLogonTime);


            string relationship;
            if (result < 0)
                relationship = "is earlier than";
            else if (result == 0)
                relationship = "is the same time as";
            else
                relationship = "is later than";

            Console.WriteLine("{0} {1} {2}", this.UserLogonTime, relationship, s.UserLogonTime);

            return result;
        }
        */
    }
}
