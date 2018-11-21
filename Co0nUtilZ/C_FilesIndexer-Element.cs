using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Co0nSearchC
{
    /// <summary>
    /// Defines an Element found
    /// </summary>
    public class C_FilesIndexerElement:IComparable<C_FilesIndexerElement>
    {
        #region statics
        public static int TYPE_FOLDER = 1;
        public static int TYPE_FILE = 2;
        #endregion

        #region variables
        private int _Type;
        private String _Name;
        #endregion

        #region constructor
        /// <summary>
        /// Creates a new Instance of C_Element
        /// </summary>
        /// <param name="ObjName">Objectname</param>
        /// <param name="ObjType">Objecttype (File or Folder - see Statics)</param>
        public C_FilesIndexerElement(String ObjName, int ObjType)
        {
            this._Name = ObjName;
            this._Type = ObjType;
        }
        #endregion

        #region properties
        public String Name
        {
            get
            {
                return this._Name;
            }
        }

        public int Type
        {
            get
            {
                return this._Type;
            }
        }

        public DirectoryInfo folderInfo
        {
            get
            {
                return new DirectoryInfo(this._Name);
            }
        }

        public FileInfo fileInfo
        {
            get
            {
                if (this._Type == TYPE_FILE)
                {
                    return new FileInfo(this._Name);
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion


        #region methods
        /// <summary>
        /// Compares one C_FilesIndexElement to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(C_FilesIndexerElement other)
        {
            return this._Name.CompareTo(other.Name);
        }

        /// <summary>
        /// Overrides the default toString() and returns Type and Name as String.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //return base.ToString();
            String myreturn = "";
            if (this._Type == TYPE_FILE)
            {
                myreturn = "[D]";
            }
            else if (this._Type == TYPE_FOLDER)
            {
                myreturn = "[V]";
            }
            myreturn = myreturn + " "+ this._Name ;

            /*
            try
            {
                if (this.Type == TYPE_FILE)
                {
                    myreturn = myreturn + " -> Letzte Änderung: " + this.fileInfo.LastWriteTime.ToLocalTime().ToString();
                }
                else if (this.Type == TYPE_FOLDER)
                {
                    myreturn = myreturn + " -> Letzte Änderung: " + this.folderInfo.LastWriteTime.ToLocalTime().ToString();
                }
            }
            catch
            {

            }
            */ 
            return myreturn;
        }
        #endregion
    }
}
