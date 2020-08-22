using System;
using System.Windows;
using Xproto;
using WebSocketSharp;
using Google.Protobuf;

namespace PokerTest
{
    public class Player : IDisposable
    {
        
        public WebSocket Ws { get; }
        public string Name { get; }

        public string UserId { get; }
        public TileStackWnd MyWnd { get; }

        public MainWindow MWnd { get; }
        public Player(string name, string userId, string tableNumber, TileStackWnd myWnd, MainWindow mWnd)
        {
            Name = name;
            MyWnd = myWnd;
            UserId = userId;
            var url = $"{ProgramConfig.ServerUrl}/ws/monkey?userID={userId}&tableNumber={tableNumber}";
            url = url.Replace("http", "ws");
            Ws = new WebSocket(string.Format(url, userId, tableNumber));
            MyWnd.SetPlayer(this);
            MWnd = mWnd;
        }

        public int ChairId
        {
            get;
            set;
        }

        public void Connect()
        {
            Ws.OnMessage += OnMessageThread;
            Ws.OnClose += OnCloseThread;

            Ws.Connect();

            MyWnd.Reset2New();
        }

        private void OnCloseThread(object sender, CloseEventArgs e)
        {
            Dispose();

            Action a = OnPlayerDisconnected;

            MyWnd.Dispatcher.Invoke(a);
        }

        private void OnPlayerDisconnected()
        {
            MyWnd.Visibility = Visibility.Hidden;
            MWnd.OnPlayerDisconnected(this);
        }

        private void OnMessageThread(object sender, MessageEventArgs messageEventArgs)
        {
            var gmsg = GameMessage.Parser.ParseFrom(messageEventArgs.RawData);
            //Console.WriteLine($"player got message, ops:{gmsg.Ops}");

            Action a = () =>
            {
                OnServerMessage(this, gmsg);
            };
            MyWnd.Dispatcher.Invoke(a);
        }

        private static void OnServerMessage(Player player, GameMessage gmsg)
        {
            switch (gmsg.Code)
            {
                case (int)MessageCode.OpactionAllowed:
                    {
                        var msg = MsgAllowAction.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageActionAllowed(player, msg);
                    }
                    break;
                case (int)MessageCode.OpreActionAllowed:
                    {
                        var msg = MsgAllowReAction.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageReActionAllowed(player, msg);
                    }
                    break;
                case (int)MessageCode.OpactionResultNotify:
                    {
                        var msg = MsgActionResultNotify.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageActionResultNotify(player, msg);
                    }
                    break;
                case (int)MessageCode.Opdeal:
                    {
                        var msg = MsgDeal.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageDeal(player, msg);
                    }
                    break;
                case (int)MessageCode.OphandOver:
                    {
                        var msg = MsgHandOver.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageHandScore(player, msg);
                    }
                    break;
                case (int)MessageCode.OpplayerEnterTable:
                    {
                        var msg = MsgEnterTableResult.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageEnterTable(player, msg);
                    }
                    break;
                case (int)MessageCode.OptableUpdate:
                    {
                        var msg = MsgTableInfo.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageTableUpdate(player, msg);
                    }
                    break;
                case (int)MessageCode.OptableShowTips:
                    {
                        var msg = MsgTableShowTips.Parser.ParseFrom(gmsg.Data);
                        OnServerMessageTableShowTips(player, msg);
                    }
                    break;
                case (int)MessageCode.OpdisbandNotify:
                    {
                        var msg = MsgDisbandNotify.Parser.ParseFrom(gmsg.Data);
                        OnServerDisbandNotify(player, msg);
                    }
                    break;
            }
        }

        private static void OnServerDisbandNotify(Player player, MsgDisbandNotify msg)
        {
            player.MyWnd.OnDisbandNotify(msg);
        }

        private static void OnServerMessageTableShowTips(Player player, MsgTableShowTips msg)
        {
            // 获得服务器分配的chair id
            player.MyWnd.OnShowTableTips(msg);
        }

        private static void OnServerMessageTableUpdate(Player player, MsgTableInfo msg)
        {
            // 获得服务器分配的chair id
            foreach (var playerInfo in msg.Players)
            {
                if (playerInfo.UserID == player.UserId)
                {
                    player.ChairId = playerInfo.ChairID;
                }
            }
        }

        private static void OnServerMessageEnterTable(Player player, MsgEnterTableResult msg)
        {
            player.MyWnd.OnEnterTable(msg);
        }

        private static void OnServerMessageHandScore(Player player, MsgHandOver msg)
        {
            player.MyWnd.OnHandScore(msg);
        }

        private static void OnServerMessageActionResultNotify(Player player, MsgActionResultNotify msg)
        {
            //throw new NotImplementedException();
            if (msg.TargetChairID == player.ChairId)
            {
                // my result
                player.MyWnd.OnActionResult(msg);
            }
            else
            {
                //if (msg.action != (int)ActionType.enumActionType_FirstReadyHand)
                //    player.MyWnd.CancelAllowedAction();
            }
        }

        private static void OnServerMessageActionAllowed(Player player, MsgAllowAction msg)
        {
            //throw new NotImplementedException();
            if (msg.ActionChairID == player.ChairId)
            {
                // my actions
                player.MyWnd.OnAllowedActions(msg);
            }
        }
        private static void OnServerMessageReActionAllowed(Player player, MsgAllowReAction msg)
        {
            //throw new NotImplementedException();
            if (msg.ActionChairID == player.ChairId)
            {
                // my actions
                player.MyWnd.OnAllowedReActions(msg);
            }
        }

        private static void OnServerMessageDeal(Player player, MsgDeal msg)
        {
            //throw new NotImplementedException();
            player.MyWnd.ResetPlayStatus();
            player.MyWnd.OnDeal(msg);
        }

        public void Dispose()
        {
            ((IDisposable)Ws)?.Dispose();
        }

        public void SendMessage(int opAction, byte[] toBytes)
        {
            ByteString data = null;
            if (toBytes != null)
            {
                data = ByteString.CopyFrom(toBytes);
            }
            else
            {
                data = ByteString.Empty;
            }

            var gmsg = new GameMessage
            {
                Code = opAction,
                Data = data
            };


            var msgBytes = gmsg.ToBytes();
            Ws?.Send(msgBytes);
        }

        public void SendReady2Server()
        {
            MyWnd.SendReady2Server();
        }
    }
}
