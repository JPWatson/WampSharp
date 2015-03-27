﻿using System;
using System.Collections.Generic;
using System.Linq;
using WampSharp.Core.Listener;
using WampSharp.Core.Message;
using WampSharp.Core.Serialization;
using WampSharp.Core.Utilities;
using WampSharp.V2.Binding;
using WampSharp.V2.Core.Contracts;

namespace WampSharp.V2.PubSub
{
    internal class WampRawTopic<TMessage> : IWampRawTopic<TMessage>, IWampRawTopicRouterSubscriber, IDisposable
    {
        #region Data Members

        private readonly RawTopicSubscriberBook mSubscriberBook;
        private readonly IWampBinding<TMessage> mBinding; 
        private readonly IWampEventSerializer<TMessage> mSerializer;
        private readonly string mTopicUri;
        private readonly IWampCustomizedSubscriptionId mCustomizedSubscriptionId;

        #endregion

        #region Constructor

        public WampRawTopic(string topicUri, IWampCustomizedSubscriptionId customizedSubscriptionId, IWampEventSerializer<TMessage> serializer, IWampBinding<TMessage> binding)
        {
            mSerializer = serializer;
            mSubscriberBook = new RawTopicSubscriberBook(this);
            mTopicUri = topicUri;
            mBinding = binding;
            mCustomizedSubscriptionId = customizedSubscriptionId;
        }

        #endregion

        #region IRawWampTopic<TMessage> Members

        public void Event<TRaw>(IWampFormatter<TRaw> formatter, long publicationId, PublishOptions options)
        {
            Func<EventDetails, WampMessage<TMessage>> action =
                eventDetails => mSerializer.Event(SubscriptionId, publicationId, eventDetails);

            InnerEvent(options, action);
        }

        public void Event<TRaw>(IWampFormatter<TRaw> formatter, long publicationId, PublishOptions options,
                                TRaw[] arguments)
        {
            Func<EventDetails, WampMessage<TMessage>> action =
                details => mSerializer.Event(SubscriptionId,
                                             publicationId,
                                             details,
                                             arguments.Cast<object>().ToArray());

            InnerEvent(options, action);
        }

        public void Event<TRaw>(IWampFormatter<TRaw> formatter, long publicationId, PublishOptions options, TRaw[] arguments, IDictionary<string, TRaw> argumentsKeywords)
        {
            Func<EventDetails, WampMessage<TMessage>> action =
                details => mSerializer.Event(SubscriptionId, publicationId, details,
                                             arguments.Cast<object>().ToArray(),
                                             argumentsKeywords.ToDictionary(x => x.Key,
                                                                            x => (object) x.Value));

            InnerEvent(options, action);
        }

        private EventDetails GetDetails(PublishOptions options)
        {
            EventDetails result = new EventDetails();

            PublishOptionsExtended extendedOptions = 
                options as PublishOptionsExtended;

            bool disclosePublisher = options.DiscloseMe ?? false;

            if ((extendedOptions != null) && disclosePublisher)
            {
                result.Publisher = extendedOptions.PublisherId;
            }

            return result;
        }

        private void Publish(WampMessage<TMessage> message, PublishOptions options)
        {
            WampMessage<TMessage> raw = mBinding.GetRawMessage(message);

            IEnumerable<RemoteObserver> subscribers = 
                mSubscriberBook.GetRelevantSubscribers(options);

            foreach (RemoteObserver subscriber in subscribers)
            {
                subscriber.Message(raw);
            }
        }

        private void InnerEvent(PublishOptions options, Func<EventDetails, WampMessage<TMessage>> action)
        {
            EventDetails details = GetDetails(options);

            WampMessage<TMessage> message = action(details);

            Publish(message, options);
        }

        public bool HasSubscribers
        {
            get
            {
                return mSubscriberBook.HasSubscribers;
            }
        }

        public long SubscriptionId
        {
            get; 
            set;
        }

        public string TopicUri
        {
            get
            {
                return mTopicUri;
            }
        }

        public IDisposable SubscriptionDisposable
        {
            get; 
            set;
        }

        public IWampCustomizedSubscriptionId CustomizedSubscriptionId
        {
            get { return mCustomizedSubscriptionId; }
        }

