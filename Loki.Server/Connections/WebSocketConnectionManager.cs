﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Data;
using Loki.Interfaces.Security;

namespace Loki.Server.Connections
{
    public class WebSocketConnectionManager : IWebSocketConnectionManager
    {
        #region Readonly Variables

        /// <summary>
        /// The client map
        /// </summary>
        private readonly ConcurrentDictionary<IWebSocketConnection, object> _clientMap = new ConcurrentDictionary<IWebSocketConnection, object>();

        /// <summary>
        /// The route table
        /// </summary>
        private readonly IRouteTable _routeTable;

        /// <summary>
        /// The security container
        /// </summary>
        private readonly ISecurityContainer _securityContainer;
        
        #endregion

        #region Properties

        /// <summary>
        /// Gets the total connections.
        /// </summary>
        /// <value>
        /// The total connections.
        /// </value>
        public int TotalConnections => _clientMap.Count;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnectionManager" /> class.
        /// </summary>
        /// <param name="routeTable">The route table.</param>
        /// <param name="securityContainer">The security container.</param>
        public WebSocketConnectionManager(IRouteTable routeTable, ISecurityContainer securityContainer)
        {
            _routeTable = routeTable;
            _securityContainer = securityContainer;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers the specified connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void RegisterConnection(TcpClient connection)
        {
            if (connection == null)
                return;

            IWebSocketConnection socket = new WebSocketConnection(connection, _securityContainer, _routeTable);
            socket.Listen();

            _clientMap[socket] = null;
        }

        /// <summary>
        /// Unregisters the specified connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void UnregisterConnection(IWebSocketConnection connection)
        {
            const int MAX_CYCLES = 5;

            int i = 0;

            while (!_clientMap.TryRemove(connection, out _))
            {
                ++i;
                if (i >= MAX_CYCLES)
                    return;
            }
        }

        /// <summary>
        /// Gets the specified set of IWebSocketConnections with the same clientIdentifier.
        /// </summary>
        /// <param name="clientIdentifier">The clientIdentifier.</param>
        /// <returns></returns>
        public IWebSocketConnection[] GetConnectionsByClientIdentifier(string clientIdentifier)
        {
            return _clientMap.Where(x => x.Key.ClientIdentifier == clientIdentifier).Select(x => x.Key).ToArray();
        }

        /// <summary>
        /// Removes the dead connections.
        /// </summary>
        public void RemoveDeadConnections()
        {
            foreach (var connection in _clientMap)
                if (!connection.Key.IsAlive)
                    UnregisterConnection(connection.Key);
        }

        
        #endregion
    }
}