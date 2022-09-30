using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FrogSharp.Common;
using FrogSharp.Tools;

namespace FrogSharp.Core
{
    public class FsScript : FsCallable
    {
        public FsScript(string code)
        {
            Parent = this;
            var lines = LoadLines(code);
            Parse(this, lines, 0);
        }

        public FsObject Run()
        {
            foreach (var e in Elements) e?.Invoke();
            return Returnable != null ? Return() : FsObject.None;
        }

        public FsObject InvokeMethod(string name, float[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            
            var method = Methods[name];
            method.Arguments.Clear();
            
            foreach (var arg in args) method.Arguments.Add(arg.ToString(CultureInfo.CurrentCulture), 0);
            
            return method.Return();
        }

        public FsObject FindVariableByName(string name) => GetObject(name);
    }
}