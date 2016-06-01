using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WampSharp.V2.Client;
using WampSharp.V2.Fluent;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Websockets.Net.WebsocketConnection.Link();

            var channel = new WampChannelFactory().ConnectToRealm("com.weareadaptive.reactivetrader")
                .WebSocketTransport("ws://web-dev.adaptivecluster.com:8080/ws")
                .JsonSerialization()
                .Build();

            try
            {
                channel.Open().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Connected");

            Console.ReadLine();
        }
    }
}