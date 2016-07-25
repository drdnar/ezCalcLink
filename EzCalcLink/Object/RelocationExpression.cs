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
        public int ReturnedByes;


        /// <summary>
        /// Also used to make processing expressions slightly less awkward.
        /// </summary>
        public int ReturnedBytesExpected;


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
            ReturnedByes = -1;
            Stack = new Stack<int>();
            for (int i = 0; i < Expression.Count; i++)
                if (Expression[i] is NumberElement)
                    Stack.Push(((NumberElement)Expression[i]).Value);
                else if (Expression[i] is Function)
                    ((Function)Expression[i]).Evaluate(this);
                else
                    throw new InvalidOperationException("Expression contains element that is neither a function nor a number.");
            if (Stack.Count == 1)
            {
                evaluated = true;
                return value = Stack.Pop();
            }
            else
                throw new RelocationParseException("Stack still has values on it after expression evaluated fully.");
        }


        /// <summary>
        /// Attempts to simplify a relocation expression
        /// </summary>
        public void SimplifyRelocation()
        {
            if (!(Expression[0] is GetSectionAddressFunction || Expression[0] is GetSymbolAddressFunction))
                return;
            Stack = new Stack<int>();
            Stack.Push(0); // Add dummy initial offset
            for (int i = 1; i < Expression.Count; i++)
                if (Expression[i] is GetSectionAddressFunction || Expression[i] is GetSymbolAddressFunction)
                    return;
                else if (Expression[i] is NumberElement)
                    Stack.Push(((NumberElement)Expression[i]).Value);
                else if (Expression[i] is Function)
                    ((Function)Expression[i]).Evaluate(this);
                else
                    throw new InvalidOperationException("Expression contains element that is neither a function nor a number.");
            if (Stack.Count == 1)
            {
                Simplified = true;
                var e = Expression[0];
                Expression.Clear();
                Expression.Add(e);
                Expression.Add(Add);
                AddNumber(Stack.Pop());
            }
            else
                throw new RelocationParseException("Stack still has values on it after expression evaluated fully.");           
        }


        /// <summary>
        /// True if the expression has been simplified to reference + offset format.
        /// </summary>
        public bool Simplified = false;


        /// <summary>
        /// Automatically set to true when the expression is evaluated successfully.
        /// </summary>
        protected bool evaluated = false;
        /// <summary>
        /// Returns true if the expression has previously been successfully evaluated.
        /// Can be manually forced to false to force reevaluation.
        /// Cannot be manually set to true.
        /// </summary>
        public bool Evaluated
        {
            get
            {
                return evaluated;
            }
            set
            {
                if (evaluated == value)
                    return;
                if (evaluated) // evaluated is true, and evaluated != value, so value == false;
                    evaluated = value; // Thus, this sets evaluated to false, manually forcing reevaluation
                else
                    throw new InvalidOperationException("Cannot manually set expression to Evaluated without actually evaluating it.");
            }
        }


        /// <summary>
        /// Holds the cached value of the last Evaluation.
        /// </summary>
        protected int value;
        /// <summary>
        /// If the expression has been evaluated, returns the cached value.
        /// If not, attempts to evaluate the expression.
        /// </summary>
        public int Value
        {
            get
            {
                if (evaluated)
                    return value;
                Evaluate();
                return value;
            }
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
            public static bool operator ==(Element a, Element b)
            {
                if (a is NumberElement)
                    if (b is NumberElement)
                        return ((NumberElement)a).Value == ((NumberElement)b).Value;
                    else
                        return false;
                if (a is GetSectionAddressFunction)
                    if (b is GetSectionAddressFunction)
                        return ((GetSectionAddressFunction)a).section == ((GetSectionAddressFunction)b).section;
                    else
                        return false;
                if (a is GetSymbolAddressFunction)
                    if (b is GetSymbolAddressFunction)
                        return ((GetSymbolAddressFunction)a).symbol == ((GetSymbolAddressFunction)b).symbol;
                    else
                        return false;
                if (a is Function)
                    if (b is Function)
                        return object.ReferenceEquals(a, b);
                    else
                        return false;
                return false;
            }


            public static bool operator !=(Element a, Element b)
            {
                return !(a == b);
            }
        }

        protected abstract class Function : Element
        {
            internal abstract void Evaluate(RelocationExpression e);
        }


        /// <summary>
        /// Represents a number in a relocation expression.
        /// </summary>
        public class NumberElement : Element
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


        public Element AddNumber(int n)
        {
            Element e = new NumberElement(n);
            Expression.Add(e);
            return e;
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


        /// <summary>
        /// Adds a reference to the start address of the given section.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Element AddSectionAddress(Section s)
        {
            Element e = GetSectionAddress(s);
            Expression.Add(e);
            return e;
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


        /// <summary>
        /// Adds a reference to the address of the given symbol.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Element AddSymbolAddress(Symbol s)
        {
            Element e = GetSymbolAddress(s);
            Expression.Add(e);
            return e;
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
        

        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddBeginExpression()
        {
            Expression.Add(BeginExpression);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddEndExpression()
        {
            Expression.Add(EndExpression);
        }


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

        
        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddAdd()
        {
            Expression.Add(Add);
        }


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

        
        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddSubtract()
        {
            Expression.Add(Subtract);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddMultiply()
        {
            Expression.Add(Multiply);
        }


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

        
        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddDivide()
        {
            Expression.Add(Divide);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddModulus()
        {
            Expression.Add(Modulus);
        }


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

        
        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddAbsoluteValue()
        {
            Expression.Add(AbsoluteValue);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddMinimum()
        {
            Expression.Add(Minimum);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddMaximum()
        {
            Expression.Add(Maximum);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddLeftShift()
        {
            Expression.Add(LeftShift);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddRightShift()
        {
            Expression.Add(RightShift);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddNot()
        {
            Expression.Add(Not);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddAnd()
        {
            Expression.Add(And);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddOr()
        {
            Expression.Add(Or);
        }


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


        /// <summary>
        /// Adds the command
        /// </summary>
        public void AddXor()
        {
            Expression.Add(Xor);
        }
    }
}
