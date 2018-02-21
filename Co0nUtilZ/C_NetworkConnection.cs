using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// This Class connects to network shares (samba/cifs)
    /// Created:           08/2017
    /// Author:            D. Marx
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    /// </summary>
    class C_NetworkConnection : IDisposable
    { 

        #region Objects
        private string _networkName;
        private NetworkCredential _credentials;
        #endregion

        #region Events
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs Fehler);
        public event ErrorEventHandler NetworkError;
        #endregion


        public Dictionary<String, String> Errorhints = new Dictionary<string, string>();

        public void InitializeErrorhints()
        {
            this.Errorhints.Add("1331", "Ist das Benutzerkonto gültig?\r\nIst das Benutzerkonto deaktiviert?");
        }

        #region Konstruktor
        /// <summary>
        /// Diese Klasse stellt den Zugriff auf eine Netzwerkfreigabe mittels API-Zugriffen her
        /// </summary>
        /// <param name="networkName">Kompletter Netzwerkpfad (\\Server\Freigabe\Ordner)</param>
        /// <param name="credentials">Zugangsdaten zur Freigabe</param>
        public C_NetworkConnection(string networkName,
            NetworkCredential credentials)
        {
            this._networkName = networkName;
            this._credentials = credentials;

            this.InitializeErrorhints();
        }

        #endregion

        #region Destruktor

        ~C_NetworkConnection()
        {
            Dispose(false);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Verbindet die zuvor definierte Netzwerkfreigabe. Im Fehlerfall ein das Event NetworkError ausgelöst.
        /// </summary>
        /// <returns>Ergebnis des Verbindugsversuchs</returns>
        public String Connect()
        {
            var netResource = new NetworkResource()
            {
                _Scope = ResourceScope.GlobalNetwork,
                _ResourceType = ResourceType.Disk,
                _DisplayType = ResourceDisplaytype.Share,
                _RemoteName = this._networkName
            };

            var userName = string.IsNullOrEmpty(this._credentials.Domain)
                ? this._credentials.UserName
                : string.Format(@"{0}\{1}", this._credentials.Domain, this._credentials.UserName);

            var result = WNetAddConnection2(
                netResource,
                this._credentials.Password,
                userName,
                0);


            if (result != 0)
            {
                if (this.NetworkError != null)
                {
                    String Errorhint = "";
                    if (this.Errorhints.ContainsKey(result.ToString()))
                    {
                        Errorhint += "\r\n" + this.Errorhints[result.ToString()];
                    }

                    this.NetworkError(this, new ErrorEventArgs("Error connecting to remote share \"" + this._networkName + "\" as user \"" + this._credentials.UserName + "@" + this._credentials.Domain + "\". Errorcode is: " + result.ToString() + Errorhint)); //Wieder einkommentieren
                    //this.NetworkError(this, new ErrorEventArgs("Error connecting to remote share \"" + this._networkName + "\" as user \"" + this._credentials.UserName + "@" + this._credentials.Domain + "\" using Password \"" + this._credentials.Password + "\". Errorcode is: " + result.ToString())); //DEBUG!! //Auskommentieren!!
                }
            }

            return result.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            WNetCancelConnection2(_networkName, 0, true);
        }
        #endregion

        #region Statics

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetworkResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags,
            bool force);
        #endregion
    }

    /// <summary>
    /// Diese Klasse stellt eine Netzwerkressource dar und ermöglicht den Zugriff auf Netzwerkfreigaben
    /// </summary>
    /// 
    #region Konstruktor
    [StructLayout(LayoutKind.Sequential)]
    public class NetworkResource
    {
        public ResourceScope _Scope;
        public ResourceType _ResourceType;
        public ResourceDisplaytype _DisplayType;
        public int _Usage;
        public string _LocalName;
        public string _RemoteName;
        public string _Comment;
        public string _Provider;
    }
    #endregion

    #region Enumerates

    public enum ResourceScope : int
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    };

    public enum ResourceType : int
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8,
    }

    public enum ResourceDisplaytype : int
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        Shareadmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        Ndscontainer = 0x0b
    }

    #endregion
}
