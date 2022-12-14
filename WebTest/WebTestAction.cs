using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

using Microsoft.WebRunner;
using Microsoft.WebRunner.Loggers;

using XInstall.Core;
using XInstall.Util.Log;

namespace XInstall.Custom.Actions {
    /// <summary>
    /// WebTest is class that wrap WebRunner SDK into it to
    /// provide an automation of webpage testing.
    /// </summary>
    public class WebTest : ActionElement {
        // private object variables
        private WebRunnerSDK _wrsWebRunner          = null;
        private Log          _lWrsLogger            = null;
        private Hashtable    _htWebRunnerTokens     = new Hashtable();
        private Hashtable    _htWebRunnerCtrlFlws   = new Hashtable();
        private Hashtable    _htActionScopeVarTable = new Hashtable();
        private XmlNode      _xnActionElement       = null;
        private Regex        _re                    = new Regex( @";" );
        private Regex        _reVarName             = new Regex( @"[A-Z][A-Z,0-9]+",
                RegexOptions.IgnoreCase );
        private ArrayList    _alRegExList           = new ArrayList();
        private ArrayList    _alLabels              = new ArrayList();

        // private variables
        private string _strLogType     = null;
        private string _strLogFileName = null;
        private string _strTestName    = null;
        private string _strTestOwner   = null;
        private string _strDescription = null;
        private string _strStartURL    = null;
        private int    _iVarNameLen    = 32;

        private bool   _bLogging       = true;

        private enum WEBRUNNER_OPR_CODE
        {
            WEBRUNNER_OPR_SUCCESS = 0,
            WEBRUNNER_OPR_BOOLEAN_PARSE_ERROR,
            WEBRUNNER_OPR_VERB_UNKNOWN,
            WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
            WEBRUNNER_OPR_METHOD_NOT_DEFINED,
            WEBRUNNER_OPR_CONSTRUCTOR_EXCEPTION_THROWN,
            WEBRUNNER_OPR_UNKNOWN_EXCEPTION,
            WEBRUNNER_OPR_INTEGER_EXPECT,
            WEBRUNNER_OPR_BYTAG_REQUIRED,
            WEBRUNNER_OPR_WIN32EXCEPTION,
            WEBRUNNER_OPR_VARIABLE_ALREADY_DEFINED,
            WEBRUNNER_OPR_TOKEN_NOT_KNOWN,
            WEBRUNNER_OPR_METHOD_PARAMETERS_TYPE_ERROR,
            WEBRUNNER_OPR_METHOD_PARAMETERS_NULLREF,
            WEBRUNNER_OPR_METHOD_GENERATES_EXCEPTIONS,
            WEBRUNNER_OPR_METHOD_PARAMETERS_COUNT_INCORRECT,
            WEBRUNNER_OPR_METHOD_NOACCESS_RIGHT,
            WEBRUNNER_OPR_VARIABLE_NOT_DEFINED,
            WEBRUNNER_OPR_INCORRECT_DEREFERENCE_VARIABLE,
            WEBRUNNER_OPR_BADVARIABLE_NAME,
            WEBRUNNER_OPR_LABEL_NOT_DEFINED,
            WEBRUNNER_OPR_INVALID_REGEX_PATTERN,
        }

        private WEBRUNNER_OPR_CODE _enumWRSOprCode =
            WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_SUCCESS;

        private string _strExitMessage     = null;
        private string[] _strMessages      =
            {
                @"{0}: operation success!",
                @"{0}: boolean parsing error!",
                @"{0}: specified verb is not known by webrunner",
                @"{0}: {1}: attribute {2} is missing",
                @"{0}: method {1} is not defined!",
                @"{0}: excption thrown by constructor. Message - {1}",
                @"{0}: generic exception happened, message - {1}",
                @"{0}: method {1}: attribute {2} expect integer for input!",
                @"{0}: method {1}: attribute {2} value does not have bytag within it",
                @"{0}: Win32 Exception happened, message: {1}",
                @"{0}: {1} - variable {2} is already defined!",
                @"{0}: specified token {1} is not known!",
                @"{0}: parameters type for method {1} is not matched, message - {2}",
                @"{0}: method {1} required parameters and you don't provide them, message - {2}!",
                @"{0}: calling method {1} generate an exception, message - {2}",
                @"{0}: number of parameters pass into method {1} is not matched, message - {2}",
                @"{0}: caller {1} does not have permission to execute this method {2}, message - {3}",
                @"{0}: variable {1} is not defined",
                @"{0}: variable {1} has to be surround with :",
                @"{0}: variable name {0} is not valid, it has to start with alphabet and not longer than 32 characters",
                @"{0}: {1} an undefined label {2} ",
                @"{0}: {1} - input regular expression {2} is not valid",
            };

