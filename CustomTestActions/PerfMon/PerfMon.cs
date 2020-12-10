using System;
using System.Collections;
using System.Xml;

using XInstall.Core;
using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class PerfMon : ActionElement {

        private PerfMonCounterCategoryCollection _PerfMonCounterCategories =
            new PerfMonCounterCategoryCollection();

        private string _DisplayName = string.Empty;

        [Action("PerfMon")]
        public PerfMon( XmlNode ActionNode ) : base( ActionNode ) {
            try {
                foreach ( XmlNode CounterCategoryNode in ActionNode.ChildNodes ) {
                    PerfMonCounterCategory MyPerfMonCounterCategory =
                        new PerfMonCounterCategory( CounterCategoryNode );
                    this._PerfMonCounterCategories.Add( MyPerfMonCounterCategory );
                }
            } catch ( Exception e ) {
                base.FatalErrorMessage( ".",
                                        String.Format( "{0}: PerfMon Error {1}",
                                                       this.ObjectName, e.Message ),
                                        1660 );
            }
        }


        [Action("runnable", Needed=false, Default="true")]
        public new string Runnable
        {
            set {
                base.Runnable = bool.Parse( value );
            }
        }


        [Action("skiperror", Needed=false, Default="false")]
        public new string SkipError
        {
            set {
                base.SkipError = bool.Parse( value );
            }
        }


        [Action("displayName", Needed=false, Default="")]
        public string DisplayName
        {
            get {
                return this._DisplayName;
            }
            set {
                this._DisplayName = value;
            }
        }


        public PerfMonCounterCategoryCollection PerfMonCounterCategories
        {
            get {
                return this._PerfMonCounterCategories;
            }
        }


#region protected properties

        protected override object ObjectInstance
        {
            get {
                return this;
            }
        }


        protected override string ObjectName
        {
            get {
                return this.GetType().Name;
            }
        }


#endregion

#region protected methods
        protected override void ParseActionElement() {
            base.ParseActionElement ();
            foreach( PerfMonCounterCategory CounterCategory in
                     this._PerfMonCounterCategories ) {
                string MachineName = CounterCategory.MachineName;
                string CounterCategoryName = CounterCategory.CategoryName;
                PerfMonCounterCollection PerfMonCounters = CounterCategory.PerfMonCounters;
                foreach (PerfMonCounter MyPerfMonCounter in PerfMonCounters) {
                    if ( MyPerfMonCounter.CounterEnable )
                        base.LogItWithTimeStamp(
                            String.Format("{0}: {1}/{2}/{3} - {4}",
                                          this.ObjectName,
                                          MachineName,
                                          CounterCategoryName,
                                          MyPerfMonCounter.CounterName,
                                          MyPerfMonCounter.CounterValue) );
                }
            }
        }
#endregion
    }
}