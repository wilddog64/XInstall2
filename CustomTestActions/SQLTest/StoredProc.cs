using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml;

using XInstall.Core;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for StoredProc.
    /// </summary>
    [SQL("StoredProc")]
    public class StoredProc {
        private DBAccess       _MyDB           = null;
        private XmlNode        _StoredProcNode = null;
        private SqlParameter[] _SqlParams      = null;
        private ArrayList      _Result         = null;

        private bool   _MatchExpectedString = false;
        private bool   _Enable              = true;
        private int    _HighThreshold       = -999;
        private int    _LowThreshold        = -999;

        private string _StoredProcName = string.Empty;
        private string _ExpectedString = string.Empty;
        private string _ColName        = string.Empty;
        private string _DisplayName    = string.Empty;

        private const string NODE_NAME        = "ExpectedResults";
        private const string PARENT_NODE_NAME = "StoredProc";


        public StoredProc( DBAccess MyDB, XmlNode StoredProcNode ) {
            this._MyDB           = MyDB;
            this._StoredProcNode = StoredProcNode;
            this._SqlParams      = this.GetStoredProcParameters();
            this.SetResultExpectation();

            XmlAttributeCollection StoredProcAttribs = this._StoredProcNode.Attributes;
            XmlNode StoredProcNameAttrb              = StoredProcAttribs.GetNamedItem( "Name" );
            if ( StoredProcNameAttrb != null )
                this._StoredProcName = StoredProcNameAttrb.Value;
        }


        public string StoredProcName
        {
            get {
                return this._StoredProcName;
            }
            set {
                this._StoredProcName = value;
            }
        }


        [SQL("ExpectedString", Required=false, Default="")]
        public string ExpectedString
        {
            get {
                return this._ExpectedString;
            }
            set {
                this._ExpectedString = value;
            }
        }


        [SQL("HighThreshold", Required=false, Default="-999")]
        public string HighThreshold
        {
            get {
                return this._HighThreshold.ToString();
            }
            set {
                this._HighThreshold = Convert.ToInt32(value);
            }
        }


        [SQL("LowThreshold", Required=false, Default="-999")]
        public string LowThreshold
        {
            get {
                return this._LowThreshold.ToString();
            }
            set {
                this._LowThreshold = Convert.ToInt32(value);
            }
        }


        [SQL("ColName2LookFor", Required=false, Default="")]
        public string ColName
        {
            get {
                return this._ColName;
            }
            set {
                this._ColName = value;
            }
        }


        [SQL("Enable", Required=false, Default="true")]
        public string Enable
        {
            set {
                this._Enable = bool.Parse( value );
            }
        }


        [SQL("DisplayName", Required=true)]
        public string DisplayName
        {
            get {
                return this._DisplayName;
            }
            set {
                this._DisplayName = value;
            }
        }

        public ArrayList Result
        {
            get {
                return this._Result;
            }
        }


        public bool MatchedExpectedString
        {
            get {
                return this._MatchExpectedString;
            }
        }


        public void Execute() {

            if ( !this._Enable )
                return;

            int HighThreshold    = -999;
            int LowThreshold     = -999;
            bool FindExpectedStr = false;
            bool IsInteger       = true;
            bool HasOtherVal     = false;
            ArrayList ResultSet  = null;

            if ( this._HighThreshold != -999 &&
                    this._LowThreshold  != -999 )
                ResultSet =
                    this._MyDB.ExecuteStoredProc(
                        this.StoredProcName,
                        false,
                        this._SqlParams );

            if ( this.ColName.Length > 0 ) {
                ResultSet =
                    this._MyDB.ExecuteStoredProc(
                        this.StoredProcName,
                        this.ColName,
                        this._SqlParams );
                IsInteger = false;
            }


            try {
                HighThreshold = Convert.ToInt32( this.HighThreshold );
                LowThreshold  = Convert.ToInt32( this.LowThreshold );
            } catch ( Exception ) {
                FindExpectedStr = this.ExpectedString.Length > 0;
            }
            Regex MatchExpectedStr = null;
            FindExpectedStr = this.ExpectedString.Length > 0;
            if ( FindExpectedStr )
                MatchExpectedStr = new Regex( this.ExpectedString );

            ArrayList Result  = new ArrayList();
            String MyVal      = String.Empty;
            string Val        = String.Empty;
            for ( int i = 0; i <= ResultSet.Count - 1; i++ ) {
                if ( this.MatchedExpectedString )
                    break;

                ArrayList Row = (ArrayList) ResultSet[i];
                for ( int j = 0; j < Row.Count; j++ ) {

                    string Col     = (string) Row[j];
                    string[] Items = Col.Split( new char[]{ ':' } );

                    object Value = null;

                    if (IsInteger)
                        Value = Convert.ToInt32( Items[1].Trim(null) );
                    else
                        Value = Items[1].Trim(null);


                    if ( this._HighThreshold != -999 &&
                            this._LowThreshold  != -999 ) {
                        if ( (int) Value >= LowThreshold &&
                                (int) Value <= HighThreshold ) {
                            MyVal = String.Format( "{0} - {1} is in range between {2} and {3}",
                                                   Items[0], Value, this.LowThreshold, this.HighThreshold );
                        } else if ( (int) Value <= LowThreshold )
                            MyVal = String.Format( "value {0} of {1} is lower than minmun threshold {2}",
                                                   Value, Items[0], this.LowThreshold );
                        else if ( (int) Value >= HighThreshold )
                            MyVal = String.Format( "value {0} of {1} is above the maximun threshold {2}",
                                                   Value, Items[0], this.HighThreshold );
                        Result.Add( MyVal );
                    }
                    if ( FindExpectedStr ) {
                        HasOtherVal = MyVal.IndexOf( "threshold" ) > -1;
                        Val         = String.Empty;
                        if ( MatchExpectedStr.IsMatch( (string) Value ) ) {
                            Val = String.Format( "Expected string {0} found",
                                                 this.ExpectedString );
                            this._MatchExpectedString = true;
                            Result.Add( Val );
                            break;
                        } else
                            continue;

                    }

                    if ( HasOtherVal )
                        MyVal = String.Format( "{0}:{1}", MyVal, Val );
                }
            }

            IsInteger    = true;
            this._Result = Result;
        }


#region private methods and properties

        private SqlParameter[] GetStoredProcParameters() {
            XmlNodeList SqlParamList = this._StoredProcNode.SelectNodes( "Params" );
            SqlParameter[] SqlParams = null;

            if ( SqlParamList != null ) {
                SqlParams    = new SqlParameter[ SqlParamList.Count ];
                int ParamIdx = 0;
                foreach( XmlNode SqlParam in SqlParamList ) {
                    if ( SqlParam.Name == "Param" &&
                            SqlParam.ParentNode.Name == "SQLTest" ) {
                        XmlAttributeCollection SqlParamAttribs = SqlParam.Attributes;
                        XmlNode SqlParamNameAttrib  = SqlParamAttribs.GetNamedItem( "Name" );
                        XmlNode SqlParamValueAttrib = SqlParamAttribs.GetNamedItem( "Value" );

                        SqlParameter MySqlParam = new SqlParameter();
                        string Message = null;
                        if ( SqlParamNameAttrib == null )
                            Message = @"Name attribute is missing from Param node";
                        else
                            MySqlParam.ParameterName = "@" + SqlParamNameAttrib.Value;

                        if ( SqlParamValueAttrib == null )
                            Message = @"Value attribute is missing from Param Node";
                        else
                            MySqlParam.Value = SqlParamValueAttrib.Value;

                        if ( Message != null )
                            throw new XmlException( Message );

                        SqlParams[ ParamIdx++ ] = MySqlParam;
                    }

                }
            }

            return SqlParams;
        }


        private void SetResultExpectation() {
            XmlNode ExpectedResultNode     = this._StoredProcNode.SelectSingleNode( NODE_NAME );
            XmlAttributeCollection Attribs = ExpectedResultNode.Attributes;

            if ( ExpectedResultNode.Name            == NODE_NAME        &&
                    ExpectedResultNode.ParentNode.Name == PARENT_NODE_NAME ) {
                Type SQLStoredProcType = this.GetType();
                PropertyInfo[] Props = SQLStoredProcType.GetProperties(
                                           BindingFlags.Public     |
                                           BindingFlags.Instance );
                if ( Props.Length > 0 )
                    foreach( PropertyInfo Prop in Props ) {
                    object[] SQLAttributes =
                        Prop.GetCustomAttributes( typeof( SQLAttribute ), false );
                    SQLAttribute MySQLAttrib = null;
                    if ( SQLAttributes.Length > 0 ) {
                        MySQLAttrib = (SQLAttribute) SQLAttributes[0];

                        string AttributeName         = MySQLAttrib.Name;
                        bool   Required              = MySQLAttrib.Required;
                        object AttributeDefaultValue = MySQLAttrib.Default;

                        XmlNode AttribNode = Attribs.GetNamedItem( AttributeName );
                        if ( AttribNode == null )
                            AttribNode =
                                ExpectedResultNode.SelectSingleNode( AttributeName );

                        if ( AttribNode == null && Required )
                            throw new XmlException(
                                String.Format( "{0} is a required attribute", AttributeName ));

                        object[] ObjValues = null;
                        if ( AttribNode == null )
                            ObjValues = new object[]{ AttributeDefaultValue };
                        else {
                            if ( AttribNode.Value != null )
                                ObjValues = new object[]{ AttribNode.Value };
                            else
                                ObjValues = new object[] {
                                                AttribNode.InnerText
                                            };
                        }
                        try {
                            Prop.GetSetMethod().Invoke( this, ObjValues );
                        } catch ( Exception e ) {
                            Console.WriteLine( e.Message );
                        }
                    }
                }
            }
        }

#endregion
    }
}
