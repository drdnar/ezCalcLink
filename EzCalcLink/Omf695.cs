using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    public class Omf695
    {
        /// <summary>
        /// Metadata.
        /// </summary>
        public string Processor;
        /// <summary>
        /// Metadata.
        /// </summary>
        public string ModuleName;
        /// <summary>
        /// Should be 8 for eZ80.
        /// </summary>
        public int BitsPerMau;
        /// <summary>
        /// Should be 3 for eZ80.  Or 2, if you're doing Z80.
        /// </summary>
        public int MausPerAddress;
        /// <summary>
        /// Should be true for eZ80.
        /// </summary>
        public bool AddressLittleEndian;
        /// <summary>
        /// Assign Pointer to AD Extension Part (ASW0)
        /// </summary>
        public int PtrToAsw0;
        /// <summary>
        /// Assign Pointer to Environment Part (ASW1)
        /// </summary>
        public int PtrToAsw1;
        /// <summary>
        /// Assign Pointer to Section Part (ASW2)
        /// </summary>
        public int PtrToAsw2;
        /// <summary>
        /// Assign Pointer to External Part (ASW3)
        /// </summary>
        public int PtrToAsw3;
        /// <summary>
        /// Assign Pointer to Debug Information Part (ASW4)
        /// </summary>
        public int PtrToAsw4;
        /// <summary>
        /// Assign Pointer to Data Part (ASW5)
        /// </summary>
        public int PtrToAsw5;
        /// <summary>
        /// Assign Pointer to Trailer Part (ASW6)
        /// </summary>
        public int PtrToAsw6;
        /// <summary>
        /// Assign Pointer to Module End Part (ASW7)
        /// </summary>
        public int PtrToAsw7;

        public AdExtensionInfo AdExtensionPart;
        public EnvironmentInfo EnvironmentPart;
        public List<OmfSection> Sections = new List<OmfSection>();
        public List<OmfContext> Contexts = new List<OmfContext>();

        public OmfSymbolList Symbols = new OmfSymbolList();

        protected OmfSection MakeGetSection(int i)
        {
            OmfSection s = Sections.Where(x => x.Index == i).FirstOrDefault();
            if (s != null)
                return s;
            s = new OmfSection();
            s.Index = i;
            Sections.Add(s);
            return s;
        }

        /// <summary>
        /// Internal array of pointers to various parts.  Used only for the WhichPart() function.
        /// </summary>
        protected int[] Parts = new int[8];

        /// <summary>
        /// Internally used for parsing files.
        /// </summary>
        protected int index = 0;

        /// <summary>
        /// Internally used for parsing files.
        /// </summary>
        protected byte[] file;

        /// <summary>
        /// Internally used for parsing files.
        /// </summary>
        protected int currentSection;

        /// <summary>
        /// Returns a parsed object file loaded from the given path.
        /// </summary>
        /// <param name="path">Path to file to parse.</param>
        /// <returns></returns>
        public static Omf695 LoadObjectFile(string path)
        {
            byte[] file = System.IO.File.ReadAllBytes(path);

            Omf695 obj = new Omf695();
            obj.file = file;
            obj.AdExtensionPart.NnRecords = new Dictionary<int, string>();
            obj.EnvironmentPart.NnRecords = new Dictionary<int, string>();
            obj.processFile();

            obj.file = null; // Don't need this anymore, so allow GC.
            return obj;
        }

        /// <summary>
        /// Internal function.
        /// </summary>
        protected void processFile()
        {
            // Header
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Parsing header. . . .");
            DebugLogger.Indent();
            parseHeaderField();
            DebugLogger.Unindent();

            //Console.ReadKey();

            // Metadata 1
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P0 and parsing. . . .");
            index = PtrToAsw0;
            DebugLogger.Indent();
            if (index != 0)
                parseP0();
            else
                DebugLogger.LogLine("P0 not specified in index.");
            DebugLogger.Unindent();
            
            // Metadata 2
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P1 and parsing. . . .");
            index = PtrToAsw1;
            DebugLogger.Indent();
            if (index != 0)
                parseP1();
            else
                DebugLogger.LogLine("P1 not specified in index.");
            DebugLogger.Unindent();

            // Section information
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P2 and parsing. . . .");
            index = PtrToAsw2;
            DebugLogger.Indent();
            if (index != 0)
            {
                parseP2();
                DebugLogger.LogLine(DebugLogger.LogType.Verbose | DebugLogger.LogType.FileHeader, "Contexts:");
                DebugLogger.Indent();
                foreach (var c in Contexts)
                    DebugLogger.LogLine("#{0}: ID: {1:X2}, Unknown: {2:X2}, Name: {3}", c.Index, c.Id, c.UnknownData, c.Name);
                DebugLogger.Unindent();
                DebugLogger.LogLine(DebugLogger.LogType.Verbose | DebugLogger.LogType.FileHeader, "Sections:");
                DebugLogger.Indent();
                foreach (var s in Sections)
                    DebugLogger.LogLine("#{0}: Misc: {2} {3} {4}, Size {5:X6}, Addr: {6:X6}, {7} {8}, Name: {9}",
                        s.Index, s.MauSize, s.ParentIndex, s.SiblingIndex, s.AlignmentDivisor, s.Size, s.Offset, s.ContextIndex, Contexts.Where(x => x.Index == s.ContextIndex).First().Name, s.Name);
                DebugLogger.Unindent();
            }
            else
                DebugLogger.LogLine("P2 not specified in index.");
            DebugLogger.Unindent();

            // Symbols
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P3 and parsing. . . .");
            index = PtrToAsw3;
            DebugLogger.Indent();
            if (index != 0)
            {
                parseP3();
                DebugLogger.LogLine(DebugLogger.LogType.Verbose | DebugLogger.LogType.FileHeader, "Symbols:");
                DebugLogger.Indent();
                for (int i = 0; i < Symbols.Count; i++)
                    if (Symbols[i] != null)
                        //DebugLogger.LogLine("#{0}: T: {2:X2}, D: {3:X4}. Value: {4:X6}, Thingy: {5:X2}. {1} ", i, Symbols[i].Name, (int)Symbols[i].SymbolType, (int)Symbols[i].AttributeDefinition, Symbols[i].Value, Symbols[i].UnknownData);
                        if (!Symbols[i].IsExternalReference)
                            DebugLogger.LogLine("#{0}: T: {2:X2}, D: {3:X4}. Value: {4:X6}, {5}. {1} ", i, Symbols[i].Name, (int)Symbols[i].SymbolType, 
                                (int)Symbols[i].AttributeDefinition, Symbols[i].Expression.IsSimpleNumber ? Symbols[i].Expression.ResolvedValue.ToString("X6") : Symbols[i].Expression.ToString(), //"EXP",
                                Symbols[i].AddressSpaceIndex != 0 ? Contexts.Where(x => x.Id == Symbols[i].AddressSpaceIndex).First().Name : "<N/A>");
                        else
                            DebugLogger.LogLine("#{0}: T: {2:X2}, D: {3:X4}. {1} ", i, Symbols[i].Name, (int)Symbols[i].SymbolType, (int)Symbols[i].AttributeDefinition);
                DebugLogger.Unindent();
            }
            else
                DebugLogger.LogLine("P3 not specified in index.");
            DebugLogger.Unindent();

            // Debug information
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P4 and parsing. . . .");
            index = PtrToAsw4;
            DebugLogger.Indent();
            if (index != 0)
                parseP4();
            else
                DebugLogger.LogLine("P4 not specified in index.");
            DebugLogger.Unindent();

            // Data
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P5 and parsing. . . .");
            index = PtrToAsw5;
            DebugLogger.Indent();
            if (index != 0)
            {
                parseP5();
                DebugLogger.Indent();
                foreach (OmfSection s in Sections)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Verbose | DebugLogger.LogType.FileHeader, "Section Index: {0} {1}", s.Index, s.Name);
                    DebugLogger.Indent();
                    foreach (ContiguousMemory m in s.Memories)
                    {
                        DebugLogger.LogLine("Memory record: {0:X6}, size: {1:X4}", m.StartAddress, m.Size);
                        //Indent(nesting + 3);
                        //for (int i = m.StartAddress; i < m.EndAddress; i++)
                        //    Log(m[i].ToString("X2"));
                        //LogLine("");
                    }
                    DebugLogger.Unindent();
                }
                DebugLogger.Unindent();
            }
            else
                DebugLogger.LogLine("P5 not specified in index.");
            DebugLogger.Unindent();
            
            // Also not implemented
            if (PtrToAsw6 != 0)
                DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "WARNING! P6 specified in index, but not implemented in parser.");
            else
                DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "(No P6.)");
            DebugLogger.LogLine(DebugLogger.LogType.Basic | DebugLogger.LogType.FileHeader, "Seeking to P7 and parsing. . . .");
            
            // Module end
            index = PtrToAsw7;
            DebugLogger.Indent();
            if (index != 0)
                parseP7();
            else
                DebugLogger.LogLine("P7 not specified in index.");
            DebugLogger.Unindent();

        }

        protected bool parseHeaderField()
        {
            //DebugLogger.Log("Parsing field {0}, ", file[index].ToString("X2"));
            
            bool isEscapedValue = false;

            if (!NextRecordIdIs(0xE0))
            {
                Console.WriteLine("ERROR! File does not begin with record 0xE0 (Module Beginning).");
                throw new FormatException("File parse error.");
            }

            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.Basic | DebugLogger.LogType.FieldHeader, 
                "Module Beginning (MB)");
            Processor = ReadString();
            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.Basic | DebugLogger.LogType.FieldValue,
                " Processor = {0}", Processor);
            ModuleName = ReadString();
            DebugLogger.LogLine(" Module name = {0}", ModuleName);

            while (WhichPart(index) == -1)
                if (NextRecordIdIs(0xEC)) // Address Descriptor
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Address Descriptor (AD)");
                    BitsPerMau = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" Bits per MAU = {0}", BitsPerMau);
                    MausPerAddress = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(" MAUs per address = {0}", MausPerAddress);
                    byte b = file[index];
                    if (b == 0xCC || b == 0xCD)
                        index++;
                    AddressLittleEndian = b == 0xCC;
                    if (AddressLittleEndian)
                        DebugLogger.LogLine(" Addresses are little-endian");
                    else
                        DebugLogger.LogLine(" Addresses are big-endian");
                }
                else if (NextRecordIdIs(0xE2D700))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to AD Extension Part (ASW0)");
                    Parts[0] = PtrToAsw0 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW0 offset = {0:X8}", PtrToAsw0);
                }
                else if (NextRecordIdIs(0xE2D701))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to Environment Part (ASW1)");
                    Parts[1] = PtrToAsw1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW1 offset = {0:X8}", PtrToAsw1);
                }
                else if (NextRecordIdIs(0xE2D702))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to Section Part (ASW2)");
                    Parts[2] = PtrToAsw2 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW2 offset = {0:X8}", PtrToAsw2);
                }
                else if (NextRecordIdIs(0xE2D703))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to External Part (ASW3)");
                    Parts[3] = PtrToAsw3 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW3 offset = {0:X8}", PtrToAsw3);
                }
                else if (NextRecordIdIs(0xE2D704))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to Debug Information Part (ASW4)");
                    Parts[4] = PtrToAsw4 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW4 offset = {0:X8}", PtrToAsw4);
                }
                else if (NextRecordIdIs(0xE2D705))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to Data Part (ASW5)");
                    Parts[5] = PtrToAsw5 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW5 offset = {0:X8}", PtrToAsw5);
                }
                else if (NextRecordIdIs(0xE2D706))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to Trailer Part (ASW6)");
                    Parts[6] = PtrToAsw6 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW6 offset = {0:X8}", PtrToAsw6);
                }
                else if (NextRecordIdIs(0xE2D707))
                {
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, DebugLogger.LogType.FieldValue);
                    DebugLogger.LogLine("Assign Pointer to Module End Part (ASW7)");
                    Parts[7] = PtrToAsw7 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogMaskChange(DebugLogger.LogType.FieldValue, DebugLogger.LogType.FieldHeader);
                    DebugLogger.LogLine(" ASW7 offset = {0:X8}", PtrToAsw7);
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown or unexpected field!");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index + i]);
                    DebugLogger.LogLine();
                }
            return true;
        }


        protected void parseP4()
        {
            bool isEscapedValue;
            while (WhichPart(index) == 4)
                if (NextRecordIdIs(0xF8)) // Declare Block Beginning (BB)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Declare block beginning (BE)");
                    DebugLogger.Indent();
                    switch (file[index++])
                    {
                        case 1:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader, 
                                "Unique typedefs for module");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue, 
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Module name: {0}", ReadString());
                            break;
                        case 2:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                                "Global typedefs");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Module name: {0}", ReadString());
                            break;
                        case 3:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                                "High level module scope beginning");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Module name: {0}", ReadString());
                            break;
                        case 4:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                                "Global function");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                                " Function name: {0}", ReadString());
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size of stack locals: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Type index for return value: {0:X4}", ReadNumber(out isEscapedValue));
                            OmfExpression e = ReadExpression();
                            if (e.IsSimpleNumber)
                                DebugLogger.LogLine(" Offset value: 0x{0:X6}", e.ResolvedValue);
                            else
                                DebugLogger.LogLine(" Offset expression: {0}", e.ToString());
                            if (NextItemIsNumber()) // This doesn't appear in the spec.  So I can't speak to its purpose.
                                DebugLogger.LogLine(" Unknown data: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 5:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                                "Source code file name");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                                " File name: {0}", ReadString());
                            if (!NextItemIsNumber())
                                break;
                            DateTime dt = new DateTime(ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " File date and time: {0}", dt);
                            break;
                        case 6:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                                "Local function");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                                " Function name: {0}", ReadString());
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size of stack locals: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Type index for return value: {0:X4}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Offset expression: 0x{0:X6}", ReadNumber(out isEscapedValue));
                            break;
                        case 10:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                                "Assembler module scope beginning");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                                " Module name: {0}", ReadString());
                            DebugLogger.LogLine(" Input object file name: {0}", ReadString());
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Tool type: {0}", ReadNumber(out isEscapedValue));
                            if (NextItemIsString())
                                DebugLogger.LogLine(" Version information: {0}", ReadString());
                            if (!NextItemIsNumber())
                                break;
                            dt = new DateTime(ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Date and time: {0}", dt);
                            break;
                        case 11:
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                                "Module section");
                            DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                                " Size: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Name should be null: <{0}>", ReadString());
                            switch (file[index++])
                            {
                                case 0:
                                    DebugLogger.LogLine("Section type: Mixed");
                                    break;
                                case 1:
                                    DebugLogger.LogLine("Section type: Code");
                                    break;
                                case 2:
                                    DebugLogger.LogLine("Section type: Read/Write data");
                                    break;
                                case 3:
                                    DebugLogger.LogLine("Section type: Read-only data");
                                    break;
                                case 4:
                                    DebugLogger.LogLine("Section type: Stack");
                                    break;
                                case 5:
                                    DebugLogger.LogLine("Section type: Memory");
                                    break;
                                default:
                                    DebugLogger.LogLine("Section type: Unknown 0x{0:X2}", file[index - 1]);
                                    break;
                            }
                            DebugLogger.LogLine(" Section index: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Offset expression: 0x{0:X6}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" HP whatever: {0}", ReadNumber(out isEscapedValue));
                            break;
                        default:
                            DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown block type: 0x{0:X2}", file[index - 1]);
                            for (int i = 0; i < 32; i++)
                                DebugLogger.Log("{0:X2}", file[index + i]);
                            DebugLogger.LogLine();
                            break;
                    }
                    DebugLogger.Unindent();
                }
                else if (NextRecordIdIs(0xF0)) // Declare Type Name (NN)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Declare type name (NN)");
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Name index: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Name: {0}", ReadString());
                }
                else if (NextRecordIdIs(0xF2)) // Define Type Characteristics (TY)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Define type characteristics (TY)");
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Type index: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(" Should be 0xCE: 0x{0:X2}", file[index++]);
                    DebugLogger.LogLine(" Local name index: {0}", ReadNumber(out isEscapedValue));
                }
                else if (NextRecordIdIs(0xF1CE)) // Variable Attributes (ATN)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable attributes (ATN)");
                    int n1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Name index: {0}", n1);
                    DebugLogger.LogLine(" Type index: {0}", ReadNumber(out isEscapedValue));
                    int n3 = ReadNumber(out isEscapedValue);
                    switch (n3)
                    {
                        case 1:
                            DebugLogger.LogLine(" Automatic variable stack offset: 0x{0:X2}", ReadNumber(out isEscapedValue));
                            break;
                        case 2:
                            DebugLogger.LogLine(" Variable register: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 3:
                            DebugLogger.LogLine(" Compiler defined static variable");
                            break;
                        case 4:
                            DebugLogger.LogLine(" External function");
                            break;
                        case 5:
                            DebugLogger.LogLine(" External variable definition");
                            break;
                        case 7:
                            DebugLogger.LogLine(" Line number: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Column number: {0}", ReadNumber(out isEscapedValue));
                            if (NextItemIsNumber())
                                DebugLogger.LogLine(" Unknown data: {0}", ReadNumber(out isEscapedValue));
                            if (NextItemIsNumber())
                                DebugLogger.LogLine(" Unknown data: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 8:
                            DebugLogger.LogLine(" Compiler global variable");
                            break;
                        case 9:
                            DebugLogger.LogLine(" Variable life time: {0:X6}", ReadNumber(out isEscapedValue));
                            if (n1 == 0)
                                DebugLogger.LogLine(" Register: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 10:
                            DebugLogger.LogLine(" Variable name for locked register");
                            DebugLogger.LogLine(" Register index: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Frame offset: 0x{0:X6}", ReadNumber(out isEscapedValue));
                            break;
                        case 11:
                            DebugLogger.LogLine(" Reserved for FORTRAN. Please no.");
                            break;
                        case 12:
                            DebugLogger.LogLine(" Based variable");
                            DebugLogger.LogLine(" Offset value: 0x{0:X6}", ReadNumber(out isEscapedValue));
                            int x2 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log(" Control number: {0} ", x2);
                            switch (x2)
                            {
                                case 0:
                                    DebugLogger.LogLine("Based from static memory");
                                    break;
                                case 1:
                                    DebugLogger.LogLine("Based from register");
                                    break;
                                case 2:
                                    DebugLogger.LogLine("Based from bank, section, or task");
                                    break;
                                case 3:
                                    DebugLogger.LogLine("Based from selector or pointer");
                                    break;
                                case 4:
                                    DebugLogger.LogLine("Indirected from reigster base");
                                    break;
                                default:
                                    DebugLogger.LogLine("Unknown");
                                    break;
                            }
                            int x3 = ReadNumber(out isEscapedValue);
                            if (x3 == 0)
                                DebugLogger.LogLine(" Local (00)");
                            else
                                DebugLogger.LogLine(" Public (0x{0:X2})", x3);
                            int m = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine(" Memory space indicator value: 0x{0:X2} {1}", m, Contexts.Where(x => x.Id == m).First().Name);
                            DebugLogger.LogLine(" Base size: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 16:
                            DebugLogger.LogLine(" Constant");
                            int x1 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log(" Symbol class: {0} ", x1);
                            switch (x1)
                            {
                                case 0:
                                    DebugLogger.LogLine("Unknown class");
                                    break;
                                case 1:
                                    DebugLogger.LogLine("EQU constant");
                                    break;
                                case 2:
                                    DebugLogger.LogLine("SET constant");
                                    break;
                                case 3:
                                    DebugLogger.LogLine("Pascal CONST constant");
                                    break;
                                case 4:
                                    DebugLogger.LogLine("C #define constant (not to be confused with C# #define");
                                    break;
                                default:
                                    DebugLogger.LogLine("Unknown");
                                    break;
                            }
                            if (!NextItemIsNumber())
                                break;
                            x2 = ReadNumber(out isEscapedValue);
                            if (x2 == 0)
                                DebugLogger.LogLine(" Local (00)");
                            else
                                DebugLogger.LogLine(" Public (0x{0:X2})", x2);
                            if (NextItemIsNumber())
                                DebugLogger.LogLine(" Numeric value: {0}", ReadNumber(out isEscapedValue));
                            else if (NextItemIsString())
                                DebugLogger.LogLine(" String value: {0}", ReadString());
                            break;
                        case 19:
                            DebugLogger.LogLine(" Static variable generated by assembler");
                            DebugLogger.LogLine(" Number of elements: {0}", ReadNumber(out isEscapedValue));
                            if (!NextItemIsNumber())
                                break;
                            x2 = ReadNumber(out isEscapedValue);
                            if (x2 == 0)
                                DebugLogger.LogLine(" Local (00)");
                            else
                                DebugLogger.LogLine(" Global (0x{0:X2})", x2);
                            break;
                        case 36:
                            DebugLogger.LogLine(" Lowest version number of input files");
                            break;
                        case 37: // See section 3.2
                        case 38: // See section 3.2
                        case 39: // See section 3.2
                        case 50: // See section 3.3
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                            DebugLogger.LogLine(" PARSING NOT IMPLEMENTED");
                            break;
                        case 62:
                            DebugLogger.LogLine(" Procedure block other");
                            DebugLogger.LogLine(" Type ID number: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Additional ATN or ASN record count: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 63:
                            DebugLogger.LogLine(" Variable other");
                            DebugLogger.LogLine(" Type ID number: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Additional ATN or ASN record count: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 64:
                            DebugLogger.LogLine(" Module other.");
                            DebugLogger.LogLine(" Type ID number: {0}", ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(" Additional ATN or ASN record count: {0}", ReadNumber(out isEscapedValue));
                            break;
                        case 65:
                            DebugLogger.LogLine(" String: {0}", ReadString());
                            break;
                        default:
                            DebugLogger.LogLine(DebugLogger.LogType.Error, " UNKNOWN: 0x{0:X2}", n3);
                            break;

                    }
                }
                else if (NextRecordIdIs(0xE2CE)) // Variable Values (ASN)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Variable values (ASN)");
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", ReadNumber(out isEscapedValue));
                    OmfExpression e = ReadExpression();
                    if (e.IsSimpleNumber)
                        DebugLogger.LogLine(" Symbol value: 0x{0:X6}", e.ResolvedValue);
                    else
                        DebugLogger.LogLine(" Symbol expression: {0}", e.ToString());
                }
                else if (NextRecordIdIs(0xE2D2)) // Variable Values (ASR), not implemented
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Variable values, ASR, not implemented");
                }
                else if (NextRecordIdIs(0xF9)) // Declare Block End (BE)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Block end (BE)");
                    if (NextItemIsNumber())
                        DebugLogger.LogLine(DebugLogger.LogType.P4 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                            " Additional number: 0x{0:X4}", ReadNumber(out isEscapedValue));
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR! Unknown record ID.");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index + i]);
                    DebugLogger.LogLine("");
                    return;
                }

        }

        protected void parseP7()
        {
            if (NextRecordIdIs(0xE1))
            {
                DebugLogger.LogLine(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose | DebugLogger.LogType.P7, "Module End (ME).");
            }
            else if (NextRecordIdIs(0xEE) || NextRecordIdIs(0xEF))
            {
                DebugLogger.LogLine(DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose | DebugLogger.LogType.P7, "Checksum records. Ignored.");
            }
        }

        protected bool parseP3()
        {
            bool isEscapedValue;
            int n1, n2, n3, n4, x1, x3;
            OmfSymbol s;
            //DebugLogger.LogLine("Current index: {0:X4}", index);

            while (WhichPart(index) == 3)
                if (NextRecordIdIs(0xE8))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader, 
                        "Public (external) symbol (NI)");
                    n1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Public name index record: {0}", n1);
                    s = Symbols.GetOrCreate(n1);
                    s.Name = ReadString();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Symbol name: {0}", s.Name);
                }
                else if (NextRecordIdIs(0xF1C9))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable attribute (ATI)");
                    n1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", n1);
                    s = Symbols.GetOrCreate(n1);
                    n2 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Symbol type index: {0}", n2);
                    s.SymbolType = (OmfSymbol.SymbolTypes)n2;
                    DebugLogger.Indent();
                    switch (n2)
                    {
                        case 0:
                            DebugLogger.LogLine("Unspecified.");
                            break;
                        case 3:
                            DebugLogger.LogLine("8-bit data byte");
                            break;
                        case 5:
                            DebugLogger.LogLine("16-bit short data word");
                            break;
                        case 7:
                            DebugLogger.LogLine("32-bit long data word");
                            break;
                        case 10:
                            DebugLogger.LogLine("32-bit floating point");
                            break;
                        case 11:
                            DebugLogger.LogLine("64-bit floating point");
                            break;
                        case 12:
                            DebugLogger.LogLine("10 or 12 byte floating point. Guess which!");
                            break;
                        case 15:
                            DebugLogger.LogLine("Instruction address");
                            break;
                        default:
                            DebugLogger.LogLine("I DON'T KNOW, PLEASE CHECK");
                            break;
                    }
                    DebugLogger.Unindent();
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.Log(" Attribute definition: {0}", n3);
                    switch (n3)
                    {
                        case 8:
                            DebugLogger.LogLine(" Global compiler symbol.");
                            s.AttributeDefinition = OmfSymbol.AttributeDefinitions.GlobalCompilerSymbol;
                            break;
                        case 16:
                            DebugLogger.Log(" Constant. ");
                            x1 = ReadNumber(out isEscapedValue);
                            s.AttributeDefinition = (OmfSymbol.AttributeDefinitions)((int)OmfSymbol.AttributeDefinitions.ConstantGeneric | x1);
                            switch (x1)
                            {
                                case 0:
                                    DebugLogger.LogLine("Unknown class.");
                                    break;
                                case 1:
                                    DebugLogger.LogLine("EQU constant");
                                    break;
                                case 2:
                                    DebugLogger.LogLine("SET constant");
                                    break;
                                case 3:
                                    DebugLogger.LogLine("Pascal CONST constant");
                                    break;
                                case 4:
                                    DebugLogger.LogLine("C #define constant");
                                    break;
                                default:
                                    DebugLogger.LogLine("{0}", x1);
                                    break;
                            }
                            if (file[index] > 0x84)
                                break; // . . . that really shouldn't be legal syntax
                            DebugLogger.LogLine(DebugLogger.LogType.Error, "I don't know. I give up. This is impossible to parse unambiguously.");
                            return false;
                        case 19:
                            DebugLogger.LogLine(" Static symbol generated by assembler.");
                            s.AttributeDefinition = OmfSymbol.AttributeDefinitions.AssemblerStaticSymbol;
                            break;
                    }
                    s.AddressSpaceIndex = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(" Address space index: {0:X2}", s.AddressSpaceIndex);
                }
                else if (NextRecordIdIs(0xE2C9))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable values (ASI)");
                    n1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol index: {0}", n1);
                    s = Symbols.GetOrCreate(n1);
                    if (NextItemIsNumber())
                    {
                        s.Expression = ReadExpression();
                        //s.Value = s.Expression.ResolvedValue;
                        DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue, 
                            " Symbol value: {0:X6}", s.Expression.ResolvedValue);
                    }
                    else
                    {
                        s.Expression = ReadExpression();
                        DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                            " Symbol expression: {0}", s.Expression.ToString());
                    }
                    
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Variable values (ASR), should not use");
                    return false;
                }
                else if (NextRecordIdIs(0xE9))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "External reference name (NX)");
                    n1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " External reference index: {0}", n1);
                    s = Symbols.GetOrCreate(n1);
                    s.IsExternalReference = true;
                    s.Name = ReadString();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Symbol name: {0}", s.Name);
                }
                else if (NextRecordIdIs(0xF1D8))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error,//DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "External reference relocation information (ATX)");
                    n1 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " External reference index: {0}", n1);
                    if (file[index] <= 0x84)
                    {
                        DebugLogger.Log(" Type index: ");
                        if (file[index] == 0x80)
                        {
                            index++;
                            DebugLogger.LogLine("Unspecified");
                        }
                        else
                            DebugLogger.LogLine("{0}", ReadNumber(out isEscapedValue));
                        if (file[index] <= 0x84)
                        {

                            DebugLogger.Log(" Section index: ");
                            if (file[index] == 0x80)
                            {
                                index++;
                                DebugLogger.LogLine("Unspecified");
                            }
                            else
                                DebugLogger.LogLine("{0}", ReadNumber(out isEscapedValue));
                            if (file[index] <= 0x84)
                            {
                                DebugLogger.LogLine(" Short external flag: {0}", ReadNumber(out isEscapedValue));
                            }
                        }
                    }
                }
                else if (NextRecordIdIs(0xF4))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Weak external reference (WX)");
                }
                else if (NextRecordIdIs(0xE5))
                {
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader, 
                        "Set current section: {0}", currentSection);
                }
                else
                {
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log(DebugLogger.LogType.Error, "{0:X2}", file[index + i]);
                    DebugLogger.LogLine();

                    DebugLogger.LogLine(DebugLogger.LogType.Error, "FAULT");
                    return false;
                }
            return false;
        }

        protected bool parseP2()
        {
            bool isEscapedValue;
            int n3;

            while (WhichPart(index) == 2)
                if (NextRecordIdIs(0xE6))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section type (ST): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Index: {0}", currentSection);
                    OmfSection s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine("Created new section.");
                        s = new OmfSection();
                        s.Index = currentSection;
                        Sections.Add(s);
                    }
                    DebugLogger.Log(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Type: ");
                    if (NextRecordIdIs(0xC1D3D0))
                    {
                        // Normal value?
                        DebugLogger.LogLine("Absolute code (ASP)");
                    }
                    else if (NextRecordIdIs(0xC1D3D2))
                    {
                        DebugLogger.LogLine("Absolute ROM data (ASR)");
                    }
                    else if (NextRecordIdIs(0xC1D3C4))
                    {
                        DebugLogger.LogLine("Absolute data (ASD)");
                    }
                    else if (NextRecordIdIs(0xC3D0))
                    {
                        DebugLogger.LogLine("Normal code (CP)");
                    }
                    else if (NextRecordIdIs(0xC3D2))
                    {
                        DebugLogger.LogLine("Normal ROM data (CR)");
                    }
                    else if (NextRecordIdIs(0xC3C4))
                    {
                        DebugLogger.LogLine("Normal data (CD)");
                    }
                    else if (NextRecordIdIs(0xC5C1D0))
                    {
                        DebugLogger.LogLine("Common absolute code (EAP)");
                    }
                    else if (NextRecordIdIs(0xC5C1D2))
                    {
                        DebugLogger.LogLine("Common absolute ROM data (EAR)");
                    }
                    else if (NextRecordIdIs(0xC5C1C4))
                    {
                        DebugLogger.LogLine("Common absolute data (EAD)");
                    }
                    else if (NextRecordIdIs(0xC5DA))
                    {
                        DebugLogger.LogLine("Something about short common with error checking.");
                        return false;
                    }
                    else if (NextRecordIdIs(0xCDC1D0))
                    {
                        DebugLogger.LogLine("Common absolute code without size constraint thingy (MAP)");
                    }
                    else if (NextRecordIdIs(0xCDC1D2))
                    {
                        DebugLogger.LogLine("Common absolute ROM data without size constraint thingy (MAR)");
                    }
                    else if (NextRecordIdIs(0xCDC1C4))
                    {
                        DebugLogger.LogLine("Common absolute data without size constraint thingy (MAD)");
                    }
                    else if (NextRecordIdIs(0xDAC3D0))
                    {
                        DebugLogger.LogLine("Short code (ZCP)");
                    }
                    else if (NextRecordIdIs(0xDAC3D2))
                    {
                        DebugLogger.LogLine("Short ROM data (ZCR)");
                    }
                    else if (NextRecordIdIs(0xDAC3C4))
                    {
                        DebugLogger.LogLine("Short data (ZCD)");
                    }
                    else if (NextRecordIdIs(0xDACD))
                    {
                        DebugLogger.LogLine("Short common relocatable sections thingies?");
                        return false;
                    }
                    s.Name = ReadString();
                    DebugLogger.LogLine(" Section name: {0}", s.Name);
                    s.ParentIndex = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Parent index: {0}", s.ParentIndex);
                    s.SiblingIndex = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(" Sibling index: {0}", s.SiblingIndex);
                    s.ContextIndex = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Context index: {0}: {1}", s.ContextIndex, Contexts.Where(x => x.Index == s.ContextIndex).First().Name);
                }
                else if (NextRecordIdIs(0xE7))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section alignment (SA): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    OmfSection s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR! Section not found.");
                        throw new FormatException();
                    }
                    s.AlignmentDivisor = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Boundary alignment divisor: {0}", s.AlignmentDivisor);
                    if (NextItemIsNumber())
                        DebugLogger.LogLine(" Page size: {0}", ReadNumber(out isEscapedValue));
                }
                else if (NextRecordIdIs(0xE2D3))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section size (ASS): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    OmfSection s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR! Section not found.");
                        throw new FormatException();
                    }
                    s.Size = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(" Section size: {0}", s.Size);
                }
                else if (NextRecordIdIs(0xE2CC))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section base address (ASL): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", ReadNumber(out isEscapedValue));
                    OmfSection s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR! Section not found.");
                        throw new FormatException();
                    }
                    s.SectionBaseAddress = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Section base address: {0}", s.SectionBaseAddress);
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable values or section offset? (ASR): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    OmfSection s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR! Section not found.");
                        throw new FormatException();
                    }
                    s.Offset = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Section offset: {0:X6}", s.Offset);
                }
                else if (NextRecordIdIs(0xFB))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Define context (NC): ");
                    OmfContext ctx = new OmfContext();
                    Contexts.Add(ctx);
                    ctx.Index = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType./*Very*/Verbose | DebugLogger.LogType.FieldValue,
                        " Context index: {0}", ctx.Index);
                    ctx.Name = ReadString();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Context name: {0}", ctx.Name);
                    if (NextItemIsNumber())
                    {
                        ctx.UnknownData = ReadNumber(out isEscapedValue);
                        DebugLogger.LogLine(" Context unknown data: {0:X2}", ctx.UnknownData);
                        ctx.Id = ReadNumber(out isEscapedValue);
                        DebugLogger.LogLine(" Context ID: {0:X2}", ctx.Id);
                    }
                }
                else if (NextRecordIdIs(0xE2C1))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Physical region size (ASA): ");
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Region size: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Parser not programmed to use this field's information.  Fixme!");
                    //return false;
                }
                else if (NextRecordIdIs(0xE2C2))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Physical region base address (ASB): ");
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Address: {0:X6}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Parser not programmed to use this field's information.  Fixme!");
                    //return false;
                }
                else if (NextRecordIdIs(0xE2C6))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "MAU size (ASF): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", ReadNumber(out isEscapedValue));
                    OmfSection s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR! Section not found.");
                        throw new FormatException();
                    }
                    s.MauSize = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " MAU size: {0}", s.MauSize);
                }
                else if (NextRecordIdIs(0xE2CD))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "M-Value (ASM): ");
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " M-Value: {0}", ReadNumber(out isEscapedValue));
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Parser not programmed to use this field's information.  Fixme!");
                    //return false;
                }
                else if (NextRecordIdIs(0xE5)) // Apparently, this is legal?
                {
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Current section index (SB): {0}", currentSection);
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown record ID.");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index + i]);
                    DebugLogger.LogLine();
                    return false;
                }
            return true;
        }


        protected bool parseP5()
        {
            bool isEscapedValue;
            OmfSection s;
            while (WhichPart(index) == 5)
                if (NextRecordIdIs(0xE5))
                {
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Current section index (SB): {0} {1}", currentSection, Sections.Where(x => x.Index == currentSection).First().Name);
                }
                else if (NextRecordIdIs(0xE2D0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Current section PC (ASP)");
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0} {1}", currentSection, Sections.Where(x => x.Index == currentSection).First().Name);
                    s = MakeGetSection(currentSection);
                    OmfExpression e = ReadExpression();
                    DebugLogger.LogLine(DebugLogger.LogType.Error, e.ToString());
                    //s.NextAddress = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(" Value: {0:X6}", s.NextAddress);
                }
                else if (NextRecordIdIs(0xED))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Load Constant MAUs (LD)");
                    s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR: Attempt to add data to undefined section!");
                        return false;
                    }
                    int n = ReadNumber(out isEscapedValue);
                    DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Data bytes: {0}  ", n);
                    for (int i = 0; i < n; i++)
                    {
                        s.SetByte(file[index + i]);
                        DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                            "{0:X2}", file[index + i]);
                    }
                    DebugLogger.LogLine();
                    index += n;
                }
                else if (NextRecordIdIs(0xE3))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error,//DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Initialize Relocation Base (IR)");
                    Console.ReadKey();
                }
                else if (NextRecordIdIs(0xF7))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error,//DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Repeat Data (RE)");
                    Console.ReadKey();
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error,//DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Variables Values (ASR)"); // "Not implemented"
                    Console.ReadKey();
                }
                else if (NextRecordIdIs(0xE2D7))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error,//DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Variables Values (ASW)");
                    Console.ReadKey();
                }
                else if (NextRecordIdIs(0xE4))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Load With Relocation (LR)");
                    for (int q = 0; q < 256; q++)
                    {
                        if (Console.ForegroundColor == ConsoleColor.White)
                            Console.ForegroundColor = ConsoleColor.Gray;
                        else
                            Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("{0:X2}", file[index + q]);
                    }
                    Console.WriteLine();

                    if (NextItemIsNumber())
                    {
                        int n = ReadNumber(out isEscapedValue);
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                            " Form A. Bytes: {0}", n);
                        DebugLogger.Log(" ");
                        for (int i = 0; i < n; i++)
                        {
                            DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                                "{0:X2}", file[index + i]);
                        }
                        DebugLogger.LogLine();
                        index += n;
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                            " Next byte: {0:X2}", ReadNumber(out isEscapedValue));
                    }
                    else if (file[index] >= 0xC0 && file[index] < 0xE0)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                            " Form B. Letter: {0}, Offset: {1}", (char)(file[index++] - 0xC0 + (int)'A'), ReadNumber(out isEscapedValue));
                    }
                    else
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                            " Form C");
                        OmfExpression exp = ReadExpression();
                        DebugLogger.LogLine(" Expression: {0}", exp.ToString());
                        DebugLogger.LogLine(" Number: {0}", ReadNumber(out isEscapedValue));
                    }
                }
                else if (NextRecordIdIs(0xFA))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error,//DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Load With Translation (LT)");
                    Console.ReadKey();
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown record ID.");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index + i]);
                    DebugLogger.LogLine();
                    return false;
                }
            return true;
        }


        protected bool parseP1()
        {
            bool isEscapedValue;
            int n3;

            while (WhichPart(index) == 1)
                if (NextRecordIdIs(0xF0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Record F0, Variable Attributes (NN)");
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Version: {0}", n3);
                    EnvironmentPart.NnRecords.Add(n3, ReadString());
                    DebugLogger.LogLine(" ID: {0}", EnvironmentPart.NnRecords[n3]);
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Record F1CE, Variable Attributes (ATN)");
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", n3);
                    if (!EnvironmentPart.NnRecords.ContainsKey(n3))
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR: No matching NN record!");
                        return false;
                    }
                    DebugLogger.LogLine(" Should be zero: {0}", ReadNumber(out isEscapedValue));
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Attribute definition: {0}, ", n3);
                    switch (n3)
                    {
                        case 50:
                            DebugLogger.Log("Creation date and time: ");
                            EnvironmentPart.DateTime = new DateTime(ReadNumber(out isEscapedValue) + 1900, ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue));
                            DebugLogger.LogLine(EnvironmentPart.DateTime.ToString());
                            break;
                        case 51:
                            DebugLogger.Log("Creation command line: ");
                            EnvironmentPart.CommandLine = ReadString();
                            DebugLogger.LogLine(EnvironmentPart.CommandLine);
                            break;
                        case 52:
                            n3 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log("Execution status: {0}", n3);
                            switch (n3)
                            {
                                case 0:
                                    DebugLogger.LogLine(": Success");
                                    break;
                                case 1:
                                    DebugLogger.LogLine(": Warning");
                                    break;
                                case 2:
                                    DebugLogger.LogLine(": Error(s)");
                                    break;
                                case 3:
                                    DebugLogger.LogLine(": Fatal error(s)");
                                    break;
                                default:
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, ": Unknown");
                                    return false;
                            }
                            EnvironmentPart.ExecutionStatus = (ExecutionStatus)n3;
                            break;
                        case 53:
                            DebugLogger.Log("Host environment: ");
                            n3 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log(n3.ToString());
                            switch (n3)
                            {
                                case 0:
                                    DebugLogger.LogLine(": Other");
                                    break;
                                case 1:
                                    DebugLogger.LogLine(": VMS");
                                    break;
                                case 2:
                                    DebugLogger.LogLine(": MS-DOS");
                                    break;
                                case 3:
                                    DebugLogger.LogLine(": UNIX");
                                    break;
                                case 4:
                                    DebugLogger.LogLine(": HP-UX");
                                    break;
                                default:
                                    DebugLogger.LogLine(": Unknown");
                                    return false;
                            }
                            EnvironmentPart.HostEnvironment = (HostEnvironment)n3;
                            break;
                        case 54:
                            DebugLogger.LogLine("Tool version information");
                            EnvironmentPart.ToolNumber = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine("  Tool number: {0}", EnvironmentPart.ToolNumber);
                            EnvironmentPart.ToolVersion = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine("  Tool {0}", EnvironmentPart.ToolVersion);
                            EnvironmentPart.ToolRevision = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine("  Tool {0}", EnvironmentPart.ToolRevision);
                            if (file[index] >= 0xC1 && file[index] <= 0xDA)
                            {
                                EnvironmentPart.ToolLetter = (char)(file[index++] - 0xC1 + (int)'A');
                                DebugLogger.LogLine("  Tool letter: {0}", EnvironmentPart.ToolLetter);
                            }
                            break;
                        case 55:
                            DebugLogger.Log("Comments: ");
                            EnvironmentPart.Comments = ReadString();
                            DebugLogger.LogLine(EnvironmentPart.Comments);
                            break;
                        case 56:
                            EnvironmentPart.IDontKnow = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine("I dunno: {0}: Total mystery.", EnvironmentPart.IDontKnow);
                            break;
                        default:
                            DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown field.");
                            for (int i = 0; i < 32; i++)
                                DebugLogger.Log("{0:X2}", file[index++]);
                            
                            return false;
                    }

                }
                else if (NextRecordIdIs(0xE2CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, 
                        "Record E2CE.  Wonder what it means.");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index++]);
                    return false;
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown field.");
                    return false;
                }

            return true;
        }


        protected bool parseP0()
        {
            bool isEscapedValue;
            int n3;

            while (WhichPart(index) == 0)
                if (NextRecordIdIs(0xF0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader, 
                        "Record F0, Variable Attributes (NN)");
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Version: {0}", n3);
                    AdExtensionPart.NnRecords.Add(n3, ReadString());
                    DebugLogger.LogLine(" ID: {0}", AdExtensionPart.NnRecords[n3]);
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Record F1CE, Variable Attributes (ATN)");
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", n3);
                    if (!AdExtensionPart.NnRecords.ContainsKey(n3))
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.Error, "ERROR: No matching NN record!");
                        return false;
                    }
                    DebugLogger.LogLine(" Should be zero: {0}", ReadNumber(out isEscapedValue));
                    n3 = ReadNumber(out isEscapedValue);
                    DebugLogger.Log(DebugLogger.LogType.P0 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Attribute definition: {0}, ", n3);
                    switch (n3)
                    {
                        case 37:
                            DebugLogger.LogLine(" Object format version");
                            AdExtensionPart.VersionNumber = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine("  Version: {0}", AdExtensionPart.VersionNumber);
                            AdExtensionPart.RevisionNumber = ReadNumber(out isEscapedValue);
                            DebugLogger.LogLine("  Revision: {0}", AdExtensionPart.RevisionNumber);
                            break;
                        case 38:
                            n3 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log(" Object format type {0}", n3);
                            DebugLogger.Indent();
                            switch (n3)
                            {
                                case 1:
                                    DebugLogger.LogLine(": Absolute");
                                    break;
                                case 2:
                                    DebugLogger.LogLine(": Relocatable");
                                    break;
                                case 3:
                                    DebugLogger.LogLine(": Loadable");
                                    break;
                                case 4:
                                    DebugLogger.LogLine(": Library");
                                    break;
                                default:
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, ": Unknown");
                                    DebugLogger.Unindent();
                                    return false;
                            }
                            DebugLogger.Unindent();
                            AdExtensionPart.ObjectFormatType = (ObjectFormatType)n3;
                            break;
                        case 39:
                            DebugLogger.Indent();
                            n3 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log(" Case sensitivity {0}", n3);
                            switch (n3)
                            {
                                case 1:
                                    DebugLogger.LogLine(": False");
                                    AdExtensionPart.CaseSensitive = false;
                                    break;
                                case 2:
                                    DebugLogger.LogLine(": True");
                                    AdExtensionPart.CaseSensitive = true;
                                    break;
                                default:
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, ": Unknown");
                                    DebugLogger.Unindent();
                                    return false;
                            }
                            DebugLogger.Unindent();
                            break;
                        case 40:
                            DebugLogger.Indent();
                            n3 = ReadNumber(out isEscapedValue);
                            DebugLogger.Log(" Memory model {0}", n3);
                            switch (n3)
                            {
                                case 0:
                                    DebugLogger.LogLine(": Tiny");
                                    break;
                                case 1:
                                    DebugLogger.LogLine(": Small");
                                    break;
                                case 2:
                                    DebugLogger.LogLine(": Medium");
                                    break;
                                case 3:
                                    DebugLogger.LogLine(": Compact");
                                    break;
                                case 4:
                                    DebugLogger.LogLine(": Large");
                                    break;
                                case 5:
                                    DebugLogger.LogLine(": Big");
                                    break;
                                case 6:
                                    DebugLogger.LogLine(": Huge");
                                    break;
                                default:
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, ": Unknown");
                                    DebugLogger.Unindent();
                                    return false;
                            }
                            DebugLogger.Unindent();
                            AdExtensionPart.MemoryModelSize = (MemoryModelSize)n3;
                            break;
                    }

                }
                else if (NextRecordIdIs(0xE2CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Record E2CE.  Wonder what it means.");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2} ", file[index++]);
                    return false;
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unknown field.");
                    return false;
                }

            return true;
        }


        /// <summary>
        /// Returns which part of the file the given pointer is in.
        /// </summary>
        /// <param name="p">Pointer into file.</param>
        /// <returns></returns>
        protected int WhichPart(int p)
        {
            // I spent, like, half-an-hour thinking of a way to do this without assuming a particular order to the parts.
            // The idea here is that the pointer p belongs to the part in which the difference between p and the part start
            // pointer of the part is the smallest non-negative value.
            //             P1        P5       P2          P7       P3
            // |===========|=========|========|=====p=====|========|=====|
            //                                      <----->-------->
            //                                    negative differences
            //             <----large difference---->
            //                                <-----> small difference
            // It probably IS safe to assume an order.  But it's easy enough to write this so it doesn't.
            int s = -1;
            int d = 0x7FFFFFFF;
            for (int i = 0; i < 8; i++)
            {
                if (Parts[i] == 0)
                    continue;
                int f = p - Parts[i];
                if (f >= 0 && f < d)
                {
                    d = f;
                    s = i;
                }
            }
            return s;
        }


        #region Info Structs

        public struct EnvironmentInfo
        {
            public Dictionary<int, string> NnRecords;
            public DateTime DateTime;
            public string CommandLine;
            public ExecutionStatus ExecutionStatus;
            public HostEnvironment HostEnvironment;
            public int ToolNumber;
            public int ToolVersion;
            public int ToolRevision;
            public char ToolLetter;
            public string Comments;
            public int IDontKnow;
        }

        public enum ExecutionStatus
        {
            Success = 0,
            Warning = 1,
            Error = 2,
            FatalError = 3,
        }

        public enum HostEnvironment
        {
            Other = 0,
            Vms = 1,
            MsDos = 2,
            Unix = 3,
            HpUx = 4,
        }


        /// <summary>
        /// Contains information about the file.
        /// </summary>
        public struct AdExtensionInfo
        {
            public Dictionary<int, string> NnRecords;
            public int VersionNumber;
            public int RevisionNumber;
            public ObjectFormatType ObjectFormatType;
            public bool CaseSensitive;
            public MemoryModelSize MemoryModelSize;
        }

        public enum ObjectFormatType
        {
            Absolute = 1,
            Relocatable = 2,
            Loadable = 3,
            Library = 4,
        }

        public enum MemoryModelSize
        {
            Tiny = 0,
            Small = 1,
            Medium = 2,
            Compact = 3,
            Large = 4,
            Big = 5,
            Huge = 6,
        }

        #endregion


        #region File Parsing Basics
        /// <summary>
        /// Tests whether the next record ID matches the ID given.  If it does,
        /// the file read index is incremented past the ID.  Otherwise, it is
        /// not so you can test against a different value.
        /// </summary>
        /// <param name="id">ID number.  Byte count is inferred from value.</param>
        /// <returns>True if match found.</returns>
        protected bool NextRecordIdIs(int id, bool eatField = true)
        {
            if (id < 0x100)
                if (file[index] == id)
                {
                    if (eatField)
                        index++;
                    return true;
                }
                else
                    return false;
            if (id < 0x10000)
                if (file[index] == id >> 8 && file[index + 1] == (id & 0xFF))
                {
                    if (eatField)
                        index += 2;
                    return true;
                }
                else
                    return false;
            if (id < 0x1000000)
                if (file[index] == id >> 16 && (file[index + 1] == (id >> 8 & 0xFF)) && file[index + 2] == (id & 0xFF))
                {
                    if (eatField)
                        index += 3;
                    return true;
                }
                else
                    return false;
            if (file[index] == id >> 24 && file[index + 1] == (id >> 16 & 0xFF) && file[index + 2] == (id >> 8 & 0xFF) && file[index + 3] == (id & 0xFF))
            {
                if (eatField)
                    index += 4;
                return true;
            }
            else
                return false;

        }
        

        /// <summary>
        /// Checks if the next item in the file can be parsed as a number.
        /// index is not incremented.
        /// </summary>
        /// <returns>True if the next item is a number</returns>
        protected bool NextItemIsNumber()
        {
            return file[index] <= 0x84;
        }

        /// <summary>
        /// Checks if the next item in the file can be parsed as a string.
        /// index is not incremented.
        /// </summary>
        /// <returns>True if the next item is a string</returns>
        protected bool NextItemIsString()
        {
            return file[index] == 0xDE || file[index] == 0xDF;
        }

        /// <summary>
        /// Reads a string, and increments index.
        /// </summary>
        /// <returns></returns>
        protected string ReadString()
        {
            bool isEscapedValue;
            int length = ReadNumber(out isEscapedValue);
            string s = ASCIIEncoding.ASCII.GetString(file, index, length);
            index += length;
            return s;
        }

        /// <summary>
        /// Reads a number from the file, and increments index.
        /// </summary>
        /// <param name="isEscapedValue">True if the returned value is not a number, but a special escaped byte of some kind.</param>
        /// <returns>The number or escaped byte read.</returns>
        protected int ReadNumber(out bool isEscapedValue)
        {
            isEscapedValue = false;
            byte b = file[index++];
            int r;
            if (b < 0x80)
                return b;
            if (b == 0x80)
                return 0;
            if (isEscapedValue = (b >= 0x89 && b != 0xDE && b != 0xDF))
                return b;
            r = file[index++];
            if (b == 0x81 || b == 0xDE)
                return r;
            r = (r << 8) | file[index++];
            if (b == 0x82 || b == 0xDF)
                return r;
            r = (r << 8) | file[index++];
            if (b == 0x83)
                return r;
            r = (r << 8) | file[index++];
            if (b == 0x84)
                return r;
            // Don't handle extra-large ints
            return -1;
        }


        public static int ReadNumber(byte[] data, ref int index, out bool isEscapedValue)
        {
            isEscapedValue = false;
            byte b = data[index++];
            int r;
            if (b < 0x80)
                return b;
            if (b == 0x80)
                return 0;
            if (isEscapedValue = (b >= 0x89 && b != 0xDE && b != 0xDF))
                return b;
            r = data[index++];
            if (b == 0x81 || b == 0xDE)
                return r;
            r = (r << 8) | data[index++];
            if (b == 0x82 || b == 0xDF)
                return r;
            r = (r << 8) | data[index++];
            if (b == 0x83)
                return r;
            r = (r << 8) | data[index++];
            if (b == 0x84)
                return r;
            // Don't handle extra-large ints
            return -1;
        }
        #endregion


        protected OmfExpression ReadExpression()
        {
            return OmfExpression.FromArray(ref index, file);
        }

    }
}