        /// <summary>
        /// constructor that initiates a WebTest object
        /// </summary>
        /// <param name="xnActionNode">an XML Action Node</param>
        /// <remarks>
        ///     WebTest takes one parameter, xnActionNode ( a node
        ///     that contains one or more action objects, and prepares
        ///     them to be executed.  It also initializes the WebRunnerSDK
        ///     for webpage testing.
        /// </remarks>
        [Action("webrunner")]
        public WebTest( XmlNode xnActionNode ) : base() {
            try {
                this._xnActionElement = xnActionNode;
                this._wrsWebRunner    = new WebRunnerSDK();
                this.SetupWebRunnerTokens();
            } catch ( System.ComponentModel.Win32Exception w32e ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_WIN32EXCEPTION,
                    this.Name, w32e.Message );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            } catch ( Exception e ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_CONSTRUCTOR_EXCEPTION_THROWN,
                    this.Name, e.Message );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode);
            } catch { }
        }


    /// <summary>
    /// gets the type of WebLog to be written to file.
    /// It only accepts one, XML, which will make WebLog
    /// to be written in XML format.
    /// </summary>
    /// <remarks></remarks>
    [Action("weblogtype", Needed=false)]
        public string WebLogFileType
        {
            get {
                return this._strLogType;
            }
            set {
                this._strLogType = value;
            }
        }

        /// <summary>
        /// gets the file name for the WebLog
        /// </summary>
        [Action("weblogfilename", Needed=false)]
        public string WebLogFileName
        {
            get {
                return this._strLogFileName;
            }
            set {
                if ( value.ToLower().Equals("auto") )
                    value = this.Name;
                this._strLogFileName =
                    String.Format( "/logname:{0}", value );
            }
        }

        /// <summary>
        /// sets a flag to indicates if WebTest object should generate an
        /// exception
        /// </summary>
        /// <remarks></remarks>
        [Action("allowgenerateexception", Needed=false, Default="false")]
        public new string AllowGenerateException
        {
            set {
                try {
                    base.AllowGenerateException = bool.Parse( value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BOOLEAN_PARSE_ERROR,
                        this.Name );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                }
            }
        }

        /// <summary>
        /// set a flag to tell whether WebTest should log every message
        /// </summary>
        [Action("startlogging", Needed=false, Default="true")]
        public string Logging
        {
            set {
                try {
                    this._bLogging = bool.Parse( value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BOOLEAN_PARSE_ERROR,
                        this.Name );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                }
            }
        }

        /// <summary>
        /// get/set the test name for WebTest
        /// </summary>
        [Action("testname", Needed=true)]
        public string TestName
        {
            get {
                return this._strTestName;
            }
            set {
                this._strTestName = value;
            }
        }

        /// <summary>
        /// get/set the owner of test
        /// </summary>
        /// <remarks>
        ///     By default, this property will return the current
        ///     domain user name whose running the script.
        /// </remarks>
        [Action("testowner", Needed=false, Default="auto")]
        public string TestOwner
        {
            get {
                if ( this._strTestOwner.Equals("auto") )
                    this._strTestOwner = Environment.UserDomainName;

                return this._strTestOwner;
            }
            set {
                this._strTestOwner = value;
            }
        }

        /// <summary>
        /// get/set the description of the Action Element Object
        /// </summary>
        /// <remarks></remarks>
        [Action("description", Needed=false)]
        public string Description
        {
            get {
                return this._strDescription;
            }
            set {
                this._strDescription = value;
            }
        }

        /// <summary>
        /// get/set the url for the WebTest to test against
        /// </summary>
        [Action("starturl", Needed=true)]
        public string StartURL
        {
            get {
                return this._strStartURL;
            }
            set {
                this._strStartURL = value;
            }
        }

