using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EzCalcLink.Linker;

namespace EzCalcLink.Object
{
    /// <summary>
    /// Contains all data in an object file
    /// </summary>
    public class ObjectFile
    {
        /// <summary>
        /// Gives the object file's source file name
        /// </summary>
        public string Name;

        /// <summary>
        /// Gives the "module name" of a file, usually source code file name.
        /// </summary>
        public string ModuleName;

        /// <summary>
        /// Lists different types of address spaces
        /// </summary>
        public NameResolver<AddressSpace> AddressSpaces = new NameResolver<AddressSpace>(caseSensitive: false);

        /// <summary>
        /// Contains a list of different sections
        /// </summary>
        public NameResolver<Section> Sections = new NameResolver<Section>(caseSensitive: false);

        /// <summary>
        /// Contains a list of all symbols
        /// </summary>
        public NameResolver<Symbol> Symbols = new NameResolver<Symbol>();


        /// <summary>
        /// Contains a list of all symbols that are defined in this object file.
        /// </summary>
        public NameResolver<Symbol> LocalSymbols = new NameResolver<Symbol>();


        /// <summary>
        /// Contains a list of all symbols that are NOT defined in this object
        /// file, and require linking with an external object file.
        /// </summary>
        public NameResolver<Symbol> ExternalSymbols = new NameResolver<Symbol>();


        /// <summary>
        /// This changes the base address of a section, updates relocations,
        /// and updates symbol addresses
        /// </summary>
        /// <param name="section"></param>
        /// <param name="newAddress"></param>
        public void ChangeSectionBaseAddress(Section section, int newAddress)
        {
            int d = newAddress - section.BaseAddress;
            section.ChangeBaseAddress(newAddress);
            // Scan list of relocations and update
            foreach (var s in Sections)
            {
                if (!s.Relocatable)
                    continue;
                Dictionary<int, RelocationExpression> newRelocations = new Dictionary<int, RelocationExpression>();
                foreach (var r in s.Relocations)
                {
                    var oldAddr = r.Key;
                    var reloc = r.Value;
                    newRelocations.Add(oldAddr + d, reloc);
                }
                section.Relocations = newRelocations;
            }
            // Scan list of symbols and update
            foreach (var s in LocalSymbols)
                if (s.Section == section)
                    s.Offset += d;
        }

        /// <summary>
        /// True if the object file contains relocation information
        /// </summary>
        public bool Relocatable;

        /// <summary>
        /// True if case-sensitive
        /// </summary>
        public bool CaseSensitive;


        /// <summary>
        /// True if the object file is extracted from a library.
        /// If marked true, the linker should exclude this ObjectFile's data if not referenced.
        /// </summary>
        public bool IsLibraryMember = false;


        /// <summary>
        /// If a library, set to true when a reference to this library is found.
        /// </summary>
        public bool LibraryMemberReferenced = false;
    }
}
