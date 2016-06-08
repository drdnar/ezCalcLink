using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    public class DebugLogger
    {
        public static int IndentLevel;

        public static LogType ExcludeLogs = LogType.ExcludeVeryVeryVerbose; //| */LogType.P2 | LogType.P3 | LogType.P4;
        
        [Flags]
        public enum LogType
        {
            Basic = 1,
            ExcludeBasic = 0xF,
            Verbose = 3,
            ExcludeVerbose = 0xE,
            VeryVerbose = 7,
            ExcludeVeryVerbose = 0xC, 
            VeryVeryVerbose = 15,
            ExcludeVeryVeryVerbose = 0x8,
            P0 = 0x10,
            P1 = 0x20,
            P2 = 0x40,
            P3 = 0x80,
            P4 = 0x100,
            P5 = 0x200,
            P6 = 0x400,
            P7 = 0x800,
            FieldHeader = 0x1000,
            FieldValue = 0x2000,
            FileHeader = 0x4000,
            Error = 0x8000,
            FatalError = 0x10000,
        }

        public static LogType CurrentLogType = LogType.Basic | LogType.FieldHeader;

        public static void LogMaskChange(LogType set, LogType reset)
        {
            CurrentLogType &= ~reset;
            CurrentLogType |= set;
        }

        public static void LogMaskSet(LogType set)
        {
            CurrentLogType |= set;
        }

        public static void LogMaskReset(LogType reset)
        {
            CurrentLogType &= ~reset;
        }

        public static bool LogIsExcluded()
        {
            return (int)(CurrentLogType & ExcludeLogs) != 0;
        }

        public static bool MaskContains(LogType t)
        {
            return (int)(CurrentLogType & t) != 0;
        }

        public static void Indent()
        {
            IndentLevel++;
        }

        public static void Unindent()
        {
            IndentLevel--;
        }

        public static bool NewLine = false;

        public static void PrintIndent()
        {
            for (int i = IndentLevel; i > 0; i--)
                Console.Write("  ");
            NewLine = false;
        }


        public static void CheckPrintIndent()
        {
            if (NewLine)
                PrintIndent();
        }

        public static void SetDebugColors()
        {
            if (MaskContains(LogType.Error) || MaskContains(LogType.FatalError))
                Console.ForegroundColor = ConsoleColor.Red;
            else if (MaskContains(LogType.ExcludeVeryVeryVerbose))
                Console.ForegroundColor = ConsoleColor.DarkGray;
            else if (MaskContains(LogType.ExcludeVeryVerbose))
                Console.ForegroundColor = ConsoleColor.DarkCyan;
            else if (MaskContains(LogType.ExcludeVerbose))
                Console.ForegroundColor = ConsoleColor.Cyan;
            else
                Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Log(string s)
        {
            if (LogIsExcluded())
                return;
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.Write(s);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void Log(LogType l, string s)
        {
            CurrentLogType = l;
            Log(s);
        }

        public static void Log(LogType l, string s, params object[] stuff)
        {
            CurrentLogType = l;
            Log(s, stuff);
        }

        public static void Log(string s, params object[] stuff)
        {
            if (LogIsExcluded())
                return;
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.Write(s, stuff);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void LogLine(LogType l)
        {
            CurrentLogType = l;
            LogLine();
        }

        public static void LogLine()
        {
            if (LogIsExcluded())
                return;
            Console.WriteLine();
            NewLine = true;
        }

        public static void LogLine(LogType l, string s)
        {
            CurrentLogType = l;
            LogLine(s);
        }

        public static void LogLine(string s)
        {
            if (LogIsExcluded())
                return;
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.WriteLine(s);
            NewLine = true;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void LogLine(LogType l, string s, params object[] stuff)
        {
            CurrentLogType = l;
            LogLine(s, stuff);
        }

        public static void LogLine(string s, params object[] stuff)
        {
            if (LogIsExcluded())
                return;
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.WriteLine(s, stuff);
            NewLine = true;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }
        
        
    }
}
