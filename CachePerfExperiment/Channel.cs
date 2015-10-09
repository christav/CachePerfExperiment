using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    /// <summary>
    /// Basic CSP-style message channel
    /// </summary>
    public class Channel<TData> : IDisposable
    {
        private ConcurrentQueue<ChannelMessage<TData>> queue = new ConcurrentQueue<ChannelMessage<TData>>();
        private SemaphoreSlim sem = new SemaphoreSlim(0);
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public async Task<ChannelMessage<TData>> ReceiveAsync()
        {
            if (tokenSource.IsCancellationRequested)
            {
                return ChannelMessage<TData>.ClosedMessage();
            }

            var token = tokenSource.Token;
            try
            {
                await sem.WaitAsync(token);
                ChannelMessage<TData> result;
                bool dequeued = queue.TryDequeue(out result);
                if (!dequeued)
                {
                    throw new InvalidOperationException("What the heck - failed to dequeue message!");
                }
                if (result.IsClosed)
                {
                    tokenSource.Cancel();
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                return ChannelMessage<TData>.ClosedMessage();
            }
        }

        public bool Publish(TData message)
        {
            if (tokenSource.IsCancellationRequested)
            {
                return false;
            }
            queue.Enqueue(ChannelMessage<TData>.DataMessage(message));
            sem.Release();
            return true;
        }

        public void Close()
        {
            if (!tokenSource.IsCancellationRequested)
            {
                queue.Enqueue(ChannelMessage<TData>.ClosedMessage());
                sem.Release();
            }
        }

        public virtual void Dispose()
        {
            if (tokenSource != null)
            {
                tokenSource.Dispose();
                tokenSource = null;
            }
            if (sem != null)
            {
                sem.Dispose();
                sem = null;
            }
        }
    }

    public class ChannelMessage<TData>
    {
        private bool closed;
        private TData message;

        private ChannelMessage()
        {
            // Marking ctor private, can only create via factories
        }

        public bool IsClosed { get { return closed; } }

        public TData Data
        {
            get
            {
                if (closed)
                {
                    throw new OperationCanceledException("Cannot get message from a closed channel");
                }
                return message;
            }
        }

        public static ChannelMessage<TData> ClosedMessage()
        {
            var message = new ChannelMessage<TData>() { closed = true };
            return message;
        }

        public static ChannelMessage<TData> DataMessage(TData data)
        {
            return new ChannelMessage<TData>() { closed = false, message = data };
        }
    }
}

