using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EzCalcLink.Object;

namespace EzCalcLink.Linker
{
    public class Linker
    {
        public Linker()
        {
            // Set up the object that contains defines.
            DefinesObject.AddressSpaces.Add(new AddressSpace() { Name = "EXTIO", MauSize = 2 });
            DefinesObject.AddressSpaces.Add(new AddressSpace() { Name = "INTIO", MauSize = 1 });
            DefinesObject.AddressSpaces.Add(new AddressSpace() { Name = "ROM", MauSize = 3 });
            DefinesObject.AddressSpaces.Add(new AddressSpace() { Name = "RAM", MauSize = 3 });
            DefinesObject.Sections.Add(new Section() { Name = "STRSECT", ExpectedSize = 0, Resolved = true, AddressSpace = DefinesObject.AddressSpaces["RAM"]});
            DefinesObject.Sections.Add(new Section() { Name = "CODE", ExpectedSize = 0, Resolved = true, AddressSpace = DefinesObject.AddressSpaces["ROM"] });
            DefinesObject.Sections.Add(new Section() { Name = "TEXT", ExpectedSize = 0, Resolved = true, AddressSpace = DefinesObject.AddressSpaces["ROM"] });
            DefinesObject.Sections.Add(new Section() { Name = "DATA", ExpectedSize = 0, Resolved = true, AddressSpace = DefinesObject.AddressSpaces["RAM"] });
            DefinesObject.Sections.Add(new Section() { Name = "BSS", ExpectedSize = 0, Resolved = true, AddressSpace = DefinesObject.AddressSpaces["RAM"] });
            
        }


        /// <summary>
        /// List of files being linked
        /// </summary>
        public List<ObjectFile> ObjectFiles = new List<ObjectFile>();


        /// <summary>
        /// List of optional libraries
        /// </summary>
        public List<ObjectFile> Libraries = new List<ObjectFile>();


        /// <summary>
        /// List of symbols passed to the linker by the user
        /// </summary>
        public NameResolver<Symbol> DefinedSymbols = new NameResolver<Symbol>();


        public ObjectFile DefinesObject = new ObjectFile();


        /// <summary>
        /// Master list of output symbols.  This list cross-references symbols
        /// </summary>
        public NameResolver<Symbol> Symbols
        {
            get
            {
                return OutputObject.Symbols;
            }
        }


        /// <summary>
        /// Master list of output sections
        /// </summary>
        public NameResolver<Section> Sections
        {
            get
            {
                return OutputObject.Sections;
            }
        }


        /// <summary>
        /// Master list of output address spaces
        /// </summary>
        public NameResolver<AddressSpace> AddressSpaces
        {
            get
            {
                return OutputObject.AddressSpaces;
            }
        }


        /// <summary>
        /// List of groupings and orderings of sections.
        /// Inner list is sections to be combined into one section, outer list
        /// is non-combined sections.
        /// </summary>
        public List<List<string>> SectionOrders = new List<List<string>>();


        /// <summary>
        /// 
        /// </summary>
        protected List<List<Section>> SectionObjectOrders = new List<List<Section>>();


        /// <summary>
        /// List of which sections are have absolute final positions, and associates those positions.
        /// </summary>
        public Dictionary<string, int> AbsoluteSectionPositions = new Dictionary<string, int>();


        /// <summary>
        /// Final linked output
        /// </summary>
        public ObjectFile OutputObject = new ObjectFile();


        /* General order:
        PopulateAddressSpacesList();
        PopulateSectionsList();
        Have the input parser generate the sections order lists
        OrderSectionsList();
        // Finalize section locations
        // Update relocation indexes
        // Update symbol definitions
        // Collate section
        CrossReferenceExternalSymbols();
        ResolveSymbolAddresses();
        ResolveStaticRelocations();
        ApplyStaticRelocations();
        */


