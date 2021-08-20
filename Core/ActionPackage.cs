using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Xml;

using XInstall.Util;
using XInstall.Util.Log;

/*
 * Class Name    : ActionLoader
 * Inherient     : ActionElement
 * Functionality : A container for Action Objects and execute
 *                 each one of them sequencially.
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------
 * mliang           05/01/2003      Initial creation
 *
 * mliang           01/27/2005      Bug fix - The ActionPackage
 *                                  falsefully report itself fail
 *                                  to executed.  The fix is to
 *                                  remove the _bIsComplete boolean
 *                                  variable and use parent class's
 *                                  IsComplete property to set/get
 *                                  the status.
 *
 * mliang           02/03/2005      Add predefined variable, PackageName
 *                                  and PackageDesc, so that user
 *                                  can access them via ${PackageName} and
 *                                  ${PackageDesc} from XML configuration
 *                                  file.
 *
 * mliang           03/15/2005      Remove hard coding inside CreateActionPackage
 *                                  and use reflection to search properties and
 *                                  set/get their value.  This makes the code more
 *                                  compact.
 *
 * mliang           03/23/2005      Fixing a bug for the onfail attribute for the
 *                                  ActionPackage, the CreateActionPackage method
 *                                  use OnFailHanderName one more time for the onfail
 *                                  attribute, which should be OnBeforeStartHandlerName.
 *
 */
namespace XInstall.Core {
    /// <summary>
    /// ActionPackage is a class that will contains one or more action objects.
    /// The class inherits ActionNodeCollection class, which will allow it to
    /// collect serveral action objects and execute them all at one time as a whole
    /// package.  The adapted IAction interface will allow it to provide a
    /// generic execute method to execute each action objects within it.
    /// </summary>
    public class ActionPackage : ActionNodeCollection {
        private const int MAX_NEST_LEVEL = 20;

#region private member variables
        private ArrayList _alActionObjectList   = new ArrayList();
        private ArrayList _alActionNameList     = new ArrayList();
        private XmlNode   _xnActionPackage      = null;
        private XmlNode   _OnSuccessHandler     = null;
        private XmlNode   _OnFailureHandler     = null;
        private XmlNode   _OnBeforeStartHandler = null;

        private string _strCurrentActionPath     = Environment.CurrentDirectory;
        private string _strActionDLL             = String.Empty;
        private string _strPackageName           = null;
        private string _strPackageDir            = null;
        private string _strPackageDescription    = null;
        private string _strExitMessage           = null;
        private string _OnSuccessHandlerName     = String.Empty;
        private string _OnFailureHandlerName     = String.Empty;
        private string _OnBeforeStartHandlerName = String.Empty;

        private bool   _bWriteToConsole       = true;
        private bool   _bWriteToFile          = true;

        private enum PACKAGE_OPR_CODE {
            PKG_OPR_CREATE_SUCCESSFULLY = 0,
            PKG_OPR_PACKAGE_NAME_REQUIRED,
            PKG_OPR_BOOLEAN_PARSING_ERROR,
            PKG_OPR_DIRECTORY_NOTFOUND,
            PKG_OPR_ACTION_NOTFOUND,
            PKG_OPR_REQUEST_NOTRUN,
            PKG_OPR_PACKAGE_NOT_PROVIDED,
            PKG_OPR_INDEX_OUTOF_RANGE,
            PKG_OPR_ACTIONOBJECT_EXCEPTION,
        }
        private PACKAGE_OPR_CODE _enumPackageOprCode =
            PACKAGE_OPR_CODE.PKG_OPR_CREATE_SUCCESSFULLY;
        private string[] _strMessages =
            {
                @"{0}: package {1} was created successfully",
                @"{0}: package has no name",
                @"{0}: boolean variable {1} parsing error",
                @"{0}: given directory {1} for package {2} does not exist!!",
                @"{0}: action {1} is not defined in this package {2}",
                @"{0}: package not execute because runnable is set to false!",
                @"{0}: package cannot be null!",
                @"{0}: index out of range",
                @"{0}: action object {1} has generated exception, message - {2}.",
            };
#endregion

