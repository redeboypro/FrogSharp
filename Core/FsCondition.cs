using System.Collections.Generic;
using System.Linq;
using FrogSharp.Tools;

namespace FrogSharp.Core
{
    public class FsCondition : FsCodeElement
    {
        private readonly FsCallable callable;
        private readonly string a, b, logic;

        public FsCondition(FsCallable callable, string a, string b, string logic)
        {
            this.callable = callable;
            this.a = a;
            this.b = b;
            this.logic = logic;
        }

        public override void Invoke()
        {
            var aValue = GetValue(a);
            var bValue = GetValue(b);

            if (!LogicEvaluator.ParseLogic(aValue, bValue, logic)) return;
            
            foreach (var e in Elements)
                e.Invoke();
        }

        private float GetValue(string variable) => variable.Any(char.IsLetter) ? callable.GetAsSingle(variable) : float.Parse(variable);
    }
}