﻿namespace WampSharp.V2.PubSub
{
    internal interface IRawWampTopic<TMessage> : ISubscriptionNotifier
    {
        bool HasSubscribers { get; }
        long SubscriptionId { get; }
        string TopicUri { get; }
        void Subscribe(ISubscribeRequest<TMessage> request, TMessage options);
        void Unsubscribe(IUnsubscribeRequest<TMessage> request);
    }
}