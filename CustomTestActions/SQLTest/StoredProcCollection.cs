using System;
using System.Collections;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for StoredProcCollection.
    /// </summary>
    public class StoredProcCollection : ICollection {
        private ArrayList _StoredProcs = new ArrayList();

        public StoredProcCollection() {}


#region ICollection Members

        public bool IsSynchronized
        {
            get {
                return this._StoredProcs.IsSynchronized;
            }
        }


        public int Count
        {
            get {
                return this._StoredProcs.Count;
            }
        }


        public void CopyTo(Array array, int index) {
            this._StoredProcs.CopyTo( array, index );
        }


        public object SyncRoot
        {
            get {
                return this._StoredProcs.SyncRoot;
            }
        }


#endregion

#region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return this._StoredProcs.GetEnumerator();
        }

#endregion

        public void Add( StoredProc ThisStoredProc ) {
            this._StoredProcs.Add( ThisStoredProc );
        }


        public StoredProc this[ string StoredProcName ]
        {
            get {
                StoredProc ThisStoredProc = null;
                for ( int i = 0; i < this._StoredProcs.Count - 1; i++ ) {
                    ThisStoredProc = (StoredProc) this._StoredProcs[ i ];
                    if ( ThisStoredProc.StoredProcName == StoredProcName )
                        break;
                }

                return ThisStoredProc;
            }
        }
    }
}
