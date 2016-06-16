using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    public class ContiguousMemory
    {
        /// <summary>
        /// Address of index 0
        /// </summary>
        protected int startAddress;
        /// <summary>
        /// First valid index in array
        /// </summary>
        protected int firstValidIndex;
        /// <summary>
        /// Last valid index in array PLUS ONE.
        /// Unlike firstValidIndex, this is exclusive.
        /// </summary>
        protected int lastValidIndex;
        /// <summary>
        /// Internal data array.
        /// </summary>
        protected byte[] data = new byte[128];


        /// <summary>
        /// Returns the first address with valid data in this ContiguousMemory
        /// </summary>
        public int StartAddress
        {
            get
            {
                return startAddress + firstValidIndex;
            }
        }


        /// <summary>
        /// Returns the number of valid bytes in this ContiguousMemory
        /// </summary>
        public int Size
        {
            get
            {
                return lastValidIndex - firstValidIndex;
            }
        }


        /// <summary>
        /// Returns the last address PLUS ONE with valid data in this ContiguousMemory;
        /// this bound is EXCLUSIVE, unlike StartAddress.
        /// </summary>
        public int EndAddress
        {
            get
            {
                return startAddress + Size;
            }
        }


        protected int RightMarginSize
        {
            get
            {
                return data.Length - lastValidIndex;
            }
        }


        public ContiguousMemory(int startAddress, byte firstByte)
        {
            this.startAddress = startAddress;
            data[0] = firstByte;
            firstValidIndex = 0;
            lastValidIndex = 1;
        }


        /// <summary>
        /// Gets or sets data in this ContiguousMemory.
        /// When adding data, the address must be within the current range of this
        /// ContiguousMemory, or one byte outside of it.  This is a ContiguousMemory,
        /// after all.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public byte this[int address]
        {
            get
            {
                int addr = address - startAddress;
                if (addr < firstValidIndex || addr >= lastValidIndex)
                    throw new IndexOutOfRangeException();
                return data[addr];
            }
            set
            {
                int addr = address - startAddress;
                if (addr < firstValidIndex - 1 || addr >= lastValidIndex + 1)
                    throw new IndexOutOfRangeException();
                if (addr == firstValidIndex - 1)
                {
                    if (addr == -1)
                        ExpandLower();
                    data[address - startAddress] = value;
                    firstValidIndex--;
                }
                else if (addr == lastValidIndex)
                {
                    if (addr == data.Length)
                        ExpandHigher();
                    data[addr] = value;
                    lastValidIndex++;
                }
                else
                    data[addr] = value;
            }
        }


        /// <summary>
        /// Expands the data array to higher addresses.
        /// </summary>
        protected void ExpandHigher()
        {
            int newSize = Size + data.Length;
            byte[] newData = new byte[newSize];
            for (int i = firstValidIndex; i < lastValidIndex; i++)
                newData[i] = data[i];
            data = newData;
        }


        /// <summary>
        /// Expands the data array to lower addresses.
        /// </summary>
        protected void ExpandLower()
        {
            int oldSize = Size;
            int newSize = oldSize + data.Length;
            byte[] newData = new byte[newSize];
            for (int i = firstValidIndex; i < lastValidIndex; i++)
                newData[i + oldSize] = data[i];
            firstValidIndex += oldSize;
            startAddress -= oldSize;
            lastValidIndex += oldSize;
            data = newData;
        }


        /// <summary>
        /// Returns a copy of the data in this ContiguousMemory.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            byte[] d = new byte[Size];
            for (int i = 0; i < Size; i++)
                d[i] = data[i + firstValidIndex];
            return d;
        }


        /// <summary>
        /// Shrinks the ContiguousMemory's internal data array to contain only valid data.
        /// </summary>
        public void Trim()
        {
            data = ToArray();
            startAddress = StartAddress;
            firstValidIndex = 0;
            lastValidIndex = data.Length;
        }


        /// <summary>
        /// Returns true if address can be added to this ContiguousMemory.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool CanAdd(int address)
        {
            return address >= StartAddress - 1 && address <= EndAddress;
        }


        /// <summary>
        /// Returns true if m can be merged with this ContiguousMemory.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool CanMergeWith(ContiguousMemory m)
        {
            return m.EndAddress == StartAddress || m.StartAddress == EndAddress;
        }


        /// <summary>
        /// Merges the data in m into this ContiguousMemory.
        /// </summary>
        /// <param name="m"></param>
        public void MergeWith(ContiguousMemory m)
        {
            if (m.EndAddress != StartAddress && m.StartAddress != EndAddress)
                throw new IndexOutOfRangeException();
            if (EndAddress == m.StartAddress)
            {
                byte[] newData = new byte[Size + m.Size + firstValidIndex + m.RightMarginSize];
                for (int i = firstValidIndex; i < lastValidIndex; i++)
                    newData[i] = data[i];
                for (int i = m.firstValidIndex; i < m.lastValidIndex; i++)
                    newData[i + lastValidIndex - m.firstValidIndex] = m.data[i];
                lastValidIndex += m.Size;
                data = newData;
            }
            else
            {
                byte[] newData = new byte[Size + m.Size + m.firstValidIndex + RightMarginSize];
                for (int i = m.firstValidIndex; i < m.lastValidIndex; i++)
                    newData[i] = m.data[i];
                for (int i = firstValidIndex; i < lastValidIndex; i++)
                    newData[i + m.lastValidIndex - firstValidIndex] = data[i];
                lastValidIndex = m.firstValidIndex + m.Size + Size;
                firstValidIndex = m.firstValidIndex;
                data = newData;
            }
        }

        /// <summary>
        /// Appends the data in the given second memory.  The second memory's virtual start address is discarded.
        /// </summary>
        /// <param name="m"></param>
        public void Concatenate(ContiguousMemory m)
        {
            if (this.data.Length - this.lastValidIndex < m.Size)
            {
                byte[] newData = new byte[this.data.Length + m.Size];
                for (int i = 0; i < this.lastValidIndex; i++)
                    newData[i] = this.data[i];
                this.data = newData;
            }
            for (int i = this.lastValidIndex, j = m.firstValidIndex; j < m.lastValidIndex; i++, j++)
                this.data[i] = m.data[j];
            this.lastValidIndex += m.Size;
        }
    }
}
