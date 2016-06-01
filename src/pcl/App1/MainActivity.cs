using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using WampSharp.V2.Client;
using WampSharp.V2.Fluent;
using WampSharp.V2.Realm;
using WampSharp.WebsocketsPcl;

namespace App1
{
    [Activity(Label = "App1", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Websockets.Droid.WebsocketConnection.Link();

    
            var channel = new WampChannelFactory().ConnectToRealm("com.weareadaptive.reactivetrader")
                                               .WebSocketTransport("ws://web-dev.adaptivecluster.com:8080/ws")
                                               .JsonSerialization()
                                               .Build();

    
                // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Text = "Connecting...";

            channel.RealmProxy.Monitor.ConnectionEstablished += (sender, args) =>
            {
                button.Text = "Connected";
            };


            channel.Open().Wait();

        }
    }
}

