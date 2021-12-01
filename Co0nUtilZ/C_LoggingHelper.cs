using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Co0nUtilZ
{
    /// <summary>
    /// This class helps logging to Windows Eventlog
    /// Created:           09/2017
    /// Author:              D. Marx
    /// Project: https://github.com/derco0n/coonutils   
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    /// </summary>
    public class C_LoggingHelper
    {

        #region === Statics
        public static String LOG_APPLICATION = "Application";
        public static String LOG_SYSTEM = "System";
        public static EventLogEntryType TYPE_INFO = EventLogEntryType.Information;
        public static EventLogEntryType TYPE_WARN = EventLogEntryType.Warning;
        public static EventLogEntryType TYPE_ERROR = EventLogEntryType.Error;
        public static int MESSAGE_MAXLENGTH = 32760;
        #endregion

        #region === Variables
        private String _Logsourcename;
        private String _Logname; //System oder Anwendungslog
        private String _MsgPrefix; // Prefix welches Nachrichten vorangestellt werden soll

        //Keep track of the numbers beeing used
        private List<UInt16> InfoNumbersInUse;
        private List<UInt16> WarnNumbersInUse;
        private List<UInt16> ErrorNumbersInUse;

        #endregion


        #region === Properties
        //Properties

        /// <summary>
        /// Name der Loggingquelle
        /// </summary>
        public String Logsourcename
        {
            get
            {
                return this._Logsourcename;
            }
        }

        /// <summary>
        /// Name des Logs (Application/System)
        /// </summary>
        public String Logname
        {
            get
            {
                return this._Logname;
            }
        }

        /// <summary>
        /// Prefix welches Nachrichten vorangestellt werden soll
        /// </summary>
        public String MsgPrefix
        {
            get
            {
                return this._MsgPrefix;
            }
            set
            {
                this._MsgPrefix = value;
            }
        }
        #endregion

        #region Destruktor
        ~C_LoggingHelper()
        {//Destruktor

        }
        #endregion

        #region Konstruktor
        /// <summary>
        /// Instanziert ein neues Objekt einer Helferklasse zum Logging im Windows-Ereignisprotokoll
        /// </summary>
        /// <param name="Sourcename">Name of the Logging-source (free of choice)</param>
        /// <param name="Log">Log to be used? Std.: Application</param>
        /// <param name="BaseID">The BaseID to be used if you prefer auto-incrementing numbers.</param>        
        public C_LoggingHelper(String Sourcename, String MsgPrefix/*=""*/, String Log = "Application", UInt16 BaseID = 10000)
        {//Konstruktor
            this._Logsourcename = Sourcename;
            this._Logname = Log;
            this._MsgPrefix = MsgPrefix;
            this.InfoNumbersInUse = new List<UInt16>();
            this.WarnNumbersInUse = new List<UInt16>();
            this.ErrorNumbersInUse = new List<UInt16>();
            UInt16 prenum = (UInt16)(BaseID - 1); // Calculate the number before the BaseID
            this.InfoNumbersInUse.Add(prenum); // and add it to the InfoNumberInUseList
            this.WarnNumbersInUse.Add((UInt16)(prenum + 5000)); // Add a warning-base at an offset of 5000
            this.ErrorNumbersInUse.Add((UInt16)(prenum + 10000)); // Add a error-base at an offset of 10000

            bool sourceexists = false;

            try
            {
                if (EventLog.SourceExists(this._Logsourcename))
                {
                    sourceexists = true;
                }
            }
            catch
            {

            }

            if (!sourceexists)
            {
                try
                {
                    EventLog.CreateEventSource(this._Logsourcename, this._Logname); //Eventlogquelle registrieren falls nicht vorhanden.
                }
                catch
                {
                }
            }


        }
        #endregion


        #region Methods

        //Methoden
        /// <summary>
        /// Logs a message to the windows eventlog. If the message is to large, it will be split into multiple chunks
        /// </summary>
        /// <param name="Message">Content of the message</param>
        /// <param name="Type">Messagetype (Info, Warning or Error)</param>
        /// <param name="Messagenumber" >Unique-messagenumber. Will use the next free if value is 0</param>
        /// <returns>Returns FALSE on error. Otherwise TRUE</returns>
        public bool Log(String Message, EventLogEntryType Type, UInt16 Messagenumber)
        {
            try
            {
                Message = this._MsgPrefix + Message; //Prefix voranstellen

                if (Message.Length > MESSAGE_MAXLENGTH)
                {//Die Nachricht ist zu lang. Daher die Nachricht aufteilen
                    List<String> Messageparts;
                    C_RegExHelper myRegEx = new C_RegExHelper();

                    double dchunks = Message.Length / (double)MESSAGE_MAXLENGTH;
                    int chunks = (int)Math.Floor(dchunks);

                    Messageparts = myRegEx.SplitString(Message, chunks);

                    foreach (String msg in Messageparts)
                    {
                        EventLog.WriteEntry(this._Logsourcename, msg, Type, (Int32)Messagenumber);
                    }
                }
                else
                {
                    EventLog.WriteEntry(this._Logsourcename, Message, Type, (Int32)Messagenumber);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Logs an ERROR-message to the windows eventlog. If the message is to large, it will be split into multiple chunks
        /// </summary>
        /// <param name="Message">Messagecontent</param>
        /// <param name="Messagenumber">Unique-messagenumber. Will use the next free if value is 0 or not specified</param>
        /// <returns>Returns FALSE on error. Otherwise TRUE</returns>
        public bool LogError(String Message, UInt16 Messagenumber = 0)
        {
            if (Messagenumber == 0)
            {
                //No Messagenumber defined, guess a new one
                Messagenumber = this.NextFreeNumber(this.ErrorNumbersInUse);
            }

            //Keep track of the messagenumbers used
            if (!this.ErrorNumbersInUse.Contains(Messagenumber))
            {
                this.ErrorNumbersInUse.Add(Messagenumber);
            }

            return this.Log(Message, C_LoggingHelper.TYPE_ERROR, Messagenumber);
        }

        /// <summary>
        /// Logs an WARNING-message to the windows eventlog. If the message is to large, it will be split into multiple chunks
        /// </summary>
        /// <param name="Message">Messagecontent</param>
        /// <param name="Messagenumber">Unique-messagenumber. Will use the next free if value is 0 or not specified</param>
        /// <returns>Returns FALSE on error. Otherwise TRUE</returns>
        public bool LogWarn(String Message, UInt16 Messagenumber = 0)
        {
            if (Messagenumber == 0)
            {
                //No Messagenumber defined, guess a new one
                Messagenumber = this.NextFreeNumber(this.WarnNumbersInUse);
            }

            //Keep track of the messagenumbers used
            if (!this.WarnNumbersInUse.Contains(Messagenumber))
            {
                this.WarnNumbersInUse.Add(Messagenumber);
            }

            return this.Log(Message, C_LoggingHelper.TYPE_WARN, Messagenumber);
        }

        /// <summary>
        /// Logs an INFO-message to the windows eventlog. If the message is to large, it will be split into multiple chunks
        /// </summary>
        /// <param name="Message">Messagecontent</param>
        /// <param name="Messagenumber">Unique-messagenumber. Will use the next free if value is 0 or not specified</param>
        /// <returns>Returns FALSE on error. Otherwise TRUE</returns>
        public bool LogInfo(String Message, UInt16 Messagenumber = 0)
        {
            if (Messagenumber == 0)
            {
                //No Messagenumber defined, guess a new one
                Messagenumber = this.NextFreeNumber(this.InfoNumbersInUse);
            }

            //Keep track of the messagenumbers used
            if (!this.InfoNumbersInUse.Contains(Messagenumber))
            {
                this.InfoNumbersInUse.Add(Messagenumber);
            }

            return this.Log(Message, C_LoggingHelper.TYPE_INFO, Messagenumber);
        }

        /// <summary>
        /// Will return the next free number-id which has not been used yet
        /// </summary>        
        /// <param name="numberpool">the numberpool to be used</param>
        /// <returns>the next available number</returns>
        protected UInt16 NextFreeNumber(List<UInt16> numberpool)
        {
            UInt16 maxValue = numberpool.Max();

            if (maxValue < UInt16.MaxValue)
            {
                return (UInt16)(maxValue + 1);
            }
            else
            {
                return 0;
            }
        }
        #endregion

    }
}
