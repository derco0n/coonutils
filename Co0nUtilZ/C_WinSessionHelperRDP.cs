using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Co0nUtilZ.Base_Classes;

namespace Co0nUtilZ
{
    #region some_enums_and_structs

    public enum WTS_INFO_CLASS : int
    {
        WTSInitialProgram = 0,
        WTSApplicationName = 1,
        WTSWorkingDirectory = 2,
        WTSOEMId = 3,
        WTSSessionId = 4,
        WTSUserName = 5,
        WTSWinStationName = 6,
        WTSDomainName = 7,
        WTSConnectState = 8,
        WTSClientBuildNumber = 9,
        WTSClientName = 10,
        WTSClientDirectory = 11,
        WTSClientProductId = 12,
        WTSClientHardwareId = 13,
        WTSClientAddress = 14,
        WTSClientDisplay = 15,
        WTSClientProtocolType = 16,
        WTSIdleTime = 17,
        WTSLogonTime = 18,
        WTSIncomingBytes = 19,
        WTSOutgoingBytes = 20,
        WTSIncomingFrames = 21,
        WTSOutgoingFrames = 22,
        WTSClientInfo = 23,
        WTSSessionInfo = 24,
        WTSSessionInfoEx = 25,
        WTSConfigInfo = 26,
        WTSValidationInfo = 27,
        WTSSessionAddressV4 = 28,
        WTSIsRemoteSession = 29
    }

