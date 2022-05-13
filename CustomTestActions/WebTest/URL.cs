using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using XInstall.Core;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for URL.
    /// </summary>
    public class URL {
        private bool   _ExpectedStringFound        = false;
        private bool   _ResponseTimeAboveThreshold = false;
        private bool   _Enable                     = true;

        private double _ExpectResponseTime  = 0.0;
        private double _MeasuredResponseTime = 0.0;

        private string _URLString      = string.Empty;
        private string _ExpectString   = string.Empty;
        private string _ResponseTime   = string.Empty;
        private string _REExpectString = string.Empty;
        private string _DisplayName    = string.Empty;
        private string _Method         = @"GET";

        public URL( XmlNode ActionNode ) {
            this.ProcessURLNode( ActionNode );
        }


        public URL() {}


        [URL("URLString", Required=true)]
        public string URLString
        {
            get {
                return this._URLString;
            }
            set {
                this._URLString = value;
            }
        }


        [URL("ExpectedString", Required=false)]
        public string ExpectedString
        {
            get {
                return this._ExpectString;
            }
            set {
                this._ExpectString = value;
            }
        }


        [URL("REExpectedString", Required=false)]
        public string REExpectedString
        {
            get {
                return this._REExpectString;
            }
            set {
                this._REExpectString = value;
            }
        }


        [URL("ResponseTime", Required=false)]
        public string ResponseTime
        {
            get {
                return this._ResponseTime;
            }
            set {
                this._ResponseTime       = value;
                if ( this._ResponseTime.Length > 0 )
                    this._ExpectResponseTime = Convert.ToDouble( this._ResponseTime );
            }
        }


        [URL("Method", Required=false, Default="GET")]
        public string Method
        {
            get {
                return this._Method;
            }
            set {
                this._Method = value;
            }
        }


        [URL("Enable", Required=false, Default="true")]
        public string Enable
        {
            set {
                this._Enable = bool.Parse( value );
            }
        }


        [URL("DisplayName", Required=false, Default="")]
        public string DisplayName
        {
            get {
                return this._DisplayName;
            }
            set {
                this._DisplayName = value;
            }
        }

        public bool FoundExpectedString
        {
            get {
                return this._ExpectedStringFound;
            }
            set {
                this._ExpectedStringFound = value;
            }
        }


        public bool ResponseTimeAboveThreshold
        {
            get {
                return this._ResponseTimeAboveThreshold;
            }
        }


        public double MeasuredResponseTime
        {
            get {
                return this._MeasuredResponseTime;
            }
        }

        public void FetchURL() {
            if ( !this._Enable )
                return;

            try {
                DateTime BeginRequest = DateTime.Now;
                HttpWebRequest MyWebRequest   =
                    (HttpWebRequest) WebRequest.Create( this.URLString );
                MyWebRequest.Method           = this.Method;
                HttpWebResponse MyWebResponse = (HttpWebResponse) MyWebRequest.GetResponse();

                DateTime EndRequest = DateTime.Now;

                TimeSpan t1 = EndRequest - BeginRequest;
                this._MeasuredResponseTime = t1.TotalSeconds;
                if ( t1.TotalSeconds > this._ExpectResponseTime )
                    this._ResponseTimeAboveThreshold = true;

                Regex RE = null;
                if ( this.ExpectedString.Length > 0 ||
                        this.REExpectedString.Length > 0 ) {
                    bool UseRE = ( this.ExpectedString.Length   == 0 &&
                                   this.REExpectedString.Length > 0 );

                    if ( UseRE )
                        RE = new Regex( this.REExpectedString );

                    Stream s = MyWebResponse.GetResponseStream();
                    using( StreamReader sr = new StreamReader( s, Encoding.Default ) ) {
                        while ( sr.Peek() > -1 ) {
                            string Line = sr.ReadLine();
                            if (UseRE) {
                                if ( RE.IsMatch( Line ) ) {
                                    this.FoundExpectedString = true;
                                    break;
                                }
                            } else {
                                if ( Line.IndexOf( this.ExpectedString ) > -1 ) {
                                    this.FoundExpectedString = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            } catch ( WebException ) {
                throw;
            }
        }


#region private methods

        private void ProcessURLNode( XmlNode ActionNode ) {
            if ( ActionNode.Name == "URL" &&
                    ActionNode.ParentNode.Name == "WebTest" ) {
                Type URLType = this.GetType();

                XmlAttributeCollection URLNodeAttribs = ActionNode.Attributes;
                PropertyInfo[] PropertyInfos =
                    URLType.GetProperties( BindingFlags.Public |
                                           BindingFlags.Instance );
                bool ThrowException = false;
                foreach( PropertyInfo pi in PropertyInfos ) {
                    object[] URLAttributes = pi.GetCustomAttributes( typeof( URLAttribute ), false );
                    if ( URLAttributes.Length > 0 ) {
                        URLAttribute MyURLAttrib = (URLAttribute) URLAttributes[0];
                        XmlNode URLNodeAttrib    = URLNodeAttribs.GetNamedItem( MyURLAttrib.Name );
                        string ValueString       = String.Empty;
                        if ( URLNodeAttrib != null )
                            ValueString = URLNodeAttrib.Value;
                        else {
                            XmlNode URLNode = ActionNode.SelectSingleNode( MyURLAttrib.Name );
                            if ( URLNode != null ) {
                                ValueString = URLNode.InnerText;
                                if ( ValueString.Length > 0 )
                                    ValueString = URLNode.InnerText;
                                else if ( ValueString.Length == 0       &&
                                          MyURLAttrib.Required == false &&
                                          MyURLAttrib.Default.Length > 0 )
                                    ValueString = MyURLAttrib.Default;
                                else if ( ValueString.Length == 0 &&
                                          MyURLAttrib.Required       ) {
                                    ThrowException = true;
                                }
                            } else if ( !MyURLAttrib.Required            &&
                                        URLNode == null                 &&
                                        MyURLAttrib.Default.Length > 0 ) {
                                ValueString = MyURLAttrib.Default;
                            } else if ( MyURLAttrib.Required && URLNode == null)
                                ThrowException = true;

                            if ( ThrowException ) {
                                ThrowException = false;
                                throw new ArgumentNullException( MyURLAttrib.Name,
                                                                 String.Format( "this is required attribute" ) );
                            }


                            pi.GetSetMethod().Invoke( this, new object[]{ ValueString } );
                        }
                    }
                }
            }
        }


#endregion
    }
}
