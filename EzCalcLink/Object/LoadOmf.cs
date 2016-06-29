using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
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
            else
                throw new IndexOutOfRangeException("Requested context index " + i.ToString() + " is not accepted by parser.");
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
            ParseHeader();

            // AD Extension Part
            index = PtrToAsw0;
            ParseP0();

            // Environment Part
            index = PtrToAsw1;
            ParseP1();
        }


        protected void ParseP1()
        {
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
                            DebugLogger.LogLine();
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
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index++]);
                    DebugLogger.LogLine();
                    return false;
                }
        }


        protected void ParseP0()
        {
            int n3;
            DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, "Parsing P0. . . .");
            while (WhichPart(index) == 0)
                if (NextRecordIdIs(0xF0))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Record F0, Variable Attributes (NN)");
                    n3 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                        " Version: {0}", n3);
                    DebugLogger.LogLine(" ID: {0}", ReadString());
                }
                else if (NextRecordIdIs(0xF1CE))
                {
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVerbose | DebugLogger.LogType.FieldHeader,
                        "Record F1CE, Variable Attributes (ATN)");
                    n3 = ReadNumber();
                    DebugLogger.LogLine(DebugLogger.LogType.P0 | DebugLogger.LogType.VeryVeryVerbose | DebugLogger.LogType.FieldValue,
                        " Symbol name index: {0}", n3);
                    DebugLogger.LogLine(" Should be zero: {0}", ReadNumber());
                    n3 = ReadNumber();
                    DebugLogger.Log(" Attribute definition: {0}, ", n3);
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
                    DebugLogger.LogLine(DebugLogger.LogType.FatalError, "Record E2CE.  Wonder what it means.");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2} ", file[index++]);
                    throw new FormatException("OMF parse: Unknown field E2CE");
                }
                else
                {
                    DebugLogger.LogLine(DebugLogger.LogType.FatalError, "Unknown field.");
                    throw new FormatException("OMF parse: Unknown field");
                }
        }


        protected void ParseHeader()
        {
            if (!NextRecordIdIs(0xE0))
            {
                DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FatalError,
                    "ERROR! File does not begin with record 0xE0 (Module Beginning).");
                throw new FormatException("File parse error.");
            }
            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, "Parsing header. . . .");
            DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldValue | DebugLogger.LogType.VeryVerbose, "Processor: {0}", ReadString());
            DebugLogger.LogLine("Module name: {0}", ReadString());
            while (WhichPart(index) == -1)
                if (NextRecordIdIs(0xEC)) // Address Descriptor
                {
                    DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.VeryVerbose, "Address Descriptor (AD)");
                    int BitsPerMau = ReadNumber();
                    if (BitsPerMau != 8)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FatalError, "Bad byte size: {0}", BitsPerMau);
                        throw new FormatException("OMF parse: Bits per MAU not 8.");
                    }
                    int MausPerAddress = ReadNumber();
                    if (MausPerAddress != 3)
                    {
                        // TODO: Short mode support
                        DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FatalError, "Bad word size: {0} (only long mode supported; no Z80 mode support)", MausPerAddress);
                        throw new FormatException("OMF parse: Word size not 3.");
                    }
                    if (file[index] == 0xCD)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FatalError, "File claims to be big-endian! (Not valid for Z80s.)");
                        throw new FormatException("OMF parse: Big-endian file");
                    }
                }
                else if (NextRecordIdIs(0xE2D7))
                {
                    int asw = ReadNumber();
                    int offset = ReadNumber();
                    Parts[asw] = offset;
                    DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldHeader | DebugLogger.LogType.Verbose, "Assign Pointer to part {0}: {1:X8}", asw, Parts[asw]);
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
                    DebugLogger.LogLine(DebugLogger.LogType.FileHeader | DebugLogger.LogType.Error, "Unknown or unexpected field!");
                    DebugLogger.Log(DebugLogger.LogType.FileHeader | DebugLogger.LogType.FieldValue | DebugLogger.LogType.VeryVerbose, "");
                    for (int i = 0; i < 32; i++)
                        DebugLogger.Log("{0:X2}", file[index + i]);
                    DebugLogger.LogLine();
                }
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
                throw new InvalidOperationException("ReadNumber expected a number, did not get a number. Index: " + index.ToString("X8") + ", value: " + b.ToString("X2"));
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
                throw new InvalidOperationException("ReadNumber/SkipNumber expected a number, did not get a number. Index: " + index.ToString("X8") + ", value: " + b.ToString("X2"));
            index += b & 0x0F;
        }
        #endregion
    }
}
