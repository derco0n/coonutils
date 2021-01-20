using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Eventargs for process-progress
    /// Author:              D. Marx
    /// Project: https://github.com/derco0n/coonutils   
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    ///</summary>
    public class ProgressEventArgs
    {
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
