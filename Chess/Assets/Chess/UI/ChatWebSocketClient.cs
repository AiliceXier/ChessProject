using System;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;

namespace Chess.UI
{
    public class ChatWebSocketClient : IDisposable
    {
        private WebSocket _ws;
        private string _serverUrl;
        private string _currentRoom;
        private string _playerName;
        private bool _disposed;

        public bool IsConnected => _ws != null && _ws.ReadyState == WebSocketState.Open;

        public event Action<string, string> OnMessageReceived;
        public event Action OnConnected;
        public event Action<string> OnError;

        public ChatWebSocketClient(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public void Connect(string room, string playerName)
        {
            if (_disposed) return;

            Disconnect();

            _currentRoom = room;
            _playerName = playerName;

            _ws = new WebSocket(_serverUrl);
            _ws.OnOpen += (sender, e) =>
            {
                Debug.Log("[ChatWS] Connected");
                var joinMsg = new { type = "join", room = _currentRoom, player = _playerName };
                _ws.Send(JsonConvert.SerializeObject(joinMsg));
                OnConnected?.Invoke();
            };

            _ws.OnMessage += (sender, e) =>
            {
                try
                {
                    var msg = JsonConvert.DeserializeObject<ChatMessage>(e.Data);
                    if (msg != null && msg.type == "chat")
                    {
                        OnMessageReceived?.Invoke(msg.sender, msg.message);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ChatWS] Parse error: {ex.Message}");
                }
            };

            _ws.OnError += (sender, e) =>
            {
                Debug.LogWarning($"[ChatWS] Error: {e.Message}");
                OnError?.Invoke(e.Message);
            };

            _ws.OnClose += (sender, e) =>
            {
                Debug.Log($"[ChatWS] Closed: {e.Reason}");
            };

            _ws.ConnectAsync();
        }

        public void SendChatMessage(string message)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(message)) return;

            var chatMsg = new { type = "chat", message };
            _ws.Send(JsonConvert.SerializeObject(chatMsg));
        }

        public void Disconnect()
        {
            if (_ws != null)
            {
                if (_ws.ReadyState == WebSocketState.Open)
                    _ws.Close();
                _ws = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
        }

        private class ChatMessage
        {
            public string type;
            public string sender;
            public string message;
            public long timestamp;
        }
    }
}
