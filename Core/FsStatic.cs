using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FrogSharp.Common;

namespace FrogSharp.Core
{
    public class FsStatic : FsCallable
    {
        private string methodString;
        private CultureInfo cultureInfo;

        public FsStatic(string methodString, CultureInfo cultureInfo)
        {
            this.methodString = methodString;
            var variable = new FsObject();
            variable.Add(0);
            Variables.Add(string.Empty, variable);
            this.cultureInfo = cultureInfo;
        }

        private static bool IsAssignableFrom(Type returnable, IEnumerable<Type> types)
        {
            return types.Any(returnable.IsAssignableFrom);
        }

        private float CalculateValue()
        {
            var args = new object[Arguments.Count];
            for (var i = 0; i < args.Length; i++)
            {
                var element = Arguments.ElementAt(i);
                var obj = GetObject(element.Key);
                if(obj != null) { args[i] = obj[element.Value]; continue; }

                args[i] = float.Parse(element.Key, NumberStyles.Any, cultureInfo);
            }
            
            var separatedMethod = methodString.Split(new[] {Constants.Separator}, StringSplitOptions.None);
            
            var type = Type.GetType(methodString.Substring(0,
                methodString.LastIndexOf(Constants.Separator, StringComparison.CurrentCulture) < 0 ?
                    0 : methodString.LastIndexOf(Constants.Separator, StringComparison.CurrentCulture)));
            
            var returned = type.GetMethod(separatedMethod.Last(), new[] { typeof(float) }).Invoke(null, args);

            if (returned != null)
                return IsAssignableFrom(returned.GetType(), new [] { typeof(float), typeof(double), typeof(int), typeof(long), typeof(bool) }) ? Convert.ToSingle(returned) : 0;
            return 0;
        }

        public override FsObject Return()
        {
            Variables.First().Value.Set(CalculateValue(), 0);
            return Variables.First().Value;
        }
    }
}