using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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
        private bool _OnErrorLogToFile = false; //Log to file if eventlog can't be written.

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
        public C_LoggingHelper(String Sourcename, String MsgPrefix/*=""*/, String Log = "Application", UInt16 BaseID = 10000, bool OnErrorLogToFile=false)
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
            this._OnErrorLogToFile = OnErrorLogToFile;

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
        /// Tries to write a log to the eventlog. If _OnErrorLogToFile is set and writing to the eventlog fails, it'll try to write to a local file instead
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Type"></param>
        /// <param name="Messagenumber"></param>
        private bool TryWriteLog(String msg, EventLogEntryType Type, UInt16 Messagenumber)
        {   try
            {
                //throw new Exception("Test exception for debugging");
                EventLog.WriteEntry(this._Logsourcename, msg, Type, (Int32)Messagenumber);
            }
            catch (Exception ex)
            { //An error occured while writing to the event-log
                if (this._OnErrorLogToFile)
                { //as OnErrorlogtoFile is set, we write log and error down to a file within user's appdata\roaming
                    try
                    {
                        string prefix = "coonutils_logexceptions";
                        string targetfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "logs", prefix, "logerror_" +
                                    String.Format("{0:yyyyMMdd_HH-mm-ss}", DateTime.Now) + "_" + Assembly.GetEntryAssembly().GetName().Name);
                        msg="The following error occured while logging to eventlog -> " + ex.Message + "\r\n\r\nStacktrace:\r\n" + ex.StackTrace + "\r\n\r\n Dumping original logmessage below:\r\n" + msg;
                        this.logtofile(targetfile, msg, 30, prefix);
                    }
                    catch
                    {
                        //giving up, as text file can't be written either
                    }

                }
                return false; //as we were unable to write to the eventlog, return false...
            }
            return true; //return true on success
            
        }

        /// <summary>
        /// Logs the string in message to the file filename. Cleans up old logfiles, if removelogsolerthan > 0
        /// </summary>
        /// <param name="filename">the filename to be written to</param>
        /// <param name="message">the content that should be written</param>
        /// <param name="removelogsolderthan">if > 0, will remove logfiles older than specified (days)</param>
        /// <param name="delprefix">the prefix of the filenames to be deleted (e.g. logfile_). Can be used and should be set to avoid deleting all old files in that folder.</param>
        /// <returns>TRUE on success, otherwise FALSE</returns>
        public bool logtofile(string filename, string message, int removelogsolderthan=-1, string delprefix="")
        {
            try
            {
                if (!filename.EndsWith(".txt")) //Avoid writing anything other than .txt-files. If the specified file doensn't end with .txt, append it.
                {
                    filename = filename + ".txt";
                }

                //Prepare
                FileInfo finfo = new FileInfo(filename);
                if (!(new DirectoryInfo(finfo.DirectoryName)).Exists)
                {
                    Directory.CreateDirectory(finfo.DirectoryName);
                }
                // Write the file
                using (StreamWriter outFile = new StreamWriter(filename))
                {
                    outFile.WriteLine(message);
                }

                //Cleanup old logfiles
                if (removelogsolderthan > 0)
                {
                    //check if the logfiles-dir exists
                    DirectoryInfo logfilesdir = new DirectoryInfo(finfo.DirectoryName);                    
                    if (logfilesdir.Exists)
                    {
                        DateTime maxage = DateTime.Now.AddDays(removelogsolderthan * -1); //Calculate maximum file age
                        foreach (string file in Directory.GetFiles(logfilesdir.FullName, delprefix+"*.txt")) // find all logfiles (.txt) files in the logfiles-dir
                        { // iterate through all files found
                            FileInfo fi = new FileInfo(file);
                            if (fi.LastWriteTime < maxage) //check if the file is older than specified
                            {
                                try
                                {
                                    fi.Delete(); // if it is old, try to delete it
                                }
                                catch
                                { //the logfile can't be deleted for whatever reason                                    
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs a message to the windows eventlog. If the message is to large, it will be split into multiple chunks
        /// </summary>
        /// <param name="Message">Content of the message</param>
        /// <param name="Type">Messagetype (Info, Warning or Error)</param>
        /// <param name="Messagenumber" >Unique-messagenumber. Will use the next free if value is 0</param>
        /// <returns>Returns FALSE on error. Otherwise TRUE</returns>
        public bool Log(String Message, EventLogEntryType Type, UInt16 Messagenumber)
        {
            bool myreturn = true; //assume / init with rseult ok
          
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
                   if (! this.TryWriteLog(msg, Type, Messagenumber)) //if any of those chunk-writes returns false, the overall return-value will be false
                    {
                        myreturn = false;
                    }
                }
            }
            else
            {
                myreturn= this.TryWriteLog(Message, Type, Messagenumber); //we only write one chunk. it's return value will be the overall return value                   
            }
           
            return myreturn;
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
