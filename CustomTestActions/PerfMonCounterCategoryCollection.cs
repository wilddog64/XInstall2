using System;
using System.Collections;


namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for PerfMonCounterCategoryCollection.
    /// </summary>
    public class PerfMonCounterCategoryCollection : ICollection {
        private ArrayList _PerfMonCounterCounterArray = new ArrayList();

        public PerfMonCounterCategoryCollection() {}

        public void Add( PerfMonCounterCategory MyCounterCategory ) {
            this._PerfMonCounterCounterArray.Add( MyCounterCategory );
        }

        public PerfMonCounterCategory this[ string CounterCategoryName ]
        {
            get {
                PerfMonCounterCategory MyPerfMonCounterCategory = null;
                for ( int i = 0; i < this._PerfMonCounterCounterArray.Count; i++ ) {
                    MyPerfMonCounterCategory =
                        (PerfMonCounterCategory) this._PerfMonCounterCounterArray[i];
                    if ( MyPerfMonCounterCategory.CategoryName == CounterCategoryName )
                        break;
                }

                return MyPerfMonCounterCategory;
            }

        }


#region ICollection Members

        public bool IsSynchronized
        {
            get {
                return this._PerfMonCounterCounterArray.IsSynchronized;
            }
        }

        public int Count
        {
            get {
                return this._PerfMonCounterCounterArray.Count;
            }
        }

        public void CopyTo(Array array, int index) {
            this._PerfMonCounterCounterArray.CopyTo( array, index );
        }

        public object SyncRoot
        {
            get {
                return this._PerfMonCounterCounterArray.SyncRoot;
            }
        }

#endregion

#region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return this._PerfMonCounterCounterArray.GetEnumerator();
        }

#endregion
    }
}