        public void Subscribe(ISubscribeRequest<TMessage> request, SubscribeOptions options)
        {
            RemoteWampTopicSubscriber remoteSubscriber =
                new RemoteWampTopicSubscriber(this.SubscriptionId,
                                              request.Client as IWampSubscriber);

            this.RaiseSubscriptionAdding(remoteSubscriber, options);

            IWampClient<TMessage> client = request.Client;

            RemoteObserver observer = mSubscriberBook.Subscribe(client);

            request.Subscribed(this.SubscriptionId);

            observer.Open();

            this.RaiseSubscriptionAdded(remoteSubscriber, options);
        }

        public void Unsubscribe(IUnsubscribeRequest<TMessage> request)
        {
            IWampClient<TMessage> client = request.Client;

            if (mSubscriberBook.Unsubscribe(client))
            {
                this.RaiseSubscriptionRemoving(client.Session);

                request.Unsubscribed();

                this.RaiseSubscriptionRemoved(client.Session);

                if (!this.HasSubscribers)
                {
                    this.RaiseTopicEmpty();
                }
            }
        }

        public void Dispose()
        {
            SubscriptionDisposable.Dispose();
            SubscriptionDisposable = null;
        }

        #endregion

        #region ISubscriptionNotifier

        public event EventHandler<WampSubscriptionAddEventArgs> SubscriptionAdding;
        public event EventHandler<WampSubscriptionAddEventArgs> SubscriptionAdded;
        public event EventHandler<WampSubscriptionRemoveEventArgs> SubscriptionRemoving;
        public event EventHandler<WampSubscriptionRemoveEventArgs> SubscriptionRemoved;
        public event EventHandler TopicEmpty;

        protected virtual void RaiseSubscriptionAdding(RemoteWampTopicSubscriber subscriber, SubscribeOptions options)
        {
            EventHandler<WampSubscriptionAddEventArgs> handler = SubscriptionAdding;

            if (handler != null)
            {
                WampSubscriptionAddEventArgs args = GetAddEventArgs(subscriber, options);

                handler(this, args);
            }
        }

        protected virtual void RaiseSubscriptionAdded(RemoteWampTopicSubscriber subscriber, SubscribeOptions options)
        {
            EventHandler<WampSubscriptionAddEventArgs> handler = SubscriptionAdded;

            if (handler != null)
            {
                WampSubscriptionAddEventArgs args = GetAddEventArgs(subscriber, options);

                handler(this, args);
            }
        }

        protected virtual void RaiseSubscriptionRemoving(long sessionId)
        {
            EventHandler<WampSubscriptionRemoveEventArgs> handler = SubscriptionRemoving;

            if (handler != null)
            {
                WampSubscriptionRemoveEventArgs args = GetRemoveEventArgs(sessionId);
                handler(this, args);
            }
        }

        protected virtual void RaiseSubscriptionRemoved(long sessionId)
        {
            EventHandler<WampSubscriptionRemoveEventArgs> handler = SubscriptionRemoved;

            if (handler != null)
            {
                WampSubscriptionRemoveEventArgs args = GetRemoveEventArgs(sessionId);
                handler(this, args);
            }
        }

