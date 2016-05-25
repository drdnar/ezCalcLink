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
            Console.ForegroundColor = ConsoleColor.Cyan;
        }

        public static void Log(string s)
        {
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.Write(s);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void Log(string s, params object[] stuff)
        {
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.Write(s, stuff);
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void Log(int i)
        {
            Log(i.ToString());
        }

        public static void Log(byte b)
        {
            Log(b.ToString("X2"));
        }

        public static void Log(char c)
        {
            Log(c.ToString());
        }

        public static void LogLine(string s)
        {
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.WriteLine(s);
            NewLine = true;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void LogLine(string s, params object[] stuff)
        {
            ConsoleColor bg = Console.BackgroundColor;
            ConsoleColor fg = Console.ForegroundColor;
            SetDebugColors();
            CheckPrintIndent();
            Console.WriteLine(s, stuff);
            NewLine = true;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }

        public static void LogLine(int i)
        {
            LogLine(i.ToString());
        }

        public static void LogLine(byte b)
        {
            LogLine(b.ToString("X2"));
        }

        public static void LogLine(char c)
        {
            LogLine(c.ToString());
        }

    }
}
