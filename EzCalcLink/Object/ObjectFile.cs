using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<AddressSpace> AddressSpaces = new List<AddressSpace>();

        /// <summary>
        /// Contains a list of different sections
        /// </summary>
        public List<Section> Section = new List<Section>();

        /// <summary>
        /// Contains a list of all symbols
        /// </summary>
        public List<Symbol> Symbols = new List<Symbol>();

        /// <summary>
        /// True if the object file contains relocation information
        /// </summary>
        public bool Relocatable;

        /// <summary>
        /// True if case-sensitive
        /// </summary>
        public bool CaseSensitive;
    }
}
