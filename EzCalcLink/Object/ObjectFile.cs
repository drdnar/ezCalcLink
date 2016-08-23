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
        /// Lists different types of address spaces
        /// </summary>
        public NameResolver<AddressSpace> AddressSpaces = new NameResolver<AddressSpace>();

        /// <summary>
        /// Contains a list of different sections
        /// </summary>
        public NameResolver<Section> Sections = new NameResolver<Section>();

        /// <summary>
        /// Contains a list of all symbols
        /// </summary>
        public NameResolver<Symbol> Symbols = new NameResolver<Symbol>();

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
    }
}
