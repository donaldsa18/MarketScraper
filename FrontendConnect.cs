using Devcat.Core.Net.Message;
using Devcat.Core.Threading;
using MarketScraper;
using Nexon.CafeAuth;
using PacketCap;
using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace MarketQuery
{
    class FrontendConnect : Connect
    {
        private static string serverIP = "192.168.0.200";
        private static ushort serverPort = 27015;

        public GameState gameState = new GameState();
        //public MMOChannelConnect mmo;

        private FrontendHandler mh = new FrontendHandler();

        private LoginState _state = LoginState.WaitSyncFeatureMatrix;
        public LoginState State {
            get {
                return _state;
            }

            set {
                _state = value;
                NextState();
            }
        }

        private string username;
        private string secondPassword;
        private int charNum = 1;
        private long checksum = 3053724526L;
        private long CID;

        public TradeSearchState searchState = new TradeSearchState();

        public FrontendConnect(string username,string secondPassword, int charNum = 0) : base() {
            this.username = username;
            this.secondPassword = secondPassword;
            this.charNum = charNum;
            encrypt = EncryptionType.Normal;
        }

        public void Login() {
            ConnectServer(serverIP, serverPort);
        }
        public enum LoginState
        {
            ProcessedTypeConverter,
            WaitSyncFeatureMatrix,
            SendUserLoginMessage,
            WaitAskSecondPassword,
            WaitHasSecondPassword,
            QueryCashShopBalance,
            WaitSecondPasswordResult,
            SelectCharacter,
            WaitLoginOk,
            EnterRegion,
            WaitMailList,
            EnterChannel,
            WaitRankAlarmInfo,
            QueryInnTalk,
            WaitNpcInnTalk,
            QueryNpcTalkTrade,
            WaitNpcTalkTrade,
            ContinueSearch,
            WaitSearch,
        }
        private LoginState NextState() {
            Console.WriteLine("State={0}",_state);
            LoginState s = _state;
            LoginState nextState = s;
            if (s == LoginState.SendUserLoginMessage)
            {
                nextState = LoginState.WaitAskSecondPassword;
                //passport
                UserLoginMessage msg = new UserLoginMessage(username);

                //LocalAddress
                IPAddress ip = IPAddress.Parse("10.0.0.0");
                byte[] ipBytes = { 10, 0, 0, (byte)rand.Next(2, 254) };
                uint localAddress = BitConverter.ToUInt32(ipBytes, 0);
                SetReadOnlyProperty<UserLoginMessage>(msg, "LocalAddress", localAddress);
                SetReadOnlyProperty<UserLoginMessage>(msg, "CheckSum", checksum);
                rand.NextBytes(msg.MachineID);

                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<UserLoginMessage>(msg))), 460);
            }
            else if (s == LoginState.QueryCashShopBalance)
            {
                nextState = LoginState.WaitSecondPasswordResult;
                QueryCashShopBalanceMessage msg = new QueryCashShopBalanceMessage();
                SendMsg<QueryCashShopBalanceMessage>(msg);

                SecondPasswordMessage passwordMsg = new SecondPasswordMessage(secondPassword);
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<SecondPasswordMessage>(passwordMsg))), 10000);
            }
            else if (s == LoginState.SelectCharacter)
            {
                nextState = LoginState.WaitLoginOk;
                SelectCharacterMessage charMsg = new SelectCharacterMessage(charNum);
                SleepRand(4765);
                SendMsg<SelectCharacterMessage>(charMsg);
                SleepRand(11);
                ClientLogMessage msg = new ClientLogMessage
                {
                    LogType = (int)ClientLogMessage.LogTypes.Character
                };
                //TODO: update these values
                Dictionary<string, string> clientInfo = new Dictionary<string, string>
                {
                    ["CPUVendor"] = "GenuineIntel",
                    ["CPUCores"] = "4",
                    ["TotalRAM"] = "4095",
                    ["GPUDevice"] = "Microsoft",
                    ["TotalVRAM"] = "0",
                    ["DXVersion"] = "95",
                    ["PixelShader"] = "1_x",
                    ["OSDescription"] = "Win8",
                    ["PrimaryDisplay"] = "1440x900x32",
                    ["LocaleLanguage"] = "English",
                    ["MultiCoreBoost"] = "1",
                    ["Controller"] = "M"
                };
                int t = 1;
                foreach (KeyValuePair<string, string> entry in clientInfo)
                {
                    msg.Key = entry.Key;
                    msg.Value = entry.Value;
                    Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<ClientLogMessage>(msg))), t++);
                    ;
                }
                msg.Key = "VirtualMachine";
                msg.Value = "VMWare";
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<ClientLogMessage>(msg))), t+43);
            }
            else if (s == LoginState.EnterRegion)
            {
                nextState = LoginState.WaitMailList;
                EnterRegion msg = new EnterRegion
                {
                    RegionCode = 0
                };
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<EnterRegion>(msg))), 55);

                QueryCharacterCommonInfoMessage query = new QueryCharacterCommonInfoMessage
                {
                    CID = CID,
                    QueryID = RandLong()
                };
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<QueryCharacterCommonInfoMessage>(query))), 67);


                RequestJoinPartyMessage request = new RequestJoinPartyMessage
                {
                    RequestType = 0
                };
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<RequestJoinPartyMessage>(request))), 184);
                
            }
            else if (s == LoginState.EnterChannel)
            {
                nextState = LoginState.WaitRankAlarmInfo;
                EnterChannel msg = new EnterChannel
                {
                    ChannelID = -1,
                    PartitionID = 0
                };

                ActionSync action = new ActionSync
                {
                    Position = new Vector3D
                    {
                        X = 7520,
                        Y = -352,
                        Z = 24
                    },
                    Velocity = new Vector3D
                    {
                        X = 0,
                        Y = 0,
                        Z = 0
                    },
                    Yaw = 90,
                    Sequence = 0,
                    ActionStateIndex = 2,
                    StartTime = 0,
                    State = 1
                };
                msg.Action = action;

                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<EnterChannel>(msg))), 6);

                QueryRankAlarmInfoMessage query = new QueryRankAlarmInfoMessage
                {
                    CID = CID
                };
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<QueryRankAlarmInfoMessage>(query))), 10);
            }
            else if (s == LoginState.QueryInnTalk)
            {
                nextState = LoginState.WaitNpcInnTalk;
                QueryNpcTalkMessage msg = new QueryNpcTalkMessage("t01_inn", "TI", "", "greeting");
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<QueryNpcTalkMessage>(msg))), 61);
            }
            else if (s == LoginState.QueryNpcTalkTrade)
            {
                nextState = LoginState.WaitNpcTalkTrade;
                QueryNpcTalkMessage msg = new QueryNpcTalkMessage("t01_trade", "TRADE", "", "greeting");
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<QueryNpcTalkMessage>(msg))), 10000);
            }
            else if (s == LoginState.ContinueSearch)
            {
                nextState = LoginState.WaitSearch;
                if (searchState.IsDone())
                {
                    Console.WriteLine("Finished searching");
                    Logout();
                }
                Console.WriteLine("Searching for {0}", searchState.GetTradeCategory());
                TradeCategorySearchMessage msg = new TradeCategorySearchMessage
                {
                    tradeCategory = searchState.GetTradeCategory(),
                    tradeCategorySub = "",
                    minLevel = 1,
                    maxLevel = 90,
                    uniqueNumber = searchState.uniqueNumber,
                    ChunkPageNumber = searchState.ChunkPageNumber,
                    Order = SortOrder.Level,
                    isDescending = true,
                    DetailOptions = new List<DetailOption>()
                };
                Scheduler.Schedule(jp, Job.Create((Action)(() => SendMsg<TradeCategorySearchMessage>(msg))), 1000);
            }
            else {
                Console.WriteLine("Nothing to do in state {0}",s);
            }
            _state = nextState;
            Console.WriteLine("Next State={0}", nextState);
            return nextState;
        }

        public override void ProcessTypeConverter(Packet p) {
            SetupDicts(p);
            mh.RegisterPrinters(mf, getGuid);
        }

        private long RandLong() {
            byte[] longBuf = new byte[8];
            long result = BitConverter.ToInt64(longBuf, 0);
            if (result < 0) {
                result = -result;
            }
            return result;
        }

        private static void SetReadOnlyProperty<T>(T msg, string propName, object val)
        {
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.Name.Contains(propName))
                {
                    field.SetValue(msg, val);
                }
            }
        }

        public static void SetPrivateProperty<T>(T msg, string propName, Object val)
        {
            FieldInfo field = typeof(T).GetField("propName",BindingFlags.NonPublic | BindingFlags.Instance);
            Console.WriteLine("going to set property");
            field.SetValue(msg, val);
            Console.WriteLine("set property");
        }

        public void SetCID() {
            int i = 0;
            foreach (CharacterSummary charSum in gameState.characterList) {
                if (i++ == charNum) {
                    CID = charSum.CID;
                    return;
                }
            }
        }

        private void Logout() {
            LogOutMessage msg = new LogOutMessage();
            SendMsg<LogOutMessage>(msg);
            SleepRand(1000);
            client.Disconnect();
            if (SQLiteConnect.conn != null) {
                SQLiteConnect.conn.Close();
            }
            System.Environment.Exit(1);
        }
    }
}
