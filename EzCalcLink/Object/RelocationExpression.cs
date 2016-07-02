using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Object
{
    public class RelocationExpression
    {
        /// <summary>
        /// Exception thrown when there's an error parsing a relocation expression.
        /// </summary>
        public class RelocationParseException : Exception
        {
            public RelocationParseException()
            {

            }

            public RelocationParseException(string message)
                : base(message)
            {
            
            }
        }


        /// <summary>
        /// Used for RPN expression parsing
        /// </summary>
        protected Stack<int> Stack;


        /// <summary>
        /// Used to make processing expressions slightly less awkward.
        /// </summary>
        protected int ReturnedByes;


        /// <summary>
        /// The actual expression.
        /// </summary>
        public readonly List<Element> Expression = new List<Element>();


        /// <summary>
        /// References the object file this RelocationExpression belongs to.
        /// Needed for expression resolution.
        /// </summary>
        public ObjectFile ObjectFile;


        /// <summary>
        /// Evaluates the expression
        /// </summary>
        /// <returns></returns>
        public int Evaluate()
        {
            int b;
            return Evaluate(out b);
        }


        /// <summary>
        /// Evaluates the expression
        /// </summary>
        /// <param name="returnedBytes">Number of bytes returned, or -1 if unspecified.</param>
        /// <returns></returns>
        public int Evaluate(out int returnedBytes)
        {
            ReturnedByes = -1;
            Stack = new Stack<int>();
            for (int i = 0; i < Expression.Count; i++)
                if (Expression[i] is NumberElement)
                    Stack.Push(((NumberElement)Expression[i]).Value);
                else if (Expression[i] is Function)
                    ((Function)Expression[i]).Evaluate(this);
                else
                    throw new InvalidOperationException("Expression contains element that is neither a function nor a number.");
            returnedBytes = ReturnedByes;
            if (Stack.Count == 1)
                return Stack.Pop();
            else
                throw new RelocationParseException("Stack still has values on it after expression evaluated fully.");
        }


        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            bool first = true;
            foreach (var e in Expression)
            {
                if (first)
                    first = false;
                else
                    s.Append(" ");
                s.Append(e.ToString());
            }
            return s.ToString();
        }


        /// <summary>
        /// Represents an element in a relocation expression.
        /// </summary>
        public abstract class Element
        {
            
        }

        protected abstract class Function : Element
        {
            internal abstract void Evaluate(RelocationExpression e);
        }


        /// <summary>
        /// Represents a number in a relocation expression.
        /// </summary>
        protected class NumberElement : Element
        {
            public int Value;

            public NumberElement(int n)
            {
                Value = n;
            }
            
            public override string ToString()
            {
                return "0x" + Value.ToString("X");
            }
        }


        public Element Number(int n)
        {
            return new NumberElement(n);
        }


        protected class GetSectionAddressFunction : Function
        {
            internal Section section;

            internal override void Evaluate(RelocationExpression e)
            {
                if (!section.Resolved)
                    throw new RelocationParseException(string.Format("Tried to get final address of section {0}, whose final address has not been resolved.", section.Name));
                e.Stack.Push(section.BaseAddress);
            }

            public override string ToString()
            {
                return "R(" + section.Name + ")";
            }
        }


        /// <summary>
        /// Returns the computed start address of the given section.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Element GetSectionAddress(Section s)
        {
            GetSectionAddressFunction f = new GetSectionAddressFunction();
            f.section = s;
            return f;
        }


        protected class GetSymbolAddressFunction : Function
        {
            internal Symbol symbol;

            internal override void Evaluate(RelocationExpression e)
            {
                if (!symbol.Resolved)
                    throw new RelocationParseException(string.Format("Tried to get final address of symbol {0}, whose final address has not been resolved.", symbol.Name));
                e.Stack.Push(symbol.Offset);
            }

            public override string ToString()
            {
                return "X(" + symbol.Name + ")";
            }
        }


        /// <summary>
        /// Returns the computed address of the given symbol.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Element GetSymbolAddress(Symbol s)
        {
            GetSymbolAddressFunction f = new GetSymbolAddressFunction();
            f.symbol = s;
            return f;
        }


        protected class BeginExpressionFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                // Do nothing
            }

            public override string ToString()
            {
                return "{";
            }
        }

        
        /// <summary>
        /// Marks the beginning of an expression which returns a specified number of bits.
        /// </summary>
        public static readonly Element BeginExpression = new BeginExpressionFunction();
        

        protected class EndExpressionFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.ReturnedByes = e.Stack.Pop();
            }

            public override string ToString()
            {
                return "}";
            }
        }


        /// <summary>
        /// Marks the end of an expression.  Takes one argument: the number of bytes to return
        /// </summary>
        public static readonly Element EndExpression = new EndExpressionFunction();


        protected class AddFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.Stack.Push(e.Stack.Pop() + e.Stack.Pop());
            }

            public override string ToString()
            {
                return "+";
            }
        }


        /// <summary>
        /// Adds the top two elements on the expression stack stack together.
        /// </summary>
        public static readonly Element Add = new AddFunction();


        protected class SubtractFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int t = e.Stack.Pop();
                e.Stack.Push(e.Stack.Pop() - t);
            }

            public override string ToString()
            {
                return "-";
            }
        }


        /// <summary>
        /// Subtracts from the top stack element the second stack element.
        /// </summary>
        public static readonly Element Subtract = new SubtractFunction();


        protected class MultiplyFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.Stack.Push(e.Stack.Pop() * e.Stack.Pop());
            }

            public override string ToString()
            {
                return "*";
            }
        }


        /// <summary>
        /// Multiplies the top two stack elements.
        /// </summary>
        public static readonly Element Multiply = new MultiplyFunction();


        protected class DivideFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int t = e.Stack.Pop();
                e.Stack.Push(e.Stack.Pop() / t);
            }

            public override string ToString()
            {
                return "/";
            }
        }


        /// <summary>
        /// Divides the second stack element by the top stack element.
        /// </summary>
        public static readonly Element Divide = new DivideFunction();


        protected class ModulusFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int t = e.Stack.Pop();
                e.Stack.Push(e.Stack.Pop() % t);
            }

            public override string ToString()
            {
                return "%";
            }
        }


        /// <summary>
        /// stack(2) % stack(top)
        /// </summary>
        public static readonly Element Modulus = new ModulusFunction();


        protected class AbsoluteValueFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int t = e.Stack.Pop();
                e.Stack.Push(t >= 0 ? t : -t);
            }

            public override string ToString()
            {
                return "abs";
            }
        }


        /// <summary>
        /// abs(stack(top))
        /// </summary>
        public static readonly Element AbsoluteValue = new AbsoluteValueFunction();


        protected class MinimumFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int b = e.Stack.Pop();
                int a = e.Stack.Pop();
                e.Stack.Push(a < b ? a : b);
            }

            public override string ToString()
            {
                return "min";
            }
        }


        /// <summary>
        /// min(stack(2), stack(top))
        /// </summary>
        public static readonly Element Minimum = new MinimumFunction();


        protected class MaximumFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int b = e.Stack.Pop();
                int a = e.Stack.Pop();
                e.Stack.Push(a > b ? a : b);
            }

            public override string ToString()
            {
                return "max";
            }
        }


        /// <summary>
        /// max(stack(2), stack(top))
        /// </summary>
        public static readonly Element Maximum = new MaximumFunction();


        protected class LeftShiftFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int t = e.Stack.Pop();
                e.Stack.Push(e.Stack.Pop() << t);
            }

            public override string ToString()
            {
                return "<<";
            }
        }


        /// <summary>
        /// stack(2) << stack(top)
        /// </summary>
        public static readonly Element LeftShift = new LeftShiftFunction();


        protected class RightShiftFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                int t = e.Stack.Pop();
                e.Stack.Push(e.Stack.Pop() >> t);
            }

            public override string ToString()
            {
                return ">>";
            }
        }


        /// <summary>
        /// stack(2) >> stack(top)
        /// </summary>
        public static readonly Element RightShift = new RightShiftFunction();


        protected class NotFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.Stack.Push(~e.Stack.Pop());
            }

            public override string ToString()
            {
                return "~";
            }
        }


        /// <summary>
        /// ~stack(top)
        /// </summary>
        public static readonly Element Not = new NotFunction();


        protected class AndFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.Stack.Push(e.Stack.Pop() & e.Stack.Pop());
            }

            public override string ToString()
            {
                return "&";
            }
        }


        /// <summary>
        /// stack(2) & stack(top)
        /// </summary>
        public static readonly Element And = new AndFunction();


        protected class OrFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.Stack.Push(e.Stack.Pop() | e.Stack.Pop());
            }

            public override string ToString()
            {
                return "|";
            }
        }


        /// <summary>
        /// stack(2) | stack(top)
        /// </summary>
        public static readonly Element Or = new OrFunction();


        protected class XorFunction : Function
        {
            internal override void Evaluate(RelocationExpression e)
            {
                e.Stack.Push(e.Stack.Pop() ^ e.Stack.Pop());
            }

            public override string ToString()
            {
                return "^";
            }
        }


        /// <summary>
        /// stack(2) ^ stack(top)
        /// </summary>
        public static readonly Element Xor = new XorFunction();

    }
}