        public void LinkTest()
        {
            foreach (var o in ObjectFiles)
            {
                foreach (var s in o.Sections)
                    s.Name = s.Name.ToUpper();
            }

            RemovedUnusedLibraries();
            PopulateAddressSpacesList();
            PopulateSectionsList();
            /*foreach (ObjectFile of in ObjectFiles)
            {
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, "Symbols in object file {0}:", of.Name);
                foreach (Symbol s in of.LocalSymbols)
                {
                    DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, "  Address space: {3}, Section: {4}, Symbol name: {0}, Offset: {1:X6}, Resolved: {2}", s.Name, s.Offset, s.Resolved, s.AddressSpace.Name, s.Section.Name);
                }
            }*/
            OrderSections();
            CollateSubsections();
            CollateSections();
            /*
            foreach (var s in Sections)
            {
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, "Output section {0}:", s.Name);
                DebugLogger.LogLine("  Relocatable: {0}", s.Relocatable);
                DebugLogger.LogLine("  Shared-Absolute: {0}", s.SharedAbsolute);
                DebugLogger.LogLine("  Base Address: 0x{0:X6}", s.BaseAddress);
                DebugLogger.LogLine("  Expected Size: 0x{0:X6} ({0}d)", s.ExpectedSize);
                DebugLogger.LogLine("  Next Address: 0x{0:X6}", s.NextAddress);
                


            }*/
            // PinFinalAddresses();
            // WriteRelocations();
            // FormatOutputBinary();
            // WriteOutputFile();
        }


