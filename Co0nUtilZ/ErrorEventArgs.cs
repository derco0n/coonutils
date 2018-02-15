using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Eventargumente für Fehler
    /// </summary>
    public class ErrorEventArgs
    {//Eventargumente für Fehler, 31.08.2017
        public ErrorEventArgs(String Error)
        {
            Err = Error;
        }
        public string Err
        {
            get;
            private set;
        } // readonly
    }
}
