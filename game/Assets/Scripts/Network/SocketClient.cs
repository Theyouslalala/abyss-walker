using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AbyssWalker.Map;

namespace AbyssWalker.Network
{
    /// <summary>
    /// TCP socket client that connects to the Python AI server. Uses length-prefixed
    /// JSON messages for communication. Provides async send/receive with automatic
    /// reconnection logic.
    /// </summary>
    public class SocketClient : MonoBehaviour
    {
        #region Constants

        /// <summary>Default server hostname.</summary>
        private const string DefaultHost = "127.0.0.1";

        /// <summary>Default server port.</summary>
        private const int DefaultPort = 9999;

        /// <summary>Maximum message size (16 MB) to prevent memory exhaustion.</summary>
        private const int MaxMessageSize = 16 * 1024 * 1024;

        /// <summary>Receive buffer size.</summary>
        private const int ReceiveBufferSize = 8192;

        #endregion

        #region Inspector Fields

        [Header("Connection Settings")]
        [SerializeField] private string _host = DefaultHost;
        [SerializeField] private int _port = DefaultPort;
        [SerializeField] private bool _autoReconnect = true;
        [SerializeField] private float _reconnectDelay = 2f;
        [SerializeField] private int _maxReconnectAttempts = 5;

        #endregion

        #region Events

        /// <summary>Fired when a raw JSON message string is received.</summary>
        public event Action<string> OnMessageReceived;

        /// <summary>Fired when a MapDataMessage is parsed from incoming data.</summary>
        public event Action<MapDataMessage> OnMapDataReceived;

        /// <summary>Fired when an AIDecisionMessage is parsed from incoming data.</summary>
        public event Action<AIDecisionMessage> OnAIDecisionReceived;

        /// <summary>Fired when the connection to the server is established.</summary>
        public event Action OnConnected;

        /// <summary>Fired when the connection to the server is lost.</summary>
        public event Action OnDisconnected;

        /// <summary>Fired when a connection attempt fails.</summary>
        public event Action<string> OnConnectionError;

        #endregion

        #region Properties

        /// <summary>Whether the client is currently connected to the server.</summary>
        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        /// <summary>The server host address.</summary>
        public string Host
        {
            get => _host;
            set => _host = value;
        }

        /// <summary>The server port.</summary>
        public int Port
        {
            get => _port;
            set => _port = value;
        }

        #endregion

        #region Private Fields

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Queue<string> _sendQueue = new Queue<string>();
        private readonly object _sendLock = new object();
        private int _reconnectAttempts;
        private bool _intentionalDisconnect;

        // Thread-safe message queue for dispatching to main thread
        private readonly Queue<string> _receivedMessages = new Queue<string>();
        private readonly object _receiveLock = new object();

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Dispatch received messages on the main thread
            ProcessReceivedMessages();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiates an asynchronous connection to the AI server.
        /// </summary>
        public async void Connect()
        {
            if (IsConnected)
            {
                Debug.LogWarning("[SocketClient] Already connected.");
                return;
            }

            _intentionalDisconnect = false;
            _reconnectAttempts = 0;
            await ConnectAsync();
        }

        /// <summary>
        /// Disconnects from the server and cleans up resources.
        /// </summary>
        public void Disconnect()
        {
            _intentionalDisconnect = true;
            CleanupConnection();
            Debug.Log("[SocketClient] Disconnected.");
        }

        /// <summary>
        /// Enqueues a JSON message to be sent to the server.
        /// Messages are sent sequentially in the background.
        /// </summary>
        /// <param name="jsonMessage">The JSON string to send.</param>
        public void Send(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage))
            {
                Debug.LogWarning("[SocketClient] Cannot send empty message.");
                return;
            }

            lock (_sendLock)
            {
                _sendQueue.Enqueue(jsonMessage);
            }