        /// <summary>
        /// Determines which libs are unused, and removes them from the list of
        /// object files to be linked.
        /// </summary>
        protected void RemovedUnusedLibraries()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Checking which libraries need to be included. . . .");
            // Cache a dictionary mapping strings to symbol names.
            // This makes it a lot faster to figure out if a symbol in an
            // object file can be fulfilled with a library.
            var libSyms = new Dictionary<string, Symbol>();
            foreach (var l in Libraries)
                foreach (var s in l.Symbols)
                    if (!s.External)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, "Library symbol: {0}", s.Name);
                        libSyms.Add(s.Name, s);
                    }
            // Libraries may also require other libraries, so we have to keep
            // doing passes of library symbol names until no more libraries are
            // required.
            List<ObjectFile> recurseList = new List<ObjectFile>();
            foreach (var of in ObjectFiles)
                foreach (var s in of.ExternalSymbols)
                {
                    Symbol ls;
                    if (libSyms.TryGetValue(s.Name, out ls))
                        if (!recurseList.Contains(ls.ObjectFile))
                        {
                            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose,
                                "Object file {0} requires symbol {1} found in lib {2}",
                                of.Name, s.Name, ls.ObjectFile.Name);
                            recurseList.Add(ls.ObjectFile);
                        }
                }
            // Now we scan each library to see if it requires more libraries.
            List<ObjectFile> newRecurseList = new List<ObjectFile>();
            while (recurseList.Count > 0)
            {
                // Add any libraries we found we need to the object list.
                // Note that this will always be added if there are libraries to add because anytime
                // libraries are added, there's then one more pass.
                ObjectFiles.AddRange(recurseList);
                // We also need to scan the added libraries to see if they require libraries of their own.
                // But we don't need to check libraries we've already scanned again.
                // So newRecurseList is the list of libraries to scan when we're done with this list.
                newRecurseList.Clear();
                // Now scan the libraries, just like before.
                foreach (var of in recurseList)
                    foreach (var s in of.ExternalSymbols)
                    {
                        Symbol ls;
                        if (libSyms.TryGetValue(s.Name, out ls))
                            if (!recurseList.Contains(ls.ObjectFile))
                            {
                                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose,
                                    "Library {0} requires symbol {1} found in lib {2}",
                                    of.Name, s.Name, ls.ObjectFile.Name);
                                newRecurseList.Add(ls.ObjectFile);
                            }
                    }
                // Instead of creating new List objects every pass, just exchange the two.
                // The old recurseList becomes the new newRecurseList, which gets .Clear()ed before use.
                var temp = recurseList;
                recurseList = newRecurseList;
                newRecurseList = temp;
            }
        }


        /// <summary>
        /// Populates the sections list from the input object files.
        /// Must be called before PopulateSectionsList()
        /// </summary>
        protected void PopulateAddressSpacesList()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Populating address space list. . . .");
            // Just assume all of them have the same address spaces
            foreach (var a in ObjectFiles[0].AddressSpaces)
            {
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose, "Added address space: {0}", a.Name);
                AddressSpaces.Add(a);
            }
        }


        /// <summary>
        /// Populates the sections list from the input object files
        /// </summary>
        protected void PopulateSectionsList()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Populating sections list. . . .");
            // Build master list of sections
            DebugLogger.Indent();
            foreach (var o in ObjectFiles)
            {
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose, "Scanning sections in object {0}. . . .", o.ModuleName);
                DebugLogger.Indent();
                foreach (var s in o.Sections)
                {
                    Section t; // t -> (comes after S)
                    if (!Sections.TryGet(s.Name, out t))
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose, "New section: {0}", s.Name);
                        DebugLogger.Indent();
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, "Initial size: 0x{0:X6}", s.ExpectedSize);
                        DebugLogger.Unindent();
                        // If the section does not already exist in the output
                        // object, then create it.
                        var n = new Section(); // n -> New (section)
                        n.Name = s.Name;
                        n.ExpectedSize = s.ExpectedSize;
                        n.AddressSpace = AddressSpaces[s.AddressSpace.Name];
                        Sections.Add(n);
                        // If shared section (e.g. MMIO), nothing more to do.
                        if (!(n.SharedAbsolute = s.SharedAbsolute))
                            continue;
                        n.BaseAddress = s.BaseAddress;
                        n.Resolved = true;
                    }
                    else
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, "Section {0} already exists", s.Name);
                        // The section already exists, so expand it.
                        // Figure out if we need to increase the section size
                        if (s.SharedAbsolute) // No data to work with
                            continue;
                        t.ExpectedSize += s.ExpectedSize;
                        DebugLogger.Indent();
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, "Added 0x{1:X6}, New size: 0x{0:X6}", t.ExpectedSize, s.ExpectedSize);
                        DebugLogger.Unindent();
                    }
                }
                DebugLogger.Unindent();
            }
            DebugLogger.Unindent();
        }


        protected void OrderSections()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Ordering sections. . . .");

            // Transform list containing section names into list containing section objects
            foreach (var ss in SectionOrders) // ss -> Sections list, Strings
            {
                var so = new List<Section>(); // so -> Section list, Objects
                SectionObjectOrders.Add(so);
                foreach (var sss in ss) // sss -> SubSections list, Strings
                {                       // I have had it with these mother****ing snakes on this mother****ing iterator! -- Samuel L. Jackson, on EzCalcLink
                    so.Add(Sections[sss]);
                }
            }

            // Compute relative final addresses for output sections.
            DebugLogger.Indent();
            foreach (var sl in SectionObjectOrders) // sl -> Section List
            {
                if (sl.Count == 0)
                    continue;

                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, "Section series:");
                DebugLogger.Indent();
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, "{0} has address 0x{1:X6} and size 0x{2:x6}", sl[0].Name, sl[0].BaseAddress, sl[0].ExpectedSize);
                for (int i = 1; i < sl.Count; i++)
                {
                    sl[i].BaseAddress = sl[i - 1].BaseAddress + sl[i - 1].ExpectedSize;
                    //sl[i].Resolved = true;
                    DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, "{0} has address 0x{1:X6} and size 0x{2:x6}", sl[i].Name, sl[i].BaseAddress, sl[i].ExpectedSize);
                }
                DebugLogger.Unindent();
            }
            DebugLogger.Unindent();
        }


        /// <summary>
        /// This combines same-name sections from individual files into a single section in the output ObjectFile.
        /// </summary>
        protected void CollateSubsections()
        {
            // Collate sections (phase 1)
            // Here, we're giving the INPUT sections new base addresses.
            // However, these aren't final addresses.  All we're doing is giving
            // them addresses relative to other input sections with the same
            // name.
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Basic, "Collating. . . .");
            DebugLogger.Indent();
            // Map names of sections to current address we're adding data to.
            Dictionary <string, int> nextSectionAddress = new Dictionary<string, int>();
            foreach (var sect in Sections)
                nextSectionAddress[sect.Name] = sect.BaseAddress;
            // Process sections
            foreach (var obj in ObjectFiles)
            {
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose, "Object file {0}", obj.Name);
                DebugLogger.Indent();
                foreach (var sect in obj.Sections)
                {
                    if (!obj.IsLibraryMember)
                        DebugLogger.Log(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, "Section {0}", sect.Name);
                    // TODO: Treat SharedAbsolute sections differently
                    if (sect.SharedAbsolute)
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, " SharedAbsolute");
                        continue;
                    }
                    // Pin address
                    var sectName = sect.Name.ToUpper();
                    var oSect = Sections[sectName];
                    var newAddr = nextSectionAddress[sectName];
                    if (!obj.IsLibraryMember)
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVeryVerbose, " New address: 0x{0:X6}", newAddr);
                    obj.ChangeSectionBaseAddress(sect, newAddr);
                    // Copy data
                    if (sect.Data.Count > 0)
                        for (int i = sect.BaseAddress; i < sect.BaseAddress + sect.ExpectedSize; i++)
                            oSect.SetByte(i, sect.Memory[i]);
                    // Copy relocations
                    foreach (var r in sect.Relocations)
                    {
                        var newReloc = r.Value.CloneFor(OutputObject);
                        oSect.Relocations.Add(r.Key, newReloc);
                    }
                    // Update location of next part of section
                    nextSectionAddress[sectName] = newAddr + sect.ExpectedSize;
                }
                // Copy symbol definitions
                foreach (var s in obj.Symbols)
                    if (!s.External)
                        OutputObject.Symbols.Add(s.CloneFor(OutputObject));
                DebugLogger.Unindent();
            }
            DebugLogger.Unindent();
        }


        /// <summary>
        /// This combines serial sections in the output ObjectFile into a single section.
        /// </summary>
        protected void CollateSections()
        {
            // Collate sections (phase 2)
            // Now we:
            //     Combine section data for serial sections into one section
            //     Combine relocations lists
            //     Combine symbol lists, and translate symbol offsets

        }


        /// <summary>
        /// Builds a list of external symbol references.
        /// </summary>
        protected void CrossReferenceExternalSymbols()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Cross-referencing symbols. . . .");
            foreach (var obj in ObjectFiles)
            {
                foreach (var section in obj.Sections)
                {
                    foreach (var symbol in obj.Symbols)
                    {
                        if (symbol.External)
                            continue;
                        Symbols.Add(symbol);
                    }
                }
            }
        }


        /// <summary>
        /// Resolves the symbols local to each section
        /// </summary>
        protected void ResolveSymbolAddresses()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Resolving symbols. . . .");
            foreach (var s in Symbols)
            {
                if (s.External)
                    continue;
                s.Offset += s.Section.BaseAddress;
                s.Resolved = true;

            }
        }


        protected void ResolveStaticRelocations()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Resolving static relocations. . . .");
            foreach (var s in Sections)
            {
                foreach (var r in s.Relocations) // r -> relocation
                {
                    var a = r.Key; // a -> address
                    var v = r.Value; // v -> value
                    s.Memory[a] = (byte)(v.Value & 0xFF);
                    s.Memory[a + 1] = (byte)((v.Value >> 8) & 0xFF);
                    s.Memory[a + 2] = (byte)(v.Value >> 16);
                }
            }
        }


        protected void ApplyStaticRelocations()
        {
            foreach (var s in Sections)
            {
                if (!s.Resolved)
                    throw new Exception("Unresolved.");

            }
        }
    }
}
