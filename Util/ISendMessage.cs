using System;
using System.Text;

namespace XInstall.Util {
    public interface ISendLogMessage {
        void SendLogMessage( Error AnError );
    }
}
