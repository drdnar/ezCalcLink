using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    /// <summary>
    /// This is a lame way of faking a C union.
    /// </summary>
    public struct OmfExpressionElement
    {
        public enum ElementTypes : byte
        {
            Number = 0,
            Function = 1,
            Variable = 2,
        }

        public enum FunctionTypes
        {
            F = 0xA0,
            T = 0xA1,
            Abs = 0xA2,
            Neg = 0xA3,
            Not = 0xA4,
            Add = 0xA5,
            Subtract = 0xA6,
            Divide = 0xA7,
            Multiply = 0xA8,
            Max = 0xA9,
            Min = 0xAA,
            Mod = 0xAB,
            LessThan = 0xAC,
            GreaterThan = 0xAD,
            EqualTo = 0xAE,
            NotEqualTo = 0xAf,
            And = 0xB0,
            Or = 0xB1,
            Xor = 0xB2,
            Ext = 0xB3,
            Ins = 0xB4,
            Err = 0xB5,
            If = 0xB6,
            Else = 0xB7,
            End = 0xB8,
            Escape = 0xB9,
            EscapeIsDef = 0x1B9,
            EscapeTrans = 0x2B9,
            EscapeSplit = 0x3B9,
            EscapeInBlock = 0x4B9,
            EscapeCall_Opt = 0x5B9,
            OpenExpressionA = 0xBA,
            CloseExpressionA = 0xBB,
            OpenExpressionB = 0xBC,
            CloseExpressionB = 0xBD,
            OpenExpressionC = 0xBE,
            CloseExpressionC = 0xBF,
        }

        public ElementTypes ElementType;

        public int Datum;

        public int Number
        {
            get
            {
                if (ElementType == ElementTypes.Number)
                    return Datum;
                throw new FormatException("Element is not a number");
            }
        }

        public char Variable
        {
            get
            {
                if (ElementType == ElementTypes.Variable)
                    return (char)Datum;
                throw new FormatException("Element is not a variable");
            }
        }

        public FunctionTypes FunctionType
        {
            get
            {
                if (ElementType == ElementTypes.Function)
                    return (FunctionTypes)Datum;
                throw new FormatException("Element is not a function");
            }
        }

        
    }
}
