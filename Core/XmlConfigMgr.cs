using System;
using System.Xml;
using System.Collections;
using System.Text;
using XInstall.Util;
using XInstall.Util.Log;

/*
 * Class Name    : XmlConfigMgr
 * Inherient     : Logger
 * Functionality : A class that handles Xml configuration file parsing
 *
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------------
 * mliang           05/01/2003      Initial creation
 * mliang           01/28/2005      Method UpdateXmlVariable was added.
 *                                  This is used to support updating
 *                                  variable node from command line
 *                                  interface.
 *
 * mliang           02/04/2005      Added Init methods so that all
 *                                  constructors will used these methods,
 *                                  which provided a better way to
 *                                  centralize the logic.
 *
 *                                  Add an overload constructor to accept
 *                                  an XmlTextReader as a parameter.
 *
 *                                  GetConfigFile( string ) and
 *                                  GetConfigFile( XmlTextReader ) were
 *                                  added.  This is for the convient of
 *                                  being used by Web Applications
 *
 * mliang           02/24/2005      Trapping xml error in the constructor
 *                                  and throw out a user friendly error messages
 */
namespace XInstall.Core {

    /// <summary>
    /// class XmlConfKeywordInfoEventArg -
    ///   this class inherits EventArgs and is used
    ///   as the Event Function's parameters.
    ///
    ///   It has implement the following member functions
    ///
    ///      string NodeName - returns the node name the parser
    ///                        return
    ///      bool HasChildren - return a boolean value to indicate
    ///                         whether a given node has children or not.
    ///      bool HasAttribute - return a given node has attributes or
    ///                           not
    ///      XmlNode Node - return a given node back
    /// </summary>
    public class XmlConfKeywordInfoEventArg : EventArgs {
#region object variables
        private readonly XmlNode _xnNode          = null;
        private readonly XmlNodeList _xnlNodeList = null;
#endregion

#region interal variables
        private String   _strNodeName                     = null;
        private XmlAttributeCollection _xacNodeAttributes = null;
        private bool     _hasAttribute                    = false;
        private bool     _hasChildren                     = false;
#endregion

        /// <summary>
        /// public XmlConfKeywordInfoEventArg( XmlNode xnNode ) -
        ///     a constructor that initializes the event arguments
        /// </summary>
        /// <param name="xnNode">an xml node</param>
        public XmlConfKeywordInfoEventArg ( XmlNode xnNode ) {
            XmlNode _xnNode    = xnNode;
            _strNodeName       = _xnNode.Name;
            _xacNodeAttributes = xnNode.Attributes;
            _xnlNodeList       = _xnNode.ChildNodes;

        }


        /// <summary>
        /// property NodeName -
        ///     gets a node name
        /// </summary>
        public string NodeName {
            get { return _strNodeName; }
        }


        /// <summary>
        /// property HasChildren -
        ///     gets a boolean value that indicates if
        ///     a given node has a child nodes
        /// </summary>
        public bool HasChildren {
            get {
                if ( _xnNode != null )
                    _hasChildren = _xnNode.HasChildNodes;
                return _hasChildren;
            }
        }


        /// <summary>
        /// property HasAttribute -
        ///     gets a boolean value that indicates if
        ///     a given node has attributes
        /// </summary>
        public bool HasAttribute {
            get {
                _hasAttribute = _xacNodeAttributes != null ? true : false;
                return _hasAttribute;
            }
        }


        /// <summary>
        /// property Attributes -
        ///     gets attributes for a given node
        /// </summary>
        public XmlAttributeCollection Attributes {
            get { return _xacNodeAttributes; }
        }


        /// <summary>
        /// property Node -
        ///     gets a particular Xml node
        /// </summary>
        public XmlNode Node {
            get { return _xnNode; }
        }

    }


