using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.Collections;
using System.DirectoryServices.Protocols;

namespace Co0nUtilZ
{

    public class C_LDAPHelper
    {
        public static string ADOBJ_TYPE_SIMPLE = "(objectClass=simpleSecurityObject)";
        public static string ADOBJ_TYPE_USER = "(objectClass=user)";
        public static string ADOBJ_TYPE_COMPUTER = "(objectClass=computer)";
        public static string ADOBJ_TYPE_PRINTQUEUE = "(&(&(&(uncName=*)(objectCategory=printQueue))))";

        private string _domain;

        /// <summary>
        /// Creates a new instance of C_LDAPHelper, which allows to query LDAP-Services like MS-Active Directory
        /// </summary>
        /// <param name="fqdn">The FQDN of the target domain.</param>
        public C_LDAPHelper(string fqdn)
        {
            this._domain = fqdn;
        }

        /// <summary>
        /// Retrieves a list of LDAP-Object of a given type
        /// </summary>
        /// <param name="objectclass"></param>
        /// <returns></returns>
        public ArrayList searchObject(string objectclass)
        {
            ArrayList results = new ArrayList();

            DirectoryEntry entry = new DirectoryEntry("LDAP://"+this._domain);
            DirectorySearcher mySearcher = new DirectorySearcher(entry);
            mySearcher.Filter = (objectclass);
            mySearcher.SizeLimit = int.MaxValue;
            mySearcher.PageSize = int.MaxValue;

            foreach (SearchResult resEnt in mySearcher.FindAll())
            {
                results.Add(resEnt);
            }

            mySearcher.Dispose();
            entry.Dispose();

            return results;
        }

        private ArrayList getObjectList(string objectclass, string propertyname)
        {
            ArrayList objs = this.searchObject(objectclass);
            ArrayList results = new ArrayList();

            foreach (SearchResult obj in objs)
            {
                DirectoryEntry entry = obj.GetDirectoryEntry();
                string val = entry.Properties[propertyname].Value.ToString();
                if (!results.Contains(val))
                {
                    results.Add(val);
                }
            }
            results.Sort();
            return results;
        }

        /// <summary>
        /// Retrieves a list of all computers which are hosting printservices from Active-Directory
        /// </summary>
        /// <returns></returns>
        public ArrayList searchPrintservers()
        {
            return this.getObjectList(ADOBJ_TYPE_PRINTQUEUE, "servername");
        }

        /// <summary>
        /// Retrieves a list of all computers from Active-Directory
        /// </summary>
        /// <returns></returns>
        public ArrayList searchComputers()
        {
            return this.getObjectList(ADOBJ_TYPE_COMPUTER, "name");
        }

        public DirectoryEntry getADObject(string objectclass, string name)
        {
            if (!name.StartsWith("CN="))
            {
                name = "CN=" + name;
            }
            name = name.ToLower();
            DirectoryEntry result = null;
            ArrayList objs = this.searchObject(objectclass); //It is highly inefficient to get a complete list of all matching object-types in the first place. TODO: adjust searchObject() to searchOne() instead of searchAll() when looking up a specific object (by name).
            foreach (SearchResult obj in objs)
            {
                DirectoryEntry entry = obj.GetDirectoryEntry();
                if (entry.Name.ToLower().Equals(name))
                {
                    result = entry;
                    break;
                }
            }
            return result;
        }

    }
}
