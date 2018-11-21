using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Co0nSearchC
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
        private String _Searchfor="";
        private Boolean _SearchForNameOnly = true;

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
        /// <param name="Recurse">NOT IN USE (YET) (Default:TRUE)</param>
        public C_FilesIndexer(String Pathname, Boolean Recurse = true)
        {
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
                if (this._SearchThread.IsAlive && this._SearchThread.ThreadState == ThreadState.Running)
                {
                    this._SearchThread.Abort();
                    if (this.OnSearchAborted != null)
                    {
                        this.OnSearchAborted(this);
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
                    foreach (String fil in newFiles)
                    {
                        newfilObjs.Add(new C_FilesIndexerElement(fil, C_FilesIndexerElement.TYPE_FILE));
                    }

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


                        // Add Folders matching searchpattern to result

                        foreach (String fol in newFolders)
                        {
                            C_FilesIndexerElement temp = new C_FilesIndexerElement(fol, C_FilesIndexerElement.TYPE_FOLDER);
                            newFolObjs.Add(temp);
                        }


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


                    //this.foldersprocessed.Add(currentfolder); //DEBUG

                    //Alle Unterordner ermitteln um diese der Suche hinzuzufügen                                
                    var foldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder).AsParallel(); // Alle Unterordner des aktuellen Ordners ermitteln...    
                                                                                                                 //var foldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, "*").AsParallel().Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.ReparsePoint)); // Alle Unterordner des aktuellen Ordners ermitteln...                            //This is much slower than checking afterwards

                    //String[] DEBUG = foldersInCurrent.ToArray(); //DEBUG

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
            if (this.OnSearchStarted != null)
            {
                this.OnSearchStarted(this, "Search started");
            }

            
            String Searchpattern = "";
           
            this._founditems = new List<C_FilesIndexerElement>(); //Ergebnisliste neu initialisieren

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
            try {

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
                    lock (this.folders) {
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
            catch (Exception ex) {
                if (this.OnErrorOccured != null)
                {
                    this.OnErrorOccured(this, ex.ToString());
                }
            }
            finally {

            
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