    public enum WTS_CONNECTSTATE_CLASS : int
    {
        #region Technet-Info
        /*
         * https://docs.microsoft.com/en-us/windows/win32/api/wtsapi32/ne-wtsapi32-wts_connectstate_class
         * 
         WTSActive
            A user is logged on to the WinStation. This state occurs when a user is signed in and actively connected to the device.
        WTSConnected
            The WinStation is connected to the client.
        WTSConnectQuery
            The WinStation is in the process of connecting to the client.
        WTSShadow
            The WinStation is shadowing another WinStation.
        WTSDisconnected
            The WinStation is active but the client is disconnected. This state occurs when a user is signed in but not actively connected to the device, such as when the user has chosen to exit to the lock screen.
        WTSIdle
            The WinStation is waiting for a client to connect.
        WTSListen
            The WinStation is listening for a connection. A listener session waits for requests for new client connections. No user is logged on a listener session. A listener session cannot be reset, shadowed, or changed to a regular client session.
        WTSReset
            The WinStation is being reset.
        WTSDown
            The WinStation is down due to an error.
        WTSInit
            The WinStation is initializing.         
         */
        #endregion

        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct WTSINFOA
    {
        public const int WINSTATIONNAME_LENGTH = 32;
        public const int DOMAIN_LENGTH = 17;
        public const int USERNAME_LENGTH = 20;
        public WTS_CONNECTSTATE_CLASS State;
        public int SessionId;
        public int IncomingBytes;
        public int OutgoingBytes;
        public int IncomingFrames;
        public int OutgoingFrames;
        public int IncomingCompressedBytes;
        public int OutgoingCompressedBytes;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = WINSTATIONNAME_LENGTH)]
        public byte[] WinStationNameRaw;
        public string WinStationName
        {
            get
            {
                return Encoding.ASCII.GetString(WinStationNameRaw).TrimEnd('\0');// +'\0';
            }
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DOMAIN_LENGTH)]
        public byte[] DomainRaw;
        public string Domain
        {
            get
            {
                return Encoding.ASCII.GetString(DomainRaw).TrimEnd('\0');// + '\0';
            }
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = USERNAME_LENGTH + 1)]
        public byte[] UserNameRaw;
        public string UserName
        {
            get
            {
                return Encoding.ASCII.GetString(UserNameRaw).TrimEnd('\0');// + '\0';
            }
        }
        public long ConnectTimeUTC;
        public DateTime ConnectTime
        {
            get
            {
                return DateTime.FromFileTimeUtc(ConnectTimeUTC);
            }
        }
        public long DisconnectTimeUTC;
        public DateTime DisconnectTime
        {
            get
            {
                return DateTime.FromFileTimeUtc(DisconnectTimeUTC);
            }
        }
        public long LastInputTimeUTC;
        public DateTime LastInputTime
        {
            get
            {
                return DateTime.FromFileTimeUtc(LastInputTimeUTC);
            }
        }
        public long LogonTimeUTC;
        public DateTime LogonTime
        {
            get
            {
                return DateTime.FromFileTimeUtc(LogonTimeUTC);
            }
        }
        public long CurrentTimeUTC;
        public DateTime CurrentTime
        {
            get
            {
                return DateTime.FromFileTimeUtc(CurrentTimeUTC);
            }
        }

        public TimeSpan IdleTime
        {
            get
            {
                return new TimeSpan(CurrentTimeUTC - LastInputTimeUTC);
            }
        }
    }
    #endregion

    /// <summary>
    /// This class will aid in enumerating and handling user sessions
    /// </summary>
    public class C_WinSessionHelperRDP : C_WinSessionHelper_Base
    {
        #region External_Library-Calls

        [DllImport("wtsapi32.dll")]
        static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll")]
        static extern Int32 WTSEnumerateSessions(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] Int32 Reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 Version,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSLogoffSession(IntPtr hServer, int SessionId, bool bWait);


        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);


        [DllImport("Wtsapi32.dll")]
        static extern bool WTSQuerySessionInformation(
       System.IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out System.IntPtr ppBuffer, out uint pBytesReturned);

        [StructLayout(LayoutKind.Sequential)]
        protected struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WTSINFO
        {
            [MarshalAs(UnmanagedType.I8)]
            public Int64 LogonTime;
        }

        private static IntPtr OpenServer(String Name)
        {
            IntPtr server = WTSOpenServer(Name);
            return server;
        }
        private static void CloseServer(IntPtr ServerHandle)
        {
            WTSCloseServer(ServerHandle);
        }

        protected override List<SessionInfo> FetchSessions(bool onlydiconnected = true, String ServerName = "localhost")
        {
            List<SessionInfo> List = new List<SessionInfo>();

            IntPtr serverHandle = IntPtr.Zero;
            List<String> resultList = new List<string>();
            serverHandle = OpenServer(ServerName);

            IntPtr SessionInfoPtr = IntPtr.Zero;
            IntPtr clientNamePtr = IntPtr.Zero;
            IntPtr wtsinfoPtr = IntPtr.Zero;
            IntPtr clientDisplayPtr = IntPtr.Zero;


            try
            {

                Int32 sessionCount = 0;
                Int32 retVal = WTSEnumerateSessions(serverHandle, 0, 1, ref SessionInfoPtr, ref sessionCount);
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                Int32 currentSession = (int)SessionInfoPtr;
                uint bytes = 0;
                if (retVal != 0)
                {
                    for (int i = 0; i < sessionCount; i++)
                    {
                        WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)currentSession, typeof(WTS_SESSION_INFO));

                        currentSession += dataSize;

                        WTSQuerySessionInformation(serverHandle, si.SessionID, WTS_INFO_CLASS.WTSClientName, out clientNamePtr, out bytes);
                        WTSQuerySessionInformation(serverHandle, si.SessionID, WTS_INFO_CLASS.WTSSessionInfo, out wtsinfoPtr, out bytes);

                        var wtsinfo = (WTSINFOA)Marshal.PtrToStructure(wtsinfoPtr, typeof(WTSINFOA));

                        //bool searchfor = (!onlydiconnected | si.State == WTS_CONNECTSTATE_CLASS.WTSDisconnected);

                        if (
                            (!onlydiconnected | si.State == WTS_CONNECTSTATE_CLASS.WTSDisconnected) &&  //if onlydisconnected is false, get all sessions, otherwise get disconnected sessions (boolean OR: NOT onyldisconnected OR Disconnected) 
                            si.SessionID != 0 //Don't ever terminate the 0-Session!! This is where all services run!!
                            )
                        {  //If the session is disconnected
                            SessionInfo temp = new SessionInfo();
                            temp.Client = Marshal.PtrToStringAnsi(clientNamePtr);
                            temp.Server = ServerName.Trim('\0');
                            temp.UserName = wtsinfo.UserName.Trim('\0');
                            temp.Domain = wtsinfo.Domain.Trim('\0');
                            temp.ConnectionState = si.State;
                            temp.SessionId = si.SessionID;
                            temp.sessionInfo = wtsinfo;
                            temp.ToString();
                            List.Add(temp);
                        }

                        WTSFreeMemory(clientNamePtr);
                        WTSFreeMemory(wtsinfoPtr);
                    }
                    WTSFreeMemory(SessionInfoPtr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                CloseServer(serverHandle);
            }

            return List;
        }


        #endregion

        #region constructor
        public C_WinSessionHelperRDP() : base()
        {

        }

        #endregion


        #region Methods
        public override ArrayList getDisconnectedSessions()
        {
            ArrayList myreturn = new ArrayList();

            myreturn.AddRange(this.FetchSessions(true, "localhost"));

            return myreturn;
        }

        public override ArrayList getAllSessions()
        {
            ArrayList myreturn = new ArrayList();

            myreturn.AddRange(this.FetchSessions(false, "localhost"));

            return myreturn;
        }

        public override SessionInfo findCurrentSession(string username)
        {
            ArrayList sessions = this.getAllSessions();
            foreach (SessionInfo si in sessions)
            {
                if (si.UserName == username && si.sessionInfo.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                {
                    //Username matches and sessions is active (A user is logged on to the WinStation. This state occurs when a user is signed in and actively connected to the device.)
                    return si;
                }
            }

            return null;
        }


        public override bool endSession(KeyValuePair<int, String> sess, String ServerName = "localhost")
        {
            IntPtr serverHandle = IntPtr.Zero;
            serverHandle = OpenServer(ServerName);

            bool result = WTSLogoffSession(serverHandle, sess.Key, true);
            if (!result)
            {
                //Some error occured

            }

            return result;
        }

        public override bool endSession(SessionInfo sessionInfo)
        {
            KeyValuePair<int, String> sess = new KeyValuePair<int, string>(sessionInfo.SessionId, sessionInfo.ToString());
            return this.endSession(sess);

        }

        #endregion
    }
}
