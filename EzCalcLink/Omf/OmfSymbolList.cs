using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    public class OmfSymbolList : List<OmfSymbol>
    {
        public OmfSymbolList()
        {
            
        }
        
        public OmfSymbol GetOrCreate(int i)
        {
            if (Count > i)
                if (this[i] != null)
                    return this[i];
                else
                    return this[i] = new OmfSymbol();
            for (int j = Count; j <= i; j++)
                Add(null);
            return this[i] = new OmfSymbol();
        }

    }
}
