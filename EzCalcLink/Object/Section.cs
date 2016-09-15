using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EzCalcLink.Linker;

namespace EzCalcLink.Object
{
    /// <summary>
    /// Contains a section's data and relocations list
    /// </summary>
    public class Section : INamed
    {
        private string _Name;
        /// <summary>
        /// Specifies the section's name
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }
        
        /// <summary>
        /// References the address space this section lives in
        /// </summary>
        public AddressSpace AddressSpace;

        /// <summary>
        /// Contains a list of all blocks of data in the section
        /// </summary>
        public List<ContiguousMemory> Data = new List<ContiguousMemory>();


        /// <summary>
        /// Indicates whether the section is known to be relocatable.
        /// </summary>
        public bool Relocatable = false;


        /// <summary>
        /// Indicates whether the section is shared and all object files have absolute addresses already.
        /// For VRAMy stuff and OS static vars, I guess.
        /// </summary>
        public bool SharedAbsolute = false;

        /// <summary>
        /// Contains a list of all relocations found in the file
        /// </summary>
        public Dictionary<int, RelocationExpression> Relocations = new Dictionary<int, RelocationExpression>();

        /// <summary>
        /// For a statically linked object file, returns the final address this
        /// section will live at. For a dynamically linked file, returns the
        /// offset from the start of the section's final start address. (Used
        /// when collating sections from different object files.)
        /// </summary>
        public int BaseAddress;


        /// <summary>
        /// True if the final address of this section has been resolved.
        /// </summary>
        public bool Resolved;


        /// <summary>
        /// Returns true if all data in the section is combined into one memory
        /// </summary>
        public bool HasOneMemory
        {
            get
            {
                return Data.Count == 1;
            }
        }

        /// <summary>
        /// If the section has exactly one ContiguousMemory, returns it;
        /// other wise, throws an InvalidOperationException.
        /// </summary>
        public ContiguousMemory Memory
        {
            get
            {
                if (HasOneMemory)
                    return Data[0];
                else
                    throw new InvalidOperationException("Section does not have exactly one memory.");
            }
        }

        /// <summary>
        /// If the section has exactly one ContiguousMemory, returns its size.
        /// If not, throws an InvalidOperationException.
        /// </summary>
        public int Size
        {
            get
            {
                return Memory.Size;
            }
        }


        /// <summary>
        /// Notation for assisting with parsing
        /// </summary>
        public int ExpectedSize;


        /// <summary>
        /// Notation for assisting with parsing
        /// </summary>
        public int NextAddress;


        /// <summary>
        /// Adds a byte to the section.
        /// </summary>
        /// <param name="b"></param>
        public void SetByte(byte b)
        {
            SetByte(NextAddress, b);
            NextAddress++;
        }


        /// <summary>
        /// Adds a byte to the section at a given address.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="b"></param>
        public void SetByte(int address, byte b)
        {
            NextAddress = address;
            ContiguousMemory m = Data.Where(x => x.CanAdd(address)).FirstOrDefault();
            if (m == null)
                Data.Add(new ContiguousMemory(address, b));
            else
            {
                m[address] = b;
                ContiguousMemory n = Data.Where(x => x != m && m.CanMergeWith(x)).FirstOrDefault();
                if (n == null)
                    return;
                m.MergeWith(n);
                Data.Remove(n);
            }
        }
    }
}
