using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public class WorkerStateEventArgs : EventArgs
    {
        public readonly int WorkerId;
        public readonly bool IsStarted;
        public readonly bool Restart;
        public readonly bool Dispose;

        public WorkerStateEventArgs(int workerId, bool isStarted, bool restart = false, bool dispose = false)
        {
            WorkerId = workerId;
            IsStarted = isStarted;
            Restart = restart;
            Dispose = dispose;
        }
    }

    public class WorkerJobEventArgs : EventArgs
    {
        public readonly int WorkerId;
        public readonly bool Executed;
        public readonly bool Result;

        public WorkerJobEventArgs(int workerId, bool result = false, bool executed = true)
        {
            WorkerId = workerId;
            Executed = executed;
            Result = result;

        }
    }

    public abstract class WorkerBase : IWorker
    {
        protected Thread _thread;
        protected ILogger _logger;
        protected int _workerId;
        protected bool _stop;
        protected bool _restart;
        protected bool _dispose;
        protected bool _running;
        protected bool _starting;

        public delegate void WorkerStartedDelegate(object sender, WorkerStateEventArgs e);
        public delegate void WorkerStoppedDelegate(object sender, WorkerStateEventArgs e);
        public delegate void WorkerJobStartingDelegate(object sender, WorkerJobEventArgs e);
        public delegate void WorkerJobExecutedDelegate(object sender, WorkerJobEventArgs e);
        public event WorkerStartedDelegate OnWorkerStarted;
        public event WorkerStoppedDelegate OnWorkerStopped;
        public event WorkerJobStartingDelegate OnWorkerJobStarting;
        public event WorkerJobExecutedDelegate OnWorkerJobExecuted;

        /// <summary>
        /// Loop sleep interval in milliseconds
        /// </summary>
        public int LoopInterval { get; set; } = 1000;

        /// <summary>
        /// Task execution interval in seconds
        /// </summary>
        public double RunInterval { get; set; } = 0;

        /// <summary>
        /// Remaining interval in seconds until next task execution
        /// </summary>
        public double IntervalUntilNextRun { get; protected set; } = 0;

        /// <summary>
        /// Gets the Worker state
        /// </summary>
        public virtual bool IsRunning => (_thread?.IsAlive ?? false) && _running || _starting;

        /// <summary>
        /// Worker task state property
        /// </summary>
        public bool IsWorking { get; protected set; }

        public bool Start()
        {
            _starting = true;
            _thread ??= new Thread(WorkerLoop) { IsBackground = true };
            if (_running && _thread.IsAlive && _thread.ThreadState != (ThreadState.Background | ThreadState.Unstarted))
            {
                return true;
            }
            _thread.Start();
            return true;
        }

        public bool Stop(bool restart = false, bool dispose = false)
        {
            _restart = restart;
            _dispose = dispose;

            if (_thread == null)
            {
                OnStop();
                return true;
            }

            if (_thread != null && (IsWorking || _thread.ThreadState == (ThreadState.Background | ThreadState.Running) || _thread.ThreadState == ThreadState.Background))
            {
                EndWorkerLoop();
                return false;
            }

            _thread = null;
            OnStop();
            return true;
        }

        public bool Restart()
        {
            return Stop(true);
        }

        protected virtual bool ExecuteJob()
        {
            _logger?.LogError("{type}::ExecuteJob method not implemented!", GetType().Name);
            return true;
        }

        protected virtual void WorkerLoop()
        {
            OnStart();
            InternalWorkerLoop();
            OnStop();
        }

        protected virtual void EndWorkerLoop()
        {
            _stop = true;
        }

        protected void InternalWorkerLoop()
        {
            while (!_stop)
            {
                if (IntervalUntilNextRun >= 0)
                {
                    Thread.Sleep(LoopInterval);
                    IntervalUntilNextRun -= (double) LoopInterval / 1000;
                    continue;
                }
                IsWorking = true;
                try
                {
                    OnWorkerJobStarting?.Invoke(this, new WorkerJobEventArgs(_workerId, false, false));
                    var result = ExecuteJob();
                    OnWorkerJobExecuted?.Invoke(this, new WorkerJobEventArgs(_workerId, result));
                }
                catch (ThreadAbortException ex)
                {
                    _stop = true;
                    _logger?.LogWarning(ex, "{type} ThreadAbortException caught", GetType().Name);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "{type} exception caught", GetType().Name);
                }
                finally
                {
                    IsWorking = false;
                }
                if (!_stop)
                {
                    IntervalUntilNextRun = RunInterval > 0 ? RunInterval : -1;
                }
            }
        }

        protected void OnStart()
        {
            _logger?.LogInformation("Starting {type} loop...", GetType().Name);
            _running = true;
            _starting = false;
            OnWorkerStarted?.Invoke(this, new WorkerStateEventArgs(_workerId, true));
        }

        protected void OnStop()
        {
            _logger?.LogInformation("Stopping {type} loop...", GetType().Name);
            _running = false;
            var e = new WorkerStateEventArgs(_workerId, false, _restart, _dispose);
            OnWorkerStopped?.Invoke(this, e);
            _dispose = _stop = _restart = false;
            if (e.Restart)
            {
                Start();
            }
        }

        public void Dispose()
        {
            _stop = true;
            _thread.Abort();
            _thread = null;
        }
    }
}
