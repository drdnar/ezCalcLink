using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
namespace EzCalcLink.LibLoad
{
    /// <summary>
    /// LibLoad Stub file. This contains information about the functions in a LibLoad library.
    /// </summary>
    public class LlsFile
    {
        /// <summary>
        /// List of all sections, and all key-value pairs in each section.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Sections = new Dictionary<string, Dictionary<string, string>>();

        public Dictionary<string, string> HeaderData = new Dictionary<string,string>();
        public Dictionary<string, string> FunctionsData;

        public List<string> ExternalFunctionNames = new List<string>();
        public List<string> InternalFunctionNames = new List<string>();

        public byte LibraryVersion;
        public string LibraryName;

        protected string CurrentSection = "";
        protected static Regex SectionHeaderExp = new Regex(@"\[(?<sname>\w+)\]");
        protected static Regex KeyValueExp = new Regex(@"(?<key>\w+)\s*=\s*(?<value>\w*)");

        public LlsFile(string path)
        {
            Sections[""] = new Dictionary<string, string>();
            string[] text = System.IO.File.ReadAllLines(path);
            foreach (var s in text)
            {
                string ss = StripComments(s);
                if (SectionHeaderExp.IsMatch(ss))
                {
                    CurrentSection = SectionHeaderExp.Match(ss).Groups["sname"].Value.ToLower();
                    if (!Sections.ContainsKey(CurrentSection))
                        Sections.Add(CurrentSection, new Dictionary<string, string>());
                }
                else if (KeyValueExp.IsMatch(ss))
                {
                    Match m = KeyValueExp.Match(ss);
                    Sections[CurrentSection].Add(m.Groups["key"].Value, m.Groups["value"].Value);
                    if (CurrentSection == "functions")
                    {
                        ExternalFunctionNames.Add(m.Groups["key"].Value);
                        if (m.Groups["value"].Length != 0)
                            InternalFunctionNames.Add(m.Groups["value"].Value);
                        else
                            InternalFunctionNames.Add(m.Groups["key"].Value);
                    }
                }
                else if (ss == "")
                    continue;
                else
                    throw new FormatException("Unknown construct in DLS file.");
            }

            if (!Sections.ContainsKey("header"))
                throw new FormatException("DLS file does not contain header information.");
            if (!Sections.ContainsKey("functions"))
                throw new FormatException("DLS file does not contain function list.");
            Dictionary<string, string> header = Sections["header"];
            Sections.Remove("header");
            FunctionsData = Sections["functions"];
            foreach (var kv in header)
                HeaderData.Add(kv.Key.ToLower(), kv.Value);

            if (!HeaderData.ContainsKey("libraryname"))
                throw new FormatException("DLS file does not contain libraryName.");
            LibraryName = HeaderData["libraryname"];
            if (!HeaderData.ContainsKey("libraryversion"))
                throw new FormatException("DLS file does not contain libraryversion.");
            if (!byte.TryParse(HeaderData["libraryversion"], out LibraryVersion))
                throw new FormatException("DLS file: libraryVersion field could not be parsed as a byte.");
        }


        /// <summary>
        /// Returns the function number of the function with the given name, or -1 if none.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public int GetFunctionIndex(string s)
        {
            for (int i = 0; i < ExternalFunctionNames.Count; i++)
                if (s == ExternalFunctionNames[i])
                    return i;
            return -1;
        }

        public static string StripComments(string input)
        {
            input = input.Trim();
            int i = input.IndexOf('#');
            int j = input.IndexOf(';');
            if (j != -1 && j < i)
                i = j;
            if (i == -1)
                return input;
            if (i == 0)
                return "";
            input = input.Substring(i);
            input = input.Trim();
            return input;
        }
    }
}