    /// <summary>
    /// public delegate void XmlConfigKeywordHandler -
    ///    a delegate that handles the user defined callback
    ///    function.  It passes two parameters to the callback
    ///    function -
    ///
    ///         object Sender - is an object who fire off the event
    ///         XmlConfKeywordInfoEventArg XConfKeywordInfo - is an
    ///         information about a given node
    /// </summary>
    public delegate void XmlConfigKeywordHandler
    ( object Sender, XmlConfKeywordInfoEventArg XConfKeywordInfo );

    /// <summary>
    /// XmlConfigMgr - a class that parse XML configuration file
    ///                It inherits XmlReader class
    /// </summary>
    public class XmlConfigMgr : ActionElement {
        private const string _cntStrXPathExpr = @"//*";

        // class level variables and initalization
        private string      _strXmlFile     = null;
        private string      _strXPathExpr   = null;
        private XmlDocument _CXmlDoc        = null;
        private XmlElement  _xeDocumentRoot = null;
        private XmlNodeList _xnlActionNodes = null;

        // a readonly string variable
        private readonly string XPathSearchFormat = @"//setup/package[@name='{0}']/defvar[@name='{1}']";

        // an event function
        public event XmlConfigKeywordHandler XmlConfigKeywordChange;

        /// <summary>
        /// protected virtual void OnXmlConfigKeywordChange is an event
        /// function that fires off when Xml node name is changed.  It
        /// actually wraps up the XmlConfigKeywordChange delegate function
        /// </summary>
        /// <param name="e"></param>

        protected virtual void OnXmlConfigKeywordChange ( XmlConfKeywordInfoEventArg e ) {
            if ( XmlConfigKeywordChange != null )
                XmlConfigKeywordChange ( this, e );
        }


        /// <summary>
        /// A default constructor
        /// </summary>
    public XmlConfigMgr() : base() {}

        public XmlConfigMgr( ISendLogMessage SendLogMessageIF ) : base( SendLogMessageIF ) {}


        /// <summary>
        /// public XmlConfigMgr - is a constructor function that
        ///        reads XmlFile in.
        /// </summary>
        /// <param name="strXmlFileName"></param>
        public XmlConfigMgr ( String strXmlFileName ) {
            // try to create an XmlDocument object and load
            // a given Xml file into DOM object and capture
            // any possible errors.
            try {
                this.Init( strXmlFileName );
            } catch ( XmlException xe ) {
                base.LogItWithTimeStamp( String.Format("incorrect xml format - {0}", xe.Message ) );
            }
        }


        public XmlConfigMgr ( XmlTextReader XTextReader ) {
            // try to create an XmlDocument object and load
            // a given Xml file into DOM object and capture
            // any possible errors.
            this.Init( XTextReader );
        }


        /// <summary>
        ///  property XmlFile -
        ///     sets an XML file to be pased.
        /// </summary>
        public string XmlFile
        {
            set { this._strXmlFile = value; }
        }


        /// <summary>
        /// property XPath -
        ///     sets the XPath expression used to look for
        ///     an XML node.
        /// </summary>
        public string XPath
        {
            set { this._strXPathExpr = value; }
        }


        /// <summary>
        /// property Name -
        ///     gets the current class name
        /// </summary>
        public new string Name
        {
            get { return this.GetType().Name.ToLower(); }
        }


        /// <summary>
        /// public void Parse - parse the Xml file and passes node to the event
        /// function OnXmlConfigKeywordChange
        /// </summary>
        public void Parse() {
            // now point to the Xml Document Root
            // and start processing everything descent from it.
            // _xeDocumentRoot = _CXmlDoc.DocumentElement;
            string strXPathExpr = this._strXPathExpr == null ? _cntStrXPathExpr : this._strXPathExpr;

            // walk through each node and pass it to the event function
            // OnXmlConfigKeywordChange
            XmlNodeList nlNodeList = _xeDocumentRoot.SelectNodes( strXPathExpr );
            for ( int iNodeIdx = 0; iNodeIdx < nlNodeList.Count; iNodeIdx++ ) {
                XmlNode xnNode = nlNodeList[iNodeIdx];
                XmlConfKeywordInfoEventArg XmlCFKeywordInfo = new XmlConfKeywordInfoEventArg( xnNode );
                OnXmlConfigKeywordChange( XmlCFKeywordInfo );
            }
        }


