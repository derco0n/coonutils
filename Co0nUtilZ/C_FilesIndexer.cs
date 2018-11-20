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
        private List<C_FilesIndexerElement> _founditems = new List<C_FilesIndexerElement>();
        public Thread _SearchThread;
        private String _Searchfor="";
        private Boolean _SearchForNameOnly = true;



        public int foldersProcessedsoFar = 0;

        public delegate void EventHandler(object sender, String msg);
        //public delegate void ResultEventHandler(object sender, List<String> items);
        public delegate void FoldersProcessedEventHandler(object sender);
        public delegate void ResultEventHandler(object sender, List<C_FilesIndexerElement> items);
        public event EventHandler OnSearchStarted, OnSearchFinished, OnErrorOccured;
        public event ResultEventHandler OnItemsFound;
        public event FoldersProcessedEventHandler OnFolderProcessed;

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
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
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

            /*
             * dynamic paras = parameters;
            

            String Searchfor = paras.Searchfor;
            String Pathname = paras._Pathname;
            Boolean SearchForNameOnly = paras.SearchForNameOnly;
            Boolean Recurse = paras._Recurse;

            */

            String Searchpattern = "";
            //List<System.IO.FileInfo> filesfound = new List<System.IO.FileInfo>();
            //List<string> founditems = new List<string>();
            //List<C_FilesIndexerElement> founditems = new List<C_FilesIndexerElement>();

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
                Queue<string> folders = new Queue<string>();


                //folders.Enqueue(this._Pathname);
                folders.Enqueue(this._Pathname); // Startordner der Queue hinzufügen


                System.DateTime lastFoldereventspawned = System.DateTime.Now;
                this.foldersProcessedsoFar = 0;
                while (folders.Count > 0) // Solange Einträge in der Queue sind...
                {
                    this.foldersProcessedsoFar++; //Ordnerzähler erhöhen
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

                    string currentfolder = folders.Dequeue(); // Holt das erste Objekt aus der Queue (Entfernt es aus der Queue und gibt es als Wert zurück)
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
                            //founditems.AddRange(newFiles);
                            //founditems.AddRange(newfilObjs);
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
                            //var matchingfoldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly);                            
                            //var matchingfoldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly).AsParallel().Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.ReparsePoint)); //This is much slower than checking afterwards
                            var matchingfoldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, Searchpattern, SearchOption.TopDirectoryOnly).AsParallel();
                            List<String> newFolders = new List<string>();
                            newFolders.AddRange(matchingfoldersInCurrent);
                            
                            if (newFolders.Count() > 0)
                            {//New folder found
                                List<C_FilesIndexerElement> newFolObjs = new List<C_FilesIndexerElement>();
                                

                                // Add Folders matching searchpattern to result
                                foreach (String fol in newFolders)
                                {
                                    newFolObjs.Add(new C_FilesIndexerElement(fol, C_FilesIndexerElement.TYPE_FOLDER));
                                }

                                if (this.OnItemsFound != null)
                                {
                                    //this.OnItemsFound(this, newFolders);
                                    this.OnItemsFound(this, newFolObjs);
                                }
                                
                                lock (this._founditems)
                                {
                                    this._founditems.AddRange(newFolObjs); // Add Folders matching searchpattern to result
                                }
                            }

                            //var foldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, "*"); // Alle Unterordner des aktuellen Ordners ermitteln...    
                            //var foldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, "*").AsParallel().Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.ReparsePoint)); // Alle Unterordner des aktuellen Ordners ermitteln...                            //This is much slower than checking afterwards
                            var foldersInCurrent = System.IO.Directory.EnumerateDirectories(currentfolder, "*").AsParallel();
                            foreach (string subdir in foldersInCurrent)
                            {
                                if (!IsSymbolic(subdir))
                                {
                                    folders.Enqueue(subdir); //Diese jeweils der Queue hinzufügen
                                }
                            }
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
                    catch (Exception ex){
                        if (this.OnErrorOccured != null)
                        {
                            this.OnErrorOccured(this, ex.ToString());
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
