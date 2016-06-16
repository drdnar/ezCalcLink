using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
    /// <summary>
    /// Contains information needed to resolve a relocation
    /// </summary>
    public struct Relocation
    {
        /// <summary>
        /// Specifies the location in the section where the relocation is
        /// </summary>
        public int Location;

        /// <summary>
        /// Specifies the section offset or final address.
        /// </summary>
        public int Target;

        public enum RelocationType
        {
            SectionOffset,
            VariableIndex,
        }

        /// <summary>
        /// Specifies the relocation's type
        /// </summary>
        public RelocationType Type;

        
    }
}
