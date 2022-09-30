namespace FrogSharp.Core
{
    public class FsOperation : FsCodeElement
    {
        private readonly OperationEvent action;
        private readonly FsCallable callable;

        public FsOperation(FsCallable callable, OperationEvent action)
        {
            this.callable = callable;
            this.action = action;
        }

        public FsObject Return => action.Invoke();

        public override void Invoke()
        {
            action.Invoke();
        }
    }
}