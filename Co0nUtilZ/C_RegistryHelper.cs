using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace Co0nUtilZ
{
    /// <summary>
    /// This is a helper class which manages to read from and write to windows registry
    /// Created:           06/2017
    /// Author:              D. Marx
    /// Project: https://github.com/derco0n/coonutils   
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    /// </summary>
    public class C_RegistryHelper
    {

        private RegistryKey _rootkey;
        private String _subkey;

        #region delegates
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs Fehler);
        public event ErrorEventHandler GeneralError;
        #endregion

        #region properties

        public RegistryKey rootkey //ReadOnly-Property
        { //Gibt private _rootkey zurück
            get
            {
                return this._rootkey;
            }
        }

        public String subkey//ReadOnly-Property
        {//Gibt private _subkey zurück
            get
            {
                return this._subkey;
            }
        }

        #endregion

        #region Konstruktor
        /// <summary>
        /// Creates a new registry-helper instance
        /// use "RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)" as root-key to force the 64Bit hive;
        /// </summary>
        /// <param name="Rootkey">rootkey</param>
        /// <param name="Subkey">subkey-path</param>
        public C_RegistryHelper(RegistryKey Rootkey, String Subkey)
        { //Konstruktor
            this._rootkey = Rootkey;
            this._subkey = Subkey;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Ermittelt alle Wertenamen eines Unterschlüssels
        /// </summary>
        /// <param name="SubKey">Unterschlüsssel</param>
        /// <returns>Gibt alle gefundenen Wertenamen zurück.</returns>
        public List<String> ListValues(String SubKey = "")
        { //Gibt eine Liste mit Namen aller Werte aus (21.08.2017)
            List<String> myreturn = new List<string>();

            if (SubKey != "")
            {//Wenn ein weiterer Unterschlüssel angegeben wurde, diesen an den Klassenweiten anhängen und damit suchen
                SubKey = this._subkey + "\\" + SubKey;
            }
            else
            {
                SubKey = this._subkey; //Den Klassenweiten verwenden.
            }

            RegistryKey tempKey = this._rootkey.OpenSubKey(SubKey);
            //using (RegistryKey tempKey = this._rootkey.OpenSubKey(SubKey))
            //{
            String[] ValueNames = null;
            //try
            //{
            if (tempKey != null)
            {
                ValueNames = tempKey.GetValueNames();
            }
            //else

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            if (ValueNames != null && ValueNames.Length > 0)
            {
                foreach (String valnam in ValueNames)
                {
                    myreturn.Add(valnam);
                }
            }

            //}

            return myreturn;
        }

        /// <summary>
        /// Ermittelt ob eine es unter dem Angebenen Schlüssel bereits Instanzen (Unterschlüssel) gibt.
        /// </summary>
        /// <returns>Wenn Instanzen (Unterschlüssel) vorhanden sind, wird TRUE zurückgegeben.</returns>
        public bool hasInstances()
        {
            List<String> myreturn = new List<string>();

            try
            {
                Microsoft.Win32.RegistryKey keyName = this.rootkey.OpenSubKey(this._subkey);
                String[] Subkeys = keyName.GetSubKeyNames();

                if (Subkeys.Length > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }


        /// <summary>
        /// Liest einen Wert auf einem Wertefeld aus
        /// </summary>
        /// <param name="Instancename">Instanzname (Unterschlüssel des aktuellen Registry-Schlüssels)</param>
        /// <param name="valuename">Name des auszulesenden Werts</param>
        /// <returns>Gibt bei Erfolg den Wert des angegebenen Felds zurück. Im Fehlerfall wird ein leerer String zurück gegeben</returns>
        public String ReadSettingFromRegistry(String Instancename, String valuename, String subkey = "")
        {
            //Liest einen Wert aus der Windows Registry

            string keyName = "";

            if (subkey.Equals(""))
            { // subkey is not given, use the legacy path-finder: root\subk\instance
                keyName = this._subkey + "\\" + Instancename;

            }
            else
            { //subkey is given explicitly. use it
                keyName = subkey;
            }

            RegistryKey Key = this._rootkey.OpenSubKey(keyName);  //This will preserve information about whether its the 32 or 64-Bit registry

            if (Key.GetValue(valuename) != null)
            {
                //Key exists. Use it, use an empty string as default.
                return Key.GetValue(valuename).ToString();
            }

            return "";
        }

        /// <summary>
        /// Schreibt einen Wert in die Registry
        /// </summary>
        /// <param name="Instancename">Instanzname (Unterschlüssel des aktuellen Registry-Schlüssels)</param>
        /// <param name="valuename">Name des zu schreibenden Felds</param>
        /// <param name="value">Wert</param>
        /// <returns>Gibt bei Erfolg TRUE zurück.</returns>
        public bool WriteSettingToRegistry(String Instancename, String valuename, object value, RegistryValueKind rvkind = RegistryValueKind.String)
        {
            //Writes a value in the registry
            if (Instancename != null && valuename != null && value != null)
            {
                //Only if all needed values are given
                try
                {
                    string keyName = this._rootkey.Name + "\\" + this._subkey + "\\" + Instancename;                    
                    Registry.SetValue(keyName, valuename, value, rvkind);
                    return true;
                }
                catch (Exception ex)
                {
                    if (this.GeneralError != null)
                    {
                        this.GeneralError(this, new ErrorEventArgs("Error writing to registry: " + ex.ToString()));
                    }
                    //Console.WriteLine("Fehler beim Schreiben in die Registrierungsdatenbank. " + ex.ToString() + " " + ex.StackTrace);
                }
            }

            return false;
        }

        /// <summary>
        /// Löscht eine komplette Instanz (Unterschlüssel des aktuellen Registry-Schlüssels)
        /// </summary>
        /// <param name="Instancename">Instanzname (Unterschlüssel des aktuellen Registry-Schlüssels)</param>
        /// <returns>Gibt bei Erfolg TRUE zurück.</returns>
        public bool dropInstance(String Instancename)
        {
            //Löscht einen kompletten Instanzunterschlüssel
            try
            {
                string keyName = this._subkey + "\\" + Instancename;
                this._rootkey.DeleteSubKeyTree(keyName);
                return true;
            }
            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(this, new ErrorEventArgs("Fehler beim Löschen der Instanz aus der Registry: " + ex.ToString()));
                }
                //Console.WriteLine("Fehler beim Löschen innerhalb der Registrierungsdatenbank. " + ex.ToString() + " " + ex.StackTrace);
                return false;
            }

        }

        /// <summary>
        /// Removes all values of a given instance
        /// </summary>
        /// <param name="instancename">The instance's name.</param>
        /// <returns>true on success otherwise false</returns>
        public bool dropAllValues(String instancename)
        {
            bool myreturn = true;
            foreach (string value in this.ListValues(instancename))
            {
                myreturn = myreturn & this.dropValue(instancename, value);                
            }
            return myreturn;
        }

        /// <summary>
        /// Löscht einen Wert in einem Instanzunterschlüssel
        /// </summary>
        /// <param name="Instancename">Instanzname (Unterschlüssel des aktuellen Registry-Schlüssels)</param>
        /// <param name="valuename">Name des zu löschenden Felds</param>
        /// <returns>Gibt bei Erfolg TRUE zurück.</returns>
        public bool dropValue(String Instancename, String valuename)
        {
            //Löscht einen Wert in einem Instanzunterschlüssel
            string keyName = this._subkey + "\\" + Instancename;
            //using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName, true))
            try
            {
                using (RegistryKey key = this._rootkey.OpenSubKey(keyName, true))
                {
                    if (key == null)
                    {
                        // Schlüssel existiert nicht
                        return false;
                    }
                    else
                    {
                        try
                        {
                            key.DeleteValue(valuename);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine("Fehler beim Löschen innerhalb der Registrierungsdatenbank. " + ex.ToString() + " " + ex.StackTrace);

                            if (this.GeneralError != null)
                            {
                                this.GeneralError(this, new ErrorEventArgs("Fehler beim Löschen innerhalb der Registrierungsdatenbank. " + ex.ToString() + " " + ex.StackTrace));
                            }

                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(this, new ErrorEventArgs("Fehler beim Schreibzugriff auf die Registrierungsdatenbank. Sind Sie berechtigt?!" + ex.ToString() + " " + ex.StackTrace));
                }

                return false;
            }
        }

        /// <summary>
        /// //Listet alle Instanzen (Unterschlüssel des aktuellen Schlüsseln) auf
        /// </summary>
        /// <returns>Gibt eine Liste mit gefundenen Instanznamen zurück</returns>
        public List<String> getInstances(string subkey = "")
        {//Listet alle Instanzen auf
            List<String> myreturn = new List<string>();

            try
            {
                string key = this._subkey;
                if (!subkey.Equals(""))
                { //different subkey is specified
                    key = subkey;
                }

                Microsoft.Win32.RegistryKey keyName = this.rootkey.OpenSubKey(key);
                String[] Subkeys = keyName.GetSubKeyNames();

                if (Subkeys.Length > 0) //Wenn mindestens ein Eintrag gefunden wurde
                {
                    foreach (String subkeyName in Subkeys)
                    {
                        //Console.WriteLine(keyName.OpenSubKey(subkeyName).GetValue("DisplayName"));
                        myreturn.Add(subkeyName.ToString());
                    }

                }
            }
            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(this, new ErrorEventArgs("Fehler beim Lesen der Instanzen. " + ex.ToString() + " " + ex.StackTrace));
                }
            }
            return myreturn;
        }

        public bool keyexists(string keyname="")
        {
            string lookup = this._subkey;
            // if keyname is given search for it. if not given lookup this._subkey only
            if (!keyname.Equals(""))
            {
                lookup = lookup + "\\" + keyname;
            }
            RegistryKey key = this._rootkey.OpenSubKey(lookup, true);
            if (null != key)
            { //key exists
                return true;
            }

            return false;
        }

        public bool createkey(string keyname = "")
        {
            string tocreate = this._subkey;
            // if keyname is use it otherwise use this._subkey only
            if (!keyname.Equals(""))
            {
                tocreate = tocreate + "\\" + keyname;
            }

            if (this._rootkey.CreateSubKey(tocreate) != null)
            {
                return true; //success
            }

            return false;
        }

        #endregion
    }
}
