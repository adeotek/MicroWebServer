using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public abstract class WorkerBase : IWorker
    {
        protected bool _restart;
        protected bool _stop;
        protected bool _dispose;
        protected int _workerId;
        protected Thread _thread;
        protected ILogger _logger;

        public delegate void NewMessage(object sender, string message);
        public event NewMessage OnNewMessage;

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
        /// Worker task state property
        /// </summary>
        public bool Working { get; protected set; }

        /// <summary>
        /// Gets the Worker state
        /// </summary>
        public bool IsRunning()
        {
            return _thread?.IsAlive ?? Working;
        }

        public bool Start()
        {
            _thread ??= new Thread(WorkerLoop) { IsBackground = true };
            if (Working || (_thread.IsAlive && _thread.ThreadState != (ThreadState.Background | ThreadState.Unstarted)))
            {
                return true;
            }
            _thread.Start();
            return true;
        }

        public bool Stop(bool dispose = false)
        {
            _restart = false;
            _dispose = dispose;
            EndWorkerLoop();
            if (_thread == null)
            {
                //OnStop(dispose);
                return true;
            }

            if (_thread != null && (_thread.ThreadState == (ThreadState.Background | ThreadState.Running)
                                   || _thread.ThreadState == ThreadState.Background
                                   || (_thread.ThreadState == ThreadState.Stopped && Working)))
            {
                //OnStop(dispose);
                return false;
            }

            if (_thread != null && (_thread.ThreadState == ThreadState.Stopped || _thread.ThreadState == (ThreadState.Background | ThreadState.Unstarted)) && !Working)
            {
                _thread = null;

            }
            return true;
        }

        public bool Restart()
        {
            return true;
        }

        protected virtual void WorkerLoop()
        {
            InternalWorkerLoop();
        }

        protected virtual void EndWorkerLoop()
        {
            _stop = true;
        }

        protected virtual void ExecuteJob()
        {
            _logger.LogError("{type}::ExecuteJob method not implemented!", GetType().Name);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void InternalWorkerLoop()
        {
            _logger.LogInformation("Starting {type} loop...", GetType().Name);
            while (!_stop)
            {
                if (IntervalUntilNextRun >= 0)
                {
                    Thread.Sleep(LoopInterval);
                    IntervalUntilNextRun -= (double) LoopInterval / 1000;
                    continue;
                }
                Working = true;
                try
                {
                    ExecuteJob();
                    OnNewMessage?.Invoke(this, $"Job executed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                }
                catch (ThreadAbortException ex)
                {
                    _stop = true;
                    _logger.LogWarning(ex, "{type} ThreadAbortException caught", GetType().Name);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{type} exception caught", GetType().Name);
                }
                finally
                {
                    Working = false;
                }
                if (!_stop)
                {
                    IntervalUntilNextRun = RunInterval > 0 ? RunInterval : -1;
                }
            }
            _logger.LogInformation("Stopping {type} loop...", GetType().Name);
            //eventAggregator.PublishOnBackgroundThreadAsync(new WorkerEventArgs()
            //{ Id = _workerId, IsRestart = restart, Exit = Exit });
            _stop = _restart = false;
        }
    }
}
