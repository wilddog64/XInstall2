using System;
using System.Xml;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for Print.
    /// </summary>
    public class Print : ActionElement {
	    private XmlNode _ActionNode = null;
	    private string  _Message    = string.Empty;

	    [Action("print")]
	    public Print( XmlNode ActionNode ) : base( ActionNode ) {
		    this._ActionNode = ActionNode;
	    }


	    [Action("message", Needed=false)]
	    public string Message {
		    get {
			    return this._Message;
		    }
		    set {
			    this._Message = value;
		    }
	    }


	    #region protected properties

	    protected override object ObjectInstance {
		    get {
			    return this;
		    }
	    }


	    protected override string ObjectName {
		    get {
			    return this.GetType().Name;
		    }
	    }


	    #endregion

	    protected override void ParseActionElement() {
		    base.ParseActionElement ();

		    if ( this.Message != null )
			    if ( this.Message.Length == 0 ) {
				    XmlNode MessageNode = this._ActionNode.SelectSingleNode( "message" );
				    if ( MessageNode != null )
					    base.LogItWithTimeStamp( MessageNode.InnerText );
			    }
			    else
				    base.LogItWithTimeStamp( string.Format( "{0}: {1}", this.ObjectName, this.Message ) );
		    else
			    base.FatalErrorMessage( ".", String.Format( "{0}:No message provided", this.ObjectName ), 1660, true );
	    }

    }
}
