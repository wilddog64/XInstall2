using System;
using System.Collections;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for SQLTestCollection.
    /// </summary>
    public class SQLTestCollection : ICollection {
        private ArrayList _SQLTestArray = new ArrayList();

        public SQLTestCollection() {}


        public void Add( SQLTest MySQLTest ) {
            this._SQLTestArray.Add( MySQLTest );
        }

        public SQLTest this[ String DBServerName ]
        {
            get {
                SQLTest ThisSQLTest = null;
                for ( int i = 0; i < this._SQLTestArray.Count - 1; i++ ) {
                    ThisSQLTest = (SQLTest) this._SQLTestArray[i];
                    if (ThisSQLTest.DBServer == DBServerName)
                        break;
                }

                return ThisSQLTest;
            }

        }

#region ICollection Members

        public bool IsSynchronized
        {
            get {
                return this._SQLTestArray.IsSynchronized;
            }
        }


        public int Count
        {
            get {
                return this._SQLTestArray.Count;
            }
        }


        public void CopyTo(Array array, int index) {
            this._SQLTestArray.CopyTo( array, index );
        }


        public object SyncRoot
        {
            get {
                return this._SQLTestArray.SyncRoot;
            }
        }


#endregion

#region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return this._SQLTestArray.GetEnumerator();
        }


#endregion
    }
}
