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
        /// Master list of output symbols
        /// </summary>
        public List<Symbol> Symbols
        {
            get
            {
                return OutputObject.Symbols;
            }
        }


        /// <summary>
        /// Master list of output symbols
        /// </summary>
        public List<Section> Sections
        {
            get
            {
                return OutputObject.Sections;
            }
        }
 

        /// <summary>
        /// Master list of output address spaces
        /// </summary>
        public List<AddressSpace> AddressSpaces
        {
            get
            {
                return OutputObject.AddressSpaces;
            }
        }


        /// <summary>
        /// List of groupings and orderings of sections
        /// </summary>
        public List<List<Section>> SectionOrders = new List<List<Section>>();


        /// <summary>
        /// Final linked output
        /// </summary>
        public ObjectFile OutputObject = new ObjectFile();


        /// <summary>
        /// Populates the sections list from the input object files.
        /// Must be called before PopulateSectionsList()
        /// </summary>
        protected void PopulateSectionsList()
        {
            // Just assume all of them have the same address spaces
            foreach (var a in ObjectFiles[0].AddressSpaces)
                AddressSpaces.Add(a);
        }


        /// <summary>
        /// Populates the sections list from the input object files
        /// </summary>
        protected void PopulateSectionsList()
        {
            // Build master list of sections
            foreach (var o in ObjectFiles)
            {
                foreach (var s in o.Sections)
                {
                    var t = Sections.Where(x => x.Name == s.Name).FirstOrDefault();
                    if (t == null)
                    {
                        var n = new Section();
                        n.Name = s.Name;
                        n.ExpectedSize = s.ExpectedSize;
                        n.AddressSpace = AddressSpaces.Where(x => x.Name == s.AddressSpace.Name).FirstOrDefault();
                        if (!(n.SharedAbsolute = s.SharedAbsolute))
                            continue;
                        n.BaseAddress = s.BaseAddress;
                        n.Resolved = true;
                    }
                    else
                    {
                        // Figure out if we need to increase the section size
                        if (s.SharedAbsolute) // No data to work with
                            continue;
                        t.ExpectedSize += s.ExpectedSize;
                        
                    }
            }
        }


        /// <summary>
        /// Takes the SectionOrders list and finds the final offsets of the sections.
        /// </summary>
        protected void OrderSectionsList()
        {
            foreach (var sl in SectionOrders)
            {
                if (sl.Length == 0)
                    continue;
                sl[0].BaseAddress = 0;
                for (int i = 1; i < sl.Length; i++)
                {
                    sl[i].BaseAddress = sl[i - 1].BaseAddress + sl[i - 1].ExpectedSize;
                    sl.Resolved = true;
                }
            }
        }


        protected void ResolveStaticRelocations()
        {
            foreach (var s in Sections)
            {
                foreach (var r in Relocations) // r -> relocation
                {
                    var a = r.Key; // a -> address
                    var v = r.Value; // v -> value
                    s.Memory[a] = (byte)(v & 0xFF);
                    s.Memory[a + 1] = (byte)((v >> 8) & 0xFF);
                    s.Memory[a + 2] = (byte)(v >> 16);
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
