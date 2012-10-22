using System;
using System.Net;
using System.Xml;

using XInstall.Core;
using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for WebTest.
    /// </summary>
    public class WebTest : ActionElement {
        private URLCollection _URLList = new URLCollection();
        private bool _AsyncRun         = false;

#region public constructors
        [Action("WebTest")]
        public WebTest( XmlNode ActionNode ) : base( ActionNode ) {
            base.ParseActionElement();
            XmlNode WebTestNode = base.ActionNode;
            foreach ( XmlNode URLNode in WebTestNode.ChildNodes ) {
                URL MyURL = new URL( URLNode );
                this._URLList.Add( MyURL );
            }

        }
#endregion

        [Action("runnable", Needed=false, Default="true")]
        public new string Runnable
        {
            set {
                base.Runnable = bool.Parse( value );
            }
        }


        [Action("AsyncRun", Needed=false, Default="false")]
        public string AsyncRun
        {
            set {
                this._AsyncRun = bool.Parse( value );
            }
        }


        public URLCollection URLList
        {
            get {
                return this._URLList;
            }
        }


#region protected properties

        protected override string ObjectName
        {
            get {
                return this.GetType().Name;
            }
        }


        protected override object ObjectInstance
        {
            get {
                return this;
            }
        }


#endregion

#region protected methods

        protected override void ParseActionElement() {
            ProcessURLList();
        }


#endregion

        private void ProcessURLList() {
            foreach( URL MyURL in this._URLList ) {
                MyURL.FetchURL();

                if ( MyURL.ResponseTime.Length > 0 ) {
                    if ( MyURL.ResponseTimeAboveThreshold )
                        base.LogItWithTimeStamp(
                            String.Format(
                                "{0}: Response time is over threshold, expect time is {1} and response time is {2}",
                                this.ObjectName, MyURL.ResponseTime, MyURL.MeasuredResponseTime) );
                    else
                        base.LogItWithTimeStamp(
                            String.Format(
                                "{0}: Reponse time for {1} is {2}",
                                this.ObjectName, MyURL.URLString, MyURL.MeasuredResponseTime ) );
                } else {
                    base.LogItWithTimeStamp(
                        String.Format( "{0}: Response Time for {1} is {2}",
                                       this.ObjectName, MyURL.URLString, MyURL.MeasuredResponseTime ) );
                }

                if ( MyURL.ExpectedString.Length > 0 ) {
                    string Message = String.Empty;
                    if ( MyURL.FoundExpectedString )
                        Message = String.Format( "{0}: Expected string '{1}' found for URL {2}",
                                                 this.ObjectName, MyURL.ExpectedString, MyURL.URLString );
                    else
                        Message = String.Format( "{0}: Cannot find expected string '{1}' for url {2}",
                                                 this.ObjectName, MyURL.ExpectedString, MyURL.URLString );
                    base.LogItWithTimeStamp( Message );
                }
            }

        }
    }
}