#region WebRunner Methods start here
        /// <summary>
        /// start IE and navigate to the desired URL
        /// </summary>
        public void StartIE() {
            string strURL = this.StartURL;
            if ( strURL.IndexOf( "url=" ) < 0 )
                strURL   = String.Format( "url={0}", strURL );
            try {
                bool bResult =
                    this._wrsWebRunner.StartApplication( AppObj.IE, strURL );
                string strLogMessage = String.Format( "IE start with {0} - {1}",
                                                      strURL, bResult ? @"PASS" : @"FAIL" );
                this._lWrsLogger.Trace( strLogMessage );
            } catch ( Exception e ) {
                base.FatalErrorMessage( ".", e.Message, 1660, this.ExitCode );
            }
        }

        /// <summary>
        /// clicks various HTML object by providing different object type
        /// and the QueryString
        /// </summary>
        /// <param name="strTestCaseName">a string type of test case name
        /// </param>
        /// <param name="strObjectType">a string type of Object Type.</param>
        /// <param name="strQueryString">a string type of Query String</param>
        /// <remarks></remarks>
        public void Click( string  strTestCaseName,
                           string  strObjectType,
                           XmlNode xnNode ) {

            string strQueryString =
                this.GetArgumentList( strObjectType, xnNode.Attributes );

            HTMLUIObj htmlUIObj = 0;
            switch ( strObjectType.ToLower() ) {
                case "button":
                    htmlUIObj = HTMLUIObj.Button;
                    break;
                case "bytag":
                    htmlUIObj = HTMLUIObj.ByTag;
                    break;
                case "checkbox":
                    htmlUIObj = HTMLUIObj.CheckBox;
                    break;
                case "link":
                    htmlUIObj = HTMLUIObj.Link;
                    break;
                case "radiobutton":
                    htmlUIObj = HTMLUIObj.RadioButton;
                    break;
                case "image":
                    htmlUIObj = HTMLUIObj.Image;
                    break;
                case "select":
                    htmlUIObj = HTMLUIObj.Select;
                    break;
                default:
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VERB_UNKNOWN,
                        this.Name);
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                    break;
            }

            bool bResult =
                this._wrsWebRunner.Click( htmlUIObj, strQueryString );
            this._lWrsLogger.Trace("method {0} - value {1}",
                                   strObjectType, strQueryString );
            string strLogMessage =
                String.Format( @"{0}: {1} - {2}",
                               xnNode.Name, strQueryString,
                               bResult ? @"PASS" : @"FAIL" );
            base.LogItWithTimeStamp( strLogMessage );

            if ( bResult )
                this._lWrsLogger.Trace( "{0} - PASS", strQueryString );
            else
                this._lWrsLogger.Error( "{0} - FAIL", strQueryString );

        }

        /// <summary>
        /// input text into an HTML object such as textbox or text area
        /// </summary>
        /// <param name="TestCaseName">a string type of Test case name</param>
        /// <param name="strObjectType">a string type of object type </param>
        /// <param name="strQueryString">a string type of QueryString</param>
        /// <remarks></remarks>
        public void InputText( string TestCaseName,
                               string strObjectType,
                               XmlNode xnNode ) {
            string strQueryString =
                this.GetArgumentList( strObjectType, xnNode.Attributes );
            HTMLUIObj htmlUIObj = 0;
            switch ( strObjectType.ToLower() ) {
                case "textbox":
                    htmlUIObj = HTMLUIObj.TextBox;
                    break;
                case "textarea":
                    htmlUIObj = HTMLUIObj.TextArea;
                    break;
                case "textbytag":
                    htmlUIObj = HTMLUIObj.ByTag;
                    break;
                default:
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VERB_UNKNOWN,
                        this.Name);
                    base.FatalErrorMessage(
                        ".", this.ExitMessage,
                        1660, this.ExitCode );
                    break;
            }

            this._lWrsLogger.Trace( "method {0} - value {1}",
                                    strObjectType, strQueryString );
            bool bResult =
                this._wrsWebRunner.InputText( htmlUIObj, strQueryString );
            string strLogMessage = String.Format( @"{0}: {1} - {2}",
                                                  xnNode.Name, strQueryString, bResult ? @"PASS" : @"FAIL" );
            base.LogItWithTimeStamp( strLogMessage );
            if ( bResult )
                this._lWrsLogger.Trace( "{0} - PASS", strQueryString );
            else
                this._lWrsLogger.Trace( "{0} - FAIL", strQueryString );
        }

        public void GetObject( string strTestCaseName,
                               string strObjectType,
                               XmlNode xnNode ) {

            string strQueryString =
                this.GetArgumentList( strObjectType, xnNode.Attributes );
            HTMLUIObj htmlUIObj = 0;
            switch ( strObjectType.ToLower() ) {
                case "buttonobject":
                    htmlUIObj = HTMLUIObj.Button;
                    break;
                case "bytag":
                    if ( strQueryString.IndexOf("tag=") < 0 ) {
                        this.SetExitMessage(
                            WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BYTAG_REQUIRED,
                            this.Name, strObjectType, "tag" );
                        base.FatalErrorMessage( ".", this.ExitMessage,
                                                1660, this.ExitCode );
                    }
                    htmlUIObj = HTMLUIObj.ByTag;
                    break;
                case "impageobject":
                    htmlUIObj = HTMLUIObj.Image;
                    break;
                case "radiobuttonobject":
                    htmlUIObj = HTMLUIObj.RadioButton;
                    break;
                case "checkboxobject":
                    htmlUIObj = HTMLUIObj.CheckBox;
                    break;
                case "textboxobject":
                    htmlUIObj = HTMLUIObj.TextBox;
                    break;
                case "textareaobject":
                    htmlUIObj = HTMLUIObj.TextArea;
                    break;
                case "formobject":
                    htmlUIObj = HTMLUIObj.Form;
                    break;
                default:
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VERB_UNKNOWN,
                        this.Name);
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                    break;
            }

            try {
                string strResult =
                    this._wrsWebRunner.GetObject( htmlUIObj, strQueryString );
                string strLogMsg =
                    String.Format( "{0}: {1} - {2}",
                                   xnNode.Name, strQueryString,
                                   strResult == null ? @"FAIL" : @"PASS" );
                if ( strResult != null )
                    this._lWrsLogger.Trace( "{0} - PASS", strQueryString );
                else
                    this._lWrsLogger.Error( "{0} - FAIL", strQueryString );
            } catch ( Exception e ) {
                this._lWrsLogger.Error( "{0} - FAIL", strQueryString );
                base.FatalErrorMessage( ".", e.Message, 1660, -3 );
            }
        }

        public void Verify ( string strTestCaseName,
                             string strObjectType,
                             XmlNode xnNode ) {
            XmlNode xnExpectedResult =
                xnNode.Attributes.GetNamedItem( @"expectedresult" );
            bool bExpectedResult     = false;
            if ( xnExpectedResult != null ) {
                try {
                    bExpectedResult = bool.Parse( xnExpectedResult.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BOOLEAN_PARSE_ERROR,
                        this.Name );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                }
            }

            string strQueryString = this.GetArgumentList( strObjectType,
                                    xnNode.Attributes );

            HTMLUIObj htmlUIObj = 0;
            switch ( strObjectType.ToLower() ) {
                case "title":
                    htmlUIObj = HTMLUIObj.Title;
                    break;
                case "url":
                    htmlUIObj = HTMLUIObj.Url;
                    break;
                case "form":
                    htmlUIObj = HTMLUIObj.Form;
                    break;
                case "celldata":
                    htmlUIObj = HTMLUIObj.CellData;
                    break;
                case "select":
                    htmlUIObj = HTMLUIObj.Select;
                    break;
                case "button":
                    htmlUIObj = HTMLUIObj.Button;
                    break;
                case "link":
                    htmlUIObj = HTMLUIObj.Link;
                    break;
                case "image":
                    htmlUIObj = HTMLUIObj.Image;
                    break;
                case "checkbox":
                    htmlUIObj = HTMLUIObj.CheckBox;
                    break;
                case "bytag":
                    if ( strQueryString.IndexOf( @"tag" ) < 0 ) {
                        this.SetExitMessage(
                            WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BYTAG_REQUIRED,
                            this.Name, strObjectType );
                        base.FatalErrorMessage( ".", this.ExitMessage,
                                                1660, this.ExitCode );
                    }
                    htmlUIObj = HTMLUIObj.ByTag;
                    break;
                default:
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VERB_UNKNOWN,
                        this.Name, strObjectType );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                    break;
            }

            try {
                bool bResult =
                    this._wrsWebRunner.Verify(
                        htmlUIObj,
                        this.ScanVariables(strQueryString),
                        bExpectedResult );
                string strLogMessage =
                    String.Format( @"{0}: {1} - {2}",
                                   xnNode.Name,
                                   strQueryString,
                                   bResult ? @"PASS" : @"FAIL" );
                base.LogItWithTimeStamp( strLogMessage );

                if ( bResult )
                    this._lWrsLogger.Trace( "verify {0} - PASS",
                                            strQueryString );
                else
                    this._lWrsLogger.Error( "verify {0} - FAIL",
                                            strQueryString );
            } catch ( Exception e ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_UNKNOWN_EXCEPTION,
                    this.Name, e.Message );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }
        }

        public void GetHTMLAttribute( string strTestCaseName,
                                      string strObjectType,
                                      XmlNode xnNode ) {
            XmlNode xnExpectedResult =
                xnNode.Attributes.GetNamedItem( @"expectedresult" );
            XmlNode xnExtractPattern =
                xnNode.Attributes.GetNamedItem( @"extractpattern" );
            XmlNode xnSearchFor      =
                xnNode.Attributes.GetNamedItem( @"searchfor" );
            XmlNode xnRC             =
                xnNode.Attributes.GetNamedItem( @"rc" );
            XmlNode xnExtractXML     =
                xnNode.Attributes.GetNamedItem( @"extractxml" );

            bool bExtractXML = false;
            if ( xnExtractXML != null ) {
                try {
                    bExtractXML = bool.Parse( xnExtractXML.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BOOLEAN_PARSE_ERROR,
                        this.Name );
                    base.FatalErrorMessage(
                        ".", this.ExitMessage, 1660, this.ExitCode );
                }
            }

            bool bExpectedResult = false;
            if ( xnExpectedResult != null ) {
                try {
                    bExpectedResult = bool.Parse( xnExpectedResult.Value );
                } catch ( Exception ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BOOLEAN_PARSE_ERROR,
                        this.Name );
                    base.FatalErrorMessage(
                        ".", this.ExitMessage,
                        1660, this.ExitCode );
                }
            }

            string strQueryString =
                this.GetArgumentList( strObjectType, xnNode.Attributes );

            HTMLUIObj htmlUIObj = 0;
            switch ( strObjectType.ToLower() ) {
                case "bytag":
                    htmlUIObj = HTMLUIObj.ByTag;
                    if ( strQueryString.IndexOf( @"tag" ) < 0 ) {
                        this.SetExitMessage(
                            WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BYTAG_REQUIRED,
                            this.Name, strObjectType );
                        base.FatalErrorMessage( ".", this.ExitMessage,
                                                1660, this.ExitCode );
                    }
                    htmlUIObj = HTMLUIObj.ByTag;
                    break;
                default:
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VERB_UNKNOWN,
                        this.Name, strObjectType );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                    break;
            }

            bool bResult     = false;
            string strResult =
                this._wrsWebRunner.GetHTMLAttribute(
                    htmlUIObj,
                    this.ScanVariables(strQueryString) );
            bResult          =
                strResult.StartsWith( @"Attribute not found" ) ? false : true;

            string strExtractedBuffer = null;
            if ( bResult ) {
                string strCriteria = null;
                if ( bExtractXML ) {
                    Match m1 = Regex.Match(
                                   strResult, @"<\?xml.*\?>", RegexOptions.Multiline |
                                   RegexOptions.IgnoreCase );
                    Match m2 = Regex.Match(
                                   strResult, @"<\/xml>", RegexOptions.Multiline |
                                   RegexOptions.IgnoreCase );
                    bool bOK = m1.Success && m2.Success;
                    if ( bOK ) {
                        strExtractedBuffer =
                            strResult.Substring(
                                m1.Index,
                                (m2.Index - m1.Index) + m2.Length );
                        base.LogItWithTimeStamp( strExtractedBuffer );
                        this.ParseXMLData( strExtractedBuffer );
                        strResult = null;
                    } else
                        base.LogItWithTimeStamp(
                            String.Format( @"{0}: can't find {1} ",
                                           xnNode.Name, strCriteria ) );
                }
            }

            string strLogMsg =
                String.Format( @"{0}: {1} - {2}",
                               xnNode.Name, strQueryString,
                               bResult ? @"PASS" : @"FAIL" );

            base.LogItWithTimeStamp( strLogMsg );
        }

        public void Navigate( string strTestCaseName,
                              string strObjectType,
                              XmlNode xnNode ) {
            HTMLUIObj htmlUIObj = HTMLUIObj.BrowserWindow;
            XmlNode   xnURL     = xnNode.Attributes.GetNamedItem( @"url" );
            if ( xnURL == null ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
                    xnNode.Name, @"url" );
                base.FatalErrorMessage(
                    ".", this.ExitMessage, 1660, this.ExitCode );
            }
            string strQueryString =
                this.GetArgumentList( strObjectType, xnNode.Attributes );
            bool bResult          =
                this._wrsWebRunner.Navigate( htmlUIObj, strQueryString );
            string strLogMessage  =
                String.Format( "{0}: {1} - {2}",
                               xnNode.Name, strQueryString,
                               bResult ? @"PASS" : @"FAIL" );
            base.LogItWithTimeStamp( strLogMessage );
        }

        public void FindIE( string strTestCaseName,
                            string strObjectType,
                            XmlNode xnNode ) {
            string strQueryString =
                this.GetArgumentList( strObjectType, xnNode.Attributes );
            bool bResult          =
                this._wrsWebRunner.FindIE( strQueryString );
            string strLogMessage  =
                String.Format( @"{0}: {1} - {2}",
                               xnNode.Name, strQueryString,
                               bResult ? "PASS" : "FAIL" );
            base.LogItWithTimeStamp( strLogMessage );
        }

