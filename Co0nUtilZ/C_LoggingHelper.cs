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
        private List<Int32> NumbersInUse;

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
        /// <param name="Sourcename">Name der Loggingquelle (Frei wählbarer Name)</param>
        /// <param name="Log">Welches Log soll verwendet werden? Std.: Application</param>
        public C_LoggingHelper(String Sourcename, String MsgPrefix/*=""*/, String Log = "Application")
        {//Konstruktor
            this._Logsourcename = Sourcename;
            this._Logname = Log;
            this._MsgPrefix = MsgPrefix;
            this.NumbersInUse = new List<Int32>();

            if (!EventLog.SourceExists(this._Logsourcename))
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
        /// Protokolliert eine Meldung im Windows Ereignisprotokoll
        /// </summary>
        /// <param name="Message">Inhalt der Nachricht</param>
        /// <param name="Type">Art der Nachricht (Info, Warning oder Error)</param>
        /// <param name="Messagenumber">Eindeutige Nummer der Meldung</param>
        /// <returns>Gibt im Fehlerfall FALSE zurück</returns>
        public bool Log(String Message, EventLogEntryType Type, Int32 Messagenumber)
        {
            //Keep track of the messagenumbers used
            if (!this.NumbersInUse.Contains(Messagenumber))
            {
                this.NumbersInUse.Add(Messagenumber);
            }

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
                        EventLog.WriteEntry(this._Logsourcename, msg, Type, Messagenumber);
                    }
                }
                else
                {
                    EventLog.WriteEntry(this._Logsourcename, Message, Type, Messagenumber);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Protokolliert eine Fehlermeldung im Windows Ereignisprotokoll
        /// </summary>
        /// <param name="Message">>Inhalt der Nachricht</param>
        /// <param name="Messagenumber">Eindeutige Nummer der Meldung</param>
        /// <returns>Gibt im Fehlerfall FALSE zurück</returns>
        public bool LogError(String Message, Int32 Messagenumber)
        {
            return this.Log(Message, C_LoggingHelper.TYPE_ERROR, Messagenumber);
        }

        /// <summary>
        /// Protokolliert eine Warnmeldung im Windows Ereignisprotokoll
        /// </summary>
        /// <param name="Message">>Inhalt der Nachricht</param>
        /// <param name="Messagenumber">Eindeutige Nummer der Meldung</param>
        /// <returns>Gibt im Fehlerfall FALSE zurück</returns>
        public bool LogWarn(String Message, Int32 Messagenumber)
        {
            return this.Log(Message, C_LoggingHelper.TYPE_WARN, Messagenumber);
        }

        /// <summary>
        /// Protokolliert eine Infomeldung im Windows Ereignisprotokoll
        /// </summary>
        /// <param name="Message">>Inhalt der Nachricht</param>
        /// <param name="Messagenumber">Eindeutige Nummer der Meldung</param>
        /// <returns>Gibt im Fehlerfall FALSE zurück</returns>
        public bool LogInfo(String Message, Int32 Messagenumber)
        {
            return this.Log(Message, C_LoggingHelper.TYPE_INFO, Messagenumber);
        }

        /// <summary>
        /// Will return a Number which has not been used yet
        /// </summary>
        /// <returns></returns>
        public Int32 NextFreeNumber()
        {
            Int32 maxValue = this.NumbersInUse.Max();

            if (maxValue < UInt16.MaxValue)
            {
                return maxValue;
            }
            else
            {
                return 0;
            }
        }
        #endregion

    }
}
