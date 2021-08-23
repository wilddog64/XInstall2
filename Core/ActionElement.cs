using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;

using XInstall.Util;
using XInstall.Util.Log;

/*
 * Class Name    : ActionElement
 * Inherient     : Logger
 * Functionality : A base class for all the ActionObjects.  This is the soul
 *                 of XInstall framework.  The primary work for it is:
 *
 *                   1. Parse the content of a gvien ActionObject and look
 *                      for corresponding attributes in Xml for object
 *                      properties by using dotnet Attribute and Reflection.
 *
 *                   2. Parse variables and retrieve in an apporiate form.
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when           what
 * ----------------------------------------------------------------------------
 * mliang           12/15/2004     Initial creation
 * mliang           01/30/2005     These methods were added:
 *
 *                                  1. AddVariable - add variable into internal
 *                                     storage, _Variables, which a a
 *                                     StringDictionary type.  The method
 *                                     accept a boolean variable that can tell
 *                                     if a particular variable is allowed
 *                                     to be overwrited or not.  There's an
 *                                     overloaded method that does not allow
 *                                     to have duplicated variable to be added
 *                                     (an VariableExistedException will be
 *                                     thrown if such case is happened).
 *
 *                                  2. ScanVariable - scanning an input string
 *                                     to find out if any reference to the
 *                                     variable in a form of ${var_name}.
 *
 * mliang          02/07/2005       Tidy up the error code handling. Use
 *                                  SetExitMessage method to set the error messages
 *                                  and use Message property to restrive them. Also
 *
 * mliang          03/15/2005       Add a boolean variable of confirm that, when set
 *                                  to true, will prompt to as confirm for executing
 *                                  a given action.
 *
 * mliang          03/23/2005       In ScanVariablesInNodes method, add an if statement
 *                                  to skip the XML comment (<!-- -->) so that element
 *                                  inside it won't be parsed.
 *
 * mliang          08/04/2005       Property ActionNode was added.  This is a read only
 *                                  property that returns the passed in XmlNode.
 */
namespace XInstall.Core {

    /// <summary>
    /// The base class that provides the ability for Action object
    /// to have a child nodes within it
    /// </summary>

    public class ActionElement : Logger {
        // private variables
        // private ArrayList        _alElementList         = new ArrayList();
        private DateTime         _ElementStopTime       = DateTime.Now;
        // private Hashtable        _htMethodLookupTable   = new Hashtable();
        private Hashtable        _ActionObjects         = null;
        private object[]         _ActionObjectInstances = null;
        private Regex            _VariableValidator     = new Regex( @"[a-zA-Z,0..9]+" );
        private Regex            _ValueExtractor        = new Regex( @"\${(\w+)}" );
        private StringDictionary _Variables             = new StringDictionary();
        private XmlNode          _xnActionNode          = null;
        private object           _ThisInstance          = null;

        private bool _bIsCompleted    = false;  // status of object, if it is completed or not
        private bool _bAllowException = false;  // flag to indicate if object wants to generate an exception
        private bool _bSkipError      = false;  // flag to indicate if object wants to skip an error generated
        private bool _bRunnable       = true;   // flag to indicate if object should be executed or not
        private bool _Confirm         = false;  // if true, request confirmation for executing the action
        private bool _bStopIt         = false;


        private string _LogFileName   = string.Empty;
        private int    _ExecuteCount  = 0;      // track how many times the object has been executed


        // messages lookup enumeration
        private enum ACTIONELEM_OPR_CODE {
            ACTIONELEM_OPR_SUCCESS = 0,
            ACTIONELEM_OPR_ELEMENT_NOTFOUND,
            ACTIONELEM_OPR_ACTION_HASNO_ELEMENTS,
            ACTIONELEM_OPR_ACTION_ATTRIBUTE_NOTFOUND,
            ACTIONELEM_OPR_EXCEPTION_REQUEST,
        }


        // error messages
        private ACTIONELEM_OPR_CODE _enumActionElmOprCode =
            ACTIONELEM_OPR_CODE.ACTIONELEM_OPR_SUCCESS;