#endregion

#region program control follow section

        /// <summary>
        /// set a variable
        /// </summary>
        /// <param name="strTestCaseName">
        ///     string type of WebRunner test case name
        /// </param>
        /// <param name="strObjectType">
        ///     string type of method name
        /// </param>
        /// <param name="xnNode">
        ///     XmlNode type of passed in Xml node
        /// </param>
        /// <remarks>
        ///     this method is not to be called directly.  It is invoked
        ///     indirectly by ParseActionElement interface method.
        /// </remarks>
        public void SetVar( string strTestCaseName,
                            string strObjectType,
                            XmlNode xnNode ) {
            if ( xnNode.HasChildNodes ) {
                foreach ( XmlNode xn in xnNode.ChildNodes ) {
                    if ( xn.Name.Equals( @"var" ) )
                        this.AddVariable( strObjectType, xn.Attributes );
                    else {
                        this.SetExitMessage(
                            WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_TOKEN_NOT_KNOWN,
                            this.Name, xn.Name );
                        base.FatalErrorMessage( ".", this.ExitMessage,
                                                1660, this.ExitCode );
                    }
                }
                return;
            }
            this.AddVariable( strObjectType, xnNode.Attributes );
        }

        public void DoPrint( string strTestCaseName,
                             string strObjectType,
                             XmlNode xnNode ) {
            XmlNode xnMessage = xnNode.Attributes.GetNamedItem("message");
            if ( xnMessage == null ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
                    this.Name, strObjectType, @"message" );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }
            string strMessage = this.ScanVariables(xnMessage.Value);
            base.LogItWithTimeStamp( strMessage );
        }

        public void DoForEach( string strTestCaseName,
                               string strObjectType,
                               XmlNode xnNode ) {
            XmlNode xnIn   = xnNode.Attributes.GetNamedItem( "in" );
            XmlNode xnType = xnNode.Attributes.GetNamedItem( "type" );
            XmlNode xnDelm = xnNode.Attributes.GetNamedItem( "delm" );

            if ( xnNode.HasChildNodes )
                foreach ( XmlNode xn in xnNode.ChildNodes )
                this.CallMethod( this, xn );
        }

        public void DoCall( string strTestCaseName,
                            string strObjectType,
                            XmlNode xnNode ) {
            XmlNode xnLabel = xnNode.Attributes.GetNamedItem( "label" );
            if ( xnLabel == null ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
                    this.Name, strObjectType, @"label" );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }

            this._alLabels.Add( this.ScanVariables(xnLabel.Value) );
            string strXPathExpression = String.Format( @"label[@name=""{0}""]",
                                        this.ScanVariables(xnLabel.Value) );
            XmlNode xnLabelNode =
                this._xnActionElement.SelectSingleNode( strXPathExpression );

            if ( xnLabelNode == null ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_LABEL_NOT_DEFINED,
                    this.Name, strObjectType, xnLabel.Value );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }

            if ( xnLabelNode.HasChildNodes )
                for ( int i = 0; i < xnLabelNode.ChildNodes.Count; i++ ) {
                    XmlNode xn = xnLabelNode.ChildNodes[ i ];
                    if ( xn.NodeType != XmlNodeType.Comment )
                        this.CallMethod( this, xn );
                }
            else
                this.CallMethod( this, xnNode );
        }

        public void DoLabel( string strTestCaseName,
                             string strObjectType,
                             XmlNode xnNode ) {
            XmlNode xnLabelName = xnNode.Attributes.GetNamedItem( @"name" );
            string strLabelName = null;
            if ( xnLabelName != null )
                strLabelName = this.ScanVariables(xnLabelName.Value);

            if ( !this._alLabels.Contains( strLabelName ) )
                if ( xnNode.HasChildNodes )
                    foreach ( XmlNode xn in xnNode.ChildNodes )
                    if ( xn.NodeType != XmlNodeType.Comment )
                        this.CallMethod( this, xn );

        }
