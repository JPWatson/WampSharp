#if PCL
using System;
using System.Diagnostics;
using WampSharp.Core.Listener;
using WampSharp.Core.Message;
using WampSharp.V2.Binding;
using Websockets;

namespace WampSharp.WebsocketsPcl
{
    public abstract class MessageWebSocketConnection<TMessage> : IControlledWampConnection<TMessage>
    {
        #region Fields

        private readonly string _serverAddress;

        #endregion

        protected MessageWebSocketConnection(string serverAddress,
            IWampBinding<TMessage> binding)
        {
            _serverAddress = serverAddress;
            Binding = binding;
            WebSocket = WebSocketFactory.Create();

            WebSocket.OnLog += WebSocketOnOnLog;
            WebSocket.OnOpened += RaiseConnectionOpen;
            WebSocket.OnClosed += RaiseConnectionClosed;
            WebSocket.OnError += WebSocketOnError;
        }

        private void WebSocketOnOnLog(string s)
        {
            Debug.WriteLine(s);
        }


        public IWampBinding<TMessage> Binding { get; }
        protected IWebSocketConnection WebSocket { get; }
        
        private void WebSocketOnError(string s)
        {
            RaiseConnectionError(new Exception(s));
        }

        public void Connect()
        {
            WebSocket.Open(_serverAddress, Binding.Name);
        }

        public virtual void Dispose()
        {
            WebSocket.Close();
        }

        void IWampConnection<TMessage>.Send(WampMessage<object> message)
        {
            Send(message);
        }

        protected abstract void Send(WampMessage<object> message);

        public event EventHandler ConnectionOpen;

        public event EventHandler<WampMessageArrivedEventArgs<TMessage>> MessageArrived;

        public event EventHandler ConnectionClosed;

        public event EventHandler<WampConnectionErrorEventArgs> ConnectionError;

        protected virtual void RaiseConnectionError(Exception ex)
        {
            EventHandler<WampConnectionErrorEventArgs> handler = ConnectionError;

            handler?.Invoke(this, new WampConnectionErrorEventArgs(ex));
        }

        protected virtual void RaiseMessageArrived(WampMessage<TMessage> message)
        {
            EventHandler<WampMessageArrivedEventArgs<TMessage>> handler = MessageArrived;

            if (handler != null)
            {
                WampMessageArrivedEventArgs<TMessage> eventArgs = new WampMessageArrivedEventArgs<TMessage>(message);
                handler(this, eventArgs);
            }
        }

        protected virtual void RaiseConnectionOpen()
        {
            EventHandler handler = ConnectionOpen;

            handler?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseConnectionClosed()
        {
            EventHandler handler = ConnectionClosed;

            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}

#endif