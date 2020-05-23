using System;

namespace Adeotek.MicroWebServer
{
    public interface IWorker : IDisposable
    {
        bool IsRunning();
        bool Start();
        bool Stop(bool dispose = false);
        bool Restart();
    }
}
