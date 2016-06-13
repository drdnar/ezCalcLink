using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink
{
    /// <summary>
    /// Contains a parsed expression.
    /// </summary>
    public class OmfExpression
    {
        /// <summary>
        /// Returns an expression, parsed from a data array.
        /// </summary>
        /// <param name="index">Index into source data. This is incremented past the end of the expression.</param>
        /// <param name="data">Source data array</param>
        /// <returns></returns>
        public static OmfExpression FromArray(ref int index, byte[] data)
        {
            OmfExpression e = new OmfExpression();

            int start = index;

            while (data[index] < 0xE0)
                e.Elements.Add(OmfExpressionElement.FromArray(ref index, data));

            e.Data = new byte[index - start];
            for (int i = 0; start < index; start++, i++)
                e.Data[i] = data[start];

            if (e.Elements.Count == 1 && e.Elements[0].ElementType == OmfExpressionElement.ElementTypes.Number)
            {
                e.IsSimpleNumber = true;
                e.ResolvedValue = e.Elements[0].Number;
            }

            return e;
        }

        /// <summary>
        /// List of all elements in the array.
        /// </summary>
        public List<OmfExpressionElement> Elements = new List<OmfExpressionElement>();

        /// <summary>
        /// Raw data the expression was parsed from.
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// True if the expression is a simple number, and not an expression that requires resolving.
        /// </summary>
        public bool IsSimpleNumber;

        /// <summary>
        /// If the expression is a simple number, holds its value.
        /// If the expression is an actual expression, holds the resolved value of the expression
        /// (assuming it has been resolved).
        /// </summary>
        public int ResolvedValue;

        public override string ToString()
        {
            if (IsSimpleNumber)
                return ResolvedValue.ToString("X6");
            StringBuilder s = new StringBuilder();
            /*for (int i = 0; i < Data.Length; i++)
                s.Append(Data[i].ToString("X2"));*/
            bool first = true;
            foreach (var e in Elements)
            {
                if (first)
                    first = false;
                else
                    s.Append(" ");
                s.Append(e.ToString());
            }
            return s.ToString();
        }
    }
}
