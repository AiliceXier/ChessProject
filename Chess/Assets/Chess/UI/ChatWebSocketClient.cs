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
                try
                {
                    var joinMsg = new { type = "join", room = _currentRoom, player = _playerName };
                    var json = JsonConvert.SerializeObject(joinMsg);
                    Debug.Log($"[ChatWS] Sending join: {json}");
                    _ws.Send(json);
                    OnConnected?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ChatWS] OnOpen error: {ex.Message}");
                }
            };

            _ws.OnMessage += (sender, e) =>
            {
                Debug.Log($"[ChatWS] Raw message received: {e.Data}");
                try
                {
                    var msg = JsonConvert.DeserializeObject<ChatMessage>(e.Data);
                    if (msg != null)
                    {
                        Debug.Log($"[ChatWS] Parsed: type={msg.type}, sender={msg.sender}, message={msg.message}");
                        if (msg.type == "chat")
                        {
                            OnMessageReceived?.Invoke(msg.sender ?? "System", msg.message ?? "");
                        }
                        else if (msg.type == "joined")
                        {
                            var joinMsg = $"{msg.player ?? "Someone"} joined the room";
                            OnMessageReceived?.Invoke("System", joinMsg);
                        }
                        else if (msg.type == "error")
                        {
                            OnMessageReceived?.Invoke("System", $"Error: {msg.message}");
                        }
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
            public string player;
            public string room;
            public long timestamp;
        }
    }
}
