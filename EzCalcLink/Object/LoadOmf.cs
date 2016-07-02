using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
    /// <summary>
    /// Exception thrown where there's an error parsing an OMF file.
    /// </summary>
    public class OmfFileParseException : Exception
    {
        public readonly int Index;

        public OmfFileParseException(int index)
        {
            Index = index;
        }

        public OmfFileParseException(int index, string message)
            : base(message)
        {
            Index = index;
        }

        public OmfFileParseException(int index, string message, Exception inner)
            : base(message, inner)
        {
            Index = index;
        }
    }


    public class LoadOmf
    {
        protected ObjectFile Obj = new ObjectFile();

        /// <summary>
        /// Should be true for eZ80.
        /// </summary>
        protected bool AddressLittleEndian;
        /// <summary>
        /// Assign Pointer to AD Extension Part (ASW0)
        /// </summary>
        protected int PtrToAsw0;
        /// <summary>
        /// Assign Pointer to Environment Part (ASW1)
        /// </summary>
        protected int PtrToAsw1;
        /// <summary>
        /// Assign Pointer to Section Part (ASW2)
        /// </summary>
        protected int PtrToAsw2;
        /// <summary>
        /// Assign Pointer to External Part (ASW3)
        /// </summary>
        protected int PtrToAsw3;
        /// <summary>
        /// Assign Pointer to Debug Information Part (ASW4)
        /// </summary>
        protected int PtrToAsw4;
        /// <summary>
        /// Assign Pointer to Data Part (ASW5)
        /// </summary>
        protected int PtrToAsw5;
        /// <summary>
        /// Assign Pointer to Trailer Part (ASW6)
        /// </summary>
        protected int PtrToAsw6;
        /// <summary>
        /// Assign Pointer to Module End Part (ASW7)
        /// </summary>
        protected int PtrToAsw7;

        /// <summary>
        /// List of address spaces
        /// </summary>
        // I think Zilog's compiler is hard-coded to produce these, so it
        // should be safe to assume they have a fixed order and IDs.
        // TODO: Accept 16-bit code for RAM and ROM, i.e. MAU = 2
        protected KeyValuePair<int, AddressSpace>[] addressSpaces = new KeyValuePair<int, AddressSpace>[] {
                new KeyValuePair<int, AddressSpace>(0, null),
                new KeyValuePair<int, AddressSpace>(0x44, new AddressSpace() { Name = "RAM", MauSize = 3 }),
                new KeyValuePair<int, AddressSpace>(0x43, new AddressSpace() { Name = "ROM", MauSize = 3 }),
                new KeyValuePair<int, AddressSpace>(0x45, new AddressSpace() { Name = "EXTIO", MauSize = 2 }),
                new KeyValuePair<int, AddressSpace>(0x49, new AddressSpace() { Name = "INTIO", MauSize = 1 }),
            };

        /// <summary>
        /// Returns the address space with a given ID.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected AddressSpace GetAddressSpaceById(int i)
        {
            return addressSpaces.Where(x => x.Key == i).First().Value;
        }

        /// <summary>
        /// Returns the address space with a given index.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected AddressSpace GetAddressSpaceByIndex(int i)
        {
            if (i > 0 && i <= 4)
                return addressSpaces[i].Value;
            ShowFatalError("Requested context index {0} is not accepted by parser.", i);
            return null;
        }


        /// <summary>
        /// List of sections, with the key as the index number.
        /// </summary>
        protected Dictionary<int, Section> sections = new Dictionary<int, Section>();


        /// <summary>
        /// List of symbols, with the key as the index number.
        /// </summary>
        protected Dictionary<int, Symbol> symbols = new Dictionary<int, Symbol>();


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



        public static ObjectFile FromFile(string path)
        {
            LoadOmf o = new LoadOmf(path);
            return o.Obj;
        }

        
        /// <summary>
        /// Internal use only
        /// </summary>
        protected LoadOmf(string path)
        {
            DebugLogger.Log(DebugLogger.LogType.Basic, "Loading file ");
            DebugLogger.LogLine(path);
            // Load file.  Might as well crash now if the file can't be read.
            file = System.IO.File.ReadAllBytes(path);
            Obj.Name = path;
            // Copy address space list to output object
            foreach (KeyValuePair<int, AddressSpace> k in addressSpaces)
                Obj.AddressSpaces.Add(k.Value);
            // Parse file
            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Parsing header. . . .");
            DebugLogger.Indent();
            ParseHeader();
            DebugLogger.Unindent();

            // AD Extension Part
            index = PtrToAsw0;
            DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Parsing P0. . . .");
            DebugLogger.Indent();
            ParseP0();
            DebugLogger.Unindent();

            // Environment Part
            index = PtrToAsw1;
            DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Parsing P1. . . .");
            DebugLogger.Indent();
            ParseP1();
            DebugLogger.Unindent();

            // Section information
            index = PtrToAsw2;
            DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Parsing P2. . . .");
            DebugLogger.Indent();
            ParseP2();
            DebugLogger.Unindent();

            // Symbols
            index = PtrToAsw3;
            DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Parsing P3. . . .");
            DebugLogger.Indent();
            ParseP3();
            DebugLogger.Unindent();

            // Debug information
            DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Skipping debug information.");

            // Data
            index = PtrToAsw5;
            DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Basic, "Parsing P5. . . .");
            DebugLogger.Indent();
            ParseP5();
            DebugLogger.Unindent();

            
            // Copy section list to output object
            foreach (KeyValuePair<int, Section> s in sections)
                Obj.Sections.Add(s.Value);
            // Copy symbol list to output object
            foreach (KeyValuePair<int, Symbol> s in symbols)
                Obj.Symbols.Add(s.Value);
        }


        protected void ParseP5()
        {
            Section s;
            int nextExpectedAddress = 0;
            while (WhichPart(index) == 5)
                if (NextRecordIdIs(0xE5))
                {
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Current section index (SB): {0} {1}", currentSection, sections[currentSection].Name);
                }
                else if (NextRecordIdIs(0xE2D0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Current section PC (ASP)");
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0} {1}", currentSection, sections[currentSection].Name);
                    s = sections[currentSection];
                    if (NextItemIsNumber())
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                        " Basic value: 0x{0:X6}", ReadNumber());
                    else
                    {
                        if (file[index++] != 0xD2) // R variable
                            ShowFatalError("Parse PC location expression error: Did not get expected R token.");
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                            " Section {0} + ", sections[ReadNumber()].Name);
                        if (file[index] == 0x90) // Ignore comma entity
                            index++;
                        DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                            " Value: 0x{0:X6}", ReadNumber());
                        if (file[index] == 0x90) // Ignore comma entity
                            index++;
                        if (file[index++] != 0xA5) // Add function
                            ShowFatalError("Parse PC location expression error: Did not end with expected add function.");
                    }
                }
                else if (NextRecordIdIs(0xED))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Load Constant MAUs (LD)");
                    int n = ReadNumber();
                    DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                        " Data bytes: {0}  ", n);
                    for (int i = 0; i < n; i++)
                    {
                        DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryHighlyVerbose | DebugLogger.LogType.FieldValue,
                            "{0:X2}", file[index + i]);
                    }
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryHighlyVerbose | DebugLogger.LogType.FieldValue);
                    index += n;
                }
                /*else if (NextRecordIdIs(0xE3)) Zilog doesn't appear to use these.
                else if (NextRecordIdIs(0xF7))
                else if (NextRecordIdIs(0xE2D2))
                else if (NextRecordIdIs(0xE2D7))*/
                else if (NextRecordIdIs(0xE4))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Load With Relocation (LR)");
                    while (file[index] < 0xE0)
                    {
                        if (NextItemIsNumber())
                        {
                            int bits = ReadNumber();
                            int bytes = bits >> 3;
                            DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                                " Data bytes: {0}  ", bytes);
                            for (int i = 0; i < bytes; i++)
                            {
                                DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryHighlyVerbose | DebugLogger.LogType.FieldValue,
                                    "{0:X2}", file[index + i]);
                            }
                            index += bytes;
                            DebugLogger.LogLine(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryHighlyVerbose | DebugLogger.LogType.FieldValue);
                        }
                        else
                        {
                            DebugLogger.Log(DebugLogger.LogType.P5 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                                " Relocation: ");
                            DebugLogger.LogLine(" Expression: {0}", OmfExpression.FromArray(ref index, file));
                        }
                    }
                }
                //else if (NextRecordIdIs(0xFA)) Zilog doesn't appear to use this.
                else
                {
                    ShowFatalError("P5: Unknown or unexpected field.");
                }
        }

        
        protected void ParseP3()
        {
            int n1, n2, n3;
            while (WhichPart(index) == 3)
                if (NextRecordIdIs(0xE8))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Public (external) symbol (NI)");
                    n1 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Public name index record: {0}", n1);
                    if (symbols.ContainsKey(n1))
                        ShowFatalError("NI: Symbol index {0} already used.", n1);
                    Symbol s = new Symbol();
                    s.External = false;
                    s.ObjectFile = Obj;
                    s.Section = sections[currentSection];
                    s.Name = ReadString();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Symbol name: {0}", s.Name);
                    symbols[n1] = s;
                }
                else if (NextRecordIdIs(0xF1C9))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Variable attribute (ATI)");
                    n1 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", n1);
                    Symbol s = symbols[n1];
                    n2 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol type index: {0}", n2);
                    if (n2 != 0)
                        DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Basic | DebugLogger.LogType.FieldValue,
                            " P3 ATI record: Well, isn't this interesting! A non-zero symbol type index: {0}", n2);
                    n3 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Attribute definition: {0}", n3);
                    if (n3 != 19)
                        DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Basic | DebugLogger.LogType.FieldValue,
                            " P3 ATI record: Well, isn't this interesting! Attribute definition is not 19: {0}", n3);
                    s.AddressSpace = GetAddressSpaceById(ReadNumber());
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Address space: {0}", s.AddressSpace.Name);
                }
                else if (NextRecordIdIs(0xE2C9))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable values (ASI)");
                    n1 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol index: {0}", n1);
                    Symbol s = symbols[n1];
                    if (NextItemIsNumber())
                    {
                        s.Offset = ReadNumber();
                        s.Resolved = true;
                        DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                            " Symbol value: 0x{0:X6}", s.Offset);
                    }
                    else
                    {
                        s.Resolved = false;
                        // I'm hard-coding the parser for relocatable symbol expressions to keep things a bit more simple.
                        if (file[index++] != 0xD2) // 'R' variable ID
                            ShowFatalError("Relocatable symbol value expression parse error: Did not get expected section variable specifier.");
                        if (!NextItemIsNumber())
                            ShowFatalError("Relocatable symbol value expression parse error: Did not get a number as the expected argument to R-variable.");
                        if ((n2 = ReadNumber()) != currentSection)
                            ShowFatalError("Relocatable symbol value expression parse error: Expected R-variable section reference ({0}) to match current section ({1}).", n2, currentSection);
                        if (file[index] == 0x90) // Ignore comma entity
                            index++;
                        s.Offset = ReadNumber();
                        if (file[index] == 0x90) // Ignore comma entity
                            index++;
                        if (file[index++] != 0xA5) // Add function
                            ShowFatalError("Relocatable symbol value expression parse error: Expression did not end with expected add function.");
                        DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                            " Symbol value: Base of section {0} + 0x{1:X6}", s.Section.Name, s.Offset);
                    }

                }
                //else if (NextRecordIdIs(0xE2D2)) Should not be used
                else if (NextRecordIdIs(0xE9))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "External reference name (NX)");
                    n1 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " External reference index: {0}", n1);
                    if (symbols.ContainsKey(n1))
                        ShowFatalError("NX: Symbol index {0} already used.", n1);
                    Symbol s = new Symbol();
                    s.External = true;
                    s.ObjectFile = Obj;
                    s.Name = ReadString();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Symbol name: {0}", s.Name);
                    symbols[n1] = s;
                }
                //else if (NextRecordIdIs(0xF1D8)) Zilog doesn't appear to use these.
                //else if (NextRecordIdIs(0xF4))
                else if (NextRecordIdIs(0xE5))
                {
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P3 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Set current section: {0}", currentSection);
                }
                else
                {
                    ShowFatalError("P3: Unknown or unexpected field.");
                }
        }


        protected void ParseP2()
        {
            int n3;
            while (WhichPart(index) == 2)
                if (NextRecordIdIs(0xE6))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section type (ST): ");
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Index: {0}", currentSection);
                    if (sections.ContainsKey(currentSection))
                        ShowFatalError("ST record for already-defined section index #{0}", currentSection);
                    Section s = new Section();
                    sections[currentSection] = s;
                    DebugLogger.Log(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Type: ");
                    if (NextRecordIdIs(0xC1D3D0)) // Normal value?
                    {
                        DebugLogger.LogLine("Absolute code (ASP)");
                        s.Relocatable = false;
                    }
                    /*else if (NextRecordIdIs(0xC1D3D2)) DebugLogger.LogLine("Absolute ROM data (ASR)");*/
                    else if (NextRecordIdIs(0xC1D3C4))
                    {
                        DebugLogger.LogLine("Absolute data (ASD)");
                        s.Relocatable = false;
                    }
                    /*else if (NextRecordIdIs(0xC3D0)) DebugLogger.LogLine("Normal code (CP)");
                    else if (NextRecordIdIs(0xC3D2)) DebugLogger.LogLine("Normal ROM data (CR)");*/
                    else if (NextRecordIdIs(0xC3C4))
                    {
                        DebugLogger.LogLine("Normal data (CD)");
                        s.Relocatable = false;
                    }
                    /*else if (NextRecordIdIs(0xC5C1D0)) DebugLogger.LogLine("Common absolute code (EAP)");
                    else if (NextRecordIdIs(0xC5C1D2)) DebugLogger.LogLine("Common absolute ROM data (EAR)");
                    else if (NextRecordIdIs(0xC5C1C4)) DebugLogger.LogLine("Common absolute data (EAD)");
                    else if (NextRecordIdIs(0xC5DA)) DebugLogger.LogLine("Something about short common with error checking.");
                    else if (NextRecordIdIs(0xCDC1D0)) DebugLogger.LogLine("Common absolute code without size constraint thingy (MAP)");
                    else if (NextRecordIdIs(0xCDC1D2)) DebugLogger.LogLine("Common absolute ROM data without size constraint thingy (MAR)");
                    else if (NextRecordIdIs(0xCDC1C4)) DebugLogger.LogLine("Common absolute data without size constraint thingy (MAD)");
                    else if (NextRecordIdIs(0xDAC3D0)) DebugLogger.LogLine("Short code (ZCP)");
                    else if (NextRecordIdIs(0xDAC3D2)) DebugLogger.LogLine("Short ROM data (ZCR)");
                    else if (NextRecordIdIs(0xDAC3C4)) DebugLogger.LogLine("Short data (ZCD)");
                    else if (NextRecordIdIs(0xDACD)) DebugLogger.LogLine("Short common relocatable sections thingies?");*/
                    else
                        ShowFatalError("Unexpected/unknown section type.");
                    s.Name = ReadString();
                    DebugLogger.LogLine(" Section name: {0}", s.Name);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Parent index: {0}", ReadNumber());
                    DebugLogger.LogLine(" Sibling index: {0}", ReadNumber());
                    n3 = ReadNumber();
                    if (n3 < 5)
                        s.AddressSpace = GetAddressSpaceByIndex(n3);
                    else
                        s.AddressSpace = GetAddressSpaceById(n3);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Context index/Address space: {0:X2}: {1}", n3, s.AddressSpace.Name);
                }
                else if (NextRecordIdIs(0xE7))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Section alignment (SA): ");
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    DebugLogger.LogLine(" Boundary alignment divisor: {0}", ReadNumber());
                    if (NextItemIsNumber())
                        DebugLogger.LogLine(" Page size: {0}", ReadNumber());
                }
                else if (NextRecordIdIs(0xE2D3))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section size (ASS): ");
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    if (!sections.ContainsKey(currentSection))
                        ShowFatalError("ASS record: Section #{0} not yet defined.", currentSection);
                    Section s = sections[currentSection];
                    s.ExpectedSize = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader, 
                        " Section (expected) size: {0}", s.ExpectedSize);
                }
                else if (NextRecordIdIs(0xE2CC))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section base address (ASL): ");
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    if (!sections.ContainsKey(currentSection))
                        ShowFatalError("ASL record: Section #{0} not yet defined.", currentSection);
                    Section s = sections[currentSection];
                    s.BaseAddress = ReadNumber();
                    s.Resolved = true;
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Section base address: 0x{0:X6}", s.BaseAddress);
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Section offset (ASR): ");
                    currentSection = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", currentSection);
                    if (!sections.ContainsKey(currentSection))
                        ShowFatalError("ASR record: Section #{0} not yet defined.", currentSection);
                    Section s = sections[currentSection];
                    s.BaseAddress = ReadNumber();
                    s.Resolved = true;
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Section base address/offset: 0x{0:X6}", s.BaseAddress);
                }
                else if (NextRecordIdIs(0xFB))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Define context (NC): ");
                    int contextIndex = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Context index: 0x{0:X2}", contextIndex);
                    if (contextIndex < 1 || contextIndex > 4)
                        ShowFatalError("NC: Context index {0} is not expected from Zilog OMF file.", contextIndex);
                    string contextName = ReadString();
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Context name: {0}", contextName);
                    if (addressSpaces[contextIndex].Value.Name.CompareTo(contextName) != 0)
                        ShowFatalError("NC: Context #{0} name given ({1}) does not match expected Zilog name ({2}).", contextIndex, contextName, addressSpaces[contextIndex].Value.Name);
                    if (!NextItemIsNumber())
                        ShowFatalError("NC: Zilog puts extra fields here that were not supplied.");
                    int unknown = ReadNumber();
                    DebugLogger.LogLine(" Context unknown data: 0x{0:X2}", unknown);
                    int contextId = ReadNumber();
                    DebugLogger.LogLine(" Context ID: 0x{0:X2}", contextId);
                    if (addressSpaces[contextIndex].Key != contextId)
                        ShowFatalError("NC: Expected context ID (0x{0:X2}) does not match expected Zilog ID (0x{1:X2}).", contextId, addressSpaces[contextIndex].Key);
                }
                /* Omitted due to not being part of Zilog's standard.
                else if (NextRecordIdIs(0xE2C1))
                else if (NextRecordIdIs(0xE2C2))*/
                else if (NextRecordIdIs(0xE2C6))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "MAU size (ASF): ");
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Section index: {0}", ReadNumber());
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " MAU size: 0x{0:X2}", ReadNumber());
                }
                /* Omitted due to not being part of Zilog's standard.
                else if (NextRecordIdIs(0xE2CD)) */
                /*else if (NextRecordIdIs(0xE5)) // Apparently, this is legal?
                {
                    currentSection = ReadNumber(out isEscapedValue);
                    DebugLogger.LogLine(DebugLogger.LogType.P2 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Current section index (SB): {0}", currentSection);
                }*/
                else
                {
                    ShowFatalError("P2: Unknown or unexpected field.");
                }
        }


        protected void ParseP1()
        {
            int n3;
            while (WhichPart(index) == 1)
                if (NextRecordIdIs(0xF0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Variable Attributes (NN)");
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Version: {0}", ReadNumber());
                    DebugLogger.LogLine(" ID: {0}", ReadString());
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable Attributes (ATN)");
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", ReadNumber());
                    DebugLogger.LogLine(" Should be zero: {0}", ReadNumber());
                    n3 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P1 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue,
                        " Attribute definition: {0}, ", n3);
                    switch (n3)
                    {
                        case 50:
                            DebugLogger.LogLine("Creation date and time: {0}", new DateTime(ReadNumber() + 1900, ReadNumber(), ReadNumber(), ReadNumber(), ReadNumber(), ReadNumber()));
                            break;
                        case 51:
                            DebugLogger.LogLine("Creation command line: {0}", ReadString());
                            break;
                        case 52:
                            n3 = ReadNumber();
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
                                    break;
                            }
                            break;
                        case 53:
                            DebugLogger.Log("Host environment: ");
                            n3 = ReadNumber();
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
                                    break;
                            }
                            break;
                        case 54:
                            DebugLogger.LogLine("Tool version information");
                            DebugLogger.LogLine("  Tool number: {0}", ReadNumber());
                            DebugLogger.LogLine("  Tool {0}", ReadNumber());
                            DebugLogger.LogLine("  Tool {0}", ReadNumber());
                            if (file[index] >= 0xC1 && file[index] <= 0xDA)
                                DebugLogger.LogLine("  Tool letter: {0}", (char)(file[index++] - 0xC1 + (int)'A'));
                            break;
                        case 55:
                            DebugLogger.Log("Comments: {0}", ReadString());
                            break;
                        case 56:
                            DebugLogger.LogLine("I dunno: {0}: Total mystery.", ReadNumber());
                            break;
                        default:
                            ShowError("P1: Unknown attribute.");
                            return;
                    }

                }
                else if (NextRecordIdIs(0xE2CE))
                {
                    ShowFatalError("P1: Record E2CE: I don't know what this is for.");
                }
                else
                {
                    ShowFatalError("P1: Unknown or unexpected field.");
                }
        }


        protected void ParseP0()
        {
            int n3;
            while (WhichPart(index) == 0)
                if (NextRecordIdIs(0xF0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Variable Attributes (NN)");
                    n3 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Version: {0}", n3);
                    DebugLogger.LogLine(" ID: {0}", ReadString());
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldHeader,
                        "Variable Attributes (ATN)");
                    n3 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", n3);
                    DebugLogger.LogLine(" Should be zero: {0}", ReadNumber());
                    n3 = ReadNumber();
                    DebugLogger.Log(DebugLogger.LogType.P0 | DebugLogger.LogType.Verbose | DebugLogger.LogType.FieldValue, 
                        " Attribute definition: {0}, ", n3);
                    switch (n3)
                    {
                        case 37:
                            DebugLogger.LogLine(" Object format version");
                            DebugLogger.LogLine("  Version: {0}", ReadNumber());
                            DebugLogger.LogLine("  Revision: {0}", ReadNumber());
                            break;
                        case 38:
                            n3 = ReadNumber();
                            DebugLogger.Log(" Object format type {0}", n3);
                            DebugLogger.Indent();
                            switch (n3)
                            {
                                case 1:
                                    Obj.Relocatable = false;
                                    DebugLogger.LogLine(": Absolute");
                                    break;
                                case 2:
                                    Obj.Relocatable = true;
                                    DebugLogger.LogLine(": Relocatable");
                                    break;
                                case 3:
                                    DebugLogger.LogLine(": Loadable");
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unsupported");
                                    break;
                                case 4:
                                    DebugLogger.LogLine(": Library");
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, "Unsupported");
                                    break;
                                default:
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, ": Unknown");
                                    break;
                            }
                            DebugLogger.Unindent();
                            break;
                        case 39:
                            DebugLogger.Indent();
                            n3 = ReadNumber();
                            DebugLogger.Log(" Case sensitivity {0}", n3);
                            switch (n3)
                            {
                                case 1:
                                    Obj.CaseSensitive = false;
                                    DebugLogger.LogLine(": False");
                                    break;
                                case 2:
                                    Obj.CaseSensitive = true;
                                    DebugLogger.LogLine(": True");
                                    break;
                                default:
                                    DebugLogger.LogLine(DebugLogger.LogType.Error, ": Unknown");
                                    DebugLogger.Unindent();
                                    break;
                            }
                            DebugLogger.Unindent();
                            break;
                        case 40:
                            DebugLogger.Log(" Memory model {0}", ReadNumber());
                            break;
                    }

                }
                else if (NextRecordIdIs(0xE2CE))
                {
                    ShowFatalError("P0: Record E2CE: I don't know what this is for.");
                }
                else
                {
                    ShowFatalError("P0: Unknown or unexpected field.");
                }
        }


        protected void ParseHeader()
        {
            if (!NextRecordIdIs(0xE0))
            {
                ShowFatalError("File does not begin with record 0xE0 (Module Beginning).");
            }
            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldValue | DebugLogger.LogType.Verbose, "Processor: {0}", ReadString());
            DebugLogger.LogLine("Module name: {0}", ReadString());
            while (WhichPart(index) == -1)
                if (NextRecordIdIs(0xEC)) // Address Descriptor
                {
                    DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, "Address Descriptor (AD)");
                    int BitsPerMau = ReadNumber();
                    if (BitsPerMau != 8)
                        ShowFatalError("Bad byte size: {0}", BitsPerMau);
                    int MausPerAddress = ReadNumber();
                    if (MausPerAddress != 3) // TODO: Short mode support
                        ShowFatalError("Bad word size: {0} (only long mode supported; no Z80 mode support)", MausPerAddress);
                    if (file[index] == 0xCC)
                    {
                        index++;
                        DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldValue | DebugLogger.LogType.VeryVeryVerbose,
                            " File claims to be little-endian.");
                    }
                    else if (file[index] == 0xCD)
                    {
                        index++;
                        DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldValue | DebugLogger.LogType.VeryVeryVerbose,
                            " File claims to be big-endian; that's probably a lie.");
                    }
                    else
                        DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldValue | DebugLogger.LogType.VeryVeryVerbose,
                            " File does not specify endianess.");
                }
                else if (NextRecordIdIs(0xE2D7))
                {
                    int asw = ReadNumber();
                    int offset = ReadNumber();
                    Parts[asw] = offset;
                    DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose,
                        "Assign Pointer to part {0}: {1:X8}", asw, Parts[asw]);
                    switch (asw)
                    {
                        case 0:
                            PtrToAsw0 = offset;
                            break;
                        case 1:
                            PtrToAsw1 = offset;
                            break;
                        case 2:
                            PtrToAsw2 = offset;
                            break;
                        case 3:
                            PtrToAsw3 = offset;
                            break;
                        case 4:
                            PtrToAsw4 = offset;
                            break;
                        case 5:
                            PtrToAsw5 = offset;
                            break;
                        case 6:
                            PtrToAsw6 = offset;
                            break;
                        case 7:
                            PtrToAsw7 = offset;
                            break;
                        default:
                            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.Error, "Unknown ASW: {0}", asw);
                            DebugLogger.LogLine(" Offset: {0:X8}", offset);
                            break;
                    }   
                }
                else
                {
                    ShowFatalError("Header: Unknown or unexpected field!");
                }
        }
        

        /// <summary>
        /// Does a debug log dump of the bytes around the current file read index.
        /// </summary>
        protected void DoHexDump()
        {
            DebugLogger.Log(DebugLogger.LogType.Error | DebugLogger.LogType.Verbose, "Hex dump starting at 0x{0:X8}: ", index >= 16 ? index - 16 : 0);
            for (int i = index >= 16 ? -16 : -index; i < 16; i++)
                if (i == 0) // Highlight index where dump is centered
                    DebugLogger.Log(" {0:X2} ", file[index + i]);
                else
                    DebugLogger.Log("{0:X2}", file[index + i]);
            DebugLogger.LogLine();
        }


        /// <summary>
        /// Shows an error message, a hex dump, but allows parsing to continue.
        /// </summary>
        /// <param name="message"></param>
        protected void ShowError(string message)
        {
            DebugLogger.LogLine(DebugLogger.LogType.Error, "OMF parse error at 0x{0:X8}: {1}", index, message);
            DoHexDump();
        }


        /// <summary>
        /// Shows an error message, a hex dump, but allows parsing to continue.
        /// </summary>
        /// <param name="message"></param>
        protected void ShowError(string message, params object[] args)
        {
            DebugLogger.Log(DebugLogger.LogType.Error, "OMF parse error at 0x{0:X8}: ", index);
            DebugLogger.LogLine(message, args);
            DoHexDump();
        }


        /// <summary>
        /// Shows an error message, a hex dump, and throws an OmfFileParseException to abort parsing.
        /// </summary>
        /// <param name="message"></param>
        protected void ShowFatalError(string message)
        {
            DebugLogger.LogLine(DebugLogger.LogType.FatalError, "OMF parse fatal error at 0x{0:X8}: {1}", index, message);
            DoHexDump();
            throw new OmfFileParseException(index, message);
        }


        /// <summary>
        /// Shows an error message, a hex dump, and throws an OmfFileParseException to abort parsing.
        /// </summary>
        /// <param name="message"></param>
        protected void ShowFatalError(string message, params object[] args)
        {
            DebugLogger.Log(DebugLogger.LogType.FatalError, "OMF parse fatal error at 0x{0:X8}: ", index);
            DebugLogger.LogLine(message, args);
            DoHexDump();
            throw new OmfFileParseException(index, string.Format(message, args));
        }


        #region File Parsing Helpers
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
        /// Reads a string, and increments index.
        /// </summary>
        /// <returns></returns>
        protected string ReadString()
        {
            int length = ReadNumber();
            string s = ASCIIEncoding.ASCII.GetString(file, index, length);
            index += length;
            return s;
        }


        /// <summary>
        /// Discards the next string in the file.
        /// </summary>
        protected void SkipString()
        {
            int length = ReadNumber();
            index += length;
        }


        /// <summary>
        /// Reads a number from the file, and increments index.
        /// </summary>
        /// <returns>The number.</returns>
        protected int ReadNumber()
        {
            byte b = file[index++];
            int r;
            if (b < 0x80)
                return b;
            if (b == 0x80)
                return 0;
            if (b >= 0x89)
                ShowFatalError("ReadNumber expected a number, did not get a number.");
            r = file[index++];
            if (b == 0x81)
                return r;
            r = (r << 8) | file[index++];
            if (b == 0x82)
                return r;
            r = (r << 8) | file[index++];
            if (b == 0x83)
                return r;
            r = (r << 8) | file[index++];
            if (b == 0x84)
                return r;
            // Don't handle extra-large ints
            ShowFatalError("ReadNumber got giant integer.");
            return -1;
        }


        /// <summary>
        /// Just ignores whatever number comes next in the file.
        /// </summary>
        protected void SkipNumber()
        {
            byte b = file[index++];
            if (b <= 0x80)
                return;
            if (b >= 0x89)
                ShowFatalError("ReadNumber/SkipNumber expected a number, did not get a number.");
            if (b >= 0x85)
                ShowFatalError("ReadNumber/SkinNumber got giant integer.");
            index += b & 0x0F;
        }
        #endregion
    }
}