        private string   _strExitMessage       = null;
        private readonly string[] _strMessages = {
                @"{0}: element {1} found",
                @"{0}: element {1} is not defined by action {2}",
                @"{0}: action  {1} contains no elements!",
                @"{0}: required attribute {1} for action {2} is not found",
                @"{0}: object {1} request generate an exception!",
            };

#region Event Handler for ProcessCompletedHandler
        public static event ProcessCompletedHandler ProcessComplete;
        protected virtual void OnProcessComplete( ProcessCompletedEventArgs e ) {
            if ( ProcessComplete != null )
                ProcessComplete( this, e );
        }
#endregion

#region Event Handler for ProcessIdCreatedHandler

#endregion

#region constructors
        /// <summary>
        /// constructor that initialize the ActionElement object.
        /// </summary>
        /// <param name="xnActionNode">an XML node that contains action and its elements</param>
        /// <param name="objConstructor">the action object constructor</param>
        /// <remarks>
        ///     The ActionElement takes xnActionNode and objConstructor.  The xnActionNode is
        ///     an XML node who has one or serveral elements.
        /// </remarks>
        public ActionElement( XmlNode ActionNode ) : base() {
            base.OutToConsole  = true;
            base.OutToFile     = true;
            this._xnActionNode = ActionNode;

            XmlNodeList VariableNodes = this._xnActionNode.SelectNodes( @"defvar" );
            this.ParseVariable( VariableNodes );
            this._ThisInstance = (this as ActionElement);

        }


        public ActionElement( XmlNode ActionNode, Hashtable ActionObjects ) : base() {
            base.OutToConsole   = true;
            base.OutToFile      = true;
            this._xnActionNode  = ActionNode;
            this._ActionObjects = ActionObjects;
            this.LoadActionObjects();

            XmlNodeList VariableNodes = this._xnActionNode.SelectNodes( @"defvar" );
            this.ParseVariable( VariableNodes );
            this._ThisInstance = (this as ActionElement);
        }


        public ActionElement() : base() {
            base.OutToConsole = true;
            base.OutToFile    = true;
            this._ThisInstance = (this as ActionElement);
        }


        public ActionElement( ISendLogMessage SendLogMessageIF ) :
                base( SendLogMessageIF ) {
            base.OutToConsole = true;
            base.OutToFile    = true;
            this._ThisInstance = (this as ActionElement);
        }


#endregion

#region protected properties
        /// <summary>
        /// get/set an output file for logging purpose.
        /// </summary>
        protected string OutputFile {
            set { base.FileName = value; }
        }


        /// <summary>
        /// get/set a flag to indicate a given object
        /// has completed or not
        /// </summary>
        public virtual bool IsComplete {
            get { return this._bIsCompleted; }
            set { this._bIsCompleted = value; }
        }


        /// <summary>
        /// get/set a flag to indicate if a gvien object
        /// is allowed to generated an exception
        /// </summary>
        [Action("allowgenerateexception", Needed=false, Default="false")]
        protected bool AllowGenerateException {
            get { return this._bAllowException; }
            set { this._bAllowException = value; }
        }


        /// <summary>
        /// get/set an object to indicate if
        /// an object is to be executed or not
        /// </summary>
        [Action("runnable", Needed=false, Default="false")]
        protected bool Runnable {
            get { return this._bRunnable; }
            set { this._bRunnable = value; }
        }


        /// <summary>
        /// get/set a flag to tell if an object
        /// is going to ignore an error or not
        /// </summary>
        [Action("skiperror", Needed=false, Default="false")]
        protected bool SkipError {
            get { return this._bSkipError; }
            set { this._bSkipError = value; }
        }


        /// <summary>
        /// gets an operation code from ActionElement object
        /// </summary>
        protected int ExitCode {
            get { return (int) this._enumActionElmOprCode; }
        }


        /// <summary>
        /// gets a message corresponding to the operation code
        /// </summary>
        protected string ExitMessage {
            get { return this._strExitMessage; }
        }


        /// <summary>
        /// gets a name of ActionElement
        /// </summary>
        public new string Name {
            get { return this.GetType().Name; }
        }


        protected bool Confirm {
            get { return this._Confirm; }
            set { this._Confirm = value; }
        }


        public virtual bool Stop {
            get { return this._bStopIt; }
            set { this._bStopIt = value; }
        }


