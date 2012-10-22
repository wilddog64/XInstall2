using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Xml;

using XInstall.Core;
using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall {
    /// <summary>
    /// class XInstall -
    ///     This class inherits the XmlConfigMgr class
    ///     for reading an input XML configuration file.
    ///     It also builds up the ActionNodeCollection for
    ///     ready to execute each action inside the input
    ///     XML file.
    /// </summary>
    public class XInstall : XmlConfigMgr {
        // prviate variables
        private string _strActionDll =         // the dll we are going to load
            Environment.CurrentDirectory +     // at run time
            Path.DirectorySeparatorChar  +
            "xinstall.core.actions.dll";

        private string      _strConfigFile  = null; // a variable to hold an xml file
        private string      _strDumpFile    = null;
        private XmlNodeList _xnlActionNodes = null;
        private Hashtable   _htCtorInfos    = new Hashtable();
        private bool        _bRestartable   = true;
        private bool        _bStartGUI      = false;

        // an ActionNodeCollection variable
        private ActionNodeCollection _acActionNodeCollection = null;

        // A Default Constructor with no parameters
        public XInstall() : base() {}

        /// <summary>
        /// public XInstall( string strXmlConfigFile ) -
        ///     This constructor will instaniciate the XInstall object.
        ///     It accepts one input parameter, which is an Xml configuration
        ///     file.
        /// </summary>
        /// <param name="strXmlConfigFile">an xml file</param>
        public XInstall( string strXmlConfigFile ) : base( strXmlConfigFile ) {

            // Configure the current application domain to use
            // windows-based principals
            AppDomain ThisAppDomain = AppDomain.CurrentDomain;
            ThisAppDomain.SetPrincipalPolicy( PrincipalPolicy.WindowsPrincipal );

            // by default, we want message print to both file and console
            // setup xml file to be parsed and alsn initialize
            // the ActionNodeCollection object.
            string strDumpFile = this.DumpFile;
            if ( File.Exists( strDumpFile ) ) {
                string dt         = DateTime.Today.ToString("ddMMyy");
                string strNewFile =
                    Path.GetFileNameWithoutExtension( strDumpFile ) + "." + dt + ".dmp";
                File.Move( strDumpFile, strNewFile );
                base.LogItWithTimeStamp(
                    String.Format(@"{0}: Dump file {1} exist! start working
                                  on incomplete object!",
                                  this.ObjectName, strNewFile));
                this.ConfigFile = strNewFile;
            } else
                this.ConfigFile = strXmlConfigFile;

            base.XPath = "//setup/*";

            this.StartParsing();
        }


        public XInstall( string strXmlConfigFile,
                         ISendLogMessage ISendThisLogMessage ) : base( ISendThisLogMessage ) {
            // Configure the current application domain to use
            // windows-based principals
            AppDomain ThisAppDomain = AppDomain.CurrentDomain;
            ThisAppDomain.SetPrincipalPolicy( PrincipalPolicy.WindowsPrincipal );


            // by default, we want message print to both file and console
            // setup xml file to be parsed and alsn initialize
            // the ActionNodeCollection object.
            string strDumpFile = this.DumpFile;
            if ( File.Exists( strDumpFile ) ) {
                string dt         = DateTime.Today.ToString("ddMMyy");
                string strNewFile =
                    Path.GetFileNameWithoutExtension( strDumpFile ) + "." + dt + ".dmp";
                File.Move( strDumpFile, strNewFile );
                base.LogItWithTimeStamp(
                    String.Format(@"{0}: Dump file {1} exist! start working
                                  on incomplete object!",
                                  this.ObjectName, strNewFile));
                this.ConfigFile = strNewFile;
            } else
                this.ConfigFile = strXmlConfigFile;

            base.XPath = "//setup/*";

            this.StartParsing();
        }

        /// <summary>
        /// property Restartable -
        ///     get/set the flag to allow whether the
        ///     program should start from where it fails
        ///     or not.
        /// </summary>
        public bool Restartable
        {
            get {
                return this._bRestartable;
            }
            set {
                this._bRestartable = value;
            }
        }


        public bool StartGUI
        {
            get {
                return this._bStartGUI;
            }
            set {
                this._bStartGUI = value;
            }
        }


        protected override string ObjectName
        {
            get {
                return this.GetType().Name;
            }
        }


        /// <summary>
        /// public void Run() -
        ///     Execute the XInstall object.
        /// </summary>
        public void Run() {

            for ( int iActionNodeIdx = 0;
                    iActionNodeIdx < this._acActionNodeCollection.Count;
                    iActionNodeIdx++ ) {
                // get the action object
                ActionElement ae = this._acActionNodeCollection[ iActionNodeIdx ];

                base.LogItWithTimeStamp( String.Format("{0}: start executing {1}", base.ObjectName, ae.Name));

                // call object Execute method through
                // the IAction inteface's execute method
                ae.Execute();

                base.LogItWithTimeStamp( String.Format("{0} : executing of {1} is {2}", this.Name, ae.Name, ae.IsComplete ?  "Complete" : "Incomplete"));
            }
        }



        /// <summary>
        /// private property ConfigFile -
        ///     get/set the Xml configuration file
        /// </summary>
        private string ConfigFile
        {
            get {
                // check to see if input is null
                if ( this._strConfigFile == null )
                    throw new ArgumentNullException(
                        this._strConfigFile,
                        ("property ConfigFile cannot be null"));

                // check file's existance
                if ( !System.IO.File.Exists( this._strConfigFile ) )
                    throw new System.IO.FileNotFoundException(
                        String.Format("File {0} cannot be found",
                                      this._strConfigFile), this._strConfigFile);

                // return the Configuration file
                return this._strConfigFile;
            }
            set { this._strConfigFile = value; }
        }


        /// <summary>
        /// private property DumpFile -
        ///     get/set the dump file name.  If DumpFile is not
        ///     set, return the running program name with the .dmp
        ///     extension.
        /// </summary>
        private string DumpFile
        {
            get {
                if ( this._strDumpFile == null )
                    this._strDumpFile =
                        Path.GetFileNameWithoutExtension(
                            Environment.GetCommandLineArgs()[0]);
                return this._strDumpFile + ".dmp";
            }
            set {
                this._strDumpFile = value;
            }
        }


        /// <summary>
        ///
        ///     this method accepts one input, an xml configuration
        ///     file and after read in the file, it will start to
        ///     execute each Action node in the input XML file.
        /// </summary>
        /// <param name="strXmlFile"></param>
        private void StartParsing( string strXmlFile ) {

            // parse an XML file and generate an XmlNodeList objects.
            this._xnlActionNodes = base.Parse( strXmlFile );

            // add each ActionNode object into our collection
            foreach ( XmlNode xnAction in _xnlActionNodes ) {
                if ( xnAction.NodeType != XmlNodeType.Comment ) {
                    object objConstructor =
                        this._acActionNodeCollection.Add( xnAction );
                    if ( objConstructor != null )
                        this._htCtorInfos.Add(xnAction, objConstructor );
                }
            }

            // walk through each ActionObject and execute them
        }


        /// <summary>
        ///  This method will parse an Xml configuration and
        ///  execute each Action node in the input XML file.
        /// </summary>
        public void StartParsing() {
            // create an ActionPackageCollection object
            ActionPackageCollection ActionPackages = new ActionPackageCollection();

            // Parse configuration file and add each object into
            // an ActionPackage container.
            this._xnlActionNodes = base.Parse( this.ConfigFile );
            foreach ( XmlNode xnActionPackage in this._xnlActionNodes ) {
                if ( xnActionPackage.Name.Equals( @"package") ) {
                    // skip comment
                    if ( xnActionPackage.NodeType != XmlNodeType.Comment ) {
                        XmlNode xnRunnable =
                            xnActionPackage.Attributes.GetNamedItem("runnable");
                        bool bRunnable     = false;
                        if ( xnRunnable != null )
                            bRunnable = bool.Parse( xnRunnable.Value );

                        // only object's runnable propery is set to true
                        // will be added into the container
                        if ( bRunnable )
                            ActionPackages.Add( xnActionPackage );
                    }
                }
            }

            // now start to execute each object
            for ( int i = 0; i < ActionPackages.Count; i++ ) {
                base.LogItWithTimeStamp(
                    String.Format( @"{0}: start executing package - {1}", this.Name,
                                   (ActionPackages[i] as ActionPackage).PackageName ) );

                ActionPackages[i].Execute();

                base.LogItWithTimeStamp(
                    String.Format( @"{0}: execute package {1} {2} ",
                                   this.Name, (ActionPackages[i] as ActionPackage).PackageName,
                                   ActionPackages[i].IsComplete ?
                                   @"successfully"         :
                                   @"failed" ) );

            }
        }


        public new string Name
        {
            get {
                return this.GetType().Name;
            }
        }


        /// <summary>
        /// public void DumpIcompleteInfoToDisk( string strDumpFileName ) -
        ///     Dumps incompleted XML tags into a file on disk. The function
        ///     accepts one parameter, which the name of file to have dumpped
        ///     information.
        /// </summary>
        /// <param name="strDumpFileName">name of dump file</param>
        public void DumpIcompleteInfoToDisk( string strDumpFileName ) {
            // create a new XML file and prepare to write
            // incomplete XML nodes into it.
            XmlDocument xdDoc       = new XmlDocument();
            XmlTextWriter xtwWriter = new XmlTextWriter(
                                          strDumpFileName,
                                          System.Text.Encoding.ASCII );
            xtwWriter.Formatting    = Formatting.Indented;
            XmlElement xe           = xdDoc.CreateElement( "setup" );

            // get a list of incomplete objects and go through them
            ArrayList alDumpList = this.GetIncompleteObjects();
            XmlNode   xnNewNode  = null;
            for ( int i = 0; i < alDumpList.Count; i++ ) {
                // retrieve the XmlNode object and its attributes
                // then create a new XmlNode and stuff values into
                // it. Finally append the newly created node into XmlElement
                // oboject.
                XmlNode xnActionNode       = (XmlNode) alDumpList[i];
                XmlAttributeCollection xac = xnActionNode.Attributes;
                xnNewNode                  = xdDoc.CreateNode(
                                                 XmlNodeType.Element, xnActionNode.Name, null);
                foreach ( XmlAttribute xa in xac ) {
                    if ( xa.Name.Equals("generateexception") &&
                            xa.Value.Equals("true") )
                        xa.Value = "false";
                    xnNewNode.Attributes.SetNamedItem( xa );
                }
                xe.AppendChild( xnNewNode );
            }

            // append newly create node into new Xml document
            // and write it to the document.
            xdDoc.AppendChild( xe );
            xdDoc.WriteTo( xtwWriter );
            xtwWriter.Flush();
        }


        /// <summary>
        /// public void CleanUp() -
        ///     provide a way to rollback to the original state
        ///     from where it fails.  It calls each object's RemoveIt()
        ///     derives from the ICleanUp interface.
        /// </summary>
        public void CleanUp() {
            ArrayList alCompleteObjects = new ArrayList();

            // retrieving the object state that is marked as complete.
            // IsComplete property set to true ... and go throght each
            // one of them.
            alCompleteObjects = this.GetCompleteObjects();
            for ( int i = 0; i < alCompleteObjects.Count; i++ ) {
                // cast the object into an ICleanUp interface
                // and call the RemoveIt method
                ICleanUp IActionCleanUpObject = ( alCompleteObjects[i] as ICleanUp );
                IActionCleanUpObject.RemoveIt();
            }
        }


        /// <summary>
        /// private ArrayList GetIncompleteObjects() -
        ///     gets the Action Object that has an incompleted
        ///     states and return them as an ArrayList.
        /// </summary>
        /// <returns>
        /// an ArrayList object that contains the incomplete action object
        /// </returns>
        private ArrayList GetIncompleteObjects() {
            // initialize an ArrayList variable alDumpList to
            // store the imcomplete object
            ArrayList alDumpList = new ArrayList();

            // go through each entry in the hash table, the value contains
            // the IAction object and the key is an XML node object
            foreach ( DictionaryEntry de in this._htCtorInfos ) {
                // if object state is incomplete then store the XmlNode object
                // into the ArrayList alDumpList
                IAction IActionObject = (IAction) de.Value;
                if ( IActionObject != null )
                    if ( !IActionObject.IsComplete )
                        alDumpList.Add( de.Key );
            }

            // reverse the alDumpList and return it.
            alDumpList.Reverse();
            return alDumpList;
        }


        /// <summary>
        /// private ArrayList GetCompleteObjects() -
        ///     get the object that has complete state set to true
        /// </summary>
        /// <returns>an array list of complete objects</returns>
        private ArrayList GetCompleteObjects() {
            // alCompleteObjects is an ArrayList object that
            // holds the action object that has IsComplete set to true.
            ArrayList alCompleteObjects = new ArrayList();

            // go throught each object in the hash table and
            // check the object that has IsComplete set to true into
            // alCompleteObjects ArrayList.
            foreach ( DictionaryEntry de in this._htCtorInfos ) {
                IAction IActionObject = (IAction) de.Value;
                if ( IActionObject != null )
                    if ( IActionObject.IsComplete )
                        alCompleteObjects.Add( IActionObject );
            }
            return alCompleteObjects;
        }
    }
}