#endregion

#region IAction Members

        /// <summary>
        /// an override method that execute each actions with a given
        /// Action Element.
        /// </summary>
        public override void Execute() {

            string[] args =
                new string[2] { this.WebLogFileType, this.WebLogFileName };
            this.StartLogging( args );
            this.StartIE();
            base.Execute();
            this.EndLogging();

            base.IsComplete = true;
        }

        /// <summary>
        /// gets an execution state.
        /// </summary>
        /// <remarks>
        ///     When WebTest executes successfully, IsComplete
        ///     return true; otherwise, false is returned.
        /// </remarks>
        public new bool IsComplete
        {
            get {
                return base.IsComplete;
            }
        }

        /// <summary>
        /// gets the name of the WebTest objects
        /// </summary>
        /// <remarks></remarks>
        public new string Name
        {
            get {
                return this.GetType().Name.ToLower();
            }
        }

        /// <summary>
        /// gets an execution return status from the WebRunner
        /// </summary>
        /// <remarks></remarks>
        public new int ExitCode
        {
            get {
                return (int) this._enumWRSOprCode;
            }
        }

        /// <summary>
        /// gets an ExitMessage from the WebTest object
        /// </summary>
        /// <remarks></remarks>
        public new string ExitMessage
        {
            get {
                return _strExitMessage;
            }
        }

        /// <summary>
        /// gets the name of the action object
        /// </summary>
        /// <remarks></remarks>
        protected override string ObjectName
        {
            get {
                return this.Name;
            }
        }
