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
    /// This will save settings to the registry and reads them from there
    /// Author: D. Marx
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html 

    /// </summary>
    public abstract class C_BasicSettingsTemplate
    {
        #region objects
        private C_RegistryHelper myRegHelper;
        protected String _instancename;
        #endregion

        #region Constructor
        public C_BasicSettingsTemplate(RegistryKey Rootkey, String Subkey, String instancename)
        {
            this.myRegHelper = new C_RegistryHelper(Rootkey, Subkey);
            this._instancename = instancename;
        }
        #endregion

        #region properties
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



        /*
         Insert properties in derived class...         
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

        }

        protected String readOneSettingFromRegistry(String Setting)
        {
           return this.myRegHelper.ReadSettingFromRegistry(this._instancename, Setting);
        }

        /// <summary>
        /// Override this Method to Write all your Settings to the registry
        /// </summary>
        /// <returns></returns>
        public virtual bool writeAllSettingsToRegistry()
        {



            return false;
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

        /*
        /// <summary>
        /// Renames this instance in the registry
        /// </summary>
        /// <param name="newName">new Name</param>
        public void renameInstance(String newName)
        {
            String oldinstancename = this._instancename; //save old name
            if (!oldinstancename.Equals(newName)) //if new and old name are different
            {
                this._instancename = newName; //

                if (this.writeAllSettingsToRegistry())
                {//Neue Werte schreiben...

                    //... und bei Erfolg: Alten Instanzschlüssel löschen
                    this.myRegHelper.dropInstance(oldinstancename);
                }
            }
        }
        */

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
