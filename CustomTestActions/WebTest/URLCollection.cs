using System;
using System.Collections;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for URLCollection.
    /// </summary>
    public class URLCollection : ICollection, IEnumerable {
        private ArrayList _URLList = new ArrayList();
        public URLCollection() {
            //
            // TODO: Add constructor logic here
            //
        }

        public void Add( URL MyURL ) {
            this._URLList.Add( MyURL );
        }

        public URL this[ string URLString ]
        {
            get {
                URL MyURL = null;
                for ( int i = 0; i < this._URLList.Count; i++ ) {
                    MyURL = (URL) this._URLList[i];
                    if (MyURL.URLString == URLString)
                        break;
                }

                return MyURL;
            }
        }

#region ICollection Members

        public bool IsSynchronized
        {
            get {
                return this._URLList.IsSynchronized;
            }
        }

        public int Count
        {
            get {
                return this._URLList.Count;
            }
        }

        public void CopyTo(Array array, int index) {
            this._URLList.CopyTo( array, index );
        }

        public object SyncRoot
        {
            get {
                return this._URLList.SyncRoot;
            }
        }

#endregion

#region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return this._URLList.GetEnumerator();
        }

#endregion
    }
}
