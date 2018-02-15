using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Co0nUtilZ
{

    /// <summary>
    /// Diese Klasse gewährt Zugriff auf eine Samba/CIFS Freigabe
    /// Erstellt:           08/2017
    /// Autor:              D. Marx
    /// Version:            0.55
    /// Letzte Änderung:    11.01.2018
    /// </summary>
    public class C_CIFSClient 
    {
        public static String AUTHTYPE_DIGEST = "Digest";
        public static String AUTHTYPE_BASIC = "Basic";


        private C_NetworkConnection nc;

        private NetworkCredential _NetworkCredential;
        private CredentialCache _NetCache;

        private String _Server, _User, _Password, _Domain, _Authtype;


        #region properties
        //Eigenschaften
        /*
        private bool _isConnected = false;
        public bool isConnected
        {
            get
            {
                return this._isConnected;
            }
        }
        */
        #endregion


        #region Events
        //Delegates und Events
        public delegate void CompleteEventhandler();
        public delegate void MultiLoadProgressEventHandler(object sender, ProgressEventArgs Args);
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs Fehler);
        public event MultiLoadProgressEventHandler CopyProgressChanged;
        public event CompleteEventhandler CopyProgressComplete;
        public event MultiLoadProgressEventHandler MoveProgressChanged;
        public event CompleteEventhandler MoveProgressComplete;
        public event ErrorEventHandler FileTransferError;
        public event ErrorEventHandler GeneralError;

        #endregion

        #region Handler
        private void Handle_UnderlyingNetworkError(object sender, ErrorEventArgs e) //Fehler beim Einbinden der Netzwerkfreigabe behandeln.
        {
            if (this.GeneralError != null)
            {
                this.GeneralError(
                    this,
                    e
                    );
            }
        }
        #endregion

        #region Konstruktor

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="Server">\\Servername oder IP</Servername></param>
        /// <param name="User">Benutzername</param>
        /// <param name="Password">Passwort</param>
        /// <param name="Domain">Domänenname</param>
        /// <param name="Authtype"></param>
        public C_CIFSClient(String Server, String User, String Password, String Domain, String Authtype = "Digest")
        {
            this._Server = @Server;
            this._User = @User;
            this._Password = @Password;
            this._Domain = @Domain;
            this._Authtype = @Authtype;

            try
            {
                if (!this._Server.Contains(@"\\"))
                {
                    //Server beginnt nicht mit \\
                    this._Server = @"\\" + this._Server;
                }
                this._NetworkCredential = new NetworkCredential(this._User, this._Password, this._Domain);
                this._NetCache = new CredentialCache();
                this._NetCache.Add(new Uri(this._Server), Authtype, this._NetworkCredential);
            }
            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(this, new ErrorEventArgs("Fehler bei der Initilaisierung des CIFS-Client:" + ex.ToString()));
                }
            }



        }
        #endregion

        #region Methods

        /// <summary>
        /// Gibt die Ordnerinformationen eines unterordners aus
        /// </summary>
        /// <param name="Subdir">Pfad auf dem Server (alles nach "\\Server\")</param>
        /// <returns>Liste mit allen enthalteten Unterordnern</returns>
        public DirectoryInfo getFolderInfo(String Subdir)
        {
            DirectoryInfo myReturn = null;
            String Searchpath = this._Server + @"\" + @Subdir;
            try
            {


                using (C_NetworkConnection nc = new C_NetworkConnection(Searchpath, this._NetworkCredential))
                {

                    nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                    if (!nc.Connect().Equals("0")) //Vebindung herstellen
                    {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                        return myReturn; //Abbruch                       

                    }


                    myReturn = new DirectoryInfo(Searchpath);
                }

            }
            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(this, new ErrorEventArgs("Fehler beim Ermitteln der Verzeichnisinformationen von \"" + Searchpath + "\". Details: " + ex.ToString()));
                }
            }

            return myReturn;
        }



        /// <summary>
        /// Ermittelt alle Ordner eines Unterordners
        /// </summary>
        /// <param name="Subdir">Pfad auf dem Server (alles nach "\\Server\")</param>
        /// <returns>Liste mit allen enthalteten Unterordnern</returns>
        public List<DirectoryInfo> listFolders(String Subdir, bool isLocaldir = false)
        {
            List<DirectoryInfo> myReturn = new List<DirectoryInfo>();
            if (!isLocaldir)
            { //Remoteordner
                String Searchpath = this._Server + @"\" + @Subdir;
                try
                {
                    using (C_NetworkConnection nc = new C_NetworkConnection(Searchpath, this._NetworkCredential))
                    {
                        nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                        if (!nc.Connect().Equals("0")) //Vebindung herstellen
                        {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                            return myReturn; //Abbruch                       

                        }

                        string[] Folders = Directory.GetDirectories(@Searchpath);
                        foreach (String Folder in Folders)
                        {
                            myReturn.Add(new DirectoryInfo(Folder));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this.GeneralError != null)
                    {
                        this.GeneralError(this, new ErrorEventArgs("Fehler beim Auflisten der Remote-Ordner: " + ex.ToString()));
                    }
                }
            }
            else
            {
                //Lokaler Ordner
                try
                {
                    string[] Folders = Directory.GetDirectories(@Subdir);
                    foreach (String Folder in Folders)
                    {
                        myReturn.Add(new DirectoryInfo(Folder));
                    }
                }
                catch (Exception ex)
                {
                    if (this.GeneralError != null)
                    {
                        this.GeneralError(this, new ErrorEventArgs("Fehler beim Auflisten der lokalen Ordner: " + ex.ToString()));
                    }
                }
            }

            return myReturn;
        }

        /// <summary>
        /// Ermittelt alle Dateien eines Ordners
        /// </summary>
        /// <param name="Subdir">Pfad auf dem Server (alles nach "\\Server\")</param>
        /// <returns>Liste mit allen enthaltenen Dateien</returns>
        public List<FileInfo> listFiles(String Subdir, bool isLocaldir = false)
        {
            List<FileInfo> myReturn = new List<FileInfo>();

            if (!isLocaldir)
            { //Remoteordner

                String Searchpath = "";
                if (Subdir[0] == '\\')
                {//Wenn der Freigabepfad (alles nach dem Server) mit '\' beginnt
                    Searchpath = this._Server + @Subdir;
                }
                else
                {
                    // Führendes '\' fehlt
                    Searchpath = this._Server + @"\" + @Subdir;
                }


                try
                {
                    using (C_NetworkConnection nc = new C_NetworkConnection(Searchpath, this._NetworkCredential))
                    {
                        nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                        if (!nc.Connect().Equals("0")) //Vebindung herstellen
                        {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                            return myReturn; //Abbruch                       

                        }

                        string[] Files = Directory.GetFiles(@Searchpath);
                        foreach (String file in Files)
                        {
                            myReturn.Add(new FileInfo(file));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this.GeneralError != null)
                    {
                        this.GeneralError(
                            this,
                            new ErrorEventArgs("Fehler beim Auflisten der Remote-Dateien: " + ex.ToString())
                            );
                    }
                }
            }
            else
            {
                //Lokaler Ordner
                try
                {
                    string[] Files = Directory.GetFiles(@Subdir);
                    foreach (String file in Files)
                    {
                        myReturn.Add(new FileInfo(file));
                    }
                }
                catch (Exception ex)
                {
                    if (this.GeneralError != null)
                    {
                        this.GeneralError(
                            this,
                            new ErrorEventArgs("Fehler beim Auflisten der lokalen Dateien: " + ex.ToString())
                            );
                    }
                }

            }

            return myReturn;
        }

        /// <summary>
        /// Ermittelt die jüngste Datei (letzer Schreibzugriff) eines Ordners
        /// </summary>
        /// <param name="Subdir">Pfad auf dem Server (alles nach "\\Server\")</param>
        /// <returns>KeyValuePair der jüngsten Datei</returns>
        public KeyValuePair<String, DateTime> youngestFile(String Subdir, bool isLocalDir)
        {
            KeyValuePair<String, DateTime> myReturn = new KeyValuePair<String, DateTime>();
            Dictionary<string, DateTime> FileDates = new Dictionary<string, DateTime>();

            if (!isLocalDir)
            { //Remoteordner

                String Searchpath = "";
                if (Subdir[0] == '\\')
                {//Wenn der Freigabepfad (alles nach dem Server) mit '\' beginnt
                    Searchpath = this._Server + @Subdir;
                }
                else
                {
                    // Führendes '\' fehlt
                    Searchpath = this._Server + @"\" + @Subdir;
                }

                using (C_NetworkConnection nc = new C_NetworkConnection(Searchpath, this._NetworkCredential))
                {
                    nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                    if (!nc.Connect().Equals("0")) //Vebindung herstellen
                    {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                        return myReturn; //Abbruch                       

                    }

                    try
                    {
                        List<FileInfo> Filelist = this.listFiles(Subdir, false);
                        foreach (FileInfo File in Filelist)
                        {
                            if (//Wenn die aktuelle Datei noch nicht im Dictionary ist....
                                !FileDates.Contains(new KeyValuePair<String, DateTime>(File.Name, File.LastWriteTimeUtc))
                                )
                            {
                                FileDates.Add(File.Name, File.LastWriteTimeUtc); //Diese dem Dictionary hinzufügen.
                            }

                            foreach (KeyValuePair<String, DateTime> entry in FileDates)
                            {//Alle Dateien parsen
                                if (entry.Value > myReturn.Value)
                                {//Wenn der aktuelle Eintrag jünger als der frisch hinzugefügte ist
                                    myReturn = entry; //Diesen übernehmen
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        if (this.GeneralError != null)
                        {
                            this.GeneralError(
                                this,
                                new ErrorEventArgs("Fehler beim Ermitteln der jüngsten Datei! " + ex.ToString()
                                )
                                );
                        }
                    }
                } //End USING
            }
            else
            {
                //Lokaler Ordner
                try
                {
                    List<FileInfo> Filelist = this.listFiles(Subdir, true);
                    foreach (FileInfo File in Filelist)
                    {
                        if (//Wenn die aktuelle Datei noch nicht im Dictionary ist....
                            !FileDates.Contains(new KeyValuePair<String, DateTime>(File.Name, File.LastWriteTimeUtc))
                            )
                        {
                            FileDates.Add(File.Name, File.LastWriteTimeUtc); //Diese dem Dictionary hinzufügen.
                        }

                        foreach (KeyValuePair<String, DateTime> entry in FileDates)
                        {//Alle Dateien parsen
                            if (entry.Value > myReturn.Value)
                            {//Wenn der aktuelle Eintrag jünger als der frisch hinzugefügte ist
                                myReturn = entry; //Diesen übernehmen
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    if (this.GeneralError != null)
                    {
                        this.GeneralError(
                            this,
                            new ErrorEventArgs("Fehler beim Ermitteln der jüngsten Datei! " + ex.ToString()
                            )
                            );
                    }
                }
            }
            return myReturn; //Jüngste, ermittelte Datei zurückgeben
        }

        /// <summary>
        /// Prüft ob eine Datei die jüngste im angegebenen Ordner ist
        /// </summary>
        /// <param name="Subdir">Pfad auf dem Server (alles nach "\\Server\")</param>
        /// <param name="Filename">Dateiname</param>
        /// <returns>Gibt im Wahrheitsfall TRUE zurück sonst FALSE</returns>
        public bool isYoungestFile(String Subdir, String Filename, bool isLocalDir = false)
        {

            if (!isLocalDir)
            {//Remoteordner

                String Searchpath = "";
                if (Subdir[0] == '\\')
                {//Wenn der Freigabepfad (alles nach dem Server) mit '\' beginnt
                    Searchpath = this._Server + @Subdir;
                }
                else
                {
                    // Führendes '\' fehlt
                    Searchpath = this._Server + @"\" + @Subdir;
                }

                using (C_NetworkConnection nc = new C_NetworkConnection(Searchpath, this._NetworkCredential))
                {
                    nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                    if (!nc.Connect().Equals("0")) //Vebindung herstellen
                    {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                        return false; //Abbruch                       

                    }

                    try
                    {
                        KeyValuePair<String, DateTime> yfile = this.youngestFile(@Subdir, isLocalDir);
                        if (yfile.Key.Equals(@Filename))
                        {
                            return true;
                        }
                    }

                    catch (Exception ex)
                    {
                        if (this.GeneralError != null)
                        {
                            this.GeneralError(
                                this,
                                new ErrorEventArgs("Fehler beim Prüfen ob die Datei " + Filename + " die jüngste ist! " + ex.ToString()
                                )
                                );
                        }
                    }
                }

            }
            else
            {//lokaler Ordner
                KeyValuePair<String, DateTime> yfile = this.youngestFile(@Subdir, isLocalDir);
                if (yfile.Key.Equals(@Filename))
                {
                    return true;
                }

            }



            return false;
        }

        /// <summary>
        /// Prüft ob eine Verbindung zum Zielordner möglich ist
        /// </summary>
        /// <param name="Subdir">Unterordner auf dem Server</param>
        /// <returns>Wenn der Ordner existiert (und der Zugriff darauf möglich ist) wird TRUE zurückgegeben.</returns>
        public bool TestConnection(String Subdir)
        {
            try
            {
                String Searchpath = "";
                if (Subdir[0] == '\\')
                {//Wenn der Freigabepfad (alles nach dem Server) mit '\' beginnt
                    Searchpath = this._Server + @Subdir;
                }
                else
                {
                    // Führendes '\' fehlt
                    Searchpath = this._Server + @"\" + @Subdir;
                }

                DirectoryInfo DirInfo = new DirectoryInfo(Searchpath);

                using (C_NetworkConnection nc = new C_NetworkConnection(Searchpath, this._NetworkCredential))
                {

                    nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                    if (!nc.Connect().Equals("0")) //Vebindung herstellen
                    {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                        return false; //Abbruch                       

                    }

                    return DirInfo.Exists; //Wenn Ordner existiert wird true zurückgegeben, sonst false
                }
            }

            catch (Exception ex)
            {
                if (this.GeneralError != null)
                {
                    this.GeneralError(
                        this,
                        new ErrorEventArgs("Fehler beim Prüfen der Verbindung! " + ex.ToString()
                        )
                        );
                }
            }


            return false; //Im Fehlerfall wird False zurückgegeben
        }


        #region fileoperations
        #region fromcifs
        //From_CIFS

        #region copyfile
        /// <summary>
        /// Erzeugt einen Thread welcher, die Dateiliste nach und nach aus einer CIFS-Freigabe herunterlädt
        /// </summary>
        /// <param name="Files">Liste mit Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameyoungest">Die jüngste Datei als zusätzlich umbennante Kopie speichern</param>
        /// <param name="renameTo">Neue Bezeichnung für die umbenannte Kopie</param>
        /// <param name="overwrite">Existierende Dateien überschreiben.</param>
        /// <returns></returns>
        public System.Threading.Thread CopyMultipleFilesFromCIFS(List<FileInfo> Files, DirectoryInfo Targetfolder, bool renameyoungest = false, String renameTo = "", bool overwrite = true)
        {            //Erzeugt einen Thread welcher, die Dateiliste nach und nach mittels DownloadFile herunterlädt
            var t = new System.Threading.Thread(() => CopyFilesFromCIFSWorker(Files, Targetfolder, renameyoungest, renameTo, overwrite));
            t.Start();
            return t;
        }

        /// <summary>
        /// Workermethode für das Kopieren der Dateien aus einer CIFS-Freigabe
        /// </summary>
        /// <param name="Files">Liste mit Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="duplicateRenameYoungest">Die jüngste Datei als zusätzlich umbennante Kopie speichern</param>
        /// <param name="DuplicateRenameYoungestTo">Neue Bezeichnung für die umbenannte Kopie</param>
        /// <param name="overwrite">Existierende Dateien überschreiben.</param>
        private void CopyFilesFromCIFSWorker(List<FileInfo> Files, DirectoryInfo Targetfolder, bool duplicateRenameYoungest, String DuplicateRenameYoungestTo, bool overwrite)
        {//Workermethode für das Kopieren der Dateien
            int Filecount = Files.Count();
            int Filestransfered = 0;
            double percentcomplete = 0;

            foreach (FileInfo File in Files)
            {
                bool success = false;

                String Subdirectory = File.DirectoryName;
                if (Subdirectory.Contains(this._Server))
                {
                    int ServerPositionInDir = Subdirectory.IndexOf(this._Server);
                    int ServerPositionLen = this._Server.Length;
                    Subdirectory = Subdirectory.Substring(ServerPositionInDir + ServerPositionLen);
                }


                bool isYoungest = this.isYoungestFile(Subdirectory, File.Name, false);

                if (duplicateRenameYoungest && isYoungest && !DuplicateRenameYoungestTo.Equals(""))
                {
                    //Wenn die jüngste Datei als zusätzliche, umbenannte Kopie abgestellt werden soll
                    success = this.CopyFileFromCIFS(File, Targetfolder, DuplicateRenameYoungestTo, true);
                }
                //else
                //{//Datei soll nicht umbenannt werden
                success = this.CopyFileFromCIFS(File, Targetfolder, "", true); //Datei kopieren (Jede Datei auch etwaige zuvor umbenannte, da wir beide haben wollen
                                                                               //}

                if (!success)
                {//Fehler

                }
                //Fortschritt aktualisieren.
                Filestransfered += 1;
                percentcomplete = 100.0 / Filecount * Filestransfered;


                if (this.CopyProgressChanged != null)
                {
                    this.CopyProgressChanged(
                       this,
                        new ProgressEventArgs(
                            (int)Math.Round(percentcomplete, 0),
                        "Datei " + File.FullName + " (" + ((double)File.Length / 1024).ToString() + " kBytes) wurde nach " + Targetfolder.FullName + " kopiert.")
                            ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                }


            }

            //Fertig:
            if (this.CopyProgressComplete != null)
            {
                this.CopyProgressComplete();//Event auslösen falls abboniert
            }

        }




        /// <summary>
        /// Kopiert eine Datei aus einer CIFS-Freigabe
        /// </summary>
        /// <param name="File">Quelldatei</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameto">Zieldatei zusätlich unter folgendem Namen speichern</param>
        /// <param name="overwrite">Etwaige vorhandene Datei überschreiben</param>
        /// <returns>Gibt bei Erfolg TRUE zurück</returns>
        public bool CopyFileFromCIFS(FileInfo File, DirectoryInfo Targetfolder, String renameto = "", bool overwrite = true)
        {
            String Target = Targetfolder.FullName + @"\" + File.Name;
            bool myreturn = false;

            using (C_NetworkConnection nc = new C_NetworkConnection(File.DirectoryName, this._NetworkCredential))
            {
                nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                if (!nc.Connect().Equals("0")) //Vebindung herstellen
                {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                    return myreturn; //Abbruch                       

                }

                try
                {
                    if (renameto.Equals("")) //Keine neue Bezeichnung gesetzt
                    {//Ursprünglichen Dateinamen beibehalten

                    }
                    else //Neue Bezeichnung übernehmen
                    {
                        Target = Targetfolder.FullName + @"\" + renameto;
                    }

                    if (System.IO.File.Exists(Target) && overwrite == true)
                    {
                        try
                        {
                            //Zieldatei existiert bereits und soll laut Parameter überschrieben werden
                            System.IO.File.Delete(Target);
                        }
                        catch (Exception ex)
                        {
                            //Nicht erfolgreich
                            if (this.FileTransferError != null)
                            {
                                this.FileTransferError(
                                        this,
                                        new ErrorEventArgs(
                                            "CopyFileFromCIFS: Fehler beim Überschreiben von: " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                            );
                            }
                        }
                    }
                    else if (System.IO.File.Exists(Target) && overwrite == false)
                    {//Nicht erfolgreich
                        if (this.FileTransferError != null)
                        {
                            this.FileTransferError(
                                    this,
                                    new ErrorEventArgs(
                                        "CopyFileFromCIFS: Zieldatei: " + Target + " existiert bereits soll aber nicht überschrieben werden! Abbruch des Kopiervorgangs.")
                                        );
                        }
                        myreturn = false;
                    }


                    File.CopyTo(Target);
                    myreturn = true;
                }

                catch (Exception ex)
                {
                    //Nicht erfolgreich
                    if (this.FileTransferError != null)
                    {
                        this.FileTransferError(
                                this,
                                new ErrorEventArgs(
                                    "CopyFileFromCIFS: Fehler beim Übertragen von: " + File.FullName + " nach " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                    );
                    }
                }
            }
            return myreturn;
        }
        #endregion

        #region movefile

        //Aus CIFS-Freigabe verschieben
        /// <summary>
        /// Verschiebt mehrere Dateien einer CIFS-Freigabe in einen lokalen ordner
        /// </summary>
        /// <param name="Files">Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameyoungest">jüngste Datei umbennen?</param>
        /// <param name="renameTo">Neuer Name für umbenannte Datei</param>
        /// <param name="overwrite">Vorhandene Dateien überschreiben?</param>
        /// <returns></returns>
        public System.Threading.Thread MoveMultipleFilesFromCIFS(List<FileInfo> Files, DirectoryInfo Targetfolder, bool renameyoungest = false, String renameTo = "", bool overwrite = true)
        {            //Erzeugt einen Thread welcher, die Dateiliste nach und nach mittels DownloadFile herunterlädt
            var t = new System.Threading.Thread(() => MoveFilesFromCIFSWorker(Files, Targetfolder, renameyoungest, renameTo, overwrite));
            t.Start();
            return t;
        }

        /// <summary>
        /// Workermethode für den Verschiebethread MoveMultipleFilesFroMCIFS
        /// </summary>
        /// <param name="Files">Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameyoungest">jüngste Datei umbennen?</param>
        /// <param name="renameTo">Neuer Name für umbenannte Datei</param>
        /// <param name="overwrite">Vorhandene Dateien überschreiben?</param>
        private void MoveFilesFromCIFSWorker(List<FileInfo> Files, DirectoryInfo Targetfolder, bool duplicateRenameYoungest, String DuplicateRenameYoungestTo, bool overwrite)
        {//Workermethode für das Verschieben der Dateien
            int Filecount = Files.Count();
            int Filestransfered = 0;
            double percentcomplete = 0;

            foreach (FileInfo File in Files)
            {
                bool success = false;

                String Subdirectory = File.DirectoryName;
                if (Subdirectory.Contains(this._Server))
                {
                    int ServerPositionInDir = Subdirectory.IndexOf(this._Server);
                    int ServerPositionLen = this._Server.Length;
                    Subdirectory = Subdirectory.Substring(ServerPositionInDir + ServerPositionLen);
                }


                bool isYoungest = this.isYoungestFile(Subdirectory, File.Name, false);

                if (duplicateRenameYoungest && isYoungest && !DuplicateRenameYoungestTo.Equals(""))
                {
                    //Wenn die jüngste Datei als zusätzliche, umbenannte Kopie abgestellt werden soll
                    success = this.CopyFileFromCIFS(File, Targetfolder, DuplicateRenameYoungestTo, true);//Datei kopieren 
                }
                //Datei soll nicht umbenannt werden
                success = this.MoveFileFromCIFS(File, Targetfolder, "", true); //Datei verschieben (Jede Datei auch etwaige zuvor umbenannte, da wir beide haben wollen


                if (!success)
                {//Fehler

                }
                //Fortschritt aktualisieren.
                Filestransfered += 1;
                percentcomplete = 100.0 / Filecount * Filestransfered;


                if (this.MoveProgressChanged != null)
                {
                    this.MoveProgressChanged(
                       this,
                        new ProgressEventArgs(
                            (int)Math.Round(percentcomplete, 0),
                        "Datei " + File.FullName + " (" + ((double)File.Length / 1024).ToString() + " kBytes) wurde nach " + Targetfolder.FullName + " verschoben.")
                            ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                }


            }

            //Fertig:
            if (this.MoveProgressComplete != null)
            {
                this.MoveProgressComplete();//Event auslösen falls abboniert
            }

        }

        /// <summary>
        /// Verschiebt eine Datei aus einer Netzwerkfreigabe in einen lokalen Ordner
        /// </summary>
        /// <param name="File">Quelldatei</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="TargetFilename">Zieldateiname</param>
        /// <param name="overwrite">Vorhandene Datei überschreiben?</param>
        /// <returns></returns>
        public bool MoveFileFromCIFS(FileInfo File, DirectoryInfo Targetfolder, String TargetFilename = "", bool overwrite = true)
        {
            String Target = Targetfolder.FullName + @"\" + File.Name;
            bool myreturn = false;

            using (C_NetworkConnection nc = new C_NetworkConnection(File.DirectoryName, this._NetworkCredential))
            {
                nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                if (!nc.Connect().Equals("0")) //Vebindung herstellen
                {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                    return myreturn; //Abbruch                       

                }

                try
                {
                    if (TargetFilename.Equals("")) //Keine neue Bezeichnung gesetzt
                    {//Ursprünglichen Dateinamen beibehalten

                    }
                    else //Neue Bezeichnung übernehmen
                    {
                        Target = Targetfolder.FullName + @"\" + TargetFilename;
                    }

                    if (System.IO.File.Exists(Target) && overwrite == true)
                    {
                        try
                        {
                            //Zieldatei existiert bereits und soll laut Parameter überschrieben werden
                            System.IO.File.Delete(Target);
                        }
                        catch (Exception ex)
                        {
                            //Nicht erfolgreich
                            if (this.FileTransferError != null)
                            {
                                this.FileTransferError(
                                        this,
                                        new ErrorEventArgs(
                                            "MoveFileFromCIFS: Fehler beim Überschreiben von: " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                            );
                            }
                        }
                    }
                    else if (System.IO.File.Exists(Target) && overwrite == false)
                    {//Nicht erfolgreich
                        if (this.FileTransferError != null)
                        {
                            this.FileTransferError(
                                    this,
                                    new ErrorEventArgs(
                                        "MoveFileFromCIFS: Zieldatei: " + Target + " existiert bereits soll aber nicht überschrieben werden! Abbruch des Verschiebevorgangs.")
                                        );
                        }
                        myreturn = false;
                    }


                    File.MoveTo(Target); //Verschieben
                    myreturn = true;
                }

                catch (Exception ex)
                {
                    //Nicht erfolgreich
                    if (this.FileTransferError != null)
                    {
                        this.FileTransferError(
                                this,
                                new ErrorEventArgs(
                                    "MoveFileFromCIFS: Fehler beim Übertragen von: " + File.FullName + " nach " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                    );
                    }
                }
            }
            return myreturn;
        }
        #endregion //movefile


        #endregion //fromcifs

        #region tocifs
        //To_CIFS

        #region copyfile
        /// <summary>
        /// Erzeugt einen Thread welcher, die Dateiliste nach und nach in eine CIFS-Freigabe hochlädt
        /// </summary>
        /// <param name="Files">Liste mit Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameyoungest">Die jüngste Datei als zusätzlich umbennante Kopie speichern</param>
        /// <param name="renameTo">Neue Bezeichnung für die umbenannte Kopie</param>
        /// <param name="overwrite">Existierende Dateien überschreiben.</param>
        /// <returns></returns>
        public System.Threading.Thread CopyMultipleFilesToCIFS(List<FileInfo> Files, DirectoryInfo Targetfolder, bool renameyoungest = false, String renameTo = "", bool overwrite = true)
        {            //Erzeugt einen Thread welcher, die Dateiliste nach und nach mittels DownloadFile herunterlädt
            var t = new System.Threading.Thread(() => CopyFilesToCIFSWorker(Files, Targetfolder, renameyoungest, renameTo, overwrite));
            t.Start();
            return t;
        }

        /// <summary>
        /// Workermethode für das Kopieren der Dateien in eine CIFS-Freigabe
        /// </summary>
        /// <param name="Files">Liste mit Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="duplicateRenameYoungest">Die jüngste Datei als zusätzlich umbennante Kopie speichern</param>
        /// <param name="DuplicateRenameYoungestTo">Neue Bezeichnung für die umbenannte Kopie</param>
        /// <param name="overwrite">Existierende Dateien überschreiben.</param>
        private void CopyFilesToCIFSWorker(List<FileInfo> Files, DirectoryInfo Targetfolder, bool duplicateRenameYoungest, String DuplicateRenameYoungestTo, bool overwrite)
        {//Workermethode für das Kopieren der Dateien
            int Filecount = Files.Count();
            int Filestransfered = 0;
            double percentcomplete = 0;

            foreach (FileInfo File in Files)
            {
                bool success = false;

                String Subdirectory = File.DirectoryName;
                if (Subdirectory.Contains(this._Server))
                {
                    int ServerPositionInDir = Subdirectory.IndexOf(this._Server);
                    int ServerPositionLen = this._Server.Length;
                    Subdirectory = Subdirectory.Substring(ServerPositionInDir + ServerPositionLen);
                }


                bool isYoungest = this.isYoungestFile(Subdirectory, File.Name, true/*Lokaler Ordner*/);

                if (duplicateRenameYoungest && isYoungest && !DuplicateRenameYoungestTo.Equals(""))
                {
                    //Wenn die jüngste Datei als zusätzliche, umbenannte Kopie abgestellt werden soll
                    success = this.CopyFileToCIFS(File, Targetfolder, DuplicateRenameYoungestTo, true);
                }
                //else
                //{//Datei soll nicht umbenannt werden
                success = this.CopyFileToCIFS(File, Targetfolder, "", true); //Datei kopieren (Jede Datei auch etwaige zuvor umbenannte, da wir beide haben wollen
                                                                             //}

                if (!success)
                {//Fehler

                }
                //Fortschritt aktualisieren.
                Filestransfered += 1;
                percentcomplete = 100.0 / Filecount * Filestransfered;


                if (this.CopyProgressChanged != null)
                {
                    this.CopyProgressChanged(
                       this,
                        new ProgressEventArgs(
                            (int)Math.Round(percentcomplete, 0),
                        "Datei " + File.FullName + " (" + ((double)File.Length / 1024).ToString() + " kBytes) wurde nach " + Targetfolder.FullName + " kopiert.")
                            ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                }


            }

            //Fertig:
            if (this.CopyProgressComplete != null)
            {
                this.CopyProgressComplete();//Event auslösen falls abboniert
            }

        }




        /// <summary>
        /// Kopiert eine Datei aus einer CIFS-Freigabe
        /// </summary>
        /// <param name="File">Quelldatei</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameto">Zieldatei zusätlich unter folgendem Namen speichern</param>
        /// <param name="overwrite">Etwaige vorhandene Datei überschreiben</param>
        /// <returns>Gibt bei Erfolg TRUE zurück</returns>
        public bool CopyFileToCIFS(FileInfo File, DirectoryInfo Targetfolder, String renameto = "", bool overwrite = true)
        {
            String Target = Targetfolder.FullName + @"\" + File.Name;
            bool myreturn = false;

            using (C_NetworkConnection nc = new C_NetworkConnection(Targetfolder.FullName, this._NetworkCredential))
            {
                nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                if (!nc.Connect().Equals("0")) //Vebindung herstellen
                {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                    return myreturn; //Abbruch                       

                }


                try
                {
                    if (renameto.Equals("")) //Keine neue Bezeichnung gesetzt
                    {//Ursprünglichen Dateinamen beibehalten

                    }
                    else //Neue Bezeichnung übernehmen
                    {
                        Target = Targetfolder.FullName + @"\" + renameto;
                    }

                    if (System.IO.File.Exists(Target) && overwrite == true)
                    {
                        try
                        {
                            //Zieldatei existiert bereits und soll laut Parameter überschrieben werden
                            System.IO.File.Delete(Target);
                        }
                        catch (Exception ex)
                        {
                            //Nicht erfolgreich
                            if (this.FileTransferError != null)
                            {
                                this.FileTransferError(
                                        this,
                                        new ErrorEventArgs(
                                            "CopyFileToCIFS: Fehler beim Überschreiben von: " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                            );
                            }
                        }
                    }
                    else if (System.IO.File.Exists(Target) && overwrite == false)
                    {//Nicht erfolgreich
                        if (this.FileTransferError != null)
                        {
                            this.FileTransferError(
                                    this,
                                    new ErrorEventArgs(
                                        "CopyFileToCIFS: Zieldatei: " + Target + " existiert bereits soll aber nicht überschrieben werden! Abbruch des Kopiervorgangs.")
                                        );
                        }
                        myreturn = false;
                    }


                    File.CopyTo(Target);
                    myreturn = true;
                }

                catch (Exception ex)
                {
                    //Nicht erfolgreich
                    if (this.FileTransferError != null)
                    {
                        this.FileTransferError(
                                this,
                                new ErrorEventArgs(
                                    "CopyFileToCIFS: Fehler beim Übertragen von: " + File.FullName + " nach " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                    );
                    }
                }
            }
            return myreturn;
        }

        #endregion //copyfile

        #region movefile

        //In eine CIFS-Freigabe verschieben
        /// <summary>
        /// Verschiebt mehrere Dateien von einem lokalen Ordner in eine CIFS-Freigabe
        /// </summary>
        /// <param name="Files">Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameyoungest">jüngste Datei umbennen?</param>
        /// <param name="renameTo">Neuer Name für umbenannte Datei</param>
        /// <param name="overwrite">Vorhandene Dateien überschreiben?</param>
        /// <returns></returns>
        public System.Threading.Thread MoveMultipleFilesToCIFS(List<FileInfo> Files, DirectoryInfo Targetfolder, bool renameyoungest = false, String renameTo = "", bool overwrite = true)
        {            //Erzeugt einen Thread welcher, die Dateiliste nach und nach mittels DownloadFile herunterlädt
            var t = new System.Threading.Thread(() => MoveFilesToCIFSWorker(Files, Targetfolder, renameyoungest, renameTo, overwrite));
            t.Start();
            return t;
        }

        /// <summary>
        /// Workermethode für den Verschiebethread MoveMultipleFilesToCIFS
        /// </summary>
        /// <param name="Files">Quelldateien</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="renameyoungest">jüngste Datei umbennen?</param>
        /// <param name="renameTo">Neuer Name für umbenannte Datei</param>
        /// <param name="overwrite">Vorhandene Dateien überschreiben?</param>
        private void MoveFilesToCIFSWorker(List<FileInfo> Files, DirectoryInfo Targetfolder, bool duplicateRenameYoungest, String DuplicateRenameYoungestTo, bool overwrite)
        {//Workermethode für das Verschieben der Dateien
            int Filecount = Files.Count();
            int Filestransfered = 0;
            double percentcomplete = 0;



            foreach (FileInfo File in Files)
            {
                bool success = false;

                String Subdirectory = File.DirectoryName;
                if (Subdirectory.Contains(this._Server))
                {
                    int ServerPositionInDir = Subdirectory.IndexOf(this._Server);
                    int ServerPositionLen = this._Server.Length;
                    Subdirectory = Subdirectory.Substring(ServerPositionInDir + ServerPositionLen);
                }


                bool isYoungest = this.isYoungestFile(Subdirectory, File.Name, true/*Lokaler Ordner*/);

                /*
                 * Umbenennen funktioniert beim Verschieben nicht, da die Quelldateien stets weniger werden
                if (duplicateRenameYoungest && isYoungest && !DuplicateRenameYoungestTo.Equals(""))
                {
                    //Wenn die jüngste Datei als zusätzliche, umbenannte Kopie abgestellt werden soll
                    success = this.CopyFileToCIFS(File, Targetfolder, DuplicateRenameYoungestTo, true);//Datei kopieren
                }
                */

                //Datei soll nicht umbenannt werden
                success = this.MoveFileToCIFS(File, Targetfolder, "", true); //Datei verschieben (Jede Datei auch etwaige zuvor umbenannte, da wir beide haben wollen


                if (!success)
                {//Fehler

                }
                //Fortschritt aktualisieren.
                Filestransfered += 1;
                percentcomplete = 100.0 / Filecount * Filestransfered;


                if (this.MoveProgressChanged != null)
                {
                    this.MoveProgressChanged(
                       this,
                        new ProgressEventArgs(
                            (int)Math.Round(percentcomplete, 0),
                        "Datei " + File.FullName + " (" + ((double)File.Length / 1024).ToString() + " kBytes) wurde nach " + Targetfolder.FullName + " verschoben.")
                            ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                }


            }

            //Fertig:
            if (this.MoveProgressComplete != null)
            {
                this.MoveProgressComplete();//Event auslösen falls abboniert
            }

        }

        /// <summary>
        /// Verschiebt eine Datei aus einem lokalen Ordner in eine Netzwerkfreigabe
        /// </summary>
        /// <param name="File">Quelldatei</param>
        /// <param name="Targetfolder">Zielordner</param>
        /// <param name="TargetFilename">Zieldateiname</param>
        /// <param name="overwrite">Vorhandene Datei überschreiben?</param>
        /// <returns></returns>
        public bool MoveFileToCIFS(FileInfo File, DirectoryInfo Targetfolder, String TargetFilename = "", bool overwrite = true)
        {
            String Target = Targetfolder.FullName + @"\" + File.Name;
            bool myreturn = false;

            //using (C_NetworkConnection nc = new C_NetworkConnection(File.DirectoryName, this._NetworkCredential))
            using (C_NetworkConnection nc = new C_NetworkConnection(Targetfolder.FullName, this._NetworkCredential))
            {
                nc.NetworkError += this.Handle_UnderlyingNetworkError; //Eventhandler abbonieren
                if (!nc.Connect().Equals("0")) //Vebindung herstellen
                {//Wenn das Ergebnis des Verbindungsaufbaus nich "0" (OK) war...
                    return myreturn; //Abbruch                       

                }

                try
                {
                    if (TargetFilename.Equals("")) //Keine neue Bezeichnung gesetzt
                    {//Ursprünglichen Dateinamen beibehalten

                    }
                    else //Neue Bezeichnung übernehmen
                    {
                        Target = Targetfolder.FullName + @"\" + TargetFilename;
                    }

                    if (System.IO.File.Exists(Target) && overwrite == true)
                    {
                        try
                        {
                            //Zieldatei existiert bereits und soll laut Parameter überschrieben werden
                            System.IO.File.Delete(Target);
                        }
                        catch (Exception ex)
                        {
                            //Nicht erfolgreich
                            if (this.FileTransferError != null)
                            {
                                this.FileTransferError(
                                        this,
                                        new ErrorEventArgs(
                                            "MoveFileToCIFS: Fehler beim Überschreiben von: " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                            );
                            }
                        }
                    }
                    else if (System.IO.File.Exists(Target) && overwrite == false)
                    {//Nicht erfolgreich
                        if (this.FileTransferError != null)
                        {
                            this.FileTransferError(
                                    this,
                                    new ErrorEventArgs(
                                        "MoveFileToCIFS: Zieldatei: " + Target + " existiert bereits soll aber nicht überschrieben werden! Abbruch des Verschiebevorgangs.")
                                        );
                        }
                        myreturn = false;
                    }


                    File.MoveTo(Target); //Verschieben
                    myreturn = true;
                }

                catch (Exception ex)
                {
                    //Nicht erfolgreich
                    if (this.FileTransferError != null)
                    {
                        this.FileTransferError(
                                this,
                                new ErrorEventArgs(
                                    "MoveFileToCIFS: Fehler beim Übertragen von: " + File.FullName + " nach " + Target + " => " + ex.ToString() + ". Stacktrace: " + ex.StackTrace)
                                    );
                    }
                }
            }
            return myreturn;
        }

        #endregion //movefile


        #endregion //tocifs


        #endregion //fileoperations


        #endregion //Methods
    }
}
