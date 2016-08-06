using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    class ArgumentsParser
    {
        public class ArgumentsParseException : Exception
        {
            public ArgumentsParseException ()
            {
                
            }

            public ArgumentsParseException (string message)
                : base(message)
            {
                
            }

            public ArgumentsParseException(string message, Exception inner)
                : base(message, inner)
            {
                
            }
        }


        public static string[] InFiles;

        public enum LinkerMode
        {
            NotSpecified,
            Static8xp,
            LibLoad8xp,
            App,
        }

        static public LinkerMode Mode;

        public static bool UseLibLoad = false;

        public static string OutputFileName;


        public static void ParseArguments(string[] args)
        {
            try
            {
                ArgumentsParser a = new ArgumentsParser(args);
                if (!a.haveSetMode)
                    throw new ArgumentsParseException("Did not set output mode.");
                if (OutputFileName == null)
                    throw new ArgumentsParseException("Did not set output file name.");
            }
            catch (ArgumentsParseException e)
            {
                Console.Write("Error parsing arguments: ");
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        string[] args;
        int arg;

        private ArgumentsParser(string[] args)
        {
            this.args = args;
            for (arg = 0; arg < args.Length; arg++)
            {
                if (args[arg] == "--8xp")
                    SetMode8xp();
                else if (args[arg] == "--app")
                    SetModeApp();
                else if (args[arg] == "--makeLib" || args[arg] == "--makelib")
                    SetModeLibLoadLib();
                else if (args[arg] == "--libLoad" || args[arg] == "--LibLoad" || args[arg] == "--libload")
                    SetUseLibLoad();
                else if (args[arg] == "--outfile" || args[arg] == "--outFile")
                    SetOutFile();
                else if (args[arg] == "--staticsection" || args[arg] == "--staticSection")
                    SetStaticSection();

            }
        }

        private void SetStaticSection()
        {
            
        }

        bool haveSetMode = false;
        void SetMode8xp()
        {
            if (haveSetMode)
                throw new ArgumentsParseException("Set mode 8xp: Already have mode specifier.");
            haveSetMode = true;
            Mode = LinkerMode.Static8xp;
        }

        void SetModeLibLoadLib()
        {
            if (haveSetMode)
                throw new ArgumentsParseException("Set mode make LibLoad library: Already have mode specifier.");
            haveSetMode = true; 
            Mode = LinkerMode.LibLoad8xp;
        }

        void SetModeApp()
        {
            if (haveSetMode)
                throw new ArgumentsParseException("Set mode app: Already have mode specifier.");
            haveSetMode = true;
            Mode = LinkerMode.App;
        }

        bool haveSetUseLibLoad = false;
        void SetUseLibLoad()
        {
            if (haveSetUseLibLoad)
                throw new ArgumentsParseException("Set use of LibLoad: Already specified.");
            haveSetUseLibLoad = true;
            UseLibLoad = true;
        }

        void SetOutFile()
        {
            if (OutputFileName != null)
                throw new ArgumentsParseException("Set output file: Name already set.");
            OutputFileName = args[++arg];
        }
    }
}
