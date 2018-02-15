using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Eventargumente für Prozessfortschritt
    /// </summary>
    public class ProgressEventArgs
    {//Eventargumente für Prozessfortschritt
        /// <summary>
        /// Eventargumente for Fortschritt
        /// </summary>
        /// <param name="val">Fortschrittswert (Prozentual 1-100)</param>
        /// <param name="Msg">Optional: Nachricht (z.B. aktuelle Datei)</param>
        public ProgressEventArgs(int val, String Msg = "")
        {
            ProgVal = val;
            Message = Msg;
        }

        public int ProgVal
        {
            get;
            private set;
        } // readonly
        public String Message
        {
            get;
            private set;
        } // readonly
    }
}
