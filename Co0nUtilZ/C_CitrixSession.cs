using System;
using System.Collections;
using System.Collections.Generic;
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
        /// Finds the name of the client from a citrix-session:
        /// A citrix-host stores information about every client session under specific registry hive...
        /// The idea is to parse the key ("HKEY_LOCAL_MACHINE\SOFTWARE\Citrix\Ica\Session\") and iterate through all session-IDs (e.g.: HKEY_LOCAL_MACHINE\SOFTWARE\Citrix\Ica\Session\8\Connection)
        /// There we'll look for a "UserName"-Value which matches the the currentuser. If found, we look for the "ClientName"-Value which tells us the current session's client-name.
        /// </summary>
        /// <param name="currentuser">the username whose youngest session should be searched.</param>
        /// <param path="path">the registry-path to parse</param>
        /// <returns>The clientname of the current citrix-session or an empty string, if it can't be found.</returns>
        public static string findCitrixSessionClient(string currentuser, C_RegistryHelper myreghelper)
        {
            string myreturn = "";
            
            ArrayList citrix_sessions = new ArrayList();
            ArrayList userlogons = new ArrayList(); //this will contain all logons            

            try
            {
                //C_RegistryHelper myreghelper = new C_RegistryHelper(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), path);
                citrix_sessions.AddRange(myreghelper.getInstances());

                foreach (string session in citrix_sessions)
                {
                    if (int.TryParse(session, out _))
                    {
                        //If the session name is numeric, get all sub element-keys

                        string subpath = myreghelper.subkey + @"\" + session;
                        ArrayList moresubkeys = new ArrayList();

                        moresubkeys.AddRange(myreghelper.getInstances(subpath));

                        string searchedsubkey = "Connection";  //The session-subkey we're searching
                        foreach (string sessionsub in moresubkeys)
                        {
                            if (sessionsub.Equals(searchedsubkey))
                            {
                                //Sessionsubkey "Connection" has been found 
                                string settingssubkey = session + @"\" + searchedsubkey;

                                List<string> allvalues = myreghelper.ListValues(settingssubkey);
                                //Find the UserName-Value, if it matches the current user: note it along with the UserLogonTime into userlogons
                                if (allvalues.Contains("UserName") && allvalues.Contains("UserLogonTime") && allvalues.Contains("ClientName") && allvalues.Contains("AppStateChangeTime"))
                                { //All values we need had been found
                                    C_CitrixSession newsession = new C_CitrixSession(
                                            myreghelper.ReadSettingFromRegistry("", "UserName", myreghelper.subkey + @"\" + settingssubkey),
                                            myreghelper.ReadSettingFromRegistry("", "ClientName", myreghelper.subkey + @"\" + settingssubkey),
                                            myreghelper.ReadSettingFromRegistry("", "UserLogonTime", myreghelper.subkey + @"\" + settingssubkey),
                                            myreghelper.ReadSettingFromRegistry("", "AppStateChangeTime", myreghelper.subkey + @"\" + settingssubkey)
                                            );
                                    //this._myLogger.LogInfo("Found valid citrix-session information: => " + newsession.ToString());
                                    userlogons.Add(newsession);
                                }
                            }
                        }
                    }
                }

                /* Finding the youngest session
                    * by iterating through all session we found and comparing the UserlogonsDate's
                    * we are looking for the newewst logon
                    */
                C_CitrixSession oursession = null;                
                foreach (C_CitrixSession logon in userlogons)
                {
                    if (logon.UserName.ToLower().Equals(currentuser.ToLower()))
                    { //if the session's-user matches the current user's username                        
                        if (null == oursession)
                        {
                            //oursession ist not set yet
                            oursession = logon; //Set to the first we got
                            continue; //proceed with the next item
                        }

                        int compareresult = DateTime.Compare(logon.UserLogonTime, oursession.UserLogonTime);
                        if (compareresult > 0)
                        {
                            //Current session is younger than the one we found before...
                            oursession = logon; // set oursession to the current session
                        }
                    }
                }

                if (null == oursession) // if oursession has not been set...
                {                    
                }
                else
                {
                    myreturn = oursession.ClientName;  // The clientname of the youngest session, that has the current-username should be the correct one.                    
                }

            }
            catch (Exception ex)
            { //log any error                
            }

        return myreturn;
    }

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
