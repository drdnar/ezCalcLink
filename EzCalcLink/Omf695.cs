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
            while (true)
                if (!parseField(1))
                    return;
        }

        protected bool parseField(int nesting)
        {
            Indent(nesting); Log("Parsing field " + file[index].ToString("X2") + ", ");

            bool isEscapedValue = false;

            switch (file[index++])
            {
                case 0xE0: // Module Beginning
                    Indent(nesting); LogLine("Module Beginning (MB)");
                    Processor = ReadString();
                    Indent(nesting); Log(" Processor = "); LogLine(Processor);
                    ModuleName = ReadString();
                    Indent(nesting); Log(" Module name = "); LogLine(ModuleName);
                    break;
                case 0xEC: // Address Descriptor
                    Indent(nesting); LogLine("Address Descriptor (AD)");
                    BitsPerMau = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Bits per MAU = "); LogLine(BitsPerMau.ToString());
                    MausPerAddress = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" MAUs per address = "); LogLine(MausPerAddress.ToString());
                    byte b = file[index];
                    if (b == 0xCC || b == 0xCD)
                        index++;
                    AddressLittleEndian = b == 0xCC;
                    Indent(nesting);
                    if (AddressLittleEndian)
                        LogLine(" Addresses are little-endian");
                    else
                        LogLine(" Addresses are big-endian");
                    break;
                case 0xE2: // Extended code
                    Indent(nesting); Log("extended code, " + file[index].ToString("X2") + ", ");
                    switch (file[index++])
                    {
                        case 0xD7:
                            Indent(nesting); Log("extended code, " + file[index].ToString("X2") + ", ");
                            switch (file[index++])
                            {
                                case 0:
                                    Indent(nesting); LogLine("Assign Pointer to AD Extension Part (ASW0)");
                                    Parts[0] = PtrToAsw0 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW0 offset = " + PtrToAsw0.ToString("X8"));
                                    break;
                                case 1:
                                    Indent(nesting); LogLine("Assign Pointer to Environment Part (ASW1)");
                                    Parts[1] = PtrToAsw1 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW1 offset = " + PtrToAsw1.ToString("X8"));
                                    break;
                                case 2:
                                    Indent(nesting); LogLine("Assign Pointer to Section Part (ASW2)");
                                    Parts[2] = PtrToAsw2 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW2 offset = " + PtrToAsw2.ToString("X8"));
                                    break;
                                case 3:
                                    Indent(nesting); LogLine("Assign Pointer to External Part (ASW3)");
                                    Parts[3] = PtrToAsw3 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW3 offset = " + PtrToAsw3.ToString("X8"));
                                    break;
                                case 4:
                                    Indent(nesting); LogLine("Assign Pointer to Debug Information Part (ASW4)");
                                    Parts[4] = PtrToAsw4 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW4 offset = " + PtrToAsw4.ToString("X8"));
                                    break;
                                case 5:
                                    Indent(nesting); LogLine("Assign Pointer to Data Part (ASW5)");
                                    Parts[5] = PtrToAsw5 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW5 offset = " + PtrToAsw5.ToString("X8"));
                                    break;
                                case 6:
                                    Indent(nesting); LogLine("Assign Pointer to Trailer Part (ASW6)");
                                    Parts[6] = PtrToAsw6 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW6 offset = " + PtrToAsw6.ToString("X8"));
                                    break;
                                case 7:
                                    Indent(nesting); LogLine("Assign Pointer to Module End Part (ASW7)");
                                    Parts[7] = PtrToAsw7 = ReadNumber(out isEscapedValue);
                                    Indent(nesting); LogLine(" ASW7 offset = " + PtrToAsw7.ToString("X8"));
                                    break;
                                default:
                                    Indent(nesting); LogLine("Unknown field.");
                                    return false;
                            }
                            break;
                        default:
                            Indent(nesting); LogLine("Unknown field.");
                            return false;
                    }
                    break;
                case 0xF0:
                    index--;
                    switch (WhichPart(index))
                    {
                        case 0:
                            Indent(nesting); LogLine("Start of ASW0");
                            return parseP0(nesting + 1);
                        case 1:
                            Indent(nesting); LogLine("Start of ASW1");
                            return parseP1(nesting + 1);
                        default:
                            Indent(nesting); LogLine("Field F0 unexpected.");
                            return false;
                    }
                case 0xE6:
                    index--;
                    //if (WhichPart(index) == 2)
                    //{
                        Indent(nesting); LogLine("Start of ASW2");
                        return parseP2(nesting + 1);
                    //}
                    //Indent(nesting); LogLine("Field E6 unexpected.");
                    //break;
                case 0xE5:
                    index--;
                    //if (WhichPart(index) == 5)
                    //{
                    LogLine("");
                    Log("IndexA: "); LogLine(index.ToString("X4"));
                        Indent(nesting); LogLine("Start of ASW5");
                        bool q = parseP5(nesting + 1);
                        Indent(nesting); LogLine("SECTIONS:");
                        Log("IndexB: "); LogLine(index.ToString("X4"));
                        foreach (OmfSection s in Sections)
                        {
                            Indent(nesting + 1); Log("Section Index: "); LogLine(s.Index.ToString());
                            foreach (ContiguousMemory m in s.Memories)
                            {
                                Indent(nesting + 2); Log("Memory record: "); Log(m.StartAddress.ToString("X6"));
                                Log(", size: "); LogLine(m.Size.ToString("X4"));
                                /*Indent(nesting + 3);
                                for (int i = m.StartAddress; i < m.EndAddress; i++)
                                    Log(m[i].ToString("X2"));
                                LogLine("");*/
                            }
                        }
                        Log("IndexC: "); LogLine(index.ToString("X4"));
                    Console.ReadKey();
                        return q;
                    /*}
                    Indent(nesting); LogLine("Field E5 unexpected.");
                    return false;*/
                case 0xE8:
                    index--;
                    Log("External part (ASW3)");
                    return parseP3(nesting + 1);
                case 0xE1:
                    LogLine("Module end (ME)! Let's go home!");
                    return false;
                default:
                    Indent(nesting); LogLine("Unknown field.");
                    for (int i = 0; i < 32; i++ )
                    {
                        if (index + i < file.Length)
                            Log(file[index + i].ToString("X2"));
                    }
                    return false;
            }
            return true;
        }


        protected bool parseP3(int nesting)
        {
            bool isEscapedValue;
            int n1, n2, n3, n4, x1, x3;

            while (true)
                if (NextRecordIdIs(0xE8))
                {
                    Indent(nesting); LogLine("Public (external) symbol (NI)");
                    Indent(nesting); Log(" Public name index record: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Symbol name: "); LogLine(ReadString());
                }
                else if (NextRecordIdIs(0xF1C9))
                {
                    Indent(nesting); LogLine("Variable attribute (ATI)");
                    n1 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Symbol name index: "); LogLine(n1.ToString());
                    n2 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Symbol type index: "); LogLine(n2.ToString());
                    Indent(nesting + 1);
                    switch (n2)
                    {
                        case 0:
                            LogLine("Unspecified.");
                            break;
                        case 3:
                            LogLine("8-bit data byte");
                            break;
                        case 5:
                            LogLine("16-bit short data word");
                            break;
                        case 7:
                            LogLine("32-bit long data word");
                            break;
                        case 10:
                            LogLine("32-bit floating point");
                            break;
                        case 11:
                            LogLine("64-bit floating point");
                            break;
                        case 12:
                            LogLine("10 or 12 byte floating point. Guess which!");
                            break;
                        case 15:
                            LogLine("Instruction address");
                            break;
                        default:
                            LogLine("I DON'T KNOW, PLEASE CHECK");
                            break;
                    }
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Attribute definition: "); Log(n3.ToString());
                    switch (n3)
                    {
                        case 8:
                            Log(" Global compiler symbol.");
                            break;
                        case 16:
                            Log(" Constant. ");
                            x1 = ReadNumber(out isEscapedValue);
                            switch (x1)
                            {
                                case 0:
                                    Log("Unknown class.");
                                    break;
                                case 1:
                                    Log("EQU constant");
                                    break;
                                case 2:
                                    Log("SET constant");
                                    break;
                                case 3:
                                    Log("Pascal CONST constant");
                                    break;
                                case 4:
                                    Log("C #define constant");
                                    break;
                                default:
                                    Log(x1.ToString());
                                    break;
                            }
                            LogLine("");
                            if (file[index] > 0x84)
                                break; // . . . that really shouldn't be legal syntax
                            LogLine("I don't know. I give up. This is impossible to parse unambiguously.");
                            return false;
                        case 19:
                            Log(" Static symbol generated by assembler.");
                            break;
                    }
                    Indent(nesting); Log(" Element count: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                }
                else if (NextRecordIdIs(0xE2C9))
                {
                    Indent(nesting); LogLine("Variable values (ASI)");
                    Indent(nesting); Log(" Symbol index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Symbol value: "); LogLine(ReadNumber(out isEscapedValue).ToString("X6"));
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    Indent(nesting); LogLine("Variable values (ASR), should not use");
                    return false;
                }
                else if (NextRecordIdIs(0xE9))
                {
                    Indent(nesting); LogLine("External reference name (NX)");
                    Indent(nesting); Log(" External reference index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Symbol name: "); LogLine(ReadString());
                }
                else if (NextRecordIdIs(0xF1D8))
                {
                    Indent(nesting); LogLine("External reference relocation information (ATX)");
                    n1 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" External reference index: "); LogLine(n1.ToString());
                    if (file[index] <= 0x84)
                    {
                        Indent(nesting); Log(" Type index: ");
                        if (file[index] == 0x80)
                        {
                            index++;
                            LogLine("Unspecified");
                        }
                        else
                            LogLine(ReadNumber(out isEscapedValue).ToString());
                        if (file[index] <= 0x84)
                        {
                            
                            Indent(nesting); Log(" Section index: ");
                            if (file[index] == 0x80)
                            {
                                index++;
                                LogLine("Unspecified");
                            }
                            else
                                LogLine(ReadNumber(out isEscapedValue).ToString());
                            if (file[index] <= 0x84)
                            {
                                Indent(nesting); Log(" Short external flag: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                            }
                        }
                    }
                }
                else if (NextRecordIdIs(0xF4))
                {
                    Indent(nesting); LogLine("Weak external reference (WX)");
                }
                else
                    return true;
        }

        protected bool parseP2(int nesting)
        {
            bool isEscapedValue;
            int n3;

            while (WhichPart(index) == 2)
            //while (true)
                if (NextRecordIdIs(0xE6))
                {
                    Indent(nesting); LogLine("Section type (ST): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Index: "); LogLine(currentSection.ToString());
                    Indent(nesting); Log(" Type: ");
                    if (NextRecordIdIs(0xC1D3D0))
                    {
                        // Normal value?
                        LogLine("Absolute code (ASP)");
                    }
                    else if (NextRecordIdIs(0xC1D3D2))
                    {
                        LogLine("Absolute ROM data (ASR)");
                    }
                    else if (NextRecordIdIs(0xC1D3C4))
                    {
                        LogLine("Absolute data (ASD)");
                    }
                    else if (NextRecordIdIs(0xC3D0))
                    {
                        LogLine("Normal code (CP)");
                    }
                    else if (NextRecordIdIs(0xC3D2))
                    {
                        LogLine("Normal ROM data (CR)");
                    }
                    else if (NextRecordIdIs(0xC3C4))
                    {
                        LogLine("Normal data (CD)");
                    }
                    else if (NextRecordIdIs(0xC5C1D0))
                    {
                        LogLine("Common absolute code (EAP)");
                    }
                    else if (NextRecordIdIs(0xC5C1D2))
                    {
                        LogLine("Common absolute ROM data (EAR)");
                    }
                    else if (NextRecordIdIs(0xC5C1C4))
                    {
                        LogLine("Common absolute data (EAD)");
                    }
                    else if (NextRecordIdIs(0xC5DA))
                    {
                        LogLine("Something about short common with error checking.");
                        return false;
                    }
                    else if (NextRecordIdIs(0xCDC1D0))
                    {
                        LogLine("Common absolute code without size constraint thingy (MAP)");
                    }
                    else if (NextRecordIdIs(0xCDC1D2))
                    {
                        LogLine("Common absolute ROM data without size constraint thingy (MAR)");
                    }
                    else if (NextRecordIdIs(0xCDC1C4))
                    {
                        LogLine("Common absolute data without size constraint thingy (MAD)");
                    }
                    else if (NextRecordIdIs(0xDAC3D0))
                    {
                        LogLine("Short code (ZCP)");
                    }
                    else if (NextRecordIdIs(0xDAC3D2))
                    {
                        LogLine("Short ROM data (ZCR)");
                    }
                    else if (NextRecordIdIs(0xDAC3C4))
                    {
                        LogLine("Short data (ZCD)");
                    }
                    else if (NextRecordIdIs(0xDACD))
                    {
                        LogLine("Short common relocatable sections thingies?");
                        return false;
                    }
                    Indent(nesting); Log(" Section name: "); LogLine(ReadString());
                    Indent(nesting); Log(" Parent index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Sibling index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Context index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                }
                else if (NextRecordIdIs(0xE7))
                {
                    Indent(nesting); LogLine("Section alignment (SA): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    // TODO: Figure out how to handle optional arguments?
                    Indent(nesting); Log(" Boundary alignment divisor: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Page size: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xE2D3))
                {
                    Indent(nesting); LogLine("Section size (ASS): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Section size: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xE2CC))
                {
                    Indent(nesting); LogLine("Section base address (ASL): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Section base address: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    Indent(nesting); LogLine("Variable values or section offset? (ASR): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Section offset: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xFB))
                {
                    Indent(nesting); LogLine("Define context (NC): ");
                    Indent(nesting); Log(" Context index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Context name: "); LogLine(ReadString());
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xE2C1))
                {
                    Indent(nesting); LogLine("Physical region size (ASA): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Region size: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xE2C2))
                {
                    Indent(nesting); LogLine("Physical region base address (ASB): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" Address: "); LogLine(ReadNumber(out isEscapedValue).ToString("X6"));
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xE2C6))
                {
                    Indent(nesting); LogLine("MAU size (ASF): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" MAU size: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                }
                else if (NextRecordIdIs(0xE2CD))
                {
                    Indent(nesting); LogLine("M-Value (ASM): ");
                    Indent(nesting); Log(" Section index: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Indent(nesting); Log(" M-Value: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    Log("Parser not programmed to use this field's information.  Fixme!");
                    return false;
                }
                else if (NextRecordIdIs(0xE5)) // Apparently, this is legal?
                {
                    //Indent(nesting); Log("Current section index (SB): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    //LogLine(currentSection.ToString());
                }
                else
                    return false;

            return true;
        }


        protected bool parseP5(int nesting)
        {
            bool isEscapedValue;
            for (int i = 0; i < 16; i++)
                Log(file[index + i].ToString("X2"));
            OmfSection s;
            while (WhichPart(index) == 5)
                if (NextRecordIdIs(0xE5))
                {
                    //Indent(nesting); Log("Current section index (SB): ");
                    currentSection = ReadNumber(out isEscapedValue);
                    //LogLine(currentSection.ToString());
                }
                else if (NextRecordIdIs(0xE2D0))
                {
                    //Indent(nesting); LogLine("Current section PC (ASP)");
                    currentSection = ReadNumber(out isEscapedValue);
                    //Indent(nesting); Log(" Section index: "); LogLine(currentSection.ToString());
                    s = MakeGetSection(currentSection);
                    s.NextAddress = ReadNumber(out isEscapedValue);
                    //Indent(nesting); Log(" Value: "); LogLine(s.NextAddress.ToString("X6"));
                }
                else if (NextRecordIdIs(0xED))
                {
                    //Indent(nesting); LogLine("Load Constant MAUs (LD)");
                    s = Sections.Where(x => x.Index == currentSection).FirstOrDefault();
                    if (s == null)
                    {
                        LogLine("ERROR: Attempt to add data to undefined section!");
                        return false;
                    }
                    int n = ReadNumber(out isEscapedValue);
                    //Indent(nesting); Log(" Data bytes: "); Log(n.ToString()); Log(": ");
                    for (int i = 0; i < n; i++)
                    {
                        s.SetByte(file[index + i]);
                        //Log(file[index + i].ToString("X2"));
                    }
                    //LogLine("");
                    index += n;
                }
                else if (NextRecordIdIs(0xE3))
                {
                    Indent(nesting); Log("Initialize Relocation Base (IR)");
                }
                else if (NextRecordIdIs(0xF7))
                {
                    Indent(nesting); Log("Repeat Data (RE)");
                }
                else if (NextRecordIdIs(0xE2D2))
                {
                    Indent(nesting); Log("Variables Values (ASR)"); // "Not implemented"
                }
                else if (NextRecordIdIs(0xE2D7))
                {
                    Indent(nesting); Log("Variables Values (ASW)");
                }
                else if (NextRecordIdIs(0xE4))
                {
                    Indent(nesting); Log("Load With Relocation (LR)");
                }
                else if (NextRecordIdIs(0xFA))
                {
                    Indent(nesting); Log("Load With Translation (LT)");
                }
                else
                    return false;
            return true;
        }


        protected bool parseP1(int nesting)
        {
            bool isEscapedValue;
            int n3;

            while (WhichPart(index) == 1)
                if (NextRecordIdIs(0xF0))
                {
                    Indent(nesting); LogLine("Record F0, Variable Attributes (NN)");
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Version: "); LogLine(n3.ToString());
                    EnvironmentPart.NnRecords.Add(n3, ReadString());
                    Indent(nesting); Log(" ID: "); LogLine(EnvironmentPart.NnRecords[n3]);
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    Indent(nesting); LogLine("Record F1CE, Variable Attributes (ATN)");
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Symbol name index: "); LogLine(n3.ToString());
                    if (!EnvironmentPart.NnRecords.ContainsKey(n3))
                    {
                        Indent(nesting); LogLine("ERROR: No matching NN record!");
                        return false;
                    }
                    Indent(nesting); Log(" Should be zero: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Attribute definition: "); Log(n3.ToString()); Log(", ");
                    switch (n3)
                    {
                        case 50:
                            LogLine("Creation date and time");
                            EnvironmentPart.DateTime = new DateTime(ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue),
                                 ReadNumber(out isEscapedValue), ReadNumber(out isEscapedValue));
                            Indent(nesting); Log("  "); LogLine(EnvironmentPart.DateTime.ToString());
                            break;
                        case 51:
                            LogLine("Creation command line");
                            EnvironmentPart.CommandLine = ReadString();
                            Indent(nesting); Log("  "); LogLine(EnvironmentPart.CommandLine);
                            break;
                        case 52:
                            LogLine("Execution status");
                            n3 = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  "); Log(n3.ToString());
                            switch (n3)
                            {
                                case 0:
                                    LogLine(": Success");
                                    break;
                                case 1:
                                    LogLine(": Warning");
                                    break;
                                case 2:
                                    LogLine(": Error(s)");
                                    break;
                                case 3:
                                    LogLine(": Fatal error(s)");
                                    break;
                                default:
                                    LogLine(": Unknown");
                                    return false;
                            }
                            EnvironmentPart.ExecutionStatus = (ExecutionStatus)n3;
                            break;
                        case 53:
                            LogLine("Host environment");
                            n3 = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  "); Log(n3.ToString());
                            switch (n3)
                            {
                                case 0:
                                    LogLine(": Other");
                                    break;
                                case 1:
                                    LogLine(": VMS");
                                    break;
                                case 2:
                                    LogLine(": MS-DOS");
                                    break;
                                case 3:
                                    LogLine(": UNIX");
                                    break;
                                case 4:
                                    LogLine(": HP-UX");
                                    break;
                                default:
                                    LogLine(": Unknown");
                                    return false;
                            }
                            EnvironmentPart.HostEnvironment = (HostEnvironment)n3;
                            break;
                        case 54:
                            LogLine("Tool version information");
                            EnvironmentPart.ToolNumber = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  Tool number: "); Log(EnvironmentPart.ToolNumber.ToString());
                            EnvironmentPart.ToolVersion = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  Tool "); Log(EnvironmentPart.ToolVersion.ToString());
                            EnvironmentPart.ToolRevision = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  Tool "); Log(EnvironmentPart.ToolRevision.ToString());
                            if (file[index] >= 0xC1 && file[index] <= 0xDA)
                            {
                                EnvironmentPart.ToolLetter = (char)(file[index++] - 0xC1 + (int)'A');
                                Indent(nesting); Log("  Tool letter: "); Log(EnvironmentPart.ToolLetter.ToString());
                            }
                            break;
                        case 55:
                            LogLine("Comments");
                            EnvironmentPart.Comments = ReadString();
                            Indent(nesting); Log("  "); Log(EnvironmentPart.Comments);
                            break;
                        case 56:
                            LogLine("I dunno.");
                            EnvironmentPart.IDontKnow = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  "); Log(EnvironmentPart.IDontKnow.ToString());
                            LogLine(": Total mystery.");
                            break;
                        default:
                            LogLine("Unknown field.");
                            for (int i = 0; i < 32; i++)
                                Log(file[index++].ToString("X2") + " ");
                            
                            return false;
                    }

                }
                else if (NextRecordIdIs(0xE2CE))
                {
                    Indent(nesting); LogLine("Record E2CE.  Wonder what it means.");
                    for (int i = 0; i < 32; i++)
                        Log(file[index++].ToString("X2") + " ");
                    return false;
                }
                else
                {
                    LogLine("Unknown field.");
                    return false;
                }

            return true;
        }


        protected bool parseP0(int nesting)
        {
            bool isEscapedValue;
            int n3;

            while (WhichPart(index) == 0)
                if (NextRecordIdIs(0xF0))
                {
                    Indent(nesting); LogLine("Record F0, Variable Attributes (NN)");
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Version: "); LogLine(n3.ToString());
                    AdExtensionPart.NnRecords.Add(n3, ReadString());
                    Indent(nesting); Log(" ID: "); LogLine(AdExtensionPart.NnRecords[n3]);
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    Indent(nesting); LogLine("Record F1CE, Variable Attributes (ATN)");
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Symbol name index: "); LogLine(n3.ToString());
                    if (!AdExtensionPart.NnRecords.ContainsKey(n3))
                    {
                        Indent(nesting); LogLine("ERROR: No matching NN record!");
                        return false;
                    }
                    Indent(nesting); Log(" Should be zero: "); LogLine(ReadNumber(out isEscapedValue).ToString());
                    n3 = ReadNumber(out isEscapedValue);
                    Indent(nesting); Log(" Attribute definition: "); Log(n3.ToString()); Log(", ");
                    switch (n3)
                    {
                        case 37:
                            LogLine(" Object format version");
                            AdExtensionPart.VersionNumber = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  Version: "); LogLine(AdExtensionPart.VersionNumber.ToString());
                            AdExtensionPart.RevisionNumber = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  Revision: "); LogLine(AdExtensionPart.RevisionNumber.ToString());
                            break;
                        case 38:
                            LogLine(" Object format type");
                            n3 = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  "); Log(n3.ToString());
                            switch (n3)
                            {
                                case 1:
                                    LogLine(": Absolute");
                                    break;
                                case 2:
                                    LogLine(": Relocatable");
                                    break;
                                case 3:
                                    LogLine(": Loadable");
                                    break;
                                case 4:
                                    LogLine(": Library");
                                    break;
                                default:
                                    LogLine(": Unknown");
                                    return false;
                            }
                            AdExtensionPart.ObjectFormatType = (ObjectFormatType)n3;
                            break;
                        case 39:
                            LogLine(" Case sensitivity");
                            n3 = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  "); Log(n3.ToString());
                            switch (n3)
                            {
                                case 1:
                                    LogLine(": False");
                                    AdExtensionPart.CaseSensitive = false;
                                    break;
                                case 2:
                                    LogLine(": True");
                                    AdExtensionPart.CaseSensitive = true;
                                    break;
                                default:
                                    LogLine(": Unknown");
                                    return false;
                            }
                            break;
                        case 40:
                            LogLine(" Memory model");
                            n3 = ReadNumber(out isEscapedValue);
                            Indent(nesting); Log("  "); Log(n3.ToString());
                            switch (n3)
                            {
                                case 0:
                                    LogLine(": Tiny");
                                    break;
                                case 1:
                                    LogLine(": Small");
                                    break;
                                case 2:
                                    LogLine(": Medium");
                                    break;
                                case 3:
                                    LogLine(": Compact");
                                    break;
                                case 4:
                                    LogLine(": Large");
                                    break;
                                case 5:
                                    LogLine(": Big");
                                    break;
                                case 6:
                                    LogLine(": Huge");
                                    break;
                                default:
                                    LogLine(": Unknown");
                                    return false;
                            }
                            AdExtensionPart.MemoryModelSize = (MemoryModelSize)n3;
                            break;
                    }

                }
                else if (NextRecordIdIs(0xE2CE))
                {
                    Indent(nesting); LogLine("Record E2CE.  Wonder what it means.");
                    for (int i = 0; i < 32; i++)
                        Log(file[index++].ToString("X2") + " ");
                    return false;
                }
                else
                {
                    LogLine("Unknown field.");
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
        protected bool NextRecordIdIs(int id)
        {
            if (id < 0x100)
                if (file[index] == id)
                {
                    index++;
                    return true;
                }
                else
                    return false;
            if (id < 0x10000)
                if (file[index] == id >> 8 && file[index + 1] == (id & 0xFF))
                {
                    index += 2;
                    return true;
                }
                else
                    return false;
            if (id < 0x1000000)
                if (file[index] == id >> 16 && (file[index + 1] == (id >> 8 & 0xFF)) && file[index + 2] == (id & 0xFF))
                {
                    index += 3;
                    return true;
                }
                else
                    return false;
            if (file[index] == id >> 24 && file[index + 1] == (id >> 16 & 0xFF) && file[index + 2] == (id >> 8 & 0xFF) && file[index + 3] == (id & 0xFF))
            {
                index += 4;
                return true;
            }
            else
                return false;

        }

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
        #endregion


        #region Logging
        /// <summary>
        /// Used for logging.  Produces an indent.
        /// </summary>
        /// <param name="nesting"></param>
        protected static void Indent(int nesting)
        {
            for (; nesting > 0; nesting--)
                Log("  ");
        }


        /// <summary>
        /// Probably some kind of logging function.  But who knows?
        /// </summary>
        /// <param name="s"></param>
        protected static void Log(string s)
        {
            System.Console.Write(s);
        }


        /// <summary>
        /// I don't know, maybe this will attempt to log something, or maybe install Windows ME.
        /// </summary>
        /// <param name="s"></param>
        protected static void LogLine(string s)
        {
            System.Console.WriteLine(s);
        }
        #endregion
    }
}