using Devcat.Core.Net.Message;
using PacketCap;
using PacketCap.Database;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using static MarketQuery.FrontendConnect;

namespace MarketQuery
{
    class FrontendHandler : MessagePrinter
    {
        public new void RegisterPrinters(MessageHandlerFactory mf, Dictionary<int, Guid> getGuid)
        {
            //Console.WriteLine("Registering printers");
            base.RegisterPrinters(mf, getGuid);
            //FrontendConnect fc = (FrontendConnect)conn;
            //fc.State = LoginState.SendUserLoginMessage;
        }

        public new static void PrintSyncFeatureMatrixMessage(SyncFeatureMatrixMessage msg, object tag)
        {
            MessagePrinter.PrintSyncFeatureMatrixMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitSyncFeatureMatrix)
            {
                fc.State = LoginState.SendUserLoginMessage;
            }
        }

        public new static void PrintCharacterListMessage(CharacterListMessage msg, object tag)
        {
            MessagePrinter.PrintCharacterListMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            fc.gameState.characterList = msg.Characters;
            fc.SetCID();
        }

        public new static void PrintAskSecondPasswordMessage(AskSecondPasswordMessage msg, object tag)
        {
            MessagePrinter.PrintAskSecondPasswordMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitAskSecondPassword)
            {
                fc.State = LoginState.WaitHasSecondPassword;
            }
            else
            {
                throw new Exception("Asked for second password at wrong time");
            }
        }
        public new static void PrintHasSecondPasswordMessage(HasSecondPasswordMessage msg, object tag)
        {
            MessagePrinter.PrintHasSecondPasswordMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitHasSecondPassword)
            {
                fc.State = LoginState.QueryCashShopBalance;
            }
        }

        public new static void PrintSecondPasswordResultMessage(SecondPasswordResultMessage msg, object tag)
        {
            MessagePrinter.PrintSecondPasswordResultMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (!msg.Passed)
            {
                Console.WriteLine("Wrong second password entered, exiting");
                throw new Exception("Wrong second password");
            }
            if (fc.State == LoginState.WaitSecondPasswordResult)
            {
                fc.State = LoginState.SelectCharacter;
            }
        }

        public new static void PrintLoginOkMessage(LoginOkMessage msg, object tag)
        {
            MessagePrinter.PrintLoginOkMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitLoginOk)
            {
                fc.State = LoginState.EnterRegion;
            }
            else
            {
                throw new Exception("Received login ok at wrong time");
            }
        }

        public new static void PrintMailListMessage(MailListMessage msg, object tag)
        {
            MessagePrinter.PrintMailListMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitMailList)
            {
                fc.State = LoginState.EnterChannel;
            }
        }

        public new static void PrintRankAlarmInfoMessage(RankAlarmInfoMessage msg, object tag)
        {
            MessagePrinter.PrintRankAlarmInfoMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitRankAlarmInfo)
            {
                fc.State = LoginState.QueryInnTalk;
            }
        }

        public new static void PrintChannelServerAddress(ChannelServerAddress msg, object tag)
        {
            MessagePrinter.PrintChannelServerAddress(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            fc.gameState.mmoChannel = msg;
        }
        /*
        public new static void PrintUpdateBattleInventoryInTownMessage(UpdateBattleInventoryInTownMessage msg, object tag) {
            MessagePrinter.PrintUpdateBattleInventoryInTownMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitUpdateBattleInventoryInTown) {
                fc.State = LoginState.SendHotSpringRequestInfo;
            }
        }
        public new static void PrintHotSpringRequestInfoResultMessage(HotSpringRequestInfoResultMessage msg, object tag) {
            MessagePrinter.PrintHotSpringRequestInfoResultMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitHotSpringRequestInfoResult) {
                fc.State = LoginState.MovePartition;
            }
        }
        */
        public new static void PrintNpcTalkMessage(NpcTalkMessage msg, object tag)
        {
            MessagePrinter.PrintNpcTalkMessage(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            if (fc.State == LoginState.WaitNpcTalkTrade)
            {
                fc.State = LoginState.ContinueSearch;
            }
            else if (fc.State == LoginState.WaitNpcInnTalk)
            {
                fc.State = LoginState.QueryNpcTalkTrade;
            }
        }

        public new static void PrintTradeSearchResult(TradeSearchResult msg, object tag)
        {
            MessagePrinter.PrintTradeSearchResult(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            Console.WriteLine("Inserting trade item info");
            MongoDBConnect.connection.InsertTradeItemInfoList(msg.TradeItemList);
            if (msg.TradeItemList != null) {
                fc.searchState.seenNumber += msg.TradeItemList.Count;
            }
            
            fc.searchState.NextSearch(msg.IsMoreResult);
            Console.WriteLine("Trade state: {0}",fc.State);
            Console.WriteLine("Found {0} items", fc.searchState.seenNumber);
            if (fc.State == LoginState.WaitSearch)
            {
                fc.State = LoginState.ContinueSearch;
            }
        }

        public new static void PrintCharacterCommonInfoMessage(CharacterCommonInfoMessage msg, object tag)
        {
            MessagePrinter.PrintCharacterCommonInfoMessage(msg, tag);

        }

        public new static void PrintNotifyAction(NotifyAction msg, object tag) {
            MessagePrinter.PrintNotifyAction(msg, tag);
            FrontendConnect fc = (FrontendConnect)tag;
            
            MongoDBConnect.connection.InsertNotifyAction(msg, fc.gameState.mmoChannel.ChannelID,fc.gameState.townID);
        }
    }
}