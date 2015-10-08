using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace Microsoft.Fast
{
    public enum LogLevel { None, Minimal, Normal, Maximal}

    public class FastLog
    {
        TextWriter tw = Console.Out;
        LogLevel lv = LogLevel.Normal;

        public FastLog() { }
        public FastLog(TextWriter tw) { this.tw = tw; }

        public void setLogLevel(LogLevel lv)
        {
            this.lv = lv;
        }

        public void SetTextWriter(TextWriter textw)
        {
            tw = textw;
        }

        public void WriteLog(LogLevel lv, string s)
        {            
            if(this.lv>=lv)
                tw.WriteLine(s);
        }

        public void Reset()
        {
            tw.Flush();           
        }
    }
}
