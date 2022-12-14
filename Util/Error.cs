using System;
using System.Collections;
using System.Collections.Specialized;

namespace XInstall.Util {
    public enum LEVEL {
        INFORMATION = 0,
        WARNING,
        ERROR,
        FATAL,
    }

    public enum SERVIRITY {
        NORMAL = 0,
        WARNING,
        ERROR,
        FATAL,
    }


    /// <summary>
    /// This class is used to stored error information
    /// </summary>
    public class Error {
        private LEVEL     _Level        = LEVEL.INFORMATION;
        private SERVIRITY _Servirty     = SERVIRITY.NORMAL;
        private DateTime  _TimeStamp    = DateTime.Now;

        private string    _ObjectName   = string.Empty;
        private int       _ErrorID      = -1;
        private string    _ErrorMessage = string.Empty;

        public Error( String ObjectName, int ErrorID, string ErrorMessage, SERVIRITY servirity ) {
            this._ObjectName   = ObjectName;
            this._ErrorID      = ErrorID;
            this._ErrorMessage = ErrorMessage;
            this._Servirty     = servirity;
        }


        public Error( DateTime  TimeStamp, string ObjectName, LEVEL l, SERVIRITY Servirity, string Message ) {
            this._TimeStamp    = TimeStamp;
            this._ObjectName   = ObjectName;
            this._Level        = Level;
            this._Servirty     = Servirity;
            this._ErrorMessage = Message;
        }


        public Error( string ObjectName, LEVEL Level, SERVIRITY Servirity, string Message ) {
            this._ObjectName   = ObjectName;
            this._Level        = Level;
            this._Servirty     = Servirity;
            this._ErrorMessage = Message;
        }


        public string TimeStamp
        {
            get { return this._TimeStamp.ToString(); }
        }


        public string ObjectName
        {
            get { return this._ObjectName; }
        }


        public LEVEL Level
        {
            get { return this._Level; }
        }


        public int ErrorID
        {
            get { return this._ErrorID; }
        }


        public string ErrorMessage
        {
            get { return this._ErrorMessage; }
        }


        public SERVIRITY Servirity
        {
            get { return this._Servirty; }
        }


        public override string ToString() {
            string OneLine = String.Format( "[{0}]: Level {1}, Serverity {2}, Object Name {3} - Msg {4}",
                               this.TimeStamp, this.Level.ToString(), this.Servirity.ToString(),
                               this.ObjectName, this.ErrorMessage );

            return OneLine;
        }

    }
}
