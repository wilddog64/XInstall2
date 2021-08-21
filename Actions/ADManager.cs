using System;
using System.Collections;
using System.DirectoryServices;
using System.Xml;


namespace XInstall.Core.Actions {
  /// <summary>
  /// Summary description for ADManager.
  /// </summary>
  public class ADManager : AdsiBase {
    private SearchResultCollection _Results;

    private string _adServer = String.Empty;
    private string _adFilter = String.Empty;
    private string _adBaseDN = String.Empty;
    private string _InPorps  = String.Empty;

    private int    _adPageSize;

    [Action("admgmr")]
    public ADManager( XmlNode ActionNode ) : base( ActionNode ) {}

    public ADManager() {}

    protected override string AdsiProvider {
      get {
        return "LDAP";
      }
    }


    [Action("adserver", Needed=true)]
    public override string MachineName {
      get {
        return this._adServer;
      }
      set {
        this._adServer = value;
      }
    }


    [Action("filter", Needed=false, Default="(classObject=*)")]
    public string Filter {
      get {
        return this._adFilter;
      }
      set {
        this._adFilter = value;
      }
    }


    [Action("pagesize", Needed=false, Default="100")]
    public string PageSize {
      set {
        this._adPageSize = int.Parse( value );
      }
    }


    [Action("basedn", Needed=false, Default="ou=Employees,dc=180Solutions,dc=com")]
    public override string AdsiPath {
      get {
        return this._adBaseDN;
      }
      set {
        this._adBaseDN = value;
      }
    }


    [Action("properties", Needed=false, Default="")]
    public string QueryProperties {
      get {
        return this._InPorps.Trim(null);
      }
      set {
        this._InPorps = value;
      }
    }


    public SearchResultCollection SearchResults {
      get {
        return this._Results;
      }
    }


    protected override object ObjectInstance {
      get {
        return this;
      }
    }


    protected override void ParseActionElement() {
      base.ParseActionElement ();

      DirectorySearcher ds = new DirectorySearcher( base.ADSIBaseObject, this.Filter );

      if ( this.QueryProperties.Length > 0 ) {
        string[] Props = this.QueryProperties.Split( new char[] { ',' } );
        ds.PropertiesToLoad.AddRange( Props );
      }

      ds.PageSize = this._adPageSize;

      SearchResultCollection Results = ds.FindAll();
      this._Results = Results;
      //            foreach ( SearchResult sr in Results )
      //            {
      //                string SearchPath = sr.Path;
      //                ResultPropertyCollection ResultProperties = sr.Properties;
      //                base.OutToFile = false;
      //                foreach ( string ResultPropertyName in ResultProperties.PropertyNames )
      //                {
      //                    foreach( object PropertyValue in ResultProperties[ResultPropertyName] )
      //                    {
      //                        string Message = String.Format(
      //                            "{0}: {1}", ResultPropertyName, PropertyValue );
      //                        base.LogItWithTimeStamp( Message );
      //
      //                    }
      //                }
      //            }
      //            // this.LogItWithTimeStamp( "----------------------------" );
      //            base.OutToFile = true;
    }
  }
}