        public virtual int[] ElementIds {
            get { return null; }
        }

        /// <summary>
        /// override this method to return the inherited object instance
        /// </summary>
        protected virtual object ObjectInstance {
            get { return null; }
        }


        /// <summary>
        /// return the ActionObjects containers or
        /// override it to set a different object
        /// container.
        /// </summary>
        protected virtual Hashtable ActionObjects {
            get { return this._ActionObjects; }
            set {}
        }


        protected virtual string ObjectName {
            get { return ""; }
        }


        protected XmlNode ActionNode {
            get { return this._xnActionNode; }
        }


        public virtual int ExecuteCount {
            get { return this._ExecuteCount; }
            set { this._ExecuteCount = value; }
        }

#endregion

#region public methods
        /// <summary>
        /// execute each element inside an action object.
        /// </summary>
        public virtual void Execute() {
            if ( this.AllowGenerateException ) {
                this.SetExitMessage( ACTIONELEM_OPR_CODE.ACTIONELEM_OPR_EXCEPTION_REQUEST, this.Name, this.ObjectName );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660, false );
            }

            try {
                if ( this.Runnable ) {
                    this.ParseActionElement();
                    if ( this.ActionObjects != null ) {
                        if ( this._ActionObjectInstances != null && this._ActionObjectInstances.Length > 0 ) {
                            foreach ( object ActionObjectInstance in this._ActionObjectInstances ) {
                                if ( this.Confirm ) {
                                    Console.WriteLine( "Confirm to execute (y/n):" );
                                    string YN = Console.ReadLine();
                                    if ( YN.ToLower() != "y" ) {
                                        base.LogItWithTimeStamp( String.Format( "{0}: user confirm not to execute, skip this action", this.Name ) );
                                        continue;
                                    }
                                }
                                ( ActionObjectInstance as ActionElement ).Execute();
                            }
                        }
                    }
                }
                else {
                    this.SetExitMessage( @"{0}: attribute runnable is set to false, stop running!", this.ObjectName);
                    base.LogItWithTimeStamp( this.ExitMessage );
                }
            } 
            catch ( System.Reflection.TargetException te ) {
                if ( this.ExitMessage.Length == 0 )
                    this.SetExitMessage( "{0}: {1} - {2}", this.ObjectName, te.Message );

                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                }
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );
            } 
            catch ( ArgumentException ae ) {
                // if ( this.ExitMessage.Length == 0 )
                this.SetExitMessage( "{0}: {1} - exception happened: {0} ", this.ObjectName, ae.ParamName, ae.Message  );

                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                }
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );
            } 
            catch ( System.Reflection.TargetInvocationException tie ) {
                if ( this.ExitMessage.Length == 0 )
                    this.SetExitMessage( "{0}: {1} - exeption happened: {2|", this.ObjectName, "Execute()", tie.InnerException.Message );

                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                }
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );

            } 
            catch ( System.ComponentModel.Win32Exception w32e ) {
                if ( this.ExitMessage.Length == 0 ) {
                    this.SetExitMessage( "{0}: {1} - Win32 error: {2}",
                                         this.ObjectName, w32e.ErrorCode, w32e.Message );
                }

                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                } 
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );
            } 
            catch ( System.Runtime.InteropServices.COMException ee ) {
                if ( this.ExitMessage != null && this.ExitMessage.Length == 0 ) {
                    this.SetExitMessage( "{0}: {1} COM component error: {2}", this.ObjectName, ee.ErrorCode, ee.Message );
                }
                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                }
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );
            } 
            catch ( System.Xml.XmlException xe ) {
                this.SetExitMessage( "{0}: XML error: line number {1}, postion {2}, message {3}", this.ObjectName, xe.LineNumber, xe.LinePosition, xe.Message );

                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                }
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );
            } 
            catch ( System.Security.SecurityException se ) {
                this.SetExitMessage( "{0} - insufficient permission {1}", this.ObjectName, se.Message );

                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    throw;
                }
                else
                    base.LogItWithTimeStamp( LEVEL.ERROR, this.ExitMessage );
            } 
            catch ( Exception e ) {
                if ( this.ExitMessage != null && this.ExitMessage.Length == 0 ) {
                    this.SetExitMessage( "{0}: exception happened, message - {1}", this.ObjectName, e.Message);
                }
                if ( !this.SkipError ) {
                    base.FatalErrorMessage( ".", e.Message, 1660 );
                    throw;
                }
            }
        }


