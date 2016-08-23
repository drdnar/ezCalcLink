using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
    /// <summary>
    /// This is simply a marker. I guess it could be an enum.
    /// </summary>
    public class AddressSpace : INamed
    {
        protected string _Name;
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
        public int MauSize;
    }
}
