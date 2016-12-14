using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace Windows
{

    class GuiConsole 
    { 
        public static void CreateConsole() 
        { 
                if (hasConsole) 
                        return; 
                if (oldOut == IntPtr.Zero) 
                        oldOut = GetStdHandle( -11 ); 
                if (! AllocConsole()) 
                        throw new Exception("AllocConsole() failed"); 
                conOut = CreateFile( "CONOUT$", 0x40000000, 2, IntPtr.Zero, 3, 0, IntPtr.Zero ); 
                if (! SetStdHandle(-11, conOut)) 
                        throw new Exception("SetStdHandle() failed"); 
                StreamToConsole(); 
                hasConsole = true; 
        } 
        public static void ReleaseConsole() 
        { 
                if (! hasConsole) 
                        return; 
                if (! CloseHandle(conOut)) 
                        throw new Exception("CloseHandle() failed"); 
                conOut = IntPtr.Zero; 
                if (! FreeConsole()) 
                        throw new Exception("FreeConsole() failed"); 
                if (! SetStdHandle(-11, oldOut)) 
                        throw new Exception("SetStdHandle() failed"); 
                StreamToConsole(); 
                hasConsole = false; 
        } 
        private static void StreamToConsole() 
        { 
                Stream cstm = Console.OpenStandardOutput(); 
                StreamWriter cstw = new StreamWriter( cstm, Encoding.Default ); 
                cstw.AutoFlush = true; 
                Console.SetOut( cstw ); 
                Console.SetError( cstw ); 
        } 
        private static bool hasConsole = false; 
        private static IntPtr conOut; 
        private static IntPtr oldOut; 
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool AllocConsole(); 
        [DllImport("kernel32.dll", SetLastError=false)]
        private static extern bool FreeConsole(); 
        [DllImport("kernel32.dll", SetLastError=true)] 
        private static extern IntPtr GetStdHandle( int nStdHandle ); 
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hConsoleOutput); 
        [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        private static extern IntPtr CreateFile( 
                string          fileName, 
                int                     desiredAccess, 
                int                     shareMode, 
                IntPtr          securityAttributes, 
                int                     creationDisposition, 
                int                     flagsAndAttributes, 
                IntPtr          templateFile ); 
        [DllImport("kernel32.dll", ExactSpelling=true, SetLastError=true)]
        private static extern bool CloseHandle(IntPtr handle); 
    } 
}