        protected virtual void RaiseTopicEmpty()
        {
            EventHandler handler = TopicEmpty;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private WampSubscriptionAddEventArgs GetAddEventArgs(RemoteWampTopicSubscriber subscriber, SubscribeOptions options)
        {
            return new WampSubscriptionAddEventArgs(subscriber, options);
        }

        private static WampSubscriptionRemoveEventArgs GetRemoveEventArgs(long sessionId)
        {
            return new WampSubscriptionRemoveEventArgs(sessionId);
        }

        #endregion

        #region Nested Types

        private class Subscription
        {
            private readonly WampRawTopic<TMessage> mParent;
            private readonly IWampClient<TMessage> mClient;
            private readonly RemoteObserver mObserver;

            public Subscription(WampRawTopic<TMessage> parent, IWampClient<TMessage> client, RemoteObserver observer)
            {
                mParent = parent;
                mClient = client;
                mObserver = observer;

                IWampConnectionMonitor monitor = mClient as IWampConnectionMonitor;
                monitor.ConnectionClosed += OnConnectionClosed;
            }

            public RemoteObserver Observer
            {
                get
                {
                    return mObserver;
                }
            }

            private void OnConnectionClosed(object sender, EventArgs e)
            {
                mParent.Unsubscribe(new DisconnectUnsubscribeRequest(mClient));
                IWampConnectionMonitor monitor = sender as IWampConnectionMonitor;
                monitor.ConnectionClosed -= OnConnectionClosed;
            }

            private class DisconnectUnsubscribeRequest : IUnsubscribeRequest<TMessage>
            {
                private readonly IWampClient<TMessage> mClient;

                public DisconnectUnsubscribeRequest(IWampClient<TMessage> client)
                {
                    mClient = client;
                }

                public IWampClient<TMessage> Client
                {
                    get
                    {
                        return mClient;
                    }
                }

                public void Unsubscribed()
                {
                }
            }
        }

        private class RemoteObserver : IWampRawClient<TMessage>
        {
            private bool mOpen = false;

            private readonly IWampRawClient<TMessage> mClient;
            private readonly long mSessionId;

            public RemoteObserver(IWampRawClient<TMessage> client)
            {
                mClient = client;
                IWampClient casted = mClient as IWampClient;
                mSessionId = casted.Session;
            }

            public long SessionId
            {
                get
                {
                    return mSessionId;
                }
            }

            public void Open()
            {
                mOpen = true;
            }

            public void Message(WampMessage<TMessage> message)
            {
                if (mOpen)
                {
                    mClient.Message(message);
                }
            }
        }

        private class RawTopicSubscriberBook
        {
            private readonly IDictionary<long, Subscription> mSubscriberIdToSubscriber =
                new SwapDictionary<long, Subscription>();

            private SwapHashSet<long> mSubscriberIds = new SwapHashSet<long>();

            private readonly WampRawTopic<TMessage> mRawTopic;
            
            private readonly object mLock = new object();

            public RawTopicSubscriberBook(WampRawTopic<TMessage> rawTopic)
            {
                mRawTopic = rawTopic;
            }

            public bool HasSubscribers
            {
                get
                {
                    return mSubscriberIds.Count > 0;
                }
            }

            public RemoteObserver Subscribe(IWampClient<TMessage> client)
            {
                Subscription subscription;

                if (mSubscriberIdToSubscriber.TryGetValue(client.Session, out subscription))
                {
                    return subscription.Observer;
                }
                else
                {
                    lock (mLock)
                    {
                        RemoteObserver result = new RemoteObserver(client);

                        mSubscriberIds.Add(client.Session);

                        mSubscriberIdToSubscriber[client.Session] =
                            new Subscription(mRawTopic, client, result);

                        return result;                        
                    }
                }
            }

            public bool Unsubscribe(IWampClient<TMessage> client)
            {
                lock (mLock)
                {
                    bool result;
                    mSubscriberIds.Remove(client.Session);
                    result = mSubscriberIdToSubscriber.Remove(client.Session);
                    return result;
                }
            }

            public IEnumerable<RemoteObserver> GetRelevantSubscribers(PublishOptions options)
            {
                IEnumerable<long> relevantSubscriberIds = 
                    GetRelevantSubscriberIds(options);

                IEnumerable<RemoteObserver> relevantSubscribers =
                    relevantSubscriberIds.Select(GetSubscriberById)
                        .Where(x => x != null);

                return relevantSubscribers;
            }

            private RemoteObserver GetSubscriberById(long subscriberId)
            {
                Subscription subscription;

                if (mSubscriberIdToSubscriber.TryGetValue(subscriberId, out subscription))
                {
                    return subscription.Observer;
                }

                return null;
            }

            private IEnumerable<long> GetRelevantSubscriberIds(PublishOptions options)
            {
                var result = new HashSet<long>(mSubscriberIds);

                if (options.Eligible != null)
                {
                    result = new HashSet<long>(options.Eligible);
                }

                bool excludeMe = options.ExcludeMe ?? true;
                
                PublishOptionsExtended casted = options as PublishOptionsExtended;

                if (excludeMe && casted != null)
                {
                    result.Remove(casted.PublisherId);
                }

                if (options.Exclude != null)
                {
                    result.ExceptWith(options.Exclude);
                }

                return result;
            }
        }

        #endregion
    }
}