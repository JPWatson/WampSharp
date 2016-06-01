#if PCL
using WampSharp.V2.Binding;
using WampSharp.Core.Message;

namespace WampSharp.WebsocketsPcl
{
    /// <summary>
    /// Represents a client WebSocket text connection implemented using WebSocket4Net.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class MessageWebSocketTextConnection<TMessage> : MessageWebSocketConnection<TMessage>
    {
        private readonly IWampTextBinding<TMessage> mBinding;

        /// <summary>
        /// Creates a new instance of <see cref="MessageWebSocketTextConnection{TMessage}"/>
        /// given the server address to connect to and the text binding to use.
        /// </summary>
        /// <param name="serverAddress">The server address to connect to.</param>
        /// <param name="binding">The <see cref="IWampTextBinding{TMessage}"/> to use.</param>
        public MessageWebSocketTextConnection(string serverAddress, IWampTextBinding<TMessage> binding)
            : base(serverAddress, binding)
        {
            mBinding = binding;
            WebSocket.OnMessage += OnMessageReceived;
        }

        private void OnMessageReceived(string m)
        {
            WampMessage<TMessage> message = mBinding.Parse(m);

            RaiseMessageArrived(message);
        }

        protected override void Send(WampMessage<object> message)
        {
            string text = mBinding.Format(message);

            WebSocket.Send(text);
        }

        public override void Dispose()
        {
            WebSocket.OnMessage -= OnMessageReceived;
            base.Dispose();
        }
    }
}

#endif