using System.Collections.Generic;
using System.Linq;
using FrogSharp.Tools;

namespace FrogSharp.Core
{
    public class FsLoop : FsCodeElement
    {
        private readonly FsCallable callable;
        private readonly string indexHolder, array;

        public FsLoop(FsCallable callable, string indexHolder, string array)
        {
            this.callable = callable;
            this.array = array;
            this.indexHolder = indexHolder;
        }

        public override void Invoke()
        {
            for (var i = (int)callable.GetObject(indexHolder)[0];
                i < callable.GetObject(array).Count; i++)
            {
                callable.GetObject(indexHolder).Set(i, 0);
                foreach (var e in Elements)
                    e.Invoke();
            }
        }
    }
}