using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Adeotek.MicroWebServer.WebSockets
{
    public class ConnectionsManager
    {
        private readonly ConcurrentDictionary<string, TcpClient> _connections = new ConcurrentDictionary<string, TcpClient>();

        public TcpClient GetClientById(string clientId)
        {
            return _connections.FirstOrDefault(x => x.Key == clientId).Value;
        }

        public ConcurrentDictionary<string, TcpClient> GetAllClients()
        {
            return _connections;
        }

        public string GetConnectionId(TcpClient client)
        {
            return _connections.FirstOrDefault(x => x.Value == client).Key;
        }

        public string AddClient(TcpClient client)
        {
            var id = GetNewConnectionId();
            return _connections.TryAdd(id, client) ? id : null;
        }

        public async Task RemoveClient(string clientId)
        {
            _connections.TryRemove(clientId, out var client);
            await Task.Run(() => client.Close());
        }

        private static string GetNewConnectionId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
