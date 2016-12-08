using EzCalcLink.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzCalcLink.Linker
{
    /// <summary>
    /// Resolves names, while allowing the underlying look up algorithm to be changed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NameResolver<T> : IEnumerable<T> where T : INamed
    {
        private List<T> Symbols = new List<T>();
        private Dictionary<string, T> SymbolsByName = new Dictionary<string, T>();
        private Dictionary<string, bool> SymbolReferenced = new Dictionary<string, bool>();

        public NameResolver(bool caseSensitive = true)
        {
            _CaseSensitive = caseSensitive;
        }

        private bool _CaseSensitive = true;
        /// <summary>
        /// If true, symbols resolved by name should be case-sensitive.
        /// </summary>
        public bool CaseSensitive
        {
            get
            {
                return _CaseSensitive;
            }
            set
            {
                _CaseSensitive = value;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        
        public IEnumerator<T> GetEnumerator()
        {
            //return Symbols.GetEnumerator();
            foreach (var v in SymbolsByName)
                yield return v.Value;
        }
        

        public int Count
        {
            get
            {
                return SymbolsByName.Count;
            }
        }


        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }


        public void Clear()
        {
            throw new NotImplementedException();
        }


        public bool Contains(T t)
        {
            if (!_CaseSensitive)
                return SymbolsByName.ContainsKey(t.Name.ToUpper());
            else
                return SymbolsByName.ContainsKey(t.Name);
        }


        public void CopyTo(T[] t, int i)
        {
            Symbols.CopyTo(t, i);
        }


        public bool Remove(T t)
        {
            Symbols.Remove(t);
            SymbolsByName.Remove(t.Name);
            return SymbolReferenced.Remove(t.Name);
        }


        /// <summary>
        /// Adds a symbol to the name resolution table.
        /// </summary>
        /// <param name="t"></param>
        public void Add(T t)
        {
            Symbols.Add(t);
            string s = t.Name;
            if (!_CaseSensitive)
                s = s.ToUpper();
            SymbolsByName.Add(s, t);
            SymbolReferenced.Add(s, false);
        }


        public T this[string name]
        {
            get
            {
                return Get(name);
            }
        }

        /// <summary>
        /// Returns the symbol associated with the given name.
        /// Will throw an exception if the name lookup fails.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T Get(string name)
        {
            if (!_CaseSensitive)
                name = name.ToUpper();
            return SymbolsByName[name];
        }


        /// <summary>
        /// Returns the symbol associated with the given name.
        /// Returns null if the name lookup fails.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetNullable(string name)
        {
            T temp;
            if (!_CaseSensitive)
                name = name.ToUpper();
            if (SymbolsByName.TryGetValue(name, out temp))
                return temp;
            else
                return default(T);
        }


        /// <summary>
        /// Attempts to lookup the symbol associated with the given name.
        /// </summary>
        /// <param name="name">Name to lookup.</param>
        /// <param name="outValue">If the symbol is found, the symbol is written to this parameter.</param>
        /// <returns>Returns true if the lookup succeeds.</returns>
        public bool TryGet(string name, out T outValue)
        {
            if (!_CaseSensitive)
                name = name.ToUpper();
            return SymbolsByName.TryGetValue(name, out outValue);
        }


        
    }
}
