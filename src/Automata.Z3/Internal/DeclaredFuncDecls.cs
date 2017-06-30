using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Z3;

using Microsoft.Automata;

namespace Microsoft.Automata.Z3.Internal
{

    internal class DeclaredFuncDecls
    {
        Dictionary<Sequence<object>, FuncDecl> funcDecls = new Dictionary<Sequence<object>, FuncDecl>();

        internal DeclaredFuncDecls()
        {
        }

        internal bool TryGetFuncDecl(out FuncDecl funcDecl, string name, params object[] keys)
        {
            object[] objs = new object[keys.Length + 1];
            objs[0] = name;
            Array.Copy(keys, 0, objs, 1, keys.Length);
            Sequence<object> s = new Sequence<object>(objs);

            if (funcDecls.TryGetValue(s, out funcDecl))
                return true;

            funcDecl = null;
            return false;
        }

        internal void AddFuncDecl(FuncDecl funcDecl, string name, params object[] keys)
        {
            object[] objs = new object[keys.Length + 1];
            objs[0] = name;
            Array.Copy(keys, 0, objs, 1, keys.Length);
            var s = new Sequence<object>(objs);
            funcDecls.Add(new Sequence<object>(objs), funcDecl);
        }
    }
}
