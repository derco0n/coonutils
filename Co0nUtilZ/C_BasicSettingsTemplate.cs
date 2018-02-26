using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Template for Settingsclasses
    /// This abstract class will hold settings, save them to the registry and reads them from registry.
    /// Author: D. Marx
    /// Project: https://github.com/derco0n/coonutils
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html 
    /// </summary>
    public abstract class C_BasicSettingsTemplate
    {
        #region objects
        protected C_RegistryHelper myRegHelper;
        protected String _instancename;
        protected const String _StartSearchString = "Start"; //Starttimes begin with the word "Start"
        protected String[] _Starttimes;
        #endregion

        #region Constructor
        public C_BasicSettingsTemplate(RegistryKey Rootkey, String Subkey, String instancename)
        {
            this.myRegHelper = new C_RegistryHelper(Rootkey, Subkey);
            this._instancename = instancename;
        }
        #endregion

        #region properties

        
        /// <summary>
        /// List with daily Start-Times
        /// </summary>
        public String[] Starttimes
        {
            get
            {
                return this._Starttimes;
            }
            set
            {
                this._Starttimes = value;
                if (this.SettingChanged != null)
                {
                    this.SettingChanged(new ProgressEventArgs(0, "Starttimes"));
                }
            }
        }


        public RegistryKey Rootkey
        {
            get
            {
                return this.myRegHelper.rootkey;
            }
        }

        public String SubKey
        {
            get
            {
                return this.myRegHelper.subkey;
            }
        }

        public virtual String Instancename
        {
            get
            {
                return this._instancename;
            }

           
            set
            {//Override Set if needed

            }
            
        }

        /// <summary>
        /// returns the hour-part of starttimes
        /// </summary>
        public String[] StartHours 
        {
            get
            {
                List<String> myreturn = new List<String>();
                if (this._Starttimes != null)
                {
                    foreach (String Starttime in this._Starttimes)
                    {
                        int colonindex = Starttime.IndexOf(":");
                        myreturn.Add(Starttime.Substring(0, colonindex));
                    }
                }
                return myreturn.ToArray();
            }
        }

        /// <summary>
        /// returns the minute-part of starttimes
        /// </summary>
        public String[] StartMinutes 
        {
            get
            {
                List<String> myreturn = new List<String>();
                if (this._Starttimes != null)
                {
                    foreach (String Starttime in this._Starttimes)
                    {
                        int colonindex = Starttime.IndexOf(":");
                        myreturn.Add(Starttime.Substring(colonindex + 1));
                    }
                }
                return myreturn.ToArray();
            }
        }

        
        /*
         Insert more specific properties in derived class...         
         */

        #endregion

        #region delegatesandevent
        public delegate void CompleteEventhandler(); 
        public delegate void SettingChangedHandler(ProgressEventArgs e); 
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs Fehler);

        public event CompleteEventhandler JobCompleted; //Should raise this when jobrun is complete
        public event SettingChangedHandler SettingChanged; //Should raise this, everytime a setting has changed        
        public event ErrorEventHandler ErrorOccured; //Should raise this in case of an error

        //Following is needed because we want that events to be raised in childclasses
        //https://docs.microsoft.com/de-de/dotnet/csharp/programming-guide/events/how-to-raise-base-class-events-in-derived-classes

        protected virtual void OnSettingChanged(ProgressEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            SettingChangedHandler handler = SettingChanged;
            if (handler != null)
            {
                handler(e);
            }
        }

        protected virtual void OnJobCompleted()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            CompleteEventhandler handler = JobCompleted;
            if (handler != null)
            {
                handler();
            }
        }

        protected virtual void OnErrorOccured(object sender, ErrorEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            ErrorEventHandler handler = ErrorOccured;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region methods
        /// <summary>
        /// Override this method to run a self defined job with current settings
        /// </summary>
        public virtual void runJob()
        {

        }

        /// <summary>
        /// Override this method to read settings from registry
        /// </summary>
        public virtual void readSettingsFromRegistry()
        {
            List<String> Starts = new List<string>(); //make a new string-list

            try
            {//Determine Starttimes
             
                List<String> valuenames = this.myRegHelper.ListValues(this._instancename); //Get a list with all valuenames of the current subkey
                
                foreach (String valnam in valuenames) //recurse all valuenames
                {
                    if (valnam.StartsWith(_StartSearchString)) //if valuename begins with seachrstring for startimes ("Start")...
                    {
                        String val = this.myRegHelper.ReadSettingFromRegistry(this._instancename, valnam); //fetch the value from current value
                        Starts.Add(val); //add that value to our starts-list
                    }
                }                

            }
            catch (Exception ex)
            {//not readable from registry
                this.OnErrorOccured(this, new ErrorEventArgs("Error while fetching starttime-values from registry.\r\n\r\nDetails:\r\n"+ex.ToString()+ "\r\n\r\nStacktrace:\r\n"+ex.StackTrace));                
            }
            finally
            {
                this._Starttimes = Starts.ToArray<String>(); //Alle ermittelten Startzeitpunkte in ein Stringarray konvertieren und dem Klassenarray
            }
        }

        protected String readOneSettingFromRegistry(String Setting)
        {
           return this.myRegHelper.ReadSettingFromRegistry(this._instancename, Setting);
        }

        /// <summary>
        /// Override this Method to Write all your Settings to the registry
        /// </summary>
        /// <returns></returns>
        protected virtual bool writeAllSettingsToRegistry()
        {
            /*
             * Starttimes.
             * ###########
             * We first need to drop all starttimes from registry, so that we will have no startentry there.
             * After that we recurse our current starttimes and write each of them to the registry
             */

            if (this._instancename!=null){              
            

                List<String> valuenames = this.myRegHelper.ListValues(this._instancename); //fetch a lists with all ValueNames in current subkey
            
                if (valuenames.Count > 0){
                    //Recurse all values and try to delete them if thex exist
             
                    foreach (String valnam in valuenames) //recurse all value-names
                    {
                        if (valnam.StartsWith(_StartSearchString)) //if the value matches the searchstring for starttimes
                        {
                            this.DeleteValue(valnam); //delete value
                        }
                    }
                }
            }

            //Write Starttimes to registry
            bool StarttimesSuccess = false;

            UInt32 StartNrCounter = 1;

            
            if (this._Starttimes != null && this._Starttimes.Count() > 0)
            { 
            //_Stattimes must be initilized
            //minimum one entry existing in Starttimes-array
                foreach (String Start in this._Starttimes)
                {//go through the starttimes-list...
                    //write every start with its unique number (StartX, StartY, ...)
                    if (StartNrCounter == 1)
                    {
                        StarttimesSuccess = this.writeSettingToRegistry("Start" + StartNrCounter.ToString(), Start);
                    }
                    else
                    {
                        StarttimesSuccess = StarttimesSuccess & this.writeSettingToRegistry("Start" + StartNrCounter.ToString(), Start); //& => Boolean Operator &(bool x, bool y); the result of x & y is true if x AND y are both TRUE. If not it is FALSE
                    }
                    StartNrCounter++; //increase Startcounter
                }
            }
            else
            {//No Starttime defined, therefore success
                StarttimesSuccess = true;
            }
            
            

            /*
             * End Starttimes...
             *################## 
             */

             return StarttimesSuccess; //& someothersuccess & anotheronesuccess....
            
        }


        /// <summary>
        /// Writes one setting to Registry
        /// </summary>
        /// <param name="valuename"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool writeSettingToRegistry(String valuename, String value)
        {//Writes a value to the registry
            return this.myRegHelper.WriteSettingToRegistry(this._instancename, valuename, value);
        }

        
        /// <summary>
        /// Override this to rename this instance in the registry
        /// </summary>
        /// <param name="newName">new Name</param>
        protected virtual bool renameInstance(String newName)
        {
            String oldinstancename = this._instancename; //save old name
            if (!oldinstancename.Equals(newName)) //if new and old name are different
            {
                this._instancename = newName; //set instancename to new name

                if (this.writeAllSettingsToRegistry())
                {//Neue Werte schreiben...

                    //... und bei Erfolg: Alten Instanzschlüssel löschen
                    return this.myRegHelper.dropInstance(oldinstancename);                    
                }
            }
            return false;
        }
        

        /// <summary>
        /// deletes the complete jobinstance from registry.
        /// </summary>
        /// <returns>TRUE if success</returns>
        public bool DeleteInstance()
        {
            return this.myRegHelper.dropInstance(this._instancename);
        }

        /// <summary>
        /// Deletes a value of the current instance from the registry...
        /// </summary>
        /// <returns>TRUE if success</returns>
        public bool DeleteValue(String valuename)
        {
            return this.myRegHelper.dropValue(this._instancename, valuename);
        }

        #endregion



    }
}
