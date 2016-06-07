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
        /// <summary>
        /// Holds both the type of the element, and if it is a function or variable, its purpose.
        /// </summary>
        private byte Type;

        /// <summary>
        /// For numbers, hold the number's value. For variables, holds the
        /// variable's argument, if any.
        /// </summary>
        private int Datum;

        /// <summary>
        /// Returns an expression element parsed from the given data at the given start index.
        /// The index is incremented past the element.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static OmfExpressionElement FromArray(ref int index, byte[] data)
        {
            OmfExpressionElement e = new OmfExpressionElement();

            bool isEscapedValue;

            byte b = data[index++];
            if (b <= 0x84)
            {
                e.Type = 0;
                index--;
                e.Datum = Omf695.ReadNumber(data, ref index, out isEscapedValue);
            }
            else if (b >= 0x90 && b < 0xC0)
                e.Type = b;
            else
            {
                e.Type = b;
                if (VariableIdsWithArgument.Contains(b))
                    e.Datum = Omf695.ReadNumber(data, ref index, out isEscapedValue);
                else if (!VariableIdsWithoutArgument.Contains(b))
                    throw new FormatException("Unknown variable letter in OMF expression.");
            }
            return e;
        }


        /// <summary>
        /// Different types of expression elements
        /// </summary>
        public enum ElementTypes
        {
            Number = 0,
            Function = 1,
            Variable = 2,
        }

        /// <summary>
        /// Different types of expression functions.
        /// </summary>
        public enum FunctionTypes
        {
            Unknown90 = 0x90,
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
/*            Escape = 0xB9,
            EscapeIsDef = 0x1B9,
            EscapeTrans = 0x2B9,
            EscapeSplit = 0x3B9,
            EscapeInBlock = 0x4B9,
            EscapeCall_Opt = 0x5B9,*/
            IsDef = 0xB9,
            OpenExpressionA = 0xBA,
            CloseExpressionA = 0xBB,
            OpenExpressionB = 0xBC,
            CloseExpressionB = 0xBD,
            OpenExpressionC = 0xBE,
            CloseExpressionC = 0xBF,
        }

        
        /// <summary>
        /// Returns the purpose of this element.
        /// </summary>
        public ElementTypes ElementType
        {
            get
            {
                if (Type == 0)
                    return ElementTypes.Number;
                else if (Type >= 0xC0 && Type < 0xE0)
                    return ElementTypes.Variable;
                else
                    return ElementTypes.Function;
            }
        }

        /// <summary>
        /// If this element is a number, returns the number. 
        /// If this element is a variable, returns the argument of the variable.
        /// Throws an exception otherwise.
        /// </summary>
        public int Number
        {
            get
            {
                if (ElementType == ElementTypes.Number || ElementType == ElementTypes.Variable)
                    return Datum;
                throw new FormatException("Element is not a number");
            }
        }

        /// <summary>
        /// If this element is a variable, returns its letter. Throws an exception otherwise.
        /// </summary>
        public char Variable
        {
            get
            {
                if (ElementType == ElementTypes.Variable)
                    if (Type == 0xC0)
                        return '_';
                    else
                        return (char)(Type - 0xC1 + (int)'A');
                throw new FormatException("Element is not a variable");
            }
        }

        /// <summary>
        /// If this element is a function, returns its function ID. Throws an exception otherwise.
        /// </summary>
        public FunctionTypes FunctionType
        {
            get
            {
                if (ElementType == ElementTypes.Function)
                    return (FunctionTypes)Type;
                throw new FormatException("Element is not a function");
            }
        }

        /// <summary>
        /// Returns true if this element is a variable, and specifically, one which has an argument.
        /// </summary>
        public bool IsVariableWithArgument
        {
            get
            {
                return VariableIdsWithArgument.Contains(Type);
            }
        }

        public override string ToString()
        {
            switch (ElementType)
            {
                case OmfExpressionElement.ElementTypes.Number:
                    return Datum.ToString("X6");
                case OmfExpressionElement.ElementTypes.Variable:
                    if (IsVariableWithArgument)
                        return Variable.ToString() + "(" + Datum.ToString("X2") + ")";
                    else
                        return Variable.ToString();
                case OmfExpressionElement.ElementTypes.Function:
                    switch (FunctionType)
                    {
                        case FunctionTypes.Unknown90:
                            return "PUSH";
                        case FunctionTypes.F:
                            return "FALSE";
                        case FunctionTypes.T:
                            return "TRUE";
                        case FunctionTypes.Abs:
                            return "ABS";
                        case FunctionTypes.Neg:
                            return "(-)";
                        case FunctionTypes.Not:
                            return "!";
                        case FunctionTypes.Add:
                            return "+";
                        case FunctionTypes.Subtract:
                            return "-";
                        case FunctionTypes.Divide:
                            return "/";
                        case FunctionTypes.Multiply:
                            return "*";
                        case FunctionTypes.Max:
                            return "MAX";
                        case FunctionTypes.Min:
                            return "MIN";
                        case FunctionTypes.Mod:
                            return "%";
                        case FunctionTypes.LessThan:
                            return "<";
                        case FunctionTypes.GreaterThan:
                            return ">";
                        case FunctionTypes.EqualTo:
                            return "==";
                        case FunctionTypes.NotEqualTo:
                            return "!=";
                        case FunctionTypes.And:
                            return "&";
                        case FunctionTypes.Or:
                            return "|";
                        case FunctionTypes.Xor:
                            return "^";
                        case FunctionTypes.Ext:
                            return "EXT";
                        case FunctionTypes.Ins:
                            return "INS";
                        case FunctionTypes.Err:
                            return "ERR";
                        case FunctionTypes.If:
                            return "IF";
                        case FunctionTypes.Else:
                            return "ELSE";
                        case FunctionTypes.IsDef:
                            return "ISDEF";
                        case FunctionTypes.OpenExpressionA:
                            return "{A{";
                        case FunctionTypes.CloseExpressionA:
                            return "}A}";
                        case FunctionTypes.OpenExpressionB:
                            return "{B{";
                        case FunctionTypes.CloseExpressionB:
                            return "}B}";
                        case FunctionTypes.OpenExpressionC:
                            return "{C{";
                        case FunctionTypes.CloseExpressionC:
                            return "}C}";
                    }
                    return "??";
            }
            return "This really shouldn't be possible to return.";
        }

        public static readonly byte[] VariableIdsWithArgument = new byte[] { 0xC1, 0xC2, 0xC6, 0xC9, 0xCC, 0xCD, 0xCE, 0xD2, 0xD3, 0xD7, 0xD8 };
        public static readonly byte[] VariableIdsWithoutArgument = new byte[] { 0xC7 };
        public static readonly char[] VariablesWithArgument = new char[] { 'A', 'B', 'F', 'I', 'L', 'M', 'N', 'R', 'S', 'W', 'X' };
        public static readonly char[] VariablesWithoutArgument = new char[] { 'G' };
    }
}