            // Start sending if not already in progress
            _ = ProcessSendQueueAsync();
        }

        #endregion

        #region Private Methods - Connection

        /// <summary>
        /// Establishes a TCP connection to the server asynchronously.
        /// </summary>
        private async Task ConnectAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = true;
                _tcpClient.ReceiveBufferSize = ReceiveBufferSize;

                Debug.Log($"[SocketClient] Connecting to {_host}:{_port}...");
                await _tcpClient.ConnectAsync(_host, _port);

                _networkStream = _tcpClient.GetStream();
                _reconnectAttempts = 0;

                _cancellationTokenSource = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_cancellationTokenSource.Token);

                Debug.Log("[SocketClient] Connected successfully.");
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketClient] Connection failed: {ex.Message}");
                OnConnectionError?.Invoke(ex.Message);
                CleanupConnection();

                if (_autoReconnect && !_intentionalDisconnect)
                {
                    await TryReconnectAsync();
                }
            }
        }

        /// <summary>
        /// Attempts to reconnect with exponential backoff.
        /// </summary>
        private async Task TryReconnectAsync()
        {
            while (_reconnectAttempts < _maxReconnectAttempts && !_intentionalDisconnect)
            {
                _reconnectAttempts++;
                float delay = _reconnectDelay * Mathf.Pow(1.5f, _reconnectAttempts - 1);
                Debug.Log($"[SocketClient] Reconnect attempt {_reconnectAttempts}/{_maxReconnectAttempts} in {delay:F1}s...");
                await Task.Delay((int)(delay * 1000));

                if (_intentionalDisconnect) break;

                await ConnectAsync();
                if (IsConnected) return;
            }

            if (!IsConnected)
            {
                Debug.LogError("[SocketClient] Max reconnect attempts reached.");
            }
        }

        /// <summary>
        /// Cleans up the TCP client, stream, and cancellation token.
        /// </summary>
        private void CleanupConnection()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _networkStream?.Close();
            _networkStream = null;

            _tcpClient?.Close();
            _tcpClient = null;

            OnDisconnected?.Invoke();
        }

        #endregion

        #region Private Methods - Receive

        /// <summary>
        /// Continuously reads length-prefixed messages from the network stream.
        /// Runs on a background thread via Task.Run.
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && IsConnected)
                {
                    string message = await ReadLengthPrefixedMessageAsync(ct);
                    if (message == null) break; // Connection closed

                    lock (_receiveLock)
                    {
                        _receivedMessages.Enqueue(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    Debug.LogError($"[SocketClient] Receive error: {ex.Message}");
                }
            }

            if (!_intentionalDisconnect)
            {
                // Unexpected disconnection
                CleanupConnection();
                if (_autoReconnect)
                {
                    _ = TryReconnectAsync();
                }
            }
        }

        /// <summary>
        /// Reads a single length-prefixed message from the stream.
        /// Format: [4-byte big-endian length][JSON payload]
        /// </summary>
        private async Task<string> ReadLengthPrefixedMessageAsync(CancellationToken ct)
        {
            // Read 4-byte length header
            byte[] lengthBuffer = new byte[4];
            int bytesRead = await ReadExactAsync(lengthBuffer, 0, 4, ct);
            if (bytesRead < 4) return null; // Connection closed

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            if (messageLength <= 0 || messageLength > MaxMessageSize)
            {
                Debug.LogError($"[SocketClient] Invalid message length: {messageLength}");
                return null;
            }

            // Read the JSON payload
            byte[] messageBuffer = new byte[messageLength];
            bytesRead = await ReadExactAsync(messageBuffer, 0, messageLength, ct);
            if (bytesRead < messageLength) return null;

            return Encoding.UTF8.GetString(messageBuffer);
        }

        /// <summary>
        /// Reads exactly 'count' bytes from the stream into the buffer.
        /// Returns the total bytes read (may be less than count if the stream closes).
        /// </summary>
        private async Task<int> ReadExactAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count && !ct.IsCancellationRequested)
            {
                int read = await _networkStream.ReadAsync(
                    buffer, offset + totalRead, count - totalRead, ct);

                if (read == 0) return totalRead; // Stream closed
                totalRead += read;
            }
            return totalRead;
        }

        /// <summary>
        /// Processes queued messages on the main Unity thread.
        /// Parses and dispatches typed messages.
        /// </summary>
        private void ProcessReceivedMessages()
        {
            lock (_receiveLock)
            {
                while (_receivedMessages.Count > 0)
                {
                    string message = _receivedMessages.Dequeue();
                    DispatchMessage(message);
                }
            }
        }

        /// <summary>
        /// Parses a raw JSON message and dispatches to the appropriate typed event.
        /// </summary>
        private void DispatchMessage(string json)
        {
            OnMessageReceived?.Invoke(json);

            // Try to determine message type
            MessageBase baseMsg = null;
            try
            {
                baseMsg = MessageProtocol.Deserialize<MessageBase>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocketClient] Failed to parse message type: {ex.Message}");
                return;
            }

            if (baseMsg == null || string.IsNullOrEmpty(baseMsg.Type))
            {
                Debug.LogWarning("[SocketClient] Received message with no type field.");
                return;
            }

            switch (baseMsg.Type)
            {
                case MessageProtocol.TypeMapData:
                    MapDataMessage mapMsg = MessageProtocol.Deserialize<MapDataMessage>(json);
                    if (mapMsg != null)
                        OnMapDataReceived?.Invoke(mapMsg);
                    break;

                case MessageProtocol.TypeAIDecision:
                    AIDecisionMessage aiMsg = MessageProtocol.Deserialize<AIDecisionMessage>(json);
                    if (aiMsg != null)
                        OnAIDecisionReceived?.Invoke(aiMsg);
                    break;

                case MessageProtocol.TypeGameState:
                    Debug.Log("[SocketClient] Received game state acknowledgment from server.");
                    break;

                default:
                    Debug.LogWarning($"[SocketClient] Unknown message type: {baseMsg.Type}");
                    break;
            }
        }

        #endregion

        #region Private Methods - Send

        /// <summary>
        /// Processes the send queue, writing length-prefixed messages to the stream.
        /// </summary>
        private async Task ProcessSendQueueAsync()
        {
            if (!IsConnected || _networkStream == null) return;

            while (true)
            {
                string message;
                lock (_sendLock)
                {
                    if (_sendQueue.Count == 0) break;
                    message = _sendQueue.Dequeue();
                }

                await WriteLengthPrefixedMessageAsync(message);
            }
        }

        /// <summary>
        /// Writes a single length-prefixed message to the stream.
        /// </summary>
        private async Task WriteLengthPrefixedMessageAsync(string message)
        {
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(message);
                byte[] lengthHeader = BitConverter.GetBytes(payload.Length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthHeader);

                await _networkStream.WriteAsync(lengthHeader, 0, 4);
                await _networkStream.WriteAsync(payload, 0, payload.Length);
                await _networkStream.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketClient] Send error: {ex.Message}");
                CleanupConnection();

                if (_autoReconnect && !_intentionalDisconnect)
                {
                    _ = TryReconnectAsync();
                }
            }
        }

        #endregion
    }
}
