using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Co0nUtilZ
{
    /// <summary>
    /// Diese Klasse ist ein FTP-Client, welcher es ermöglicht Dateien mit einem FTP-Server Auszutauschen
    /// Created:           08/2017
    /// Author:              D. Marx
    /// Project: https://github.com/derco0n/coonutils    
    /// License: 
    /// GPLv2 - Means, this is free software which comes without any warranty but can be used, modified and redistributed free of charge
    /// You should have received a copy of that license: If not look here: https://www.gnu.org/licenses/gpl-2.0.de.html

    /// </summary>
    public class C_FTPClient
    {
        public static int PAUSEMS = 2;
        public static int CONNECTIONTIMEOUT = 3600000;
        public static int BUFFERSIZE = 2048;


        #region === Properties

       
        private Exception _lastException = new Exception("No Exception so far");
        /// <summary>
        /// Gibt die letzte Exception zurück
        /// </summary>
        public Exception lastException
        {
            get
            {
                return this._lastException;
            }
        }

        /// <summary>
        /// FTP-Server-Adresse
        /// </summary>
        public string Adress { get; set; }

        /// <summary>
        /// FTP-Benutzer
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// FTP-Passwort
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// FTP-Passive-Modus.
        /// </summary>
        public Boolean UsePassive
        {
            get
            {
                return this._passive;
            }
            set
            {
                this._passive = value;
            }
        }

        /// <summary>
        /// FTP über TLS benutzen
        /// </summary>
        public Boolean UseTLS
        {
            get
            {
                return this._usetls;
            }
            set
            {
                this._usetls = value;
            }
        }

        private Boolean _usetls = false;
        private Boolean _passive = true;
        #endregion

        #region === Constructor

        /// <summary>
        /// Initialisiert eine neue Instanz der FTP Helper Klasse anhand von Standardwerten
        /// </summary>
        /// <param name="adress">Name oder IP Adresse des Servers</param>
        /// <param name="user">Benutzername</param>
        /// <param name="password">Passwort</param>
        public C_FTPClient(string adress, string user, string password)
        {//Konstruktor
            this.Adress = adress;
            this.User = user;
            this.Password = password;
        }


        /// <summary>
        /// Initialisiert eine neue Instanz der FTP Helper Klasse anhand von gegebenen Werten
        /// </summary>
        /// <param name="adress">Name oder IP Adresse des Servers</param>
        /// <param name="port">TCP-port</param>
        /// <param name="user">Benutzername</param>
        /// <param name="password">Passwort</param>
        /// <param name="usetls">Verschlüsselte Verbindung True/False</param>
        /// <param name="passive">FTP-Passive-Mode True/False</param>
        public C_FTPClient(string adress, UInt16 port, string user, string password, Boolean usetls = false, Boolean passive = true)
        {//Konstruktor
            this.Adress = adress + ":" + port.ToString();
            this.User = user;
            this.Password = password;
            this._usetls = usetls;
            this._passive = passive;
        }



        #endregion

        #region === Events


        public delegate void CompleteEventhandler();
        /// <summary>
        /// Ereignis: Dateiliste komplett empfangen
        /// </summary>
        //public event CompleteEventhandler ReceivedFileListComplete;

        public delegate void MultiLoadProgressEventHandler(object sender, ProgressEventArgs Args);
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs Fehler);

        /// <summary>
        /// Ereignis: Uploadfortschritt hat sich geändert
        /// </summary>
        public event MultiLoadProgressEventHandler UploadProgressChanged;

        /// <summary>
        /// Ereignis: Upload abgeschlossen
        /// </summary>
        public event CompleteEventhandler UploadComplete;

        /// <summary>
        /// Ereignis: Downloadfortschritt hat sich geändert
        /// </summary>
        public event MultiLoadProgressEventHandler DownloadProgressChanged;

        /// <summary>
        /// Ereignis: Download abgeschlossen
        /// </summary>
        public event CompleteEventhandler DownloadComplete;

        /// <summary>
        /// Ereignis: Dateiübertragunsfehler
        /// </summary>
        public event ErrorEventHandler FileTransferError;

        #endregion

        #region === Methods

        /// <summary>
        /// Erstellt einen Ordner auf dem FTP-Server
        /// </summary>
        /// <param name="Path">Zielverzeichnis</param>
        public bool CreateFolder(String Path)
        {//Erstellt einen Ordner auf dem FTP-Server
            Boolean myReturn = false;
            WebResponse webResponse = null;
            try
            {
                FtpWebRequest.DefaultWebProxy = null;
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + Adress + "/" + Path));
                request.Credentials = new NetworkCredential(this.User, this.Password);
                request.EnableSsl = this._usetls;
                request.UsePassive = this._passive;
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                webResponse = request.GetResponse();
                myReturn = true;
            }
            catch (Exception ex)
            {
                String error = ex.ToString();
                if (error.Equals("Das Remotezertifikat ist laut Validierungsverfahren ungültig."))
                {
                    // Console.WriteLine(error + " Zertifikat ist abgelaufen, Hostname entspricht nicht dem im Zertifikat, der ausstellenden CA wird nicht vertraut oder es handelt sich um ein selbst erstelltes Zertifikat.");
                }
                this._lastException = ex;
                //throw;
                myReturn = false;
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Close();
                }

            }
            return myReturn;
        }

        /// <summary>
        /// Überprüft ob eine Verbindung zum FTP Server besteht. Gibt bei Erfolg TRUE zurück
        /// </summary>
        public bool CheckConnection()
        {
            bool myReturn = false;

            WebResponse webResponse = null;
            try
            {
                FtpWebRequest.DefaultWebProxy = null;

                FtpWebRequest ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + this.Adress + "/"));
                ftpWebRequest.Credentials = new NetworkCredential(this.User, this.Password);
                ftpWebRequest.EnableSsl = this._usetls;
                ftpWebRequest.UsePassive = this._passive;
                ftpWebRequest.KeepAlive = true;
                //Als Methode muss ListDirectory gewählt werden!
                ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                webResponse = ftpWebRequest.GetResponse();
                myReturn = true;
            }
            catch (Exception ex)
            {
                String error = ex.ToString();
                if (error.Equals("Das Remotezertifikat ist laut Validierungsverfahren ungültig."))
                {
                    // Console.WriteLine(error + " Zertifikat ist abgelaufen, Hostname entspricht nicht dem im Zertifikat, der ausstellenden CA wird nicht vertraut oder es handelt sich um ein selbst erstelltes Zertifikat.");
                }
                this._lastException = ex;
                myReturn = false;
            }
            finally
            {
                if (webResponse != null)
                {
                    try
                    {
                        webResponse.Close();
                    }
                    catch (Exception ex)
                    {
                        myReturn = false;
                    }
                }
            }

            return myReturn;
        }



        /// <summary>
        /// Lädt eine kompletten Ordner (nicht rekursiv) vom Server herunter. Die Aktion wird in einem neuen Thread ausgeführt. Gibt den Thread zurück.
        /// </summary>
        /// <param name="RemoteFiles">Quellordner auf dem Server</param>
        /// <param name="localfolder">Lokaler Zielordner</param>
        public System.Threading.Thread DownloadMultipleFiles(List<String> RemoteFiles, String localfolder, bool removeFileAfterTransfer)
        {
            //Erzeugt einen Thread in welchem die Methode DownloadFilesWorker() ausgeführt wird. -> Lädt nach und nach die Dateiliste mittel DownloadFile() herunter...
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    DownloadFilesWorker(RemoteFiles, localfolder, removeFileAfterTransfer);
                }
                catch (System.Threading.ThreadAbortException ex)
                {
                    this.AbortDownload(); //Beim Abbruch des threads Dateizugriffe und Verbindung schließen
                }
            }

            ); //Neuen Thread erzeugen
            t.Start(); //Thread starten
            return t; //Das Threadobjekt zurückgeben um es vom Aufrufer aus überwachen zu können
        }



        private Stream currentDownloadFTPStream;
        private FileStream currentDownloadFileStream;
        /// <summary>
        /// Schließt den Zugriff auf Quelldatei und FTP-Verbindung. Wird beim Abbruch eines Downloadthreads mit aufgerufen werden.
        /// </summary>
        private void AbortDownload()
        {

            if (this.currentDownloadFileStream != null)
            {
                try
                {
                    this.currentDownloadFileStream.Close(); //DateiStream schließen
                }

                catch (Exception ex)
                {

                }
            }

            if (this.currentDownloadFTPStream != null)
            {
                try
                {
                    this.currentDownloadFTPStream.Close(); //FTP-Stream schließen
                }

                catch (Exception ex)
                {

                }
            }

            return;
        }


        /// <summary>
        /// Workermethode für den Downloadthread.
        /// </summary>
        /// <param name="RemoteFiles"></param>
        /// <param name="localfolder"></param>
        private void DownloadFilesWorker(List<String> RemoteFiles, String localfolder, bool removeFileAfterTransfer)
        {//Workermethode für den Downloadthread
            int Filecount = RemoteFiles.Count(); //Anzahl der Quelldateien (auf dem FTP-Server) ermitteln.
            int Filestransfered = 0;
            double percentcomplete = 0;



            if (new System.IO.DirectoryInfo(localfolder).Exists == false) //Zielordner existiert nicht
            { 
              //Datei existiert nicht
                this.FileTransferError(
                   this,
                   new ErrorEventArgs(
                       "Fehler beim Download nach " + localfolder + ". Zielordner (auf lokaler Maschine) wurde nicht gefunden!! - Abbruch der Übertragung!")
                       );//Event auslösen falls abboniert

            }

            else
            {//Zielordner existiert. Übertragung beginnen
                int TotalKBCount = 0;
                Stopwatch sW = new Stopwatch();
                sW.Start();



                foreach (String File in RemoteFiles)
                {//Alle Dateien des Quellordners durchgehen




                    /*
                    if (this.FileExists("????",File) == false)
                    {
                        //Datei existiert nicht
                        this.FileTransferError(
                           this,
                           new ErrorEventArgs(
                               "Fehler beim Übertragen von: " + File + " nach " + localfolder + ". Quelldatei nicht gefunden!! - Datei wird übersprungen!")
                               );//Event auslösen falls abboniert
                        continue; //Mit nächster Datei weiter machen
                    }*/

                    bool success = DownloadFile(File, localfolder, true); //Download der aktuellen Datei durchführen. Gibt bei Erfolg true zurück.
                    bool retry = false;


                    if (!success) //Download nicht erfolgreich
                    {
                        //Fehler bei der Übertragung
                        if (this.FileTransferError != null) //Wenn Fehler-Event abbonniert
                        {
                            this.FileTransferError(
                            this,
                            new ErrorEventArgs(
                                "Fehler beim Übertragen der Datei: " + File + " nach " + localfolder + ". Ausnahme: " + this._lastException + "  => Versuche erneut...")
                                );//Event auslösen falls abboniert
                            retry = DownloadFile(File, localfolder, true); //Datei erneut versuchen
                            if (!retry) //Erneuter Versuch ebenfalls fehlgeschlagen
                            {
                                //Fehler bei der erneuten Übertragung
                                if (this.FileTransferError != null) //Wenn Fehler-Event abbonniert
                                {
                                    this.FileTransferError(
                                    this,
                                    new ErrorEventArgs(
                                        "Fehler beim Übertragen der Datei: " + File + " nach " + localfolder + ". Ausnahme: " + this._lastException + "  => Erneuter Versuch auch fehlgeschlagen!")
                                        );//Event auslösen falls abboniert
                                }
                            }
                        }
                    }

                    if (removeFileAfterTransfer && (success | retry))
                    {
                        //Wenn die Datei nach erfolgreichem Transfer gelöscht werden soll und der erste oder der zweite Kopierversuch erfolgreich waren
                        this.DeleteFile(File, true); //Remotedatei nach Übertragung löschen
                    }

                    //Fortschritt aktualisieren.
                    FileInfo TargetInfo = new FileInfo(localfolder + "\\" + File);

                    TotalKBCount += (int)Math.Round((double)(TargetInfo.Length / 1024), 0); //Statistik: Gesamtzahl übertragener KiB erhöhen

                    Filestransfered += 1; //Zähkler für übertragene Dateien erhöhen
                    percentcomplete = 100.0 / Filecount * Filestransfered; //Prozentsatz des Fortschritts berechnen
                    if (this.DownloadProgressChanged != null) //Wenn Event für den Downloadfortschritt abboniert wurde...
                    {

                        this.DownloadProgressChanged(
                            this,
                            new ProgressEventArgs(
                                (int)Math.Round(percentcomplete, 0),
                            "Datei " + File + " wurde bearbeitet.")
                                ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                    }
                    //mit nächster Datei fortfahren
                }
                int AverangeRate = (int)Math.Round((TotalKBCount / sW.Elapsed.TotalSeconds), 0); //Errechnet die durchschnittliche Übertragungsrate in KiB/Sekunde                
                sW.Stop(); //Statistik: Stoppuhr anhalten
                if (this.DownloadProgressChanged != null) //Wenn Event für den Uploadfortschritt abboniert wurde...
                {
                    this.DownloadProgressChanged(
                       this,
                        new ProgressEventArgs(100, "Durchschnittliche Übertratungsrate: " + AverangeRate.ToString() + " KiB/Sek.")
                            ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                }
            }
            //Alle Dateien wurden bearbeitet


            //Fertig:
            if (this.DownloadComplete != null) //Wenn das Event Download-Abgeschlossen abboniert wurde...
            {
                this.DownloadComplete();//Event auslösen falls abboniert
            }
        }

        /// <summary>
        /// Workermethode für den Uploadthread
        /// </summary>
        /// <param name="remoteFolder">Zielordner auf dem Server</param>
        /// <param name="Filelist">Array mit Quelldateien</param>
        private void UploadFilesWorker(string remoteFolder, FileInfo[] Filelist, bool removeFileAfterTransfer)
        {//Workermethode für den Uploadthread
            int Filecount = Filelist.Count(); //Anzahl der Quelldateien (lokal) ermitteln.
            int Filestransfered = 0;
            double percentcomplete = 0;


            if (!this.PathExists(remoteFolder)) //Zielordner existiert nicht
            { 
              //Datei existiert nicht
                this.FileTransferError(
                   this,
                   new ErrorEventArgs(
                       "Fehler beim Upload nach " + remoteFolder + ". Zielordner (auf dem Server) wurde nicht gefunden!! - Abbruch der Übertragung!")
                       );//Event auslösen falls abboniert

            }
            else
            {
                //Zielordner existiert. Übertragung beginnen.         
                int TotalKBCount = 0;
                Stopwatch sW = new Stopwatch();
                sW.Start();



                foreach (System.IO.FileInfo File in Filelist)
                {//Alle Dateien des Quellordners durchgehen



                    if (!File.Exists)
                    {
                        //Datei existiert nicht
                        this.FileTransferError(
                           this,
                           new ErrorEventArgs(
                               "Fehler beim Übertragen von: " + File + " nach " + remoteFolder + ". Quelldatei nicht gefunden!! - Datei wird übersprungen!")
                               );//Event auslösen falls abboniert
                        continue; //Mit nächster Datei weiter machen
                    }



                    bool success = UploadFile(remoteFolder, File, true); //Datei mittels UploadFile() hochladen Gibt bei Erfolg true zurück.
                    bool retry = false;

                    if (!success)//Upload nicht erfolgreich
                    {
                        //Fehler bei der Übertragung
                        if (this.FileTransferError != null) //Wenn Fehler-Event abbonniert
                        {

                            this.FileTransferError(
                            this,
                            new ErrorEventArgs(
                                "Fehler beim Übertragen von: " + File + " nach " + remoteFolder + " Ausnahme: " + this._lastException + " => Versuche erneut...")
                                );//Event auslösen falls abboniert
                            retry = UploadFile(remoteFolder, File, true); //Datei erneut versuchen
                            if (!retry) //Erneuter Versuch ebenfalls fehlgeschlagen
                            {
                                //Fehler bei der erneuten Übertragung
                                if (this.FileTransferError != null) //Wenn Fehler-Event abbonniert
                                {
                                    this.FileTransferError(
                                    this,
                                    new ErrorEventArgs(
                                        "Fehler beim erneuten Übertragen von: " + File + " nach " + remoteFolder + " Ausnahme: " + this._lastException + "  => Erneuter Versuch auch fehlgeschlagen!")
                                        );//Event auslösen falls abboniert
                                }
                            }
                        }
                    }

                    if (removeFileAfterTransfer && (success | retry))
                    {
                        //Wenn die Datei nach erfolgreichem Transfer gelöscht werden soll und der erste oder der zweite Kopierversuch erfolgreich waren
                        try
                        {
                            File.Delete(); //Lokale (Quell-) Datei löschen
                        }
                        catch (Exception ex)
                        {
                            this._lastException = ex;
                            this.FileTransferError(
                                   this,
                                   new ErrorEventArgs(
                                       "Fehler beim löschen der Quelldatei: " + File + " - Details: " + ex.ToString())
                                       );//Event auslösen falls abboniert
                        }

                    }


                    //Fortschritt aktualisieren.
                    TotalKBCount += (int)Math.Round((double)(File.Length / 1024), 0); //Statistik: Gesamtzahl übertragener KiB erhöhen

                    Filestransfered += 1; //Zähler für übertragene Dateien erhöhen
                    percentcomplete = 100.0 / Filecount * Filestransfered; //Prozentsatz des Fortschritts berechnen
                    if (this.UploadProgressChanged != null) //Wenn Event für den Uploadfortschritt abboniert wurde...
                    {
                        this.UploadProgressChanged(
                           this,
                            new ProgressEventArgs(
                                (int)Math.Round(percentcomplete, 0),
                            "Datei " + File.Name + " (" + ((double)File.Length / 1024).ToString() + " kBytes) wurde bearbeitet.")
                                ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                    }


                    //mit nächster Datei fortfahren
                }
                int AverangeRate = (int)Math.Round((TotalKBCount / sW.Elapsed.TotalSeconds), 0); //Errechnet die durchschnittliche Übertragungsrate in KiB/Sekunde                
                sW.Stop(); //Statistik: Stoppuhr anhalten
                if (this.UploadProgressChanged != null) //Wenn Event für den Uploadfortschritt abboniert wurde...
                {
                    this.UploadProgressChanged(
                       this,
                        new ProgressEventArgs(100, "Durchschnittliche Übertratungsrate: " + AverangeRate.ToString() + " KiB/Sek.")
                            ); //Event auslösen falls abboniert und den aktuellen Fortschritt mitgeben
                }




            }
            //Alle Dateien wurden bearbeitet

            //Fertig:
            if (this.UploadComplete != null)
            {
                this.UploadComplete();//Event auslösen falls abboniert
            }
        }


        /// <summary>
        /// Lädt eine komplette Dateiliste hoch. Die Aktion wird in einem neuen Thread ausgeführt. Gibt den Thread zurück.
        /// </summary>
        /// <param name="remoteFolder">Zielordner auf dem Server</param>
        /// <param name="Filelist">Array mit Quelldateien</param>
        /// <returns></returns>
        public System.Threading.Thread UploadMultipleFiles(string remoteFolder, FileInfo[] Filelist, bool removeFileAfterTransfer)
        {//Lädt eine komplette Dateiliste hoch. Die Aktion wird in einem neuen Thread ausgeführt.
            //Erzeugt einen Thread welcher, die Dateiliste nach und nach mittels UploadFile hochlädt
            var t = new System.Threading.Thread(() =>
            {
                try
                {

                    UploadFilesWorker(remoteFolder, Filelist, removeFileAfterTransfer);

                }
                catch (System.Threading.ThreadAbortException)
                {
                    AbortUpload(); //Beim Abbruch des threads Dateizugriffe und Verbindung schließen
                }
            }
            );
            t.Start();
            return t;
        }


        private Stream currentUploadFTPStream;
        private FileStream currentUploadFileStream;
        /// <summary>
        /// Schließt den Zugriff auf  Quelldatei und FTP-Verbindung. Wird beim Abbruch eines Uploadthreads mit aufgerufen werden.
        /// </summary>
        private void AbortUpload()
        {

            if (this.currentUploadFileStream != null)
            {
                try
                {
                    this.currentUploadFileStream.Close(); //DateiStream schließen
                }

                catch (Exception ex)
                {

                }
            }

            if (this.currentUploadFTPStream != null)
            {
                try
                {
                    this.currentUploadFTPStream.Close(); //FTP-Stream schließen
                }

                catch (Exception ex)
                {

                }
            }

            return;
        }


        /// <summary> 
        /// Lädt eine Datei auf einen FTP Server hoch
        /// </summary>
        /// <param name="remoteFolder">Zielverzeichnis</param>
        /// <param name="fileInfo">Quelldatei</param>
        /// <param name="keepalive">Verbindung nach Transfer geöffnet lassen?</param>
        /// <returns></returns>
        public bool UploadFile(string remoteFolder, FileInfo fileInfo, bool keepalive = false)
        {
             

            System.Threading.Thread.Sleep(PAUSEMS); //Kurz warten, damit Server und Netzwerk bei vielen Dateien hintereinander wieder bereits sind.

            Stream ftpStream = null;
            FileStream file = null;
            Boolean myReturn = false;

            if (!File.Exists(fileInfo.FullName))
            {
                //Quelldatei existiert nicht.
                return false; //Abbruch
            }

            try
            {

                //FTP-Verbindung initialisieren...
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + Adress + "/" + remoteFolder + "/" + fileInfo.Name));

                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.EnableSsl = this._usetls;
                request.UsePassive = this._passive;
                request.Credentials = new NetworkCredential(User, Password);
                request.KeepAlive = keepalive;
                request.Timeout = CONNECTIONTIMEOUT; //Timeout
                ftpStream = request.GetRequestStream(); //Ausgabedatenstrom initialisieren
                this.currentUploadFTPStream = ftpStream;

                //debug.LogWarn("Öffne Datei "+fileInfo.FullName+" lesend.", 9999);
                file = File.OpenRead(@fileInfo.FullName); //Quelldatei lesend öffnen
                this.currentUploadFileStream = file;
                //debug.LogWarn("Datei " + fileInfo.FullName + " wurde lesend geöffnet.", 9999);

                int length = BUFFERSIZE; //Puffergröße anhand des statischen Klassenwerts definieren.
                byte[] buffer = new byte[length]; //Neuen Puff (Bytearray) initialisieren
                int bytesRead = 0; //Zähler für gelesene Bytes initialisieren.

                do
                {//Schleife: Die komplette Quelldatei Byteweise durchgehen
                    try
                    {
                        //debug.LogWarn("Versuche Bytes in FTP-Stream zu schreiben...", 9999);

                        bytesRead = file.Read(buffer, 0, length); //Einen Teil der Datei lesen (Größe des Puffers) und diesen in den Puffer kopieren...
                        ftpStream.Write(buffer, 0, bytesRead); //... in des Ausgabedatenstrom den Inhalt des Puffers schreiben.
                    }
                    catch (Exception ex)
                    {//Fehler beim Kopieren der Datei
                        //debug.LogError("Fehler beim Schreiben in den FTP-Stream: "+ex.ToString(), 9999);
                        this._lastException = ex; //Exception auffangen
                        myReturn = false; //Rückgabewert false, da fehlschlag
                        break; //Schleife (Kopiervorgang) abbrechen          
                    }
                }
                while (bytesRead != 0); //Schleife solange ausführen bis keine Bytes mehr gelesen wurden.

                myReturn = true; //Datei Fertig kopiert: Rückgabewert=TRUE (Erfolg)
            }
            catch (WebException ex)
            {
                //Fehler beim Aufbau der FTP-Verbindung
                //debug.LogError("Fehler beim Aufbau der FTP-Verbindung " + ex.ToString(), 9999);
                String error = ex.ToString();
                if (error.Equals("Das Remotezertifikat ist laut Validierungsverfahren ungültig."))
                {
                    //  Console.WriteLine(error + " Zertifikat ist abgelaufen, Hostname entspricht nicht dem im Zertifikat, der ausstellenden CA wird nicht vertraut oder es handelt sich um ein selbst erstelltes Zertifikat.");
                }

                this._lastException = ex;
                myReturn = false; //Fehler beim kopieren: Rückgabewert=FALSE

            }
            finally
            {//Zum Schluss (egal ob Erfolg oder nicht)
                if (file != null)
                {
                    file.Close(); //Die Quelldatei wieder schließen.
                }

                if (ftpStream != null)
                {
                    try
                    {
                        ftpStream.Close(); //Den Zieldatenstrom schließen
                    }
                    catch (Exception ex)
                    {
                        //  this._lastException = ex;
                    }
                }
            }
            //debug.LogWarn("Methode UploadFile() beendet. Ergebnis: " + myReturn.ToString(), 9999);
            return myReturn;
        }


        /// <summary>
        /// Prüft ob ein Pfad auf dem Server existiert
        /// </summary>
        /// <param name="FullServerPath">Kopletter Pfad (inklusive Server) zum Zielordner </param>
        /// <returns>Gibt True zurück, wenn der Ordner existiert.</returns>
        /// 
        public bool PathExists(String FullServerPath)
        {//Prüft ob ein bestimmter Ordner auf dem Server existiert:
            //Zunächst prüfen ob der gesuchte Ordner einen Inhalt hat
            List<String> CurrentLevelContent = this.GetFileList(FullServerPath); //Wenn Ordner existiert aber keinen Inhalt halt, hat die Liste dennoch einen Eintrag


            if (this.existingPath(FullServerPath).Equals(FullServerPath)) //Wenn der Angegebene Ordner auf dem Server existiert.
            {
                return true; //ordner existiert
            }

            return false; //Ordner existiert nicht

        }

        /*
        public bool OLD_PathExists(String FullServerPath)
        {//Prüft ob ein bestimmter Ordner auf dem Server existiert:
            //Zunächst prüfen ob der gesuchte Ordner einen Inhalt hat
            List<String> CurrentLevelContent = this.GetFileList(FullServerPath); //Wenn Ordner existiert aber keinen Inhalt halt, hat die Liste dennoch einen Eintrag
            
           
            if (CurrentLevelContent!=null && CurrentLevelContent.Count > 0) 
            {
                return true; //ordner existiert
            }

            return false; //Ordner existiert nicht
            
        }
        */


        /// <summary>
        /// Gibt den auf dem Server existierenden Teil eines Pfads zurück
        /// </summary>        
        /// <param name="FullServerPath">Kopletter Pfad (inklusive Server) zum Zielordner</param>
        /// <returns>Gibt den existierenden Teilpfad zurück</returns>
        public String existingPath(String FullServerPath)
        {//Prüft ob ein bestimmter Ordner auf dem Server existiert und gibt den Teilpfad, der existiert zurück
            //Bsp: /Ordner1/Unterordner/Weiterer_Unterordner
            String myReturn = "";

            //Zunächst prüfen ob der Ordnerpfad wie angegeben existiert...
            List<String> CurrentLevelContent = this.GetFileList(FullServerPath);
            if (CurrentLevelContent != null && CurrentLevelContent.Count > 0)
            {
                return FullServerPath;
            }

            //Falls der ordner nicht wie angegeben exisitert, der Teil des Ordners ermitteln, welcher vorhanden ist...

            if (FullServerPath.Length < 1)
            {
                return FullServerPath;
            }

            String[] FolderPathElements = FullServerPath.Split('/');

            if (FolderPathElements.Length < 1)
            {
                return FullServerPath;
            }

            String CurrentPathLevel = "/"; //Pfad: /

            int counter;

            for (counter = 0; counter < FolderPathElements.Length - 1; counter++)
            {
                String CurrentElement = FolderPathElements[counter + 1];
                if (counter == 1)
                {//Pfad: /Ordner1
                    CurrentPathLevel = CurrentPathLevel + FolderPathElements[counter];
                }
                if (counter > 1)
                {//Pfad: /Ordner1/Unterordner[/Weiterer_Unterordner]
                    CurrentPathLevel = CurrentPathLevel + "/" + FolderPathElements[counter];
                }
                CurrentLevelContent = this.GetFileList(CurrentPathLevel);

                if (!CurrentLevelContent.Contains(CurrentElement) && CurrentElement != "")
                {
                    //Aktuelle Ebene enthält auf dieser Ebene gesuchtes Element nicht
                    return CurrentPathLevel; //Pfad existiert so nicht.
                }
            }
            //Es wurden alle (Unter)-Ordner gefunden, der komplette Pfad existiert also.
            myReturn = FullServerPath;
            return myReturn;
        }

        /// <summary>
        /// Prüft ob eine angebende Datei im Zielordner existiert
        /// </summary>
        /// <param name="remoteFolder">Zielordner auf dem Server</param>
        /// <param name="filename">Zu überprüfender Dateiname</param>
        /// <param name="keepalive">Verbind nach Aufruf aktiv halten</param>
        /// <returns>TRUE wenn die Datei gefunden wurde, ansonsten FALSE</returns>
        public Boolean FileExists(string remoteFolder, string filename, bool keepalive = false)
        {
            List<String> Files = this.GetFileList(remoteFolder, keepalive);
            if (Files.Count > 0)
            {
                if (Files.Contains(filename))
                {
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// Liefert eine Liste von Dateien zurück, die sich in einem bestimmten Verzeichnis auf dem Server befinden
        /// </summary>        
        /// <param name="remoteFolder">Zielordner auf dem Server</param>
        /// <param name="keepalive">Verbindung anschließend geöffnet lassen?</param>
        /// <returns>Gibt eine Liste mit gefundenen Dateien zurück</returns>
        public List<string> GetFileList(string remoteFolder, bool keepalive = false)
        {
            FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create("ftp://" + Adress + "/" + remoteFolder);
            ftpWebRequest.Credentials = new NetworkCredential(User, Password);
            ftpWebRequest.EnableSsl = this._usetls;
            ftpWebRequest.UsePassive = this._passive;
            ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            ftpWebRequest.KeepAlive = keepalive;
            ftpWebRequest.Timeout = CONNECTIONTIMEOUT; //Timeout
            WebResponse webResponse = null;

            try
            {
                webResponse = ftpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                String status = "";
                if (webResponse != null)
                {
                    status = (
                         (FtpWebResponse)webResponse
                         ).StatusDescription;
                }

                String error = ex.ToString() + status;
                if (error.Equals("Das Remotezertifikat ist laut Validierungsverfahren ungültig."))
                {
                    // Console.WriteLine(error + " Zertifikat ist abgelaufen, Hostname entspricht nicht dem im Zertifikat, der ausstellenden CA wird nicht vertraut oder es handelt sich um ein selbst erstelltes Zertifikat.");
                    /*
                     * if (this.DownloadComplete != null)
                    {
                        this.DownloadComplete();
                    }
                    */
                }
                this._lastException = ex;
                return new List<string>();
            }

            List<string> files = new List<string>();
            StreamReader streamReader = new StreamReader(webResponse.GetResponseStream());

            //bool hasdata = false;
            while (!streamReader.EndOfStream)
            {
                String CurrentLine = streamReader.ReadLine();

                /*
                if (CurrentLine.Length>0 && hasdata == false)
                {
                    hasdata = true;
                }
                */

                if (CurrentLine.Contains('/'))
                { //Das Letzte Element des Pfades (unterste Ordnerebene) ausgeben
                    String[] pathelement = CurrentLine.Split('/');
                    files.Add(pathelement[pathelement.Length - 1]);
                }
                else
                {
                    files.Add(CurrentLine);
                }

            }

            /*
            if (hasdata)
            {               
                files.Add("."); //Hässlicher Workaround: fügt dem Ordner, falls Daten kommen einen . (aktuelles Verzeichnis) hinzu, damit mindestens eine Datei in solchen Fällen vorhanden ist.
            }
            */

            streamReader.Close();
            webResponse.Close();
            return files;
        }




        /// <summary>
        /// Liefert eine Liste von Dateien/Ordnern zurück
        /// </summary>
        /// <returns>Gibt eine Liste aller Dateien und Ordner zurück</returns>
        public List<string> GetFileList()
        {
            return this.GetFileList("");
        }

        /// <summary>
        /// Lädt eine Datei vom FTP Server herunter
        /// </summary>
        /// <param name="RemoteFileFullPath">Kompletter Pfad (inklusive Dateiname) zur Quelldatei</param>
        /// <param name="dstLocalFolder">Lokaler Zielordner in welchen die Datei kopiert werden soll</param>
        /// <param name="keepalive">Nach Transfer die Verbindung zum Server geöffnet lassen?</param>
        /// <returns>Gibt bei Erfolg TRUE zurück.</returns>        
        public bool DownloadFile(string RemoteFileFullPath, string dstLocalFolder, bool keepalive = false)
        {
            bool myreturn = true;

            System.Threading.Thread.Sleep(PAUSEMS); //Kurz warten, damit Server und Netzwerk bei vielen Dateien hintereinander wieder bereit sind.

            FileStream fileStream = null;
            int bytesRead = 0;
            byte[] buffer = new byte[BUFFERSIZE];

            String destination;
            String[] rempathparts = RemoteFileFullPath.Split('/'); //Den Quellpfad splitten
            string destinationFile = rempathparts[rempathparts.Length - 1]; //Das letzte Element des Quellpfads ist der Dateiname
            if (dstLocalFolder[dstLocalFolder.Length - 1].Equals('\\'))
            {
                destination = dstLocalFolder + destinationFile; //Ordnerpfad hat abschließendes \
            }
            else
            {
                destination = dstLocalFolder + "\\" + destinationFile;
            }

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + Adress + "/" + RemoteFileFullPath));
                request.EnableSsl = this._usetls;
                request.UsePassive = this._passive;
                request.Credentials = new NetworkCredential(User, Password);
                request.Timeout = CONNECTIONTIMEOUT; //Timeout
                request.KeepAlive = keepalive;



                Stream reader = request.GetResponse().GetResponseStream();
                this.currentDownloadFTPStream = reader; //Aktuellen Stream in Klassenweites objekt übernehmen, damit dieses geschlossen werden kann.
                fileStream = new FileStream(destination, FileMode.Create);
                this.currentDownloadFileStream = fileStream; //Aktuellen Stream in Klassenweites objekt übernehmen, damit dieses geschlossen werden kann.

                while (true)
                {//Kopiervorgang
                    try
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                            break;
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                    catch (Exception ex)
                    {
                        myreturn = false;
                        this._lastException = ex;
                        this.FileTransferError(
                            this,
                            new ErrorEventArgs("Fehler beim Download von: " + RemoteFileFullPath + " nach " + dstLocalFolder + ". Handelt es sich um einen Ordner? Fehlerdetails: " + ex.ToString())
                            );
                        break; //Schleife abbrechen
                    }
                }



            }
            catch (WebException ex)
            {
                this._lastException = ex;
                myreturn = false;
                this.FileTransferError(
                    this,
                     new ErrorEventArgs("Fehler beim Download von: " + RemoteFileFullPath + " nach " + dstLocalFolder + ". Handelt es sich um einen Ordner? Fehlerdetails: " + ex.ToString())
                    );

            }
            finally
            {
                if (fileStream != null)
                {
                    try
                    {
                        fileStream.Close();

                        //Zeitstempel der Datei korrigieren. (Lastmodified ist aktuell noch der jetzige Zeitpunkt, da es sich ja um eine neue Datei handelt)
                        System.IO.FileInfo Destinationfileinfo = new System.IO.FileInfo(destination);

                        Destinationfileinfo.LastWriteTime = this.LastModifiedTime(RemoteFileFullPath);
                    }
                    catch (Exception ex)
                    {
                        this._lastException = ex;

                        myreturn = false;
                    }
                }
            }



            //Wenn bis hierhin nichts schief gelaufen ist, sollte myreturn true sein...

            return myreturn;
        }

        /// <summary>
        /// Ermittelt den Zeitpunkt an dem eine Remotedatei zuletzt verändert wurde.
        /// </summary>
        /// <param name="RemoteFileFullPath"></param>
        /// <returns></returns>
        public DateTime LastModifiedTime(string RemoteFileFullPath)
        {
            DateTime myReturn = new DateTime();
            System.Threading.Thread.Sleep(PAUSEMS); //Kurz warten, damit Server und Netzwerk beim vielen Dateien hintereinander wieder bereits sind.

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + Adress + "/" + RemoteFileFullPath));
                request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                request.EnableSsl = this._usetls;
                request.UsePassive = this._passive;
                request.Credentials = new NetworkCredential(User, Password);
                request.Timeout = CONNECTIONTIMEOUT; //Timeout
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                myReturn = response.LastModified;

            }
            catch (Exception ex)
            {
                this._lastException = ex;

            }


            return myReturn;
        }



        /// <summary>
        /// Löscht eine Datei vom FTP Server
        /// </summary>
        /// <param name="remoteFolder">Zielverzeichnis</param>
        /// <param name="RemotefileFullPath">Datei</param>
        /// <param name="keepalive">Verbindung zum Server anschließend geöffnet lassen?</param>
        public void DeleteFile(String RemotefileFullPath, bool keepalive = false)
        {
            System.Threading.Thread.Sleep(PAUSEMS); //Kurz warten, damit Server und Netzwerk beim vielen Dateien hintereinander wieder bereits sind.

            try
            {
                FtpWebRequest ftpWebRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + Adress + "/" + RemotefileFullPath));
                ftpWebRequest.EnableSsl = _usetls;
                ftpWebRequest.UsePassive = this._passive;
                ftpWebRequest.UseBinary = true;
                ftpWebRequest.Credentials = new NetworkCredential(User, Password);
                ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                ftpWebRequest.Proxy = null;
                ftpWebRequest.KeepAlive = keepalive;
                ftpWebRequest.Timeout = CONNECTIONTIMEOUT; //Timeout
                ftpWebRequest.GetResponse();

            }
            catch (Exception ex)
            {
                this._lastException = ex;

            }
        }

        /// <summary>
        /// Löscht eine Datei vom FTP Server
        /// </summary>
        /// <param name="RemoteFilePath">Datei auf dem Server</param>
        /// <param name="keepalive">Verbindung zum Server anschließend geöffnet lassen?</param>
        public void DeleteFile(FileInfo RemoteFilePath, bool keepalive = false)
        {
            DeleteFile(RemoteFilePath, keepalive);
        }


        public List<String> GetFileListByRegex(String srcdir, String regex)
        {
            C_RegExHelper myRegEx = new C_RegExHelper();
            List<String> myInput = this.GetFileList(srcdir, true);
            return myRegEx.FilterStringListByRegex(myInput, regex);
        }

        /// <summary>
        /// Prüft ob ein Web-Zertifikat gültig ist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool ServicePointManager_ServerCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool allowCertificate = true;

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                // Console.WriteLine("Accepting the certificate with errors:");
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    // Console.WriteLine("\tThe certificate subject {0} does not match.", certificate.Subject);
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    // Console.WriteLine("\tThe certificate chain has the following errors:");
                    foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                    {
                        //  Console.WriteLine("\t\t{0}", chainStatus.StatusInformation);

                        if (chainStatus.Status == X509ChainStatusFlags.Revoked)
                        {
                            allowCertificate = false;
                        }
                    }
                }

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
                {
                    // Console.WriteLine("No certificate available.");
                    allowCertificate = false;
                }

                // Console.WriteLine();
            }

            return allowCertificate;
        }
        #endregion
    }
}
