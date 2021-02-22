using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Co0nUtilZ
{
    /// <summary>
    /// Sucht in einem separaten Thread nach Dateien in einer bestimmten Ordnerstruktur
    /// </summary>
    public class C_FilesIndexer

    {
        #region Variables
        private String _Pathname;
        private Boolean _Recurse;
        //private List<String> _founditems=new List<String>();
        private Queue<string> folders = new Queue<string>(); //List with fodlers to parse
        private List<C_FilesIndexerElement> _founditems = new List<C_FilesIndexerElement>();
        public Thread _SearchThread;
        private String _Searchfor = "";
        private Boolean _SearchForNameOnly = true;
        private Boolean _includehiddenelement = false; //Also find hidden files?

        //private List<String> foldersprocessed = new List<string>(); //DEBUG



        public int foldersProcessedsoFar = 0;
        //public int foldersAddedToQueue = 0; //DEBUG
        //private int skippedfoldersastheyrsymbolic = 0; //DEBUG

        public delegate void EventHandler(object sender, String msg);
        //public delegate void ResultEventHandler(object sender, List<String> items);
        public delegate void SimpleEventHandler(object sender);
        public delegate void ResultEventHandler(object sender, List<C_FilesIndexerElement> items);
        public event EventHandler OnSearchStarted, OnSearchFinished, OnErrorOccured;
        public event ResultEventHandler OnItemsFound;
        public event SimpleEventHandler OnFolderProcessed, OnSearchAborted;

        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new FileSearcher-instance
        /// </summary>
        /// <param name="Pathname">Path to Searchbase-Directory</param>
        /// <param name="showhiddenfiles">Include hiddenfiles in result (Default: FALSE)</param>
        /// <param name="Recurse">NOT IN USE (YET) (Default:TRUE)</param>
        public C_FilesIndexer(String Pathname, Boolean showhiddenfiles = false, Boolean Recurse = true)
        {
            this._includehiddenelement = showhiddenfiles;
            this._Pathname = Pathname;
            this._Recurse = Recurse;
        }
        #endregion

        #region methods


        public List<C_FilesIndexerElement> FoundItems
        {
            get
            {
                return this._founditems;
            }
        }

        /// <summary>
        /// Stops a running searcher Thread
        /// </summary>
        public void StopSearch()
        {
            if (this._SearchThread != null)
            {
                lock (this._SearchThread)
                {
                    //if (this._SearchThread.IsAlive && this._SearchThread.ThreadState == ThreadState.Running)
                    if (this._SearchThread.ThreadState == ThreadState.Running)
                    {
                        this._SearchThread.Abort();
                        if (this.OnSearchAborted != null)
                        {
                            this.OnSearchAborted(this);
                        }
                    }
                }
            }
        }

        #region FileSearch
        /// <summary>
        /// Starts a Filesearch
        /// </summary>
        /// <param name="Searchfor">Searchpattern</param>
        /// <param name="SearchForNameOnly">NOT IN USE (YET). Default=True</param>
        public void FindItems(String Searchfor, Boolean SearchForNameOnly = true)
        {

            this._Searchfor = Searchfor;
            this._SearchForNameOnly = SearchForNameOnly;
            //this._SearchThread = new Thread(new ParameterizedThreadStart(findItemsWorker));
            this._SearchThread = new Thread(new ThreadStart(findItemsWorker));
            //this._SearchThread.Start(new { Searchfor, SearchForNameOnly, this._Recurse, this._Pathname });
            this._SearchThread.Name = "FilesSearcher " + this._Pathname;
            lock (this._SearchThread)
            {
                if (this.OnSearchStarted != null)
                {
                    this.OnSearchStarted(this, "Search started");
                }
            }
            this._SearchThread.Start();

            return;

        }


        /// <summary>
        /// Checks if Item ist symbolic
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsSymbolic(string path)
        {
            FileInfo pathInfo = new FileInfo(path);

            /*
            //DEBUG
            if (pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }
            else
            {
                return false;
            }
            */

            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        /// <summary>
        /// Checks if Hidden-Attribute is set
        /// </summary>
        /// <param name="path">PATH to Fiile or Folder</param>
        /// <returns>True if object is hidden, otherwise false</returns>
        private bool Ishidden(string path)
        {
            FileInfo pathInfo = new FileInfo(path);

            /*
            //DEBUG
            if (pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }
            else
            {
                return false;
            }
            */

            return pathInfo.Attributes.HasFlag(FileAttributes.Hidden);
        }

        /// <summary>
        /// Parses a Directory
        /// </summary>
        /// <param name="currentfolder">Directory to parse</param>
        /// <param name="Searchpattern">What to search for...</param>
        private void parseDirectoryWorker(String currentfolder, String Searchpattern)
        {
            try
            {
                //var filesInCurrent = System.IO.Directory.EnumerateFiles(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly); //Alle Dateien dieses ordners aufzählen
                var filesInCurrent = System.IO.Directory.EnumerateFiles(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly).AsParallel(); //Alle Dateien dieses ordners aufzählen                        
                List<String> newFiles = new List<string>();
                newFiles.AddRange(filesInCurrent);
                if (newFiles.Count() > 0)
                {
                    // Add files matching pattern to result
                    List<C_FilesIndexerElement> newfilObjs = new List<C_FilesIndexerElement>();
                    bool newobjectsreallyfound = false; //This indicates that an event should be raised or not

                    foreach (String fil in newFiles)
                    {
                        if (!this._includehiddenelement)
                        {
                            //Hidden objects should not be shown. first check if attribute is set...
                            if (!this.Ishidden(fil))
                            {//Object is not hidden...
                                newfilObjs.Add(new C_FilesIndexerElement(fil, C_FilesIndexerElement.TYPE_FILE)); //Add object to result
                                newobjectsreallyfound = true;
                            }
                            /*
                            else
                            {
                                //Object is hidden and hidden objects should not be shown....
                            }
                            */
                        }
                        else
                        {//Hidden objects should also be shown. no further checking necessary...
                            newfilObjs.Add(new C_FilesIndexerElement(fil, C_FilesIndexerElement.TYPE_FILE)); //Add object to result
                            newobjectsreallyfound = true;
                        }

                    }

                    if (newobjectsreallyfound /*Will be false if all found items are hidden*/)
                    {

                        lock (this._founditems)
                        {
                            this._founditems.AddRange(newfilObjs);
                        }
                        //New folder found
                        if (this.OnItemsFound != null)
                        {
                            //this.OnItemsFound(this, newFiles);
                            this.OnItemsFound(this, newfilObjs);
                        }
                    }

                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            catch (Exception ex)
            {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            try
            {


                //if (this._Recurse)
                if (this._Recurse)
                {
                    //Unterordner ermitteln die mit den Suchkriterien übereinstimmen
                    var matchingfoldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly).AsParallel();
                    //var matchingfoldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly).AsParallel().Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.ReparsePoint)); //This is much slower than checking afterwards

                    List<String> newFolders = new List<string>();
                    newFolders.AddRange(matchingfoldersInCurrent);



                    if (newFolders.Count() > 0)
                    {//New folder found
                        List<C_FilesIndexerElement> newFolObjs = new List<C_FilesIndexerElement>();
                        bool newobjectsreallyfound = false; //This indicates that an event should be raised or not

                        // Add Folders matching searchpattern to result

                        foreach (String fol in newFolders)
                        {
                            if (!this._includehiddenelement)
                            {
                                //Hidden objects should not be shown. first check if attribute is set...
                                if (!this.Ishidden(fol))
                                {//Object is not hidden...
                                    C_FilesIndexerElement temp = new C_FilesIndexerElement(fol, C_FilesIndexerElement.TYPE_FOLDER); //Add object to result
                                    newFolObjs.Add(temp);
                                    newobjectsreallyfound = true;
                                }
                                /*
                                else
                                {
                                    //Object is hidden and hidden objects should not be shown....
                                }
                                */
                            }
                            else
                            {//Hidden objects should also be shown. no further checking necessary...
                                C_FilesIndexerElement temp = new C_FilesIndexerElement(fol, C_FilesIndexerElement.TYPE_FOLDER); //Add object to result
                                newFolObjs.Add(temp);
                                newobjectsreallyfound = true;
                            }

                        }

                        if (newobjectsreallyfound /*Will be false if all found items are hidden*/)
                        {

                            if (this.OnItemsFound != null)
                            {
                                //this.OnItemsFound(this, newFolders);
                                try
                                {
                                    this.OnItemsFound(this, newFolObjs);
                                }
                                catch (Exception ex)
                                {
                                    if (this.OnErrorOccured != null)
                                    {
                                        this.OnErrorOccured(this, ex.ToString());
                                    }
                                }
                            }

                            lock (this._founditems)
                            {
                                this._founditems.AddRange(newFolObjs); // Add Folders matching searchpattern to result
                            }
                        }
                    }


                    //this.foldersprocessed.Add(currentfolder); //DEBUG

                    //Alle Unterordner ermitteln um diese der Suche hinzuzufügen                                
                    var foldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder).AsParallel(); // Alle Unterordner des aktuellen Ordners ermitteln...    


                    foreach (string subdir in foldersInCurrent)
                    //foreach (string subdir in DEBUG) //DEBUG
                    {
                        if (!IsSymbolic(subdir))
                        {
                            lock (this.folders)
                            {
                                this.folders.Enqueue(subdir); //Diese jeweils der Queue hinzufügen
                            }
                            //this.foldersAddedToQueue++; //DEBUG
                        }
                        /*
                        else
                        {
                            Console.WriteLine("\""+subdir + "\"is Symbolic link. Skipping..."); //DEBUG
                            skippedfoldersastheyrsymbolic++; //DEBUG
                        }
                        */
                    }
                    //                            this.foldersprocessed.Add(currentfolder); //DEBUG
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                //Console.WriteLine(UAEx.Message);
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            catch (Exception ex)
            {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
        }


        /// <summary>
        /// Workermethod for searcher Thread
        /// </summary>
        /// <param name="parameters"></param>
        private void findItemsWorker(/*object parameters*/ /*String Searchfor, Boolean SearchForNameOnly=true*/)
        {


            String Searchpattern = "";

            lock (this._founditems)
            {

                this._founditems = new List<C_FilesIndexerElement>(); //Ergebnisliste neu initialisieren
            }

            lock (this.folders)
            {
                this.folders.Clear();
            }

            if (this._SearchForNameOnly)
            {
                Searchpattern = "*" + this._Searchfor + "*";
            }
            else
            {
                Searchpattern = "*";
            }
            try
            {

                //Iterate through all subdirs...


                //this.foldersAddedToQueue = 0; //DEBUG
                //folders.Enqueue(this._Pathname);
                lock (this.folders)
                {
                    this.folders.Enqueue(this._Pathname); // Startordner der Queue hinzufügen
                }
                //this.foldersAddedToQueue++; //DEBUG

                System.DateTime lastFoldereventspawned = System.DateTime.Now;
                this.foldersProcessedsoFar = 0;

                while (folders.Count > 0) // Solange Einträge in der Queue sind...
                {

                    System.DateTime now = System.DateTime.Now;
                    TimeSpan ts = now - lastFoldereventspawned;
                    if (ts.TotalSeconds > 15) // Zyklisches Event zur Aktualisierung der bearbeiteten Ordner nur maximal alle 15 Sekunden werfen
                    {
                        if (OnFolderProcessed != null)
                        {
                            this.OnFolderProcessed(this);
                            lastFoldereventspawned = System.DateTime.Now;
                        }
                    }

                    string currentfolder = "";
                    lock (this.folders)
                    {
                        currentfolder = folders.Dequeue(); // Holt das erste Objekt aus der Queue (Entfernt es aus der Queue und gibt es als Wert zurück)
                    }

                    this.foldersProcessedsoFar++; //Zähler der abgearbeiteten Ordner erhöhen

                    this.parseDirectoryWorker(currentfolder, Searchpattern);

                }

            }

            catch (UnauthorizedAccessException ex)
            {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            catch (PathTooLongException ex)
            {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            catch (ThreadAbortException ex)
            {

            }
            catch (Exception ex)
            {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            finally
            {


            }

            if (this.OnSearchFinished != null)
            {
                this.OnSearchFinished(this, "Search finished.");
            }
            return;
        }
        #endregion

        #endregion
    }
}
