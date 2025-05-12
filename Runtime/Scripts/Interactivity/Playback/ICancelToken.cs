using System.Threading;

namespace UnityGLTF.Interactivity.Playback
{
    public interface ICancelToken
    {
        public bool isCancelled { get; }
    }

    public struct NodeEngineCancelToken : ICancelToken
    {
        public CancellationToken engineToken;
        public CancellationToken nodeToken;

        public NodeEngineCancelToken(CancellationToken engineToken, CancellationToken nodeToken)
        {
            this.engineToken = engineToken;
            this.nodeToken = nodeToken;
        }

        public bool isCancelled => engineToken.IsCancellationRequested || nodeToken.IsCancellationRequested;
    }

    public struct EngineCancelToken : ICancelToken
    {
        public CancellationToken engineToken;

        public EngineCancelToken(CancellationToken engineToken)
        {
            this.engineToken = engineToken;
        }

        public bool isCancelled => engineToken.IsCancellationRequested;

        public static implicit operator CancellationToken(EngineCancelToken d) => d.engineToken;
        public static implicit operator EngineCancelToken(CancellationToken d) => new EngineCancelToken(d);
    }
}
