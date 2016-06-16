using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    public class OmfSection
    {
        public List<ContiguousMemory> Memories = new List<ContiguousMemory>();

        /// <summary>
        /// Section index
        /// </summary>
        public int Index;

        /// <summary>
        /// Section's specified MAU size.  Should be 1?
        /// </summary>
        public int MauSize;

        /// <summary>
        /// Section's specified base address.  Should be . . . zero?
        /// </summary>
        public int SectionBaseAddress;

        /// <summary>
        /// Section . . . name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Some kind of tree
        /// </summary>
        public int ParentIndex;

        /// <summary>
        /// Some kind of linked list
        /// </summary>
        public int SiblingIndex;

        /// <summary>
        /// Specifies the address space (RAM, ROM, &c.)
        /// </summary>
        public int ContextIndex;

        /// <summary>
        /// Specifies nothing useful?
        /// </summary>
        public int AlignmentDivisor;

        /// <summary>
        /// Specifies the section's size
        /// </summary>
        public int Size;

        /// <summary>
        /// Section base address
        /// </summary>
        public int Offset;

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
            ContiguousMemory m = Memories.Where(x => x.CanAdd(address)).FirstOrDefault();
            if (m == null)
                Memories.Add(new ContiguousMemory(address, b));
            else
            {
                m[address] = b;
                ContiguousMemory n = Memories.Where(x => x != m && m.CanMergeWith(x)).FirstOrDefault();
                if (n == null)
                    return;
                m.MergeWith(n);
                Memories.Remove(n);
            }
        }


        internal struct Relocation
        {
            /// <summary>
            /// Address/offset in the output section the relocation exists at
            /// </summary>
            public int Address;

            /// <summary>
            /// Number of MAUs this relocation takes up (useful if mixed-mode and IN/OUT are used)
            /// </summary>
            public byte Size;

            /// <summary>
            /// Defines what this relocation refers to
            /// </summary>
            public enum TargetType : byte
            {
                SectionOffset,
                ExternalVariableIndex,
            }

            public TargetType Type;

            /// <summary>
            /// Gives the target section index or target variable number
            /// </summary>
            public int TargetIndex;

            /// <summary>
            /// For section indexes, gives the index into the section
            /// </summary>
            public int TargetOffset;


            public void FinalizeRelocation()
            {

            }
        }
    }
}
