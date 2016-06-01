using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    public class OmfSymbol
    {
        public string Name;

        public enum SymbolTypes
        {
            Other = 0,
            Byte = 3,
            ShortWord = 5,
            LongWord = 7,
            Float = 10,
            Double = 11,
            LargeFloat = 12,
            InstructionAddress = 15,
        }

        public SymbolTypes SymbolType;

        public enum AttributeDefinitions
        {
            GlobalCompilerSymbol = 0x800,
            ConstantGeneric = 0x1000,
            ConstantEqu = 0x1001,
            ConstantSet = 0x1002,
            ConstantConst = 0x1003,
            ConstantDefine = 0x1004,
            AssemblerStaticSymbol = 0x1300,
        }

        public AttributeDefinitions AttributeDefinition;

        public int Value;

        public int UnknownData;
    }
}
