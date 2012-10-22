using System;
using System.Collections;
using System.Diagnostics;
using System.DirectoryServices;
using System.Xml;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Class that interfaces with Performance Monitor
    /// </summary>
    public class PerfMon : ActionElement {
        private bool _ListCategories          = false;
        private bool _ListCountersForCategory = false;

        private string _MachineName  = String.Empty;
        private string _CategoryName = String.Empty;
        private string _CounterName  = String.Empty;

        [Action("perfmon")]
        public PerfMon( XmlNode ActionNode ) : base( ActionNode ) {}

        [Action("machinename", Needed=false, Default=".")]
        public string MachineName
        {
            get {
                return this._MachineName;
            }
            set {
                this._MachineName = value;
            }
        }


        [Action("categoryname", Needed=false, Default="")]
        public string CategoryName
        {
            get {
                return this._CategoryName;
            }
            set {
                this._CategoryName = value;
            }
        }


        [Action("countername", Needed=false, Default="")]
        public string CounterName
        {
            get {
                return this._CounterName;
            }
            set {
                this._CounterName = value;
            }
        }


        [Action("listcountercategories", Needed=false, Default="false")]
        public string ListCounterCategory
        {
            get {
                return this._ListCategories.ToString();
            }
            set {
                this._ListCategories = bool.Parse( value );
            }
        }


        [Action("listcounterforcategory", Needed=false, Default="false")]
        public string ListCounterForCategory
        {
            get {
                return this._ListCountersForCategory.ToString();
            }
            set {
                this._ListCountersForCategory = bool.Parse(value);
            }
        }


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


        protected override void ParseActionElement() {
            base.ParseActionElement ();

            if ( this.MachineName.Length > 0 ) {
                base.LogItWithTimeStamp(
                    String.Format( "{0}: Get PerfMon info from {1}",
                                   this.ObjectName, this.MachineName ) );

                if ( this._ListCategories == true ) {
                    PerformanceCounterCategory[] PerfCountCategories =
                        PerformanceCounterCategory.GetCategories( this.MachineName );

                    if ( PerfCountCategories != null &&
                            PerfCountCategories.Length > 0 )
                        foreach( PerformanceCounterCategory PerCountCategory in
                                 PerfCountCategories ) {
                        base.LogItWithTimeStamp(
                            String.Format( "{0}: {1} - Perfmon Category {2}",
                                           this.ObjectName,
                                           this.MachineName,
                                           PerCountCategory.CategoryName) );
                    }
                } else if ( this._ListCountersForCategory == true &&
                            this.CategoryName.Length       > 0 ) {
                    if ( PerformanceCounterCategory.Exists(
                                this.CategoryName,
                                this.MachineName ) ) {
                        PerformanceCounterCategory PerfCounterCate =
                            new PerformanceCounterCategory( this.CategoryName, this.MachineName );
                        PerformanceCounter[] PerformanceCounters = PerfCounterCate.GetCounters();
                        string[] CounterInstanceNames = PerfCounterCate.GetInstanceNames();
                        foreach( PerformanceCounter PerformanceCounter in PerformanceCounters ) {
                            base.LogItWithTimeStamp(
                                String.Format( "{0}: {1} - Perfmon Counter {2}",
                                               this.ObjectName,
                                               this.MachineName,
                                               PerformanceCounter.CounterName) );
                        }
                    }
                }
            } else {
                // foreach ( XmlNode Node in base.ActionNode.
            }
        }


    }
}
