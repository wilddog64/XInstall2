using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XInstall.Core.Actions
{

    /// <summary>
    /// Summary description for WebConfigWriter.
    /// The WebConfigWriter.cs is a class that perform an
    /// update to the web.config file.
    ///
    /// Currently, it supports only one sub-element - update.
    ///
    /// The structure of the update tag contains only one element,
    /// modifykey, which use xpath to search for a gvien key then use
    /// regular expression to perform a substitution.
    /// </summary>
    public class XmlWriter : ActionElement
    {
	    // private variables
	    private XmlNode _ActionNode         = null;
	    private string  _InputWebConfigFile = String.Empty;

	    // error handling codes
	    private enum WEBCONFIG_OPR_CODE
	    {
		    WEBCONFIG_OPR_SUCCESS = 0,
		    WEBCONFIG_OPR_BOOLEAN_PARSE_ERROR,
		    WEBCONFIG_OPR_AUTOEXCEPTION_GENERATED,
		    WEBCONFIG_OPR_CONFIG_FILE_NOTFOUND,
		    WEBCONFIG_OPR_EMPTY_UPDATE_PARAMETERS,
	    };

	    private WEBCONFIG_OPR_CODE _WebConfOprCode =
		WEBCONFIG_OPR_CODE.WEBCONFIG_OPR_SUCCESS;

	    private string[] _strMessages =
	    {
		    "{0}: operation {1} successfully complete",
		    "{0}: error parsing boolean value",
		    "{0}: exception was required by the user",
		    "{0}: input file {1} cannot not be found!",
		    "{0}: method {1} - parameter {2} cannot be empty!",

	    };
	    private string _strExitMessage = null;

	    /// <summary>
	    /// A constructor that initlaizes the WebConfigWriter class.
	    /// It took only one parameter, xn, which is a passed in XML
	    /// node.
	    /// </summary>
	    /// <param name="xn">
	    /// a type of XmlNode.
	    /// </param>
	    [Action("xmlwriter")]
	    public XmlWriter( XmlNode xn )
	    {
		    this._ActionNode = xn;
	    }

	    #region IAction Members

	    /// <summary>
	    /// get/set the web.config file
	    /// </summary>
	    [Action("filename", Needed=true)]
	    public string WebConfigFile
	    {
		    get
		    {
			    return this._InputWebConfigFile;
		    }
		    set
		    {
			    this._InputWebConfigFile = value;
			    if ( !File.Exists( this._InputWebConfigFile ) )
			    {
				    this.SetExitMessage(
					WEBCONFIG_OPR_CODE.WEBCONFIG_OPR_CONFIG_FILE_NOTFOUND,
					this.Name, this._InputWebConfigFile);
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
			    }
		    }
	    }


	    /// <summary>
	    /// get/set a flag that tells if webconfig should run or not
	    /// </summary>
	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse(value);
		    }
	    }


	    /// <summary>
	    /// get/set a flag that tells if webconfig should ignore the error or
	    /// not
	    /// </summary>
	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {
		    set
		    {
			    base.SkipError = bool.Parse( value );
		    }
	    }


	    /// <summary>
	    /// is an overrided method that derives from ActionElement.
	    /// It is used to parse the content of a gvien XML node.
	    /// </summary>
	    protected override void ParseActionElement()
	    {
		    // first we have to call base class's
		    // ParseActionElement method
		    // then perform our own work.
		    // base.ParseActionElement ();
		    this.UpdateWebConfig( this._ActionNode );
	    }


	    /// <summary>
	    /// get the status of object's completeness.
	    /// </summary>
	    public new bool IsComplete
	    {
		    get
		    {
			    return base.IsComplete;
		    }
	    }


	    /// <summary>
	    /// gets an message from webonfig object
	    /// </summary>
	    public new string ExitMessage
	    {
		    get
		    {
			    return this._strExitMessage;
		    }
	    }


	    /// <summary>
	    /// gets a name for the webconfig object
	    /// </summary>
	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }

	    /// <summary>
	    /// gets an exit code of the object
	    /// </summary>
	    public new int ExitCode
	    {
		    get
		    {
			    return (int) this._WebConfOprCode;
		    }
	    }


	    /// <summary>
	    /// an override properties that return the name of current object
	    /// </summary>
	    protected override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }


	    #endregion

	    #region private methods/properties

	    /// <summary>
	    /// a private method that uses to set the message
	    /// </summary>
	    /// <param name="WebConfigCode">a enumeration type of WEBCONFIG_OPR_CODE</param>
	    /// <param name="objParams">a parameter array</param>
	    private void SetExitMessage(
		WEBCONFIG_OPR_CODE WebConfigCode,
		params object[] objParams )
	    {
		    this._WebConfOprCode = WebConfigCode;
		    this._strExitMessage = String.Format(
					       this._strMessages[ this.ExitCode ], objParams );
	    }

	    private Hashtable GetUpdateInfo( XmlNodeList UpdateNodes )
	    {
		    string MethodName = "GetUpdateInfo";

		    Hashtable UpdateInfo = new Hashtable();

		    foreach ( XmlNode xn in UpdateNodes )
		    {
			    // make sure node name is modifykey
			    if ( xn.Name.Equals( @"modifykey" ) )
			    {
				    // retrieves required attributes
				    // and check to make sure they all have values
				    XmlNode XPath   = xn.Attributes.GetNamedItem("xpath");
				    XmlNode Pattern = xn.Attributes.GetNamedItem("pattern");
				    XmlNode Value   = xn.Attributes.GetNamedItem("value");
				    if ( XPath == null )
				    {
					    this.SetExitMessage(
						WEBCONFIG_OPR_CODE.WEBCONFIG_OPR_EMPTY_UPDATE_PARAMETERS,
						this.Name, MethodName, "key" );
				    }
				    if ( Value == null )
				    {
					    this.SetExitMessage(
						WEBCONFIG_OPR_CODE.WEBCONFIG_OPR_EMPTY_UPDATE_PARAMETERS,
						this.Name, MethodName, "value" );
				    }

				    if ( !UpdateInfo.ContainsKey( XPath ) )
					    if ( Pattern != null )
						    UpdateInfo.Add(
							XPath,
							new object[2]
					    {
						    new Regex( Pattern.Value ), Value
					    } );
				    else
					    UpdateInfo.Add(
						XPath,
						new object[2]
				    {
					    null, Value
				    }
				    );
			    }
		    }

		    return UpdateInfo;
	    }

	    /// <summary>
	    /// This method is used to perform an update in web.config.
	    /// It first uses XPath to locate the element to be updated;
	    /// then uses regular expression to perform a substitution (Regular
	    /// expression is optional, if not provided, value will be used to
	    /// replaced by whatever is found by the XPath expression).
	    /// </summary>
	    /// <param name="ActionNode"></param>
	    private void UpdateWebConfig( XmlNode ActionNode )
	    {

		    // an xpath expression to collect all the update elements and their
		    // content
		    XmlNodeList UpdateNodes = ActionNode.SelectNodes( @"update/*" );

		    // a hash table to hold passed in parameters from xml node, modifykey
		    Hashtable UpdateInfo = new Hashtable();

		    // if we have any nodes, start processing them
		    UpdateInfo = GetUpdateInfo( UpdateNodes );

		    // now start updating the web.config file by
		    // 1. read them into XmlDocument structure.
		    // 2. point to root element and start
		    XmlDocument xd = new XmlDocument();
		    xd.Load( this.WebConfigFile );
		    XmlElement Root = xd.DocumentElement;

		    // if root is not null then retrieve information
		    // from the hash table and start updating the node.
		    if ( Root != null )
		    {
			    IDictionaryEnumerator de = UpdateInfo.GetEnumerator();
			    while ( de.MoveNext() )
			    {
				    XmlNode xpath = (XmlNode)  de.Key;
				    object[] objs = (object[]) de.Value;
				    Regex Pattern = (Regex)    objs[0];
				    XmlNode Value = (XmlNode)  objs[1];

				    // 1. use XPath to collect the node we want to udpate
				    // 2. use regex to replace the value.
				    XmlNode UpdateNode = Root.SelectSingleNode( xpath.Value );
				    if ( UpdateNode != null )
				    {
					    string OldValue = UpdateNode.Value;
					    if ( Pattern != null )
						    UpdateNode.Value =
							Pattern.Replace( UpdateNode.Value, Value.Value );
					    else
					    {
						    UpdateNode.Value = Value.Value;
					    }

					    base.LogItWithTimeStamp(
						String.Format( "{0}: successfully update {1} with {2}",
							       this.Name, OldValue, UpdateNode.Value ) );
				    }
			    }
			    xd.Save( this.WebConfigFile );
		    }
	    }
	    #endregion
    }
}