#endregion

#region private utility functions
        /// <summary>
        /// setup the ExitMessage by lookuping up the Message table.
        /// </summary>
        /// <param name="wrsOprCode">
        ///     a WEBRUNNER_OPR_CODE enum type that contains a status
        ///     of each method execution
        /// </param>
        /// <param name="objParams">an array of parameters to be passed in</param>
        /// <remarks></remarks>
        private void SetExitMessage ( WEBRUNNER_OPR_CODE wrsOprCode,
                                      params object[] objParams ) {
            this._enumWRSOprCode = wrsOprCode;
            this._strExitMessage =
                String.Format( this._strMessages[ this.ExitCode ], objParams );
        }

        /// <summary>
        /// a private method that signal to start logging
        /// </summary>
        /// <remarks></remarks>
        private void StartLogging(string[] args) {
            if ( !this._bLogging )
                return;
            try {
                this._lWrsLogger  = new Log( args );
                string strLogName = Environment.CurrentDirectory +
                                    System.IO.Path.DirectorySeparatorChar        +
                                    this.TestName + @".log";
                this._lWrsLogger.BeginLog( this.TestName, this.TestOwner,
                                           strLogName, this.Description );
                this._lWrsLogger.BeginTestCase( this.TestName,
                                                this.Description );
            } catch ( Exception e ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_UNKNOWN_EXCEPTION,
                    this.Name, e.Message );
                base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
            }
        }

        private string GetArgumentList( string strObjectType,
                                        XmlAttributeCollection xac ) {
            StringBuilder sbQueryString = new StringBuilder();
            foreach ( XmlAttribute xa in xac ) {
                string strName = xa.Name.ToLower();
                if ( strName.Equals("expectedresult") ||
                        strName.Equals("extractpattern") ||
                        strName.Equals("label")          ||
                        strName.Equals("objecttype")     ||
                        strName.Equals("searchfor")      ||
                        strName.Equals("extractxml")     ||
                        strName.Equals("rc") )
                    continue;
                string strValue = this.ScanVariables( xa.Value );
                sbQueryString.AppendFormat( @"{0}={1}&&", xa.Name, strValue );
            }

            int iPos = sbQueryString.ToString().LastIndexOf( @"&&" );
            if ( iPos > 0 )
                sbQueryString = sbQueryString.Remove( iPos, @"&&".Length );

            return sbQueryString.ToString();
        }

        /// <summary>
        /// a private method that signal to stop logging
        /// </summary>
        private void EndLogging() {
            this._lWrsLogger.EndTestCase();
            this._lWrsLogger.EndLog();
        }

        /// <summary>
        /// a private method to setup the symbol table for WebTest object
        /// to recognize
        /// </summary>
        private void SetupWebRunnerTokens() {
            // for staring IE
            this._htWebRunnerTokens.Add( @"startie", "StartIE" );

            // for Click Method
            this._htWebRunnerTokens.Add( @"click",   "Click" );

            // for InputText method
            this._htWebRunnerTokens.Add( @"input",   "InputText" );

            // for Navigate method
            this._htWebRunnerTokens.Add( @"navigate", "Navigate" );

            // for GetObject method
            this._htWebRunnerTokens.Add( @"getobject", "GetObject" );

            // for Verify method
            this._htWebRunnerTokens.Add( @"verify", "Verify" );

            // for GetHTMLAttribute
            this._htWebRunnerTokens.Add( @"gethtmlattribute", "GetHTMLAttribute" );

            // for FindIE
            this._htWebRunnerTokens.Add( @"findie", "FindIE" );

            // the following statments define a set of
            // control flow function tokens
            this._htWebRunnerCtrlFlws.Add( @"defvar",  "SetVar" );
            this._htWebRunnerCtrlFlws.Add( @"call",    "DoCall" );
            this._htWebRunnerCtrlFlws.Add( @"foreach", "DoForEach" );
            this._htWebRunnerCtrlFlws.Add( @"if",      "DoIf" );
            this._htWebRunnerCtrlFlws.Add( @"print",   "DoPrint" );
            this._htWebRunnerCtrlFlws.Add( @"label",   "DoLabel" );

        }

        private void AddVariable ( string strObjectType,
                                   string strVariableName,
                                   string strVariableValue ) {
            if ( strVariableName.Length > this._iVarNameLen ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BADVARIABLE_NAME,
                    this.Name, strVariableName );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }

            if ( this._htActionScopeVarTable.ContainsKey( strVariableName ) ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VARIABLE_ALREADY_DEFINED,
                    this.Name, strObjectType, strVariableName );
                base.FatalErrorMessage ( ".", this.ExitMessage,
                                         1660, this.ExitCode );
            }

            this._htActionScopeVarTable.Add(
                String.Format( @":{0}", strVariableName ),
                strVariableValue );
            this._alRegExList.Add(
                new Regex( String.Format( @"(?<varname>(:{0}))",
                                          strVariableName ),
                           RegexOptions.IgnoreCase ) );
        }

        private void AddVariable ( string strObjectType,
                                   XmlAttributeCollection xac ) {
            XmlNode xnVarName  = xac.GetNamedItem( "name" );
            XmlNode xnVarValue = xac.GetNamedItem( "value" );

            // check if required attributes do exist!!
            if ( xnVarName == null ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
                    this.Name, strObjectType, @"name" );
                base.FatalErrorMessage ( ".", this.ExitMessage,
                                         1660, this.ExitCode );
            }
            if ( xnVarValue == null ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
                    this.Name, strObjectType, @"value" );
                base.FatalErrorMessage ( ".", this.ExitMessage,
                                         1660, this.ExitCode );
            }

            // make sure we don't have the variable defined yet;
            // otherwise an exception will be thrown for variable duplication
            string strVariableName  = xnVarName.Value;
            string strVariableValue = this.ScanVariables( xnVarValue.Value );

            if ( strVariableName.Length > this._iVarNameLen ||
                    !this._reVarName.IsMatch( strVariableName, 0 ) ) {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_BADVARIABLE_NAME,
                    this.Name, strVariableName );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }


            if ( !this._htActionScopeVarTable.ContainsKey( strVariableName ) ) {
                this._htActionScopeVarTable.Add(
                    String.Format(":{0}", strVariableName), strVariableValue );
                this._alRegExList.Add(
                    new Regex( String.Format( @"(?<varname>(:{0}))",
                                              strVariableName ),
                               RegexOptions.IgnoreCase ) );
            } else {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VARIABLE_ALREADY_DEFINED,
                    this.Name, strObjectType, strVariableName );
                base.FatalErrorMessage ( ".", this.ExitMessage,
                                         1660, this.ExitCode );
            }
        }

        private string GetVar ( string strVarName ) {
            string strVar = null;
            if ( this._htActionScopeVarTable.ContainsKey( strVarName ) )
                strVar = (string) this._htActionScopeVarTable[ strVarName ];
            else {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_VARIABLE_NOT_DEFINED,
                    this.Name, strVarName );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }
            return strVar;
        }
