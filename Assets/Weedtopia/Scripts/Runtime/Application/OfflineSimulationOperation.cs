using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Weedtopia.Application
{
    /// <summary>
    /// Owns an unpublished offline model until its pure-domain worker has stopped.
    /// Progress reads never touch the model. Only the creating thread may commit.
    /// </summary>
    public sealed class OfflineSimulationOperation : IDisposable
    {
        private readonly object gate = new object();
        private readonly int ownerThreadId = Thread.CurrentThread.ManagedThreadId;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly Task worker;
        private readonly Func<OfflineSummary> commit;
        private readonly Action rollback;
        private int completedSeconds, workerThreadId;
        private bool disposed, finalized, finalizing;
        private OfflineSummary result;
        private ExceptionDispatchInfo failure;

        public int TotalSeconds { get; }
        public int CompletedSeconds => Volatile.Read(ref completedSeconds);
        public int WorkerThreadId => Volatile.Read(ref workerThreadId);
        public double Fraction => TotalSeconds == 0 ? (worker.IsCompleted ? 1 : 0) : (double)CompletedSeconds / TotalSeconds;
        public bool SimulationFinished => worker.IsCompleted;

        internal OfflineSimulationOperation(int seconds, Action<CancellationToken, Action<int>> simulate,
            Func<OfflineSummary> commit, Action rollback)
        {
            TotalSeconds = seconds;
            this.commit = commit;
            this.rollback = rollback;
            worker = Task.Run(() =>
            {
                Volatile.Write(ref workerThreadId, Thread.CurrentThread.ManagedThreadId);
                cancellation.Token.ThrowIfCancellationRequested();
                simulate(cancellation.Token, completed => Volatile.Write(ref completedSeconds, completed));
            });
        }

        /// <summary>Never waits. A finished simulation is committed exactly once on the owning thread.</summary>
        public bool TryComplete(out OfflineSummary summary)
        {
            EnsureOwnerThread();
            summary = null;
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(OfflineSimulationOperation));
                if (finalizing) throw new InvalidOperationException("Offline completion is already in progress.");
                if (finalized)
                {
                    failure?.Throw();
                    summary = result;
                    return true;
                }
                if (!worker.IsCompleted) return false;
                finalizing = true;
                try
                {
                    worker.GetAwaiter().GetResult();
                    cancellation.Token.ThrowIfCancellationRequested();
                    result = commit();
                    summary = result;
                    return true;
                }
                catch (Exception error)
                {
                    rollback();
                    failure = ExceptionDispatchInfo.Capture(error);
                    throw;
                }
                finally { finalized = true; finalizing = false; cancellation.Dispose(); }
            }
        }

        /// <summary>Requests cancellation; keep polling TryComplete until the worker acknowledges it.</summary>
        public void Cancel()
        {
            EnsureOwnerThread();
            lock (gate)
                if (!finalized && !disposed && !finalizing) cancellation.Cancel();
        }

        /// <summary>
        /// Abandons a private, unpublished session without waiting on the owner thread.
        /// After the worker stops, only its pure model snapshot is restored; no save or UI callback runs.
        /// </summary>
        public void Dispose()
        {
            EnsureOwnerThread();
            lock (gate)
            {
                // A synchronous store callback may reenter disposal on this same thread.
                // Once committing, finish that atomic save; never restore a committed disk write.
                if (disposed || finalized || finalizing) return;
                disposed = true;
                cancellation.Cancel();
                worker.ContinueWith(finished =>
                {
                    lock (gate)
                    {
                        // Observe any worker failure even when scene destruction abandoned the result.
                        if (finished.IsFaulted) failure = ExceptionDispatchInfo.Capture(finished.Exception.GetBaseException());
                        try { rollback(); }
                        finally { finalized = true; cancellation.Dispose(); }
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void EnsureOwnerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Offline completion and disposal must run on their creating thread.");
        }
    }
}