#endregion

        public string LogFileName {
            set { _LogFileName = value; }
            get { return _LogFileName; }
        }

        public virtual int ProgramReturnCode {
            get { return 0; }
        }

        public virtual int ElementID {
            get { return 0; }
        }

        public virtual DateTime ElementStartTime {
            get { return DateTime.Now; }
        }

        public virtual DateTime ElementStopTime {
            get { return this._ElementStopTime; }
            set { this._ElementStopTime = value; }
        }


#region protected methods

        /// <summary>
        /// protected void ParseActionElement() -
        ///   this virutal method provides the ability
        ///   to parse out a given node's content and
        ///   search and replace any vaild variable.  It
        ///   also allow inheried class to override it
        ///   to have their own behair.  Be sure to call
        ///   this methods when inherited; otherwise, your
        ///   object may not execute correctly.
        /// </summary>
        protected virtual void ParseActionElement() {
            // this.SetSelfPropertyValues();
            // obtain the derived class's instance
            object Instance = this.ObjectInstance;
            if ( Instance != null )
                this.SetPropertyValues( Instance );

            if ( this._xnActionNode != null )
                this.ScanVariablesInNodes( this._xnActionNode );
        }


        protected void AddVariable( string Name, string Value, bool Overwrite ) {
            if ( this._VariableValidator.IsMatch( Name ) ) {
                if ( !_Variables.ContainsKey( Name ) )
                    this._Variables.Add( Name, Value );
                else if ( !Overwrite && _Variables.ContainsKey( Name ) )
                    throw new VariableExistedException( Name, "variable already existed!" );
                else
                    this._Variables[ Name ] = Value;
            } 
            else
                throw new InvalidVariableNameException( Name, "variable name is not valid!" );
        }


        protected void AddVariable( string Name, string Value ) {
            this.AddVariable( Name, Value, false );
        }


        protected string ScanVariable( string InputString ) {
            if ( InputString == null || InputString.Length == 0 )
                return InputString;

            if ( this._ValueExtractor.IsMatch( InputString ) ) {
                Match m = this._ValueExtractor.Match( InputString );
                while ( m.Success ) {
                    string VariableName = m.Groups[1].Value;
                    // int Pos = m.Index;
                    if ( ActionVariables.IsVariableExist( VariableName ) ) {
                        string Value         = ActionVariables.DirectGetValue( VariableName );
                        string ReplaceString = "${" + VariableName + "}";
                        InputString          = InputString.Replace( ReplaceString, Value );
                        m                    = this._ValueExtractor.Match( InputString );
                    } 
                    else
                        throw new VariableNotDefinedException( VariableName, String.Format("variable {0} is not defined", VariableName) );

                }
            }
            return InputString;
        }


        /// <summary>
        /// protected virtual void SetExitMessage() -
        ///   sets the error messages
        /// </summary>
        /// <param name="Formatter">format string</param>
        /// <param name="Params">parameter array</param>
        protected virtual void SetExitMessage( string Formatter, params object[] Params ) {
            this._strExitMessage = String.Format( Formatter, Params );
        }


        /// <summary>
        /// protected object CreateObject() - creates an
        /// object instance by using ObjectName and xn (XmlNode)
        /// </summary>
        /// <param name="ObjectName">a name of object, string type</param>
        /// <param name="xn">XmlNode type</param>
        /// <returns></returns>
        protected object CreateObject( string ObjectName, XmlNode xn ) {

            ConstructorInfo Ctor = null;
            object ObjInstance   = null;
            if ( this.ActionObjects.ContainsKey( ObjectName ) ) {
                object[] objs = (object[]) this.ActionObjects[ ObjectName ];
                Ctor = (ConstructorInfo) objs[0];

                object[] Params = null;
                if ( objs.Length == 1 )
                    Params = new object[0];
                else if ( objs.Length == 2 )
                    Params = new object[]{ this._xnActionNode };
                else if ( objs.Length == 3 )
                    Params = new object[]{ this._xnActionNode, this.ActionObjects };

                ObjInstance = Ctor.Invoke( Params );
                this.InitialConstructorProperties( ObjInstance, xn );
            }

            return ObjInstance;
        }


        /// <summary>
        /// an overloaded version of CreateObject
        /// </summary>
        /// <param name="ObjectName">name of the object</param>
        /// <param name="xn">an XmlNode object</param>
        /// <param name="ActionObjects">table of loaded objects</param>
        /// <returns></returns>
        protected object CreateObject( string    ObjectName, XmlNode   xn, Hashtable ActionObjects ) {
            ConstructorInfo Ctor = null;
            object ObjInstance   = null;
            if ( ActionObjects.ContainsKey( ObjectName ) ) {
                object[] objs = (object[]) ActionObjects[ ObjectName ];
                Ctor = (ConstructorInfo) objs[0];
                // ParameterInfo[] ParamInfos = Ctor.GetParameters();

                object[] Params = null;
                if ( objs.Length == 1 )
                    Params = new object[]{ xn };
                else if ( objs.Length == 2 )
                    Params = new object[]{ xn };
                else if ( objs.Length == 3 )
                    Params = new object[]{ xn, ActionObjects };

                ObjInstance = Ctor.Invoke( Params );
                this.InitialConstructorProperties( ObjInstance, xn );
            }

            return ObjInstance;
        }


