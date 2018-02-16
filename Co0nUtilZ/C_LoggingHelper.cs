using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Diese Klasse ermöglicht das Protokollieren
    /// Erstellt:           09/2017
    /// Autor:              D. Marx
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

            if (!EventLog.SourceExists(this._Logsourcename))
            {
                EventLog.CreateEventSource(this._Logsourcename, this._Logname); //Eventlogquelle registrieren falls nicht vorhanden.
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
            try
            {
                Message = this._MsgPrefix + Message; //Prefix voranstellen

                if (Message.Length > MESSAGE_MAXLENGTH)
                {//Die Nachricht ist zu lang. Daher die Nachricht aufteilen
                    List<String> Messageparts;
                    C_RegExHelper myRegEx = new C_RegExHelper();

                    double dchunks = (double)Message.Length / (double)MESSAGE_MAXLENGTH;
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
        #endregion

    }
}