        /// <summary>
        /// The ActionPackage constructor will accepts an XML node and
        /// creates a package that contains every action object inside
        /// it.
        /// </summary>
        /// <param name="xnPackages">an XML node type variable</param>
        /// <remarks>
        ///     When using this constructor it will actually load default
        ///     action object library, XInstall.Core.Actions.dll.  To load
        ///     a different action library, use the overload constructor
        ///     ActionPackage( XmlNode xnPackage, string strDll2Load )
        ///     instead.
        /// </remarks>
        public ActionPackage( XmlNode xnPackage ) : base( xnPackage ) {
            // if custom action dll is provided load it.
            XmlNode xnActionDLL       = xnPackage.Attributes.GetNamedItem("actiondllpath");
            string strCustomActionDLL = null;

            if ( xnActionDLL == null )
                strCustomActionDLL = "XInstall.Core.Actions.dll";
            else
                strCustomActionDLL = xnActionDLL.Value;

            base.LoadAssembly = Environment.CurrentDirectory +
                                                Path.DirectorySeparatorChar  +
                                                strCustomActionDLL;

            // the default core action dll will be loaded all the time.
            this.CreateActionPackage ( xnPackage );
        }


        public ActionPackage( XmlNode xnPackage, string DllPath ) : base( xnPackage ) {
            // if custom action dll is provided load it.
            XmlNode xnActionDLL       = xnPackage.Attributes.GetNamedItem("actiondllpath");
            string strCustomActionDLL = null;

            if ( xnActionDLL == null )
                strCustomActionDLL = "XInstall.Core.Actions.dll";
            else
                strCustomActionDLL = xnActionDLL.Value;

            base.LoadAssembly = DllPath + Path.DirectorySeparatorChar  + strCustomActionDLL;

            // the default core action dll will be loaded all the time.
            this.CreateActionPackage( xnPackage );
        }


        /// <summary>
        /// an overloaded constructor that takes only one parameter, strDll2Load.
        /// </summary>
        /// <param name="strDll2Load">
        ///     type of string contains a path points to the custom dll to be loaded
        /// </param>
        /// <remarks>
        ///     after successfully load the custom dll, use object.LoadActionPackage to
        ///     load the ActionPackage Xml node.
        /// </remarks>
        public ActionPackage ( string strDll2Load ) : base ( strDll2Load ) {
            this.LoadAssembly = this._strCurrentActionPath  + Path.DirectorySeparatorChar + this._strActionDLL;
        }


        /// <summary>
        /// get/set the custom Action DLL
        /// </summary>
        /// <remarks>
        ///     this property call the base class's LoadAssembly to load
        ///     the custom dll. If DLL is not provide, the default dll,
        ///     XInstall.Core.Actions.dll will be loaded.
        /// </remarks>
        public string ActionDLL
        {
            get { return this._strActionDLL; }
            set { this._strActionDLL = base.LoadAssembly = value; }
        }


        /// <summary>
        /// set an ActionPackage.
        /// </summary>
        /// <remarks>
        ///     this property calls the private method, CreateActionPackage
        ///     to create an ActionPackage object by reading the XML node.
        /// </remarks>
        public XmlNode LoadActionPackage
        {
            set {
                this._xnActionPackage = value;
                this.CreateActionPackage( _xnActionPackage );
            }
        }


