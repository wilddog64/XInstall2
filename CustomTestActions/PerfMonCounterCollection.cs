using System;
using System.Collections;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for PerfMonCounterCollection.
    /// </summary>
    public class PerfMonCounterCollection : ICollection {
        private ArrayList _PerfMonCounterList = new ArrayList();

        public PerfMonCounterCollection() {}

        public void Add( PerfMonCounter Counter ) {
            this._PerfMonCounterList.Add( Counter );
        }


        public PerfMonCounter this[ string CounterName ]
        {
            get {
                PerfMonCounter ThisPerfMonCounter = null;
                for ( int i = 0; i < this._PerfMonCounterList.Count; i++ ) {
                    ThisPerfMonCounter = (PerfMonCounter) this._PerfMonCounterList[ i ];
                    if (ThisPerfMonCounter.CounterName == CounterName)
                        break;
                }

                return ThisPerfMonCounter;
            }

        }


#region ICollection Members

        public bool IsSynchronized
        {
            get {
                return this._PerfMonCounterList.IsSynchronized;
            }
        }


        public int Count
        {
            get {
                return this._PerfMonCounterList.Count;
            }
        }


        public void CopyTo(Array array, int index) {
            this._PerfMonCounterList.CopyTo( array, index );
        }


        public object SyncRoot
        {
            get {
                return this._PerfMonCounterList.SyncRoot;
            }
        }

#endregion

#region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return this._PerfMonCounterList.GetEnumerator();
        }


#endregion
    }
}
