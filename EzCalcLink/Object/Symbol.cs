﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
    /// <summary>
    /// Contains information about a symbol
    /// </summary>
    public class Symbol
    {
        /// <summary>
        /// The symbol's name, used to resolve references
        /// </summary>
        public string Name;

        /// <summary>
        /// The address space the symbol lives in
        /// </summary>
        public AddressSpace AddressSpace;

        /// <summary>
        /// True if the Offset value is valid as a final value to write to the output file
        /// </summary>
        public bool Resolved;

        /// <summary>
        /// Before the linking process, this is the offset from the start of the
        /// object file's local segment.
        /// After linking, for statically linked files, this is the final address;
        /// for dynamically linked files, this is the offset from the collated
        /// segment start.
        /// </summary>
        public int Offset;

        /// <summary>
        /// Reference back to the referencing object file
        /// </summary>
        public ObjectFile ObjectFile;

        /// <summary>
        /// True if the symbol is an external reference
        /// </summary>
        public bool External;

        /// <summary>
        /// If the symbol is not external, references the section the symbol lives in
        /// </summary>
        public Section Section;
    }
}