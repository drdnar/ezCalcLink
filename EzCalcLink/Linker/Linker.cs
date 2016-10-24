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
        /// <summary>
        /// List of files being linked
        /// </summary>
        public List<ObjectFile> ObjectFiles = new List<ObjectFile>();


        /// <summary>
        /// List of optional libraries
        /// </summary>
        public List<ObjectFile> Libraries = new List<ObjectFile>();


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
        public List<List<Section>> SectionOrders = new List<List<Section>>();


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
            Note: You don't know the size of the sections until you know the
            optional libraries to include.  
        CrossReferenceExternalSymbols();
        ResolveSymbolAddresses();
        ResolveStaticRelocations();
        ApplyStaticRelocations();
        */


        public void LinkTest()
        {
            RemovedUnusedLibraries();
            PopulateAddressSpacesList();
            PopulateSectionsList();
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
            foreach (var o in ObjectFiles)
            {
                DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose, "Scanning sections in object {0}. . . .", o.ModuleName);
                foreach (var s in o.Sections)
                {
                    Section t;
                    if (!Sections.TryGet(s.Name, out t))
                    {
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.Verbose, " New section: {0}", s.Name);
                        // If the section does not already exist in the output
                        // object, then create it.
                        var n = new Section();
                        n.Name = s.Name;
                        //n.ExpectedSize = s.ExpectedSize; // We can't populate the expected size until scanning the library list
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
                        DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase | DebugLogger.LogType.VeryVerbose, " Section {0} already exists", s.Name);
                        // The section already exists, so expand it.
                        // Figure out if we need to increase the section size
                        if (s.SharedAbsolute) // No data to work with
                            continue;
                        //t.ExpectedSize += s.ExpectedSize;
                    }
                }
            }
        }


        /// <summary>
        /// Takes the SectionOrders list and finds the final offsets of the sections.
        /// </summary>
        protected void OrderSectionsList()
        {
            DebugLogger.LogLine(DebugLogger.LogType.LinkerPhase, "Ordering sections. . . .");
            // TODO: Combine sections from different input objects.
            // Combine sections that need to be combined serially.
            foreach (var sl in SectionOrders)
            {
                if (sl.Count == 0)
                    continue;
                for (int i = 1; i < sl.Count; i++)
                {
                    sl[i].BaseAddress = sl[i - 1].BaseAddress + sl[i - 1].ExpectedSize;
                    sl[i].Resolved = true;
                }
            }
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