#endregion

#region private utility methods
        protected void InitialConstructorProperties( object ObjInstance, XmlNode xn ) {
            Type ThisType = ObjInstance.GetType();
            PropertyInfo[] PropertyInfos = ThisType.GetProperties();
            foreach ( PropertyInfo pi in PropertyInfos ) {
                object[] ActionAttributes = pi.GetCustomAttributes( typeof( ActionAttribute ), false );
                for ( int i = 0; i < ActionAttributes.Length; i++ ) {
                    ActionAttribute aa = (ActionAttribute) ActionAttributes[i];
                    XmlNode AttributeNode = xn.Attributes.GetNamedItem( aa.Name );
                    object[] Params = new object[1];
                    if ( AttributeNode == null && aa.Needed ) {
                        this.SetExitMessage( "{0}: {1} is a required attribute", this.ObjectName, aa.Name );
                        base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                    }
                    else if ( AttributeNode == null && !aa.Needed && aa.Default != null )
                        Params[0] = aa.Default;
                    else if ( xn == null && !aa.Needed )
                        continue;
                    else if (AttributeNode != null) {
                        string Value = AttributeNode.Value;
                        if ( Value.Length == 0 && aa.Default == null ) {
                            this.SetExitMessage( "{0}: {1} cannot be empty!", this.ObjectName, aa.Name );
                            base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
                        }
                        else if ( Value.Length == 0 && aa.Default != null )
                            Params[0] = aa.Default;
                        else
                            Params[0] = ActionVariables.ScanVariable(Value);
                    }
                    pi.GetSetMethod().Invoke( ObjInstance, Params );
                }
            }
        }


        private void LoadActionObjects() {
            if ( this._ActionObjects != null &&
                    this._xnActionNode.HasChildNodes ) {
                XmlNodeList ChildNodes = this._xnActionNode.ChildNodes;
                this._ActionObjectInstances = new object[ ChildNodes.Count ];
                int NodeCount = 0;
                foreach ( XmlNode ChildNode in ChildNodes ) {
                    object ObjInstance = this.CreateObject( ChildNode.Name, ChildNode );
                    this._ActionObjectInstances[ NodeCount++ ] = ObjInstance;
                }
            }
        }


        private void SetExitMessage( ACTIONELEM_OPR_CODE ActionElemOprCode, params object[] objParams) {
            this._enumActionElmOprCode = ActionElemOprCode;
            this._strExitMessage       = String.Format( this._strMessages[ this.ExitCode ], objParams );
        }


        // if not variable is defined, then this will create an endless loop ...
        // need to fix this bug
        private void ScanVariablesInNodes( XmlNode xn ) {
            XmlNodeList XmlNodes = null;

            if ( xn.HasChildNodes ) {
                XmlNodes = xn.ChildNodes;
                foreach ( XmlNode Node in XmlNodes ) {
                    if ( Node.NodeType == XmlNodeType.Comment )
                        continue;
                    if ( Node.HasChildNodes ) {
                        this.ScanVariablesInNodes( Node );
                    } 
                    else if ( Node.NodeType == XmlNodeType.Text ) {
                        Node.Value = this.ScanVariable( Node.Value );
                    } 
                    else if ( Node.Attributes != null ) {
                        XmlAttributeCollection xac = Node.Attributes;
                        foreach ( XmlAttribute xa in xac ) {
                            string AttribValue = ActionVariables.ScanVariable( xa.Value );
                            XmlNode AttribNode = xac.GetNamedItem( xa.Name );
                            AttribNode.Value   = AttribValue;
                        }
                    } 
                    else {
                        if ( Node.Value.Length != 0 )
                            Node.Value = ActionVariables.ScanVariable( Node.Value );
                        else if ( Node.InnerText.Length != 0 )
                            Node.InnerText = ActionVariables.ScanVariable( Node.InnerText );
                    }
                }
            } 
            else {
                XmlAttributeCollection xac = xn.Attributes;
                foreach ( XmlAttribute xa in xac ) {
                    string AttribValue = ActionVariables.ScanVariable( xa.Value );
                    XmlNode AttribNode = xac.GetNamedItem( xa.Name );
                    AttribNode.Value = AttribValue;
                }
            }
        }

        private void ParseVariable( XmlNodeList VariableNodes ) {
            if ( VariableNodes != null && VariableNodes.Count > 0 )
                foreach ( XmlNode VariableNode in VariableNodes ) {
                // ActionVariables.Add( VariableNode.Name, VariableNode.Value, true );
                XmlNode VariableNameNode  = VariableNode.Attributes.GetNamedItem( "name" );
                XmlNode VariableValueNode = VariableNode.Attributes.GetNamedItem( "value" );

                if ( VariableNameNode != null ) {
                    string VariableName = VariableNameNode.Value;
                    if ( !ActionVariables.IsVariableExist( VariableName ) ) {
                        string VariableValue = VariableValueNode != null ?  VariableValueNode.Value : "";
                        ActionVariables.Add( VariableName, VariableValue );
                    }

                }
            }
        }


        private void SetPropertyValues( object Instance ) {
            Type ThisType = Instance.GetType();
            PropertyInfo[] Properties = ThisType.GetProperties();

            foreach( PropertyInfo Property in Properties ) {
                object[] Attributes = Property.GetCustomAttributes( typeof( ActionAttribute ), false );
                if ( Attributes != null && Attributes.Length > 0 ) {
                    MethodInfo GetMethod = Property.GetGetMethod();
                    MethodInfo SetMethod = Property.GetSetMethod();

                    if ( GetMethod == null || SetMethod == null )
                        continue;

                    object ReturnValue = GetMethod.Invoke( Instance, new object[]{} );
                    object Value       = null;
                    if ( ReturnValue != null && ReturnValue.ToString().Length == 0 ) {
                        ActionAttribute aa = ( Attributes[0] as ActionAttribute );
                        if ( aa != null )
                            ReturnValue = ( Attributes[0] as ActionAttribute ).Default;
                    }

                    Value = ActionVariables.ScanVariable( (string) ReturnValue );
                    object[] Params = new object[1]{ Value };
                    SetMethod.Invoke( Instance, Params );
                }
            }
        }


        // private void SetSelfPropertyValues() {
        //     XmlAttributeCollection xac = this._xnActionNode.Attributes;

        //     XmlNode RunnableNode  = xac.GetNamedItem( "runnable" );
        //     XmlNode SkipErrorNode = xac.GetNamedItem( "skiperror" );
        //     XmlNode AllowGenerateExceptionNode = xac.GetNamedItem( "allowgenerateexception" );

        //     ActionElement ae = (this._ThisInstance as ActionElement);

        //     ae.Runnable  = RunnableNode.Value == null   ? true : false;
        //     ae.SkipError = SkipErrorNode.Value == null ? true : false ;
        //     ae.AllowGenerateException = AllowGenerateExceptionNode.Value == null ? true : false;
        // }
#endregion
    }
}
