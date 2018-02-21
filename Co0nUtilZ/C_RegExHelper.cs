using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Co0nUtilZ
{
    /// <summary>
    /// Helperclass for regular expressions
    /// Created:           12.10.2017
    /// Author:              D. Marx
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    /// </summary>
    public class C_RegExHelper
    { 

        public List<String> SplitString(String str, int chunks)
        {
            List<String> myreturn = new List<string>();

            String regexFilter = @".{1," + chunks.ToString() + "}";
            MatchCollection matches = Regex.Matches(str, regexFilter);

            foreach (Match m in matches)
            {
                myreturn.Add(m.Value);
            }

            return myreturn;
        }



        /// <summary>
        /// Filtert aus einer Liste mit Strings, die Einträge heraus die mit dem angegebenen Filter übereinstimmen.
        /// </summary>
        /// <param name="input">Eingabeliste</param>
        /// <param name="RegexFilter">Filter</param>
        /// <returns>Liste mit Einträgen die mit dem Filter übereinstimmen</returns>
        public List<String> FilterStringListByRegex(List<String> input, String RegexFilter)
        {
            List<String> myreturn = new List<string>();

            if (this.RegExFilterisGood(RegexFilter))
            {

                Regex rgx = new Regex(RegexFilter);
                foreach (String file in input)
                {//Alle Quelldateien abarbeiten
                    if (this.CheckRegEx(file, RegexFilter) == 0)
                    {
                        //Diese Datei wird vom Regex-Filter erfasst
                        myreturn.Add(file); //Passende Datei der Rückgabeliste hinzufügen
                    }
                }
            }

            return myreturn;
        }

        /// <summary>
        /// Testet ob ein RegEx-Filter gültig ist
        /// </summary>
        /// <param name="RegexFilter">Filterstring</param>
        /// <returns>Wenn gültig wird TRUE zurückgegeben, sonst FALSE</returns>
        public bool RegExFilterisGood(String RegexFilter)
        {
            try
            {
                Regex rgx = new Regex(RegexFilter);
                return true;
            }
            catch
            {

            }

            return false;
        }

        /// <summary>
        /// Prüft ob eine Zeichenfolge vom Filter erfasst wird.
        /// </summary>
        /// <param name="Testword">Zu testende Zeichenfolge</param>
        /// <param name="RegexFilter">Reg.-Ex-Filter</param>
        /// <returns>0=Übereinstimmung,1=Keine Übereinstimmung,2=Filter ungültig,3=Testword oder RegexFilter nicht gesetzt.</returns>
        public int CheckRegEx(String Testword, String RegexFilter)
        {
            if (!this.RegExFilterisGood(RegexFilter))
            {
                return 2;
            }

            if (!Testword.Equals("") && !RegexFilter.Equals(""))
            {
                /*
                try
                {*/
                //String escapedRegExFilter = RegexFilter;  //Regex.Escape(RegexFilter);
                Regex rgx = new Regex(RegexFilter.ToLower());
                Match newmatch = rgx.Match(Testword.ToLower());
                bool result = newmatch.Success;
                if (result)
                {//Match
                    return 0;
                }
                else
                {//Kein Match
                    return 1;
                }
                /* }
                 catch (Exception ex)
                 {

                     return 2;
                 }*/
            }
            else
            {
                //Filter oder Testbegriff nicht eingegeben
                return 3;
            }

        }
    }
}
