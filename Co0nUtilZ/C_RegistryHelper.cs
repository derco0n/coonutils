using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Dies ist eine Hilfsklasse, welche die Aufgabe übernimmt, Daten in die Registry zu schreiben bzw. aus dieser auszulesen.
    /// Erstellt:           06/2017
    /// Autor:              D. Marx
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


            using (RegistryKey
                tempKey = this._rootkey.OpenSubKey(SubKey))
            {
                String[] ValueNames = null;
                try
                {
                    ValueNames = tempKey.GetValueNames();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                if (ValueNames != null && ValueNames.Length > 0)
                {
                    foreach (String valnam in ValueNames)
                    {
                        myreturn.Add(valnam);
                    }
                }

            }

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
        public String ReadSettingFromRegistry(String Instancename, String valuename)
        {
            //Liest einen Wert aus der Windows Registry
            string keyName = this._rootkey.Name + "\\" + this._subkey + "\\" + Instancename; 

            if (Registry.GetValue(keyName, valuename, null) != null)
            {
                //Key existiert. Wert benutzen.
                return Registry.GetValue(keyName, valuename, "").ToString();
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
        public bool WriteSettingToRegistry(String Instancename, String valuename, String value)
        {
            //Schreibt einen Wert in die Windows Registry
            try
            {
                string keyName = this._rootkey.Name + "\\" + this._subkey + "\\" + Instancename;
                Registry.SetValue(keyName, valuename, value);
                return true;
            }
            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(this, new ErrorEventArgs("Fehler beim Schreiben in die Registry: " + ex.ToString()));
                }
                //Console.WriteLine("Fehler beim Schreiben in die Registrierungsdatenbank. " + ex.ToString() + " " + ex.StackTrace);
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
        public List<String> getInstances()
        {//Listet alle Instanzen auf
            List<String> myreturn = new List<string>();

            try
            {
                Microsoft.Win32.RegistryKey keyName = this.rootkey.OpenSubKey(this._subkey);
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

        #endregion
    }
}
