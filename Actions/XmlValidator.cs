using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

/*
 * Class Name    : XmlValidator
 * Inherient     : ActionElement
 * Functionality : Validate a gvien Xml node match a specific required
 *
 * Created Date  : 01/02/2005
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------------
 * mliang           01/02/2005      Initial creation
 */

namespace XInstall.Core.Actions {
  /// <summary>
  /// Summary description for XmlValidator.
  /// </summary>
  public class XmlValidator : ActionElement {
    private XmlNode     _ActionNode            = null;
    private XmlDocument _ConfigXml             = new XmlDocument();
    private Regex       _RegExSearchContains   = null;
    private Regex       _RegExCheckNotContains = null;
    private string      _FullXmlFilePath       = String.Empty;
    private string      _Section               = String.Empty;
    private string      _NodeName              = String.Empty;
    private string      _SearchFor             = String.Empty;
    private string      _SearchContains        = String.Empty;
    private string      _CheckFor              = String.Empty;
    private string      _CheckNotContains      = String.Empty;


    [Action("xmlvalidator")]
    public XmlValidator( XmlNode ActionNode ) : base( ActionNode ) {
      this._ActionNode = ActionNode;
    }

#region public properties

    public new string Name {
      get {
        return this.GetType().Name;
      }
    }


    [Action("runnable", Needed=false, Default="true")]
    public new string Runnable {
      set {
        base.Runnable = bool.Parse( value );
      }
    }


    [Action("xmlfilepath", Needed=true)]
    public string XmlFilePath {
      get {
        return this._FullXmlFilePath;
      }
      set {
        this._FullXmlFilePath = value;
        if ( !File.Exists( this._FullXmlFilePath ) )
          throw new FileNotFoundException( "can find this file", this._FullXmlFilePath );
        this._ConfigXml.Load( this._FullXmlFilePath );
      }
    }


    [Action("section", Needed=false, Default="appSettings")]
    public string Section {
      get {
        return this._Section;
      }

      set {
        this._Section = value;
      }
    }


    [Action("nodename", Needed=false, Default="")]
    public string NodeName {
      get {
        return this._NodeName;
      }

      set {
        this._NodeName = value;
      }
    }


    [Action("searchfor", Needed=true)]
    public string SearchFor {
      get {
        return this._SearchFor;
      }

      set {
        this._SearchFor = value;
      }
    }


    [Action("searchcontains", Needed=false, Default="")]
    public string SearchContains {
      get {
        return this._SearchContains;
      }

      set {
        this._SearchContains = value;
      }
    }


    [Action("checkfor", Needed=true)]
    public string CheckFor {
      get {
        return this._CheckFor;
      }

      set {
        this._CheckFor = value;
      }
    }

    [Action("checknotcontains", Needed=false, Default="")]
    public string CheckNotContains {
      get {
        return this._CheckNotContains;
      }

      set {
        this._CheckNotContains = value;
      }
    }

#endregion

#region protected properties

    protected override object ObjectInstance {
      get {
        return this;
      }
    }


    protected override string ObjectName {
      get {
        return this.Name;
      }
    }

#endregion

#region public methods

    protected override void ParseActionElement() {
      base.ParseActionElement ();

      StringBuilder XPathExpression = new StringBuilder();
      XmlElement  Root  = this._ConfigXml.DocumentElement;

      if ( this.SearchContains.Length > 0 )
        this._RegExSearchContains = new Regex( this.SearchContains, RegexOptions.IgnoreCase );

      if ( this.CheckNotContains.Length > 0 )
        this._RegExCheckNotContains = new Regex( this.CheckNotContains, RegexOptions.IgnoreCase );

      XPathExpression.Append( "//./" );
      if ( this.Section.Length > 0 ) {
        XPathExpression.AppendFormat( "{0}/", this.Section );
      }

      XPathExpression.Append( "*" );

      XmlNodeList Nodes = Root.SelectNodes( XPathExpression.ToString() );

      if ( this._RegExSearchContains != null && this._RegExCheckNotContains != null )
        this.ValidateXmlNode( Nodes );
    }


#endregion

#region private methods

    private void ValidateXmlNode( XmlNodeList Nodes ) {
      foreach ( XmlNode xn in Nodes ) {
        if ( xn.HasChildNodes )
          this.ValidateXmlNode( xn.ChildNodes );
        else if ( xn.Attributes.Count > 0 ) {
          XmlAttributeCollection xac = xn.Attributes;
          XmlNode SearchForNode = xac.GetNamedItem( this.SearchFor );
          if ( SearchForNode != null )
            if ( this._RegExSearchContains.IsMatch( SearchForNode.Value ) ) {
            XmlNode CheckForNode = xac.GetNamedItem( this.CheckFor );
            if ( CheckForNode != null )
              if ( this._RegExCheckNotContains.IsMatch( CheckForNode.Value ) )
                base.FatalErrorMessage( ".", String.Format( "{0} : section {1}, key {2} value : {3} contains invaliad value {4}",
                      this.XmlFilePath, this.Section, SearchForNode.Value, CheckForNode.Value, this.CheckNotContains ), 1660, false );
          }

        }
      }
    }

#endregion
  }
}
