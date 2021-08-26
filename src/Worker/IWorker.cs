using System;

namespace Adeotek.Worker
{
    public interface IWorker : IDisposable
    {
        bool IsRunning { get; }
        bool IsWorking { get; }
        bool Start();
        bool Stop(bool restart = false);
        bool Restart();
    }
}
