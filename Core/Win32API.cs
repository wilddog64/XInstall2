using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for Win32API.
    /// </summary>
    public class Win32API {
        private Win32API() {
            // a private constructor here to prevent user
            // to instanicate the static class
        }

#region Win32 Declaration/and Dllimport functions
        [DllImport("kernel32", EntryPoint="GetShortPathName")]
        private static extern UInt32 GetShortPathName(
                string        strLongPath,
                StringBuilder sbShortPathName,
                UInt32        uiBufferSize );

        [DllImport("kerenl32", EntryPoint="GetLastError")]
        private static extern UInt32 GetLastError();

        [DllImport("User32.DLL",EntryPoint="SendMessage")]
        private static extern int SendMessage(
                IntPtr hWnd,
                UInt32 Msg,
                Int32 wParam,
                Int32 lParam );

        [DllImport("User32.DLL", EntryPoint="RegisterWindowMessage")]
        private static extern UInt32 RegisterWindowMessage( StringBuilder lpString );

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow( IntPtr hWnd );

        [DllImport("User32.dll")]
        private static extern bool SetActiveWindow( IntPtr hWnd );
#endregion

#region public static methods
        public static UInt32 GetShortPathName( string strLongPathName, out string strShortPathName ) {

            StringBuilder sbShortPathName = new StringBuilder(strLongPathName.Length);

            if ( !Directory.Exists( Path.GetDirectoryName(strLongPathName) ) )
                throw new System.IO.DirectoryNotFoundException(
                    String.Format( "Directory {0} does not exist!", strLongPathName ) );
            UInt32 uiRC = Win32API.GetShortPathName(
                              strLongPathName, sbShortPathName,
                              (UInt32) strLongPathName.Length
                          );
            strShortPathName = sbShortPathName.Length > 0 ?
                               sbShortPathName.ToString() :
                               strLongPathName;
            return uiRC;
        }


        public static bool ForegroundWindow( IntPtr hWnd ) {
            return SetActiveWindow( hWnd );
        }


        public static bool ActivateWindow( IntPtr hWnd ) {
            return SetActiveWindow( hWnd );
        }

#endregion
    }
}