        /// <summary>
        /// Private method CreateActionPackage will read
        /// the incoming XML node and create an Action Package.
        /// </summary>
        /// <param name="xnPackage">type of XmlNode that constains an XML node</param>
        /// <remarks>
        /// a properly formed ActionPackage node will contains the following attributes,
        ///   <ul>
        ///       <li>
        ///           runnable - boolean variable that indicates if an
        ///                      ActionPackage will be executed or not.
        ///                      By default this is set to true.
        ///       </li>
        ///       <li>
        ///           name     - name of the package. It is a required
        ///                      attribute.
        ///       </li>
        ///       <li>
        ///           packagedir  - a directory that package works with.
        ///                         If not provided, the current directory
        ///                         will be used.
        ///       </li>
        ///       <li>
        ///           description - a brief description of what does package
        ///                         do
        ///       </li>
        ///   </ul>
        /// </remarks>
        private void CreateActionPackage( XmlNode xnPackage ) {

            // if xnPackage is null, raise an exception
            if ( xnPackage == null ) {
                this._enumPackageOprCode = PACKAGE_OPR_CODE.PKG_OPR_PACKAGE_NOT_PROVIDED;
                this._strExitMessage     = String.Format( this._strMessages[ this.ExitCode ], this.Name );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
            }

            this.LookForProperties( this, xnPackage );

            string OnSuccessHandlerName     = String.Empty;
            string OnFailHandlerName        = String.Empty;
            string OnBeforeStartHandlerName = String.Empty;

            if ( this.OnSuccessHandler.Length > 0 )
                OnSuccessHandlerName = this.OnSuccessHandler;

            if ( this.OnFailHandler.Length > 0 )
                OnFailHandlerName = this.OnFailHandler;

            if ( this.OnBeforeStartHandler.Length > 0 )
                OnBeforeStartHandlerName = this.OnBeforeStartHandler;

            if ( !ActionVariables.IsVariableExist( "PackageName" ) )
                ActionVariables.Add( "PackageName", this.PackageName );

            if ( !ActionVariables.IsVariableExist( "PackageDir" ) )
                ActionVariables.Add( "PackageDir", this.PackageDirectory );

            if ( !ActionVariables.IsVariableExist( "PackageDesc" ) )
                ActionVariables.Add( "PackageDesc", this.PackageDescription );

            // now come to setup the log file
            base.OutToConsole = this._bWriteToConsole;
            base.OutToFile    = this._bWriteToFile;

            // add actions that are contained in this package to our action collection
            if ( xnPackage.HasChildNodes ) {
                foreach ( XmlNode xnAction in xnPackage.ChildNodes ) {
                    if ( xnAction.NodeType != XmlNodeType.Comment ) {
                        if ( xnAction.Name.Equals( OnSuccessHandlerName ) )
                            this._OnSuccessHandler = xnAction;
                        else if ( xnAction.Name.Equals( OnFailHandlerName ) )
                            this._OnFailureHandler = xnAction;
                        else if ( xnAction.Name.Equals( OnBeforeStartHandlerName ) )
                            this._OnBeforeStartHandler = xnAction;
                        else {
                            object objConstructor = base.Add( xnAction );

                            if ( objConstructor != null ) {
                                this._alActionObjectList.Add( objConstructor );
                                this._alActionNameList.Add( xnAction );
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// an indexer method that retrieves IAction by using an
        /// index.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public new ActionElement this[ int iActionIdx ]
        {
            get {
                if ( iActionIdx < 0 || iActionIdx > this._alActionObjectList.Count ) {
                    this._enumPackageOprCode = PACKAGE_OPR_CODE.PKG_OPR_INDEX_OUTOF_RANGE;
                    this._strExitMessage     = String.Format( this._strMessages [ this.ExitCode ], this.Name );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false);
                }
                return this._alActionObjectList[ iActionIdx ] as ActionElement;
            }
        }


        public XmlNode GetXmlActionNodeByIndex( int iActionNodeIdx ) {
            XmlNode xn = this._alActionNameList[ iActionNodeIdx ] as XmlNode;
            return xn;
        }


        /// <summary>
        /// return the number of Action Objects
        /// </summary>
        public new int Count
        {
            get { return this._alActionObjectList.Count; }
        }


        /// <summary>
        /// gets description of a given package
        /// </summary>
        /// <remarks>
        ///     The package description is used to help
        ///     document the package itself. It's not a
        ///     required attribute.
        /// </remarks>
        [Action("description", Needed=false, Default="")]
        public string PackageDescription
        {
            get { return this._strPackageDescription; }
            set { this._strPackageDescription = value; }
        }


        /// <summary>
        /// set a flag that tells the package should generate an
        /// exception by itself.
        /// </summary>
        /// <remarks>
        ///     The purpose of the property is for testing how
        ///     the system react to the exception generated from
        ///     within itself.
        /// </remarks>
        [Action("allowgenerateexception", Needed=false, Default="false")]
        public new string AllowGenerateException
        {
            set {
                try {
                    base.AllowGenerateException = bool.Parse( value );
                } 
								catch ( Exception ) {
                    this._enumPackageOprCode = PACKAGE_OPR_CODE.PKG_OPR_BOOLEAN_PARSING_ERROR;
                    this._strExitMessage     = String.Format( this._strMessages[ this.ExitCode ], this.Name );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                }
            }
        }


        /// <summary>
        /// gets the name of the class
        /// </summary>
        /// <remarks></remarks>
        public new string Name
        {
            get { return this.GetType().Name; }
        }


        /// <summary>
        /// gets the name of the package
        /// </summary>
        /// <remarks></remarks>
        [Action("name", Needed=true)]
        public string PackageName
        {
            get { return this._strPackageName; }
            set { this._strPackageName = value; }
        }


        /// <summary>
        /// gets a package directory
        /// </summary>
        /// <remarks>
        ///     a base directory that package will start working with.
        ///     If not provided, the current directory will be used.
        /// </remarks>
        [Action("packagedir", Needed=false, Default=".")]
        public string PackageDirectory
        {
            get { return this._strPackageDir; }
            set {
                this._strPackageDir = value;
                if ( !Directory.Exists( this._strPackageDir  ) ) {
                    this._enumPackageOprCode = PACKAGE_OPR_CODE.PKG_OPR_DIRECTORY_NOTFOUND;
                    this._strExitMessage     = String.Format( this._strMessages[ this.ExitCode ], this.Name, this._strPackageDir , this.PackageName );
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
                }
            }
        }


        /// <summary>
        /// gets an exit message that is corresponding to the
        /// exit message
        /// </summary>
        /// <remarks></remarks>
        public new string ExitMessage
        {
            get { return this._strExitMessage; }
        }


        /// <summary>
        /// gets an exit code from the execution of an ActionPackage
        /// </summary>
        /// <remarks></remarks>
        public new int ExitCode
        {
            get { return (int) this._enumPackageOprCode; }
        }


        /// <summary>
        /// gets a boolean value that shows if a given
        /// ActionPackage's execution is completed or not.
        /// </summary>
        /// <remarks></remarks>
        public new bool IsComplete
        {
            get { return base.IsComplete; }
        }


        /// <summary>
        /// gets an boolean value that shows if a given ActionPackage
        /// should be executed or not.
        /// </summary>
        [Action("runnable", Needed=false, Default="true")]
        public new string Runnable
        {
            // get { return this._bRunnable; }
            get { return base.Runnable.ToString(); }
            set { base.Runnable = bool.Parse( value ); }
        }


        [Action("onsuccess", Needed=false, Default="")]
        public string OnSuccessHandler
        {
            get { return this._OnSuccessHandlerName; }
            set { this._OnSuccessHandlerName = value; }
        }


        [Action("onfail", Needed=false, Default="")]
        public string OnFailHandler
        {
            get { return this._OnFailureHandlerName; }
            set { this._OnFailureHandlerName = value; }
        }


        [Action("onbeforestart", Needed=false, Default="")]
        public string OnBeforeStartHandler
        {
            get { return this._OnBeforeStartHandlerName; }
            set { this._OnBeforeStartHandlerName = value; }
        }


        /// <summary>
        /// execute each Action Object in a given ActionPackage object.
        /// </summary>
        /// <remarks>
        ///     The underlying object are all casted into an IAction interface
        ///     and the Execute() method of the interface was called.
        /// </remarks>
        public override void Execute() {
            ActionElement ae = null;
            WindowsIdentity ThisIdentity = WindowsIdentity.GetCurrent();
            string CurrentUser = ThisIdentity.Name;

            base.LogItWithTimeStamp( String.Format("{0}: package {1} executed by {2}", this.Name, this.PackageName, CurrentUser ) );

            try {
                try {
                    base.Execute();
                    this.ProcessHandler( this._OnBeforeStartHandler, 0 );
                    for ( int iActionIdx = 0; iActionIdx < base.Count; iActionIdx++ ) {
                        ae = base[ iActionIdx ] as ActionElement;

                        if ( ae != null )
                            ae.Execute();
                    }
                } 
								catch ( Exception e ) {
                    this.ProcessHandler( this._OnFailureHandler, 0 );
                    base.IsComplete = false;

                    throw e;
                }

                this.ProcessHandler( this._OnSuccessHandler, 0 );
            } 
						finally {
                ActionVariables.Clear();
            }
            base.IsComplete = true;
        }


        /// <summary>
        /// Create an ActionPackage that contains the Action Objects that
        /// are not completed during the package's execution.
        /// </summary>
        /// <returns>
        ///     an XmlNode type object that contains one or more incompleted
        ///     Action Objects.
        /// </returns>
        public XmlNode CreateIncompletePackage() {
            XmlDocument xdDoc = new XmlDocument();

            // create ActionPackage element
            // XmlNode xn = xdDoc.CreateNode( XmlNodeType.Element, this.PackageName );
            XmlNode xn = xdDoc.CreateNode( XmlNodeType.Element, "package", null );

            // create name attribute
            XmlNode xnPackageName = xdDoc.CreateNode( XmlNodeType.Attribute, "name", null );
            xnPackageName.Value   = this.PackageName;
            xn.Attributes.SetNamedItem( xnPackageName );


            // create runnable attribute
            XmlNode xnRunnable = xdDoc.CreateNode( XmlNodeType.Attribute, "runnable", null );
            xnRunnable.Value   = this.Runnable.ToString().ToLower();
            xn.Attributes.SetNamedItem( xnRunnable );

            // create package dir
            XmlNode xnPackageDir = xdDoc.CreateNode( XmlNodeType.Attribute, "packagedir", null );
            xnPackageDir.Value   = this.PackageDirectory;
            xn.Attributes.SetNamedItem( xnPackageDir );

            // create description attribute
            XmlNode xnPackageDesc = xdDoc.CreateNode( XmlNodeType.Attribute, "description", null );
            xnPackageDesc.Value   = this.PackageDescription;
            xn.Attributes.SetNamedItem( xnPackageDesc );

            if ( !this.ActionDLL.Equals( this._strActionDLL ) ) {
                XmlNode xnActionDll = xdDoc.CreateNode( XmlNodeType.Attribute, "actiondllpath", null );
                xnActionDll.Value   = this.ActionDLL;
                xn.Attributes.SetNamedItem( xnActionDll );
            }

            // retrieving incompleted Action Objects
            for ( int iActionIdx = 0; iActionIdx < this.Count; iActionIdx++ ) {
                // IAction IActionObject = this[ iActionIdx ];
                ActionElement ae = this[ iActionIdx ];
                if ( !ae.IsComplete ) {
                    XmlNode xnActionNode    = this.GetXmlActionNodeByIndex( iActionIdx );
                    XmlNode xnNewActionNode = xdDoc.CreateNode( XmlNodeType.Element, xnActionNode.Name, null );
                    foreach ( XmlAttribute xa in xnActionNode.Attributes ) {
                        if ( xa.Name.Equals( "generateexception" ) )
                            xa.Value = "false";
                        xnNewActionNode.Attributes.SetNamedItem( xa );
                    }
                    xn.AppendChild( xnNewActionNode );
                }
            }
            return xn;
        }


#region private uitlity methods
        private void SetExitMessage( PACKAGE_OPR_CODE pkgOprCode, params object[]  objParams ) {
            this._enumPackageOprCode = pkgOprCode;
            this._strExitMessage     = String.Format( this._strMessages[ this.ExitCode ], objParams );
        }


        // ProcessHandler will process a register XML event within a package
        // The event can be registered from the attributes OnSuccess and OnFail
        // of package node.
        private void ProcessHandler( XmlNode HandlerNode, int Level ) {
            // if the register handler has any sub-element, process them.
            Level++;
            if ( Level > MAX_NEST_LEVEL ) {
                string Message = string.Format( "{0}: {1} recursive too deep, greater than default {2}, program abort", this.Name, "ProcessHandler", MAX_NEST_LEVEL );
                base.FatalErrorMessage( ".", Message, 1660 );
                Environment.Exit( -1 );
            }

            if ( HandlerNode != null && HandlerNode.HasChildNodes ) {
                XmlNodeList xnl = HandlerNode.ChildNodes;
                object Ctor     = null;

                foreach ( XmlNode xn in xnl ) {
                    // check if any element exists in the ActionObjectTable
                    // if so, retrieve it, create an instance from it, and
                    // execute it.
                    if ( xn.NodeType != XmlNodeType.Comment ) {
                        try {
                            if ( base.ActionObjectTable.ContainsKey( xn.Name ) ) {
                                Ctor = base.ActionObjectTable[ xn.Name ];
                                Ctor = this.InvokeConstructor( xn );
                                (Ctor as ActionElement).Execute();
                            }
                        } catch ( Exception ) {
                            throw;
                        }
                    }
                }
            }
        }


        // Invoke a given Xml Action node and fill out all the
        // required parameters
        private object InvokeConstructor( XmlNode xnAction ) {
            // make name to be lower case
            string strConstructorName = xnAction.Name;
            object objConstructor     = null;

            // search the constructor in the hash table
            // and return an instance back if found.
            if ( base.ActionObjectTable != null )
                if ( this.ActionObjectTable.ContainsKey(strConstructorName) ) {
                    // here is the process to retrive a correspond
                    // ActionNode's constructor. First, get it from
                    // ActionObjectTable; Second, check to see if this
                    // constructor has parameter or not and take apporiate
                    // steps to deal with it, and finally, invoke this particular
                    // constructor
                    object[] objs                     = (object[]) this.ActionObjectTable[ strConstructorName ];
                    object[] objParams                = null;
                    ConstructorInfo ciConstructorInfo = (ConstructorInfo) objs[0];
                    if ( objs[1] != null )
                        objParams = new object[1] { xnAction };
                    else
                        objParams = new object[0];

                    try {
                        objConstructor = ciConstructorInfo.Invoke( objParams );
                    } 
										catch ( System.Reflection.TargetInvocationException tie ) {
                        base.FatalErrorMessage( ".", tie.GetBaseException().Message, 1660 );
                    }
                }

            // once the constructor is invoke, we start looking for
            // all the property values and fill them with values provides
            // by xml.  This is done by using reflection and custom attributes
            objConstructor = this.LookForProperties( objConstructor, xnAction );
            return objConstructor;
        }


        // LookForProperties - looking into a given object's properties and
        //     fill them with apporiate values supplied by XML.  This done
        //     by using the custom attributes and reflection.
        private object LookForProperties( object objConstructor, XmlNode xnActionNode ) {
            // get the type of a given constructor and
            // retrieve its property methods and initialize
            // the attribute class
            Type tThisType                 = objConstructor.GetType();
            PropertyInfo[] piPropertyInfos = tThisType.GetProperties();
            object[] objActionAttributes   = null;

            // loop through each property in a give constructor
            foreach ( PropertyInfo pi in piPropertyInfos ) {
                // retrieve custom attribute associate with this property
                // and if it has it then ...
                // objActionAttributes = pi.GetCustomAttributes(false);
                objActionAttributes = pi.GetCustomAttributes( typeof( ActionAttribute ), false );
                if ( objActionAttributes != null ) {
                    // go through each attribute and retrieve its value
                    for ( int i = 0; i < objActionAttributes.Length; i++) {
                        ActionAttribute aa = (ActionAttribute) objActionAttributes[i];
                        object[] objParams = new object[1];

                        // retrieve a given xml node's attribute and if
                        // an attribute is required but not found,
                        // raise an ArgumentException exception
                        XmlNode xn = xnActionNode.Attributes.GetNamedItem ( aa.Name );
                        if ( xn == null && aa.Needed == true) {
                            base.FatalErrorMessage( ".", String.Format( "attribute {0} is requred", aa.Name ), 1660 );
                        } else if ( xn == null && aa.Needed == false && aa.Default != null )
                            objParams[0] = aa.Default;
                        else if ( xn == null && aa.Needed == false)
                            continue;
                        else {
                            // get the attribute name and its value
                            // if the value is not provided and there's no
                            // default value for it, raise an ArgumentNullException
                            // exception
                            // string objName  = xn.Name;
                            object objValue = xn.Value;
                            if ( (string) objValue == String.Empty && aa.Default == null ) {
                                base.FatalErrorMessage(".", String.Format("{0} does not accept empty value", aa.Name ), 1660);
                            } else
                                objParams[0] = ActionVariables.ScanVariable( (string) objValue );

                        }
                        // now invoke property's set method to have a
                        // given value set.
                        try {
                            pi.GetSetMethod().Invoke( objConstructor, objParams );
                        } catch ( System.Reflection.TargetException te) {
                            base.FatalErrorMessage( ".", te.Message, 1660 );
                        } catch ( ArgumentException ae ) {
                            base.FatalErrorMessage( ".", ae.Message, 1660 );
                        } catch ( System.Reflection.TargetInvocationException tie) {
                            base.FatalErrorMessage( ".", tie.Message, 1660 );
                        } catch ( System.Reflection.TargetParameterCountException tpce ) {
                            base.FatalErrorMessage( ".", tpce.Message, 1660 );
                        } catch ( System.MethodAccessException ma ) {
                            base.FatalErrorMessage( ".", ma.Message, 1660 );
                        }
                    }
                }
            }
            return objConstructor;
        }

#endregion

    }
}
