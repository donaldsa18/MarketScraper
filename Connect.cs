using Devcat.Core;
using Devcat.Core.Net;
using Devcat.Core.Net.Message;
using Devcat.Core.Threading;
using MarketScraper;
using PacketCap;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Utility;

namespace MarketQuery
{
    abstract class Connect
    {
        public TcpClient client = new TcpClient();
        
        public EncryptionType encrypt = EncryptionType.Normal;

        public bool firstMsg = true;

        public MessageHandlerFactory mf = new MessageHandlerFactory();

        public Random rand = new Random();

        public JobProcessor jp = new JobProcessor();

        private static Dictionary<int, String> classNames = new Dictionary<int, String>();
        public static Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();

        private static HashSet<string> unhandledTypes = new HashSet<string>();

        //public MessagePrinter mh;

        public Connect()
        {
            Console.WriteLine("Registering functions");
            client.PacketReceive += new EventHandler<EventArgs<ArraySegment<byte>>>(OnPacketReceive);
            client.ConnectionFail += new EventHandler<EventArgs<Exception>>(OnConnectionFail);
            client.ConnectionSucceed += new EventHandler<EventArgs>(OnConnectionSucceed);
            client.Disconnected += new EventHandler<EventArgs>(OnDisconnected);
            client.ExceptionOccur += new EventHandler<EventArgs<Exception>>(OnExceptionOccur);
            jp.Start();
        }

        private void OnExceptionOccur(object sender, EventArgs<Exception> e)
        {
            Console.WriteLine("Exception occured");
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnected");
        }

        private void OnConnectionSucceed(object sender, EventArgs e)
        {
            Console.WriteLine("Connection succeeded");
        }

        private void OnConnectionFail(object sender, EventArgs<Exception> e)
        {
            Console.WriteLine("Connection failed");
        }


        public enum EncryptionType
        {
            None,
            Normal,
            Relay,
            Pipe
        }

        public void SendMsg<T>(T msg)
        {
            Console.WriteLine("Sending a {0}", typeof(T));
            Packet p = SerializeWriter.ToBinary<T>(msg);
            client.Transmit(p);
        }

        private void OnPacketReceive(object sender, EventArgs<ArraySegment<byte>> e)
        {
            //Console.WriteLine("Received packet");
            jp.Enqueue(Job.Create(() =>
            {
                Packet p = new Packet(e.Value);

                if (firstMsg)
                {
                    firstMsg = false;
                    Console.WriteLine("Received TypeConverter");
                    ProcessTypeConverter(p);
                }
                else
                {
                    Console.WriteLine("Received {0}", classNames[p.CategoryId]);
                    mf.Handle(p, this);
                }
            }));
        }

        public abstract void ProcessTypeConverter(Packet p);

        public void SetupDicts(Packet p)
        {
            SerializeReader.FromBinary<Object>(p, out Object obj);
            String contents = obj.ToString();

            if (contents.StartsWith("TypeConverter"))
            {
                MatchCollection mc = Regex.Matches(contents, @"0x([A-F0-9]+)[,\s\{]*FullName = ([A-Za-z\._]+), GUID = ([a-z0-9-]+)");
                Dictionary<String, bool> loaded = new Dictionary<String, bool>();
                foreach (Match m in mc)
                {
                    int categoryId = int.Parse(m.Groups[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                    String className = m.Groups[2].ToString();
                    Guid guid = Guid.Parse(m.Groups[3].ToString());
                    if (classNames.TryGetValue(categoryId, out string dictClassName))
                    {
                        if (dictClassName != null && dictClassName != className)
                        {
                            Console.WriteLine("Error! Conflicting types going to dictionaries, please remove the static keyword");
                        }
                    }
                    else
                    {
                        classNames.Add(categoryId, className);
                        getGuid.Add(categoryId, guid);
                    }
                }
            }
            Console.WriteLine("Have {0} types in dict", classNames.Count);
        }

        public void ConnectServer(string ip, ushort port)
        {
            Console.WriteLine("Connecting to {0}", ip);
            MessageAnalyzer ma = new MessageAnalyzer();
            if (encrypt == EncryptionType.Normal)
            {
                ma.CryptoTransform = new CryptoTransformHeroes();
                Console.WriteLine("Using encryption");
            }

            client.Connect(jp, ip, port, ma);
        }

        public void SleepRand(int milliseconds)
        {
            if (milliseconds < 15)
            {
                return;
            }
            int lower = (int)(milliseconds * 0.9);
            int upper = (int)(milliseconds * 1.1);
            int randTime = rand.Next(lower, upper);
            Thread.Sleep(randTime);
        }

    }
}