#endregion

#region IActionElement Members

        /// <summary>
        /// parse an input xml node and execution each element by
        /// looking up the symbol table.
        /// </summary>
        protected override void ParseActionElement() {
            if ( this._xnActionElement != null ) {
                foreach ( XmlNode xn in this._xnActionElement ) {
                    if ( xn.Name.ToLower().Equals( @"end" ) )
                        break;
                    if ( xn.NodeType != XmlNodeType.Comment )
                        this.CallMethod( this, xn );
                }
            }
        }

        private void CallMethod ( object obj, XmlNode xn ) {
            // string strMethodName = mi.Name;
            string strToken      = xn.Name;
            string strMethodName = null;
            object[] objParams   = null;

            if ( this._htWebRunnerTokens.ContainsKey( strToken ) ) {
                strMethodName = (string) this._htWebRunnerTokens[ strToken ];
                XmlNode xnObjectType =
                    xn.Attributes.GetNamedItem( @"objecttype" );
                if ( xnObjectType == null ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_ATTRIBUTE_NOTFOUND,
                        this.Name, strToken, @"action" );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                }
                objParams =
                    new object[3] { this.TestName, xnObjectType.Value, xn };
            } else if ( this._htWebRunnerCtrlFlws.ContainsKey( strToken ) ) {
                strMethodName = (string) this._htWebRunnerCtrlFlws[ strToken ];
                objParams =
                    new object[3]{ this.TestName, strToken, xn };
            } else {
                this.SetExitMessage(
                    WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_METHOD_NOT_DEFINED,
                    this.Name, strToken );
                base.FatalErrorMessage( ".", this.ExitMessage,
                                        1660, this.ExitCode );
            }

            Type tObjectType     = obj.GetType();
            MethodInfo miMethod  = tObjectType.GetMethod( strMethodName );
            if ( miMethod != null ) {
                try {
                    miMethod.Invoke( this, objParams );
                } catch ( ArgumentException ae ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_METHOD_PARAMETERS_TYPE_ERROR,
                        this.Name, strMethodName,
                        ae.GetBaseException().Message );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                } catch ( TargetException te ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_METHOD_PARAMETERS_NULLREF,
                        this.Name, strMethodName,
                        te.GetBaseException().Message );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                } catch ( TargetInvocationException tie ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_METHOD_GENERATES_EXCEPTIONS,
                        this.Name, strMethodName,
                        tie.GetBaseException().Message );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                } catch ( TargetParameterCountException tpce ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_METHOD_PARAMETERS_COUNT_INCORRECT,
                        this.Name, strMethodName,
                        tpce.GetBaseException().Message );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                } catch ( MethodAccessException mae ) {
                    this.SetExitMessage(
                        WEBRUNNER_OPR_CODE.WEBRUNNER_OPR_METHOD_NOACCESS_RIGHT,
                        this.Name, Environment.UserDomainName, strMethodName,
                        mae.GetBaseException().Message );
                    base.FatalErrorMessage( ".", this.ExitMessage,
                                            1660, this.ExitCode );
                }
            }
        }

        private string ScanVariables( string strVariableString ) {
            return this.ScanVariables( strVariableString, true );
        }

        private string ScanVariables( string strVariableString,
                                      bool bRequestScan ) {
            int iPos = strVariableString.IndexOf( @":" );

            if ( iPos > -1 && bRequestScan ) {
                for ( int i = 0; i < this._alRegExList.Count; i++ ) {
                    Regex re = (Regex) this._alRegExList[ i ];
                    Match m  = Match.Empty;
                    m = re.Match( strVariableString );
                    if ( m.Success ) {
                        string strVariableName  = m.Groups["varname"].Value;
                        string strVariableValue = this.GetVar( strVariableName );
                        strVariableString       =
                            re.Replace( strVariableString, strVariableValue );
                    }
                }
            }
            return strVariableString;
        }

        private void ParseXMLData( string strXMLString ) {
            XmlDocument xdDoc = new XmlDocument();
            XmlDocumentFragment xdf = xdDoc.CreateDocumentFragment();
            // xdDoc.LoadXml( strXMLString );

            try {
                xdf.InnerXml = strXMLString;
                XPathNavigator xpn = xdDoc.CreateNavigator();
                if ( xpn.HasChildren )
                    while ( xpn.MoveToFirst() )
                        Console.WriteLine( xpn.Name );
            } catch ( Exception e ) {
                throw e;
            }
        }
    }
#endregion
}
