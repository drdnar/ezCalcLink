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
        public List<Symbol> Symbols = new List<Symbol>();

        /// <summary>
        /// Master list of output symbols
        /// </summary>
        public List<Section> Sections = new List<Section>();

        protected virtual void OrganizeThings()
        {
            // Build master list of sections
            foreach (var o in ObjectFiles)
            {
                foreach (var s in o.Sections)
                {
                    if (Sections.Where(x => x.Name == s.Name).Count() == 0)
                    {
                        var n = new Section();
                        n.Name = s.Name;
                        n.ExpectedSize = s.ExpectedSize;
                    }
                    else
                    {
                        // Figure out if we need to increase the section size
                    }
            }
        }
    }
}
