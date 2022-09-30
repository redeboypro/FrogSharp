using System.Collections.Generic;

namespace FrogSharp.Core
{
    public delegate FsObject OperationEvent();
    public delegate FsObject ReturnEvent();

    public abstract class FsCodeElement
    {
        public abstract void Invoke();
        
        protected readonly List<FsCodeElement> Elements = new List<FsCodeElement>();

        public void AddEvent(FsCodeElement action)
        {
            Elements.Add(action);
        }
    }
}