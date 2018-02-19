using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{
    /// <summary>
    /// Eventargs for errors
    /// License: 
    /// GPLv3 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-3.0.de.html
    /// </summary>
    public class ErrorEventArgs
    {
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
