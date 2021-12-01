using System;
using System.Collections;
using System.Collections.Generic;

namespace Co0nUtilZ.Base_Classes
{

    public class SessionInfo
    {
        public string UserName;
        public string Domain;
        public int SessionId;
        public string Client;
        public string Server;
        public WTS_CONNECTSTATE_CLASS ConnectionState;
        public WTSINFOA sessionInfo;

        public override string ToString()
        {
            return /* string myreturn = */ this.UserName +
                " (ID: " + this.SessionId.ToString() + ") " +
                this.ConnectionState.ToString();//+ 
                                                //" => Times: Now=" + this.sessionInfo.CurrentTime.ToString() + 
                                                //", Lastinput= " + this.sessionInfo.LastInputTime.ToString() + 
                                                //", IdleMinutes=" + this.sessionInfo.IdleTime.TotalMinutes;
                                                //return myreturn;
        }
    }

    /// <summary>
    /// This Class represents a Windows-User-Session
    /// </summary>
    public abstract class C_WinSessionHelper_Base
    {
        /// <summary>
        /// Gets existing user-sessions (internally used)
        /// </summary>
        /// <param name="onlydisconnected">only return sessions, which are disconnected at the moment (Default:true)</param>
        /// <param name="server">the server from which sessions should be enumerated (Default: localhost)</param>
        /// <returns></returns>
        protected abstract List<SessionInfo> FetchSessions(bool onlydisconnected, string server);

        /// <summary>
        /// List disconnected user sessions
        /// </summary>
        /// <returns>An Arraylist containing all active, disconnected sessions</returns>
        public abstract ArrayList getDisconnectedSessions();

        /// <summary>
        /// List all user sessions (active and disconnected)
        /// </summary>
        /// <returns>An Arraylist containing all active sessions</returns>
        public abstract ArrayList getAllSessions();

        /// <summary>
        /// Finds the currently active session of a given user.
        /// </summary>
        /// <param name="username"></param>
        /// <returns>SessionInfo on success, otherwise NULL</returns>
        public abstract SessionInfo findCurrentSession(string username);

        /// <summary>
        /// Terminates an existing session
        /// </summary>
        /// <param name="sess">the session to be terminated</param>
        /// <returns>True on success, otherwise false</returns>
        public abstract bool endSession(KeyValuePair<int, String> sess, String ServerName);

        public abstract bool endSession(SessionInfo sessionInfo);
    }
}
