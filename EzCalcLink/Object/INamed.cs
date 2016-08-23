using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
    /// <summary>
    /// Interface for things in an object file which are cross-referenced by name.
    /// </summary>
    public interface INamed
    {
        /// <summary>
        /// Returns the name of an item.
        /// </summary>
        string Name
        {
            get;
        }
    }
}
