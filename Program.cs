using PacketCap.Database;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using PacketCap;

namespace MarketQuery
{
    class Program
    {
        public static void Main(string[] args) {
            string mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
            SQLiteConnect.SetupDicts();
            MongoDBConnect.SetupConnect(mongoUri);
            Properties.Settings settings = Properties.Settings.Default;
            FrontendConnect c = new FrontendConnect(settings.username, settings.pin.ToString(), 1);
            c.Login();
            c.jp.Join();
        }
    }
}
