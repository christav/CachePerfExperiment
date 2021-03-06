﻿using System;
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
        private SemaphoreSlim readSem = new SemaphoreSlim(0);
        private SemaphoreSlim writeSem;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public Channel(int maxSize)
        {
            writeSem = new SemaphoreSlim(maxSize);
        } 

        public async Task<ChannelMessage<TData>> ReceiveAsync()
        {
            if (tokenSource.IsCancellationRequested)
            {
                return ChannelMessage<TData>.ClosedMessage();
            }

            var token = tokenSource.Token;
            try
            {
                await readSem.WaitAsync(token);
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
                writeSem.Release();
                return result;
            }
            catch (OperationCanceledException)
            {
                return ChannelMessage<TData>.ClosedMessage();
            }
        }

        public async Task ReceiveAllAsync(Func<TData, Task> messageProcessor)
        {
            var channelMessage = await ReceiveAsync();
            while (!channelMessage.IsClosed)
            {
                await messageProcessor(channelMessage.Data);
                channelMessage = await ReceiveAsync();
            }
        }

        public async Task ReceiveAllAsync(Action<TData> messageProcessor)
        {
            var channelMessage = await ReceiveAsync();
            while (!channelMessage.IsClosed)
            {
                messageProcessor(channelMessage.Data);
                channelMessage = await ReceiveAsync();
            }
        }

        public bool Publish(TData message)
        {
            if (tokenSource.IsCancellationRequested)
            {
                return false;
            }
            var token = tokenSource.Token;
            try
            {
                writeSem.Wait(token);
                queue.Enqueue(ChannelMessage<TData>.DataMessage(message));
                readSem.Release();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public async Task<bool> PublishAsync(TData message)
        {
            if (tokenSource.IsCancellationRequested)
            {
                return false;
            }
            var token = tokenSource.Token;
            try
            {
                await writeSem.WaitAsync(token);
                queue.Enqueue(ChannelMessage<TData>.DataMessage(message));
                readSem.Release();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public void Close()
        {
            if (!tokenSource.IsCancellationRequested)
            {
                queue.Enqueue(ChannelMessage<TData>.ClosedMessage());
                readSem.Release();
            }
        }

        public virtual void Dispose()
        {
            if (tokenSource != null)
            {
                tokenSource.Dispose();
                tokenSource = null;
            }
            if (readSem != null)
            {
                readSem.Dispose();
                readSem = null;
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