        /// <summary>
        /// public XmlNodeList Parse( string strXmlFile ) -
        ///     an overloaded method that accepts an Xml file
        ///     as a parameter and return XmlNodeList object
        /// </summary>
        /// <param name="strXmlFile">
        /// a string that contains the name of a given xml file
        /// </param>
        /// <returns>a xml node list</returns>
        public XmlNodeList Parse ( string strXmlFile ) {
            string strXPathExpr = this._strXPathExpr == null ? _cntStrXPathExpr : this._strXPathExpr;

            // this.Init( strXmlFile );
            if ( this._CXmlDoc == null ) {
                this._CXmlDoc = new XmlDocument();
                this._CXmlDoc.Load( strXmlFile );
            }

            this._xnlActionNodes = this._CXmlDoc.SelectNodes( strXPathExpr );
            return this._xnlActionNodes;
        }



        public void UpdateXmlVariable( string PackageName, string VarName, string UpdateValue ) {
            string XPath = String.Format( this.XPathSearchFormat, PackageName, VarName );

            XmlNode VarNode  = this._xeDocumentRoot.SelectSingleNode( XPath );
            if ( VarNode != null ) {
                XmlNode VarValue = VarNode.Attributes.GetNamedItem( "value" );
                if ( VarValue != null )
                    VarValue.Value = UpdateValue;
            }
        }



        public void GetConfigFile( string FileName ) {
            if ( this._CXmlDoc == null )
                this.Init( FileName );
        }


        public void GetConfigFile( XmlTextReader Stream ) {
            this.Init( Stream );
        }


        /// <summary>
        /// private void Init() -
        ///     Initialize XmlDocument object
        ///     for paring a given Xml file
        /// </summary>
        private void Init() {
            try {
                _CXmlDoc = new XmlDocument();
                _CXmlDoc.Load ( this._strXmlFile );
            }
            catch ( XmlException xe ) {
                base.FatalErrorMessage( ".", String.Format("{0}: {1}", this.Name, xe.Message), 1660 );
                throw new Exception();
            }
            catch ( System.IO.FileNotFoundException ) {
                base.FatalErrorMessage( ".", String.Format( "{0}: cannot find file {1}", this.Name, this._strXmlFile ), 1660 );
                throw;
            }
        }


        /// <summary>
        /// private void Init( string strXmlFile ) -
        ///     an overloaded method that takes an
        ///     input Xml file and initialize the
        ///     XmlDocument object.
        /// </summary>
        /// <param name="strXmlFile"></param>
        private void Init( string strXmlFile ) {
            this.XmlFile = strXmlFile;

            try {
                this._CXmlDoc = new XmlDocument();
                this._CXmlDoc.Load( this._strXmlFile );
            }
            catch ( XmlException xe ) {
                string strExceptionMessage = String.Format( @"{0}: Parsing Error with {1}, message: {2}", this.Name, strXmlFile, xe.Message);

                base.FatalErrorMessage( ".", strExceptionMessage, 1660, false );
            }
            catch ( System.IO.FileNotFoundException ) {
                base.FatalErrorMessage( ".", String.Format( "{0}: cannot find file {1}", this.Name, this._strXmlFile ), 1660 );
                throw;
            }
        }


        private void Init( XmlTextReader xt ) {
            try {
                this._CXmlDoc = new XmlDocument();
                this._CXmlDoc.Load( xt );
            } catch ( XmlException xe ) {
                base.FatalErrorMessage( ".", xe.Message, 1660, false );
                throw xe;
            } catch ( System.IO.FileNotFoundException ) {
                base.FatalErrorMessage( ".", String.Format( "{0}: cannot find file {1}", this.Name, this._strXmlFile ), 1660 );
                throw;
            }
        }
    }
}
