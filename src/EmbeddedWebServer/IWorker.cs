using System;

namespace Adeotek.EmbeddedWebServer
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
