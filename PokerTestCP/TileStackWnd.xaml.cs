﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Xproto;


namespace PokerTest
{
    /// <summary>
    /// TileStackWnd.xaml 的交互逻辑
    /// </summary>
    public partial class TileStackWnd : UserControl
    {
        private int expectedReadyHandFlags;
        public const int maxTileCountOnHand = 16;
        public TileStackWnd()
        {
            InitializeComponent();

            InitButtonArray();

            HideAllButtons();
        }


        public Button[] ButtonsSp1 { get; } = new Button[maxTileCountOnHand];
        public Button[] ButtonsSp2 { get; } = new Button[maxTileCountOnHand];
        public Button[] ButtonsSp3 { get; } = new Button[maxTileCountOnHand];
        public Button[] ButtonsSp4 { get; } = new Button[maxTileCountOnHand];
        public Button[] ButtonsAct { get; } = new Button[5];

        public Dictionary<int, BitmapImage> ImagesSrc { get; private set; }

        public List<int> TilesHandList { get; } = new List<int>();
        public List<int> TilesFlowerList { get; } = new List<int>();
        public List<MsgCardHand> MeldList { get; } = new List<MsgCardHand>();
        public int BankerChairId { get; private set; }

        public bool IsBandker => BankerChairId == MyPlayer.ChairId;

        public MainWindow MyOwner { get; private set; }
        public MsgAllowAction CurrentAllowPlayerAction { get; private set; }

        public MsgAllowReAction CurrentAllowPlayerReAction { get; private set; }

        public IEnumerator<Button> GetEnumerator()
        {
            foreach (var b in ButtonsSp1)
            {
                yield return b;
            }
            foreach (var b in ButtonsSp2)
            {
                yield return b;
            }
            foreach (var b in ButtonsSp3)
            {
                yield return b;
            }
            foreach (var b in ButtonsSp4)
            {
                yield return b;
            }
            foreach (var b in ButtonsAct)
            {
                yield return b;
            }
        }

        private void InitButtonArray()
        {
            var i = 0;
            foreach (var child in Sp1.Children)
            {
                ButtonsSp1[i++] = child as Button;
            }

            i = 0;
            foreach (var child in Sp2.Children)
            {
                ButtonsSp2[i++] = child as Button;
            }

            i = 0;
            foreach (var child in Sp3.Children)
            {
                ButtonsSp3[i++] = child as Button;
            }

            i = 0;
            foreach (var child in Sp4.Children)
            {
                ButtonsSp4[i++] = child as Button;
            }

            ButtonsAct[0] = BtnAction1;
            ButtonsAct[1] = BtnAction2;
            ButtonsAct[2] = BtnAction3;
            ButtonsAct[3] = BtnAction4;
            ButtonsAct[4] = BtnAction5;
        }

        public void SetImageSrc(Dictionary<int, BitmapImage> imageDict, MainWindow owner)
        {
            MyOwner = owner;
            ImagesSrc = imageDict;

            var i = 0;
            foreach (var button in ButtonsSp1)
            {
                button.Content = new Image() { Source = ImagesSrc[i++] };
            }

            i = 0;
            foreach (var button in ButtonsSp2)
            {
                button.Content = new Image() { Source = ImagesSrc[i++] };
            }
            i = 0;
            foreach (var button in ButtonsSp3)
            {
                button.Content = new Image() { Source = ImagesSrc[i++] };
            }
            i = 0;
            foreach (var button in ButtonsSp4)
            {
                button.Content = new Image() { Source = ImagesSrc[i++] };
            }
        }

        private void OnAction1_Button_Click(object sender, RoutedEventArgs e)
        {
            //enumActionType_KONG_Concealed
            // enumActionType_KONG_Exposed

            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            //var action = (int)button.Tag;
            //var tile1 = -1;
            //List<MsgMeldTile> meldList;
            //switch (action)
            //{
            //    case (int)ActionType.enumActionType_KONG_Concealed:
            //        meldList = CurrentAllowPlayerAction.meldsForAction.Select(x => x).Where(x => x.meldType == (int)MeldType.enumMeldTypeConcealedKong).ToList();
            //        if (!ChowPongKongWnd.ShowDialog(meldList, out tile1, this))
            //        {
            //            return;
            //        }
            //        OnTakeActionKongConcealedTile(tile1);
            //        break;
            //    case (int)ActionType.enumActionType_KONG_Exposed:
            //        meldList = CurrentAllowPlayerReAction.meldsForAction.Select(x => x).Where(x => x.meldType == (int)MeldType.enumMeldTypeExposedKong).ToList();
            //        if (!ChowPongKongWnd.ShowDialog(meldList, out tile1, this))
            //        {
            //            return;
            //        }
            //        OnTakeActionKongExposedTile(tile1);
            //        break;
            //}
            HideAllActionButtons();
        }
        private void OnAction2_Button_Click(object sender, RoutedEventArgs e)
        {
            //enumActionType_KONG_Triplet2
            //enumActionType_CHOW
            //throw new NotImplementedException();

            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            var action = (int)button.Tag;
            var completed = false;
            //var tile1 = -1;
            //List<MsgMeldTile> meldList;
            switch (action)
            {
                //case (int)ActionType.enumActionType_KONG_Triplet2:
                //    meldList = CurrentAllowPlayerAction.meldsForAction.Select(x => x).Where(x => x.meldType == (int)MeldType.enumMeldTypeTriplet2Kong).ToList();
                //    if (!ChowPongKongWnd.ShowDialog(meldList, out tile1, this))
                //    {
                //        return;
                //    }
                //    OnTakeActionKong2TripletTile(tile1);
                //    break;
                case (int)ActionType.EnumActionTypeDiscard:
                    completed = doCommonDiscardClick();
                    break;
            }
            if (completed)
                HideAllActionButtons();
        }

        private bool doCommonDiscardClick()
        {
            //MsgCardHand prevCardHand = null;
            //int specialCardID = -1;
            //if (CurrentAllowPlayerReAction != null)
            //{
            //    prevCardHand = CurrentAllowPlayerReAction.PrevActionHand;
            //    if (CurrentAllowPlayerReAction.timeoutInSeconds > 255)
            //    {
            //        specialCardID = (int)pokerface.CardID.R2H;
            //    }
            //}
            //else if (CurrentAllowPlayerAction != null)
            //{
            //    if (CurrentAllowPlayerAction.timeoutInSeconds > 255)
            //    {
            //        specialCardID = (int)pokerface.CardID.R3H;
            //    }
            //}

            var discardedList = new List<int>();
            if (!DiscardWnd.ShowDialog(discardedList, this))
            {
                return false;
            }

            OnTakeActionDiscardTile(discardedList);

            return true;
        }

        private void OnAction3_Button_Click(object sender, RoutedEventArgs e)
        {
            //enumActionType_WIN_SelfDrawn
            //enumActionType_PONG
            //throw new NotImplementedException();
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            //var action = (int)button.Tag;
            //switch (action)
            //{
            //    case (int)ActionType.enumActionType_WIN_SelfDrawn:
            //        OnTakeActionWinSelfDraw();
            //        break;
            //    case (int)ActionType.enumActionType_PONG:
            //        var tile1 = -1;
            //        var meldList = CurrentAllowPlayerReAction.meldsForAction.Select(x => x).Where(x => x.meldType == (int)MeldType.enumMeldTypeTriplet).ToList();
            //        if (!ChowPongKongWnd.ShowDialog(meldList, out tile1, this))
            //        {
            //            return;
            //        }
            //        OnTakeActionPongTile(tile1);
            //        break;
            //}

            HideAllActionButtons();
        }

        private void OnAction4_Button_Click(object sender, RoutedEventArgs e)
        {
            //enumActionType_WIN_FirstReadyHand
            //enumActionType_SKIP
            //throw new NotImplementedException();
            //var button = sender as Button;
            //if (button == null)
            //{
            //    return;
            //}

            //var action = (int)button.Tag;
            //switch (action)
            //{
            //    //case (int)ActionType.enumActionType_FirstReadyHand:
            //    //    OnNonBankerTakeActionFirstHand(0);
            //    //    break;
            //    case (int)ActionType.enumActionType_SKIP:
            //        OnTakeActionSkip();
            //        break;
            //    //case (int)ActionType.enumActionType_AccumulateWin:
            //    //    OnTakeActionAccumulativeWin();
            //    //    break;
            //}

            HideAllActionButtons();
        }

        private void OnAction5_Button_Click(object sender, RoutedEventArgs e)
        {
            //enumActionType_DISCARD
            //enumActionType_WIN_Chuck
            //throw new NotImplementedException();
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            var action = (int)button.Tag;
            var completed = false;
            switch (action)
            {
                //case (int)ActionType.enumActionType_FirstReadyHand:
                //    if (!RichiWnd.ShowDialog(CurrentAllowPlayerAction.tipsForAction[0], this))
                //    {
                //        return;
                //    }
                //    OnNonBankerTakeActionFirstHand(1);
                //    break;
                case (int)ActionType.EnumActionTypeDiscard:
                    // var tile1 = -1;

                    //if (IsBandkerReadyHand || expectedReadyHandFlags != 0)
                    //{
                    //    int readyHandFlags;
                    //    if (!DiscardWnd.ShowDialog(CurrentAllowPlayerAction.tipsForAction, out tile1, out readyHandFlags, expectedReadyHandFlags, this))
                    //    {
                    //        return;
                    //    }

                    //    IsBandkerReadyHand = false;
                    //    OnTakeActionDiscardTile(tile1, readyHandFlags);
                    //}
                    //else
                    //{
                    //if (!DiscardWnd.ShowDialog(CurrentAllowPlayerAction.tipsForAction, out tile1, this))
                    //{
                    //    return;
                    //}

                    //OnTakeActionDiscardTile(tile1);
                    ////}
                    completed = doCommonDiscardClick();
                    break;
                //case (int)ActionType.enumActionType_WIN_Chuck:
                //    OnTakeActionWinChuck();
                //    break;
            }
            if (completed)
                HideAllActionButtons();
        }

        public bool IsBandkerReadyHand { get; set; }

        private void OnTakeActionWinSelfDraw()
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerAction.qaIndex,
            //    action = (int)ActionType.enumActionType_WIN_SelfDrawn,
            //};

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
            //MyOwner.AppendActionLog($"[winselfdraw]({MyPlayer.Name}),({CurrentAllowPlayerAction.qaIndex})");
        }

        private void OnTakeActionAccumulativeWin()
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerAction.qaIndex,
            //    action = (int)ActionType.enumActionType_AccumulateWin,
            //};

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
            //MyOwner.AppendActionLog($"[winselfdraw]({MyPlayer.Name}),({CurrentAllowPlayerAction.qaIndex})");
        }

        private void OnTakeActionKong2TripletTile(int tile1)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerAction.qaIndex,
            //    action = (int)ActionType.enumActionType_KONG_Triplet2,
            //    tile = tile1,
            //};

            //var sb = new StringBuilder();
            //for (var i = 0; i < 4; i++)
            //{
            //    var tileId = tile1;
            //    sb.Append($"{MyOwner.TileId2Name(tileId)},");
            //}

            ////MyOwner.AppendActionLog($"[triplet2kong]({MyPlayer.Name}):{sb}({CurrentAllowPlayerAction.qaIndex})");

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
        }

        private void OnTakeActionChowTile(int tile1)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerReAction.qaIndex,
            //    action = (int)ActionType.enumActionType_CHOW,
            //    tile = CurrentAllowPlayerReAction.victimTileID,
            //};

            //msgAction.meldType = (int)MeldType.enumMeldTypeSequence;
            //msgAction.meldTile1 = tile1;

            //var sb = new StringBuilder();
            //for (var i = 0; i < 3; i++)
            //{
            //    var tileId = tile1 + i;
            //    sb.Append($"{MyOwner.TileId2Name(tileId)},");
            //}

            ////MyOwner.AppendActionLog($"[chow]({MyPlayer.Name}):{sb}({CurrentAllowPlayerReAction.qaIndex})");

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
        }

        private void OnTakeActionPongTile(int tile1)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerReAction.qaIndex,
            //    action = (int)ActionType.enumActionType_PONG,
            //    tile = tile1,
            //};

            //msgAction.meldType = (int)MeldType.enumMeldTypeTriplet;
            //msgAction.meldTile1 = tile1;

            //var sb = new StringBuilder();
            //for (var i = 0; i < 3; i++)
            //{
            //    var tileId = tile1;
            //    sb.Append($"{MyOwner.TileId2Name(tileId)},");
            //}

            ////MyOwner.AppendActionLog($"[pong]({MyPlayer.Name}):{sb}({CurrentAllowPlayerReAction.qaIndex})");
            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
        }

        private void OnTakeActionKongExposedTile(int tile1)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerReAction.qaIndex,
            //    action = (int)ActionType.enumActionType_KONG_Exposed,
            //    tile = tile1,
            //};

            //msgAction.meldType = (int)MeldType.enumMeldTypeExposedKong;
            //msgAction.meldTile1 = tile1;

            //var sb = new StringBuilder();
            //for (var i = 0; i < 4; i++)
            //{
            //    var tileId = tile1;
            //    sb.Append($"{MyOwner.TileId2Name(tileId)},");
            //}

            ////MyOwner.AppendActionLog($"[kongExposed]({MyPlayer.Name}):{sb}({CurrentAllowPlayerReAction.qaIndex})");
            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
        }

        private void OnTakeActionKongConcealedTile(int tile1)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerAction.qaIndex,
            //    action = (int)ActionType.enumActionType_KONG_Concealed,
            //    tile = tile1,
            //};

            ////msgAction.actionMeld = new MsgMeldTile() { meldType = (int)MeldType.enumMeldTypeConcealedKong, tile1 = tile1 };
            //var sb = new StringBuilder();
            //for (var i = 0; i < 4; i++)
            //{
            //    var tileId = tile1;
            //    sb.Append($"{MyOwner.TileId2Name(tileId)},");
            //}

            ////MyOwner.AppendActionLog($"[kongConcealed]({MyPlayer.Name}):{sb}({CurrentAllowPlayerAction.qaIndex})");
            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
        }

        private void OnTakeActionSkip()
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerReAction.qaIndex,
            //    action = (int)ActionType.enumActionType_SKIP,
            //};

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());

            //MyOwner.AppendActionLog($"[skip]({MyPlayer.Name}),({CurrentAllowPlayerReAction.qaIndex})");
        }

        private void OnNonBankerTakeActionFirstHand(int tid)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerAction.qaIndex,
            //    action = (int)ActionType.enumActionType_FirstReadyHand,
            //    tile = tid, // 1表示听牌，0表示不听牌
            //    flags = tid
            //};

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
            //MyOwner.AppendActionLog($"[richi]({MyPlayer.Name}),({CurrentAllowPlayerAction.qaIndex}),({tid})");
        }

        private void OnTakeActionWinChuck()
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerReAction.qaIndex,
            //    action = (int)ActionType.enumActionType_WIN_Chuck,
            //    tile = CurrentAllowPlayerReAction.victimTileID
            //};

            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
            //MyOwner.AppendActionLog($"[winchuck]({MyPlayer.Name}),({CurrentAllowPlayerReAction.qaIndex})");
        }

        private void OnTakeActionDiscardTile(List<int> tiles2Discarded)
        {
            var qaIndex2 = 0;
            if (CurrentAllowPlayerAction != null)
            {
                qaIndex2 = CurrentAllowPlayerAction.QaIndex;
            }
            else
            {
                qaIndex2 = CurrentAllowPlayerReAction.QaIndex;
            }

            var msgAction = new MsgPlayerAction
            {
                QaIndex = qaIndex2,
                Action = (int)ActionType.EnumActionTypeDiscard,
                
            };

            msgAction.Cards.AddRange(tiles2Discarded);
            MyPlayer.SendMessage((int)MessageCode.Opaction, msgAction.ToBytes());
            //MyOwner.AppendActionLog($"[discard]({MyPlayer.Name}):{MyOwner.TileId2Name(tile2Discarded)},({CurrentAllowPlayerAction.qaIndex})");
        }

        private void OnTakeActionDiscardTile(int tile2Discarded, int readyHandFlags)
        {
            //var msgAction = new MsgPlayerAction
            //{
            //    qaIndex = CurrentAllowPlayerAction.qaIndex,
            //    action = (int)ActionType.enumActionType_DISCARD,
            //    tile = tile2Discarded,
            //    flags = readyHandFlags,
            //};
            //// var ix = readyHand ? 1 : 0;
            //MyPlayer.SendMessage((int)MessageCode.OPAction, msgAction.ToBytes());
            //MyOwner.AppendActionLog($"[discard]({MyPlayer.Name}):{MyOwner.TileId2Name(tile2Discarded)},({CurrentAllowPlayerAction.qaIndex}),({ix})");
        }

        private void HideAllButtons()
        {
            foreach (var button in this)
            {
                button.Visibility = Visibility.Hidden;
            }
        }

        public void ResetPlayStatus()
        {
            //throw new NotImplementedException();
            // hide all
            Reset2New();
        }

        public void OnDeal(MsgDeal msg)
        {
            Reset2New();

            MsgPlayerCardList myPlayList = null;
            foreach (var ptl in msg.PlayerCardLists)
            {
                if (ptl.ChairID == MyPlayer.ChairId)
                {
                    myPlayList = ptl;
                    break;
                }
            }

            if (myPlayList == null)
                return;

            if (myPlayList.CardsOnHand.Count < 1)
                return;

            //// 庄家标记
            //TbName.Text = msg.bankerChairID == MyPlayer.ChairId ? $"{MyPlayer.Name}(庄)" : $"{MyPlayer.Name}";
            //BankerChairId = msg.bankerChairID;
            //TbScore.Text = "";

            //if (!MyOwner.IsPlaying)
            //{
            //    MyOwner.IsPlaying = true;
            //    MyOwner.ClearLog();
            //    MyOwner.AppendLog("[begin]\r\n");
            //    MyOwner.ResetActionListWndIndex();
            //}

            //if (IsBandker)
            //{
            //    TbPseudoFlower.Text = MyOwner.TileId2Name(msg.windFlowerID);
            //    MyOwner.AppendLog($"[bank]:{MyPlayer.Name}\r\n");
            //    MyOwner.AppendLog($"[wind]:{MyOwner.TileId2Name(msg.windFlowerID)}\r\n");
            //    MyOwner.TbTileInWallRemain.Text = msg.tilesInWall.ToString();
            //    MyOwner.ResetScoreWnd();
            //}

            // 手牌列表
            TilesHandList.AddRange(myPlayList.CardsOnHand);
            Hand2Buttons();

            //// 花牌列表
            //TilesFlowerList.AddRange(myPlayList.tilesFlower);
            //Flower2Buttons();

            //var sb = new StringBuilder();
            //sb.Append($"[deal]({MyPlayer.Name})(hand):");
            //foreach (var i in TilesHandList)
            //{
            //    sb.Append(MyOwner.TileId2Name(i));
            //    sb.Append(",");
            //}
            //sb.AppendLine();
            //sb.Append("\t(flower):");
            //foreach (var i in TilesFlowerList)
            //{
            //    sb.Append(MyOwner.TileId2Name(i));
            //    sb.Append(",");
            //}
            //sb.AppendLine();
            //MyOwner.AppendLog(sb.ToString());
        }

        private void Flower2Buttons()
        {
            SortFlowerTiles();
            var i = 0;
            foreach (var t in TilesFlowerList)
            {
                if (i >= ButtonsSp3.Length)
                {
                    return;
                }

                ButtonsSp3[i].Content = new Image() { Source = ImagesSrc[t] };
                ButtonsSp3[i].Visibility = Visibility.Visible;
                ++i;
            }
        }

        private void Hand2Buttons()
        {
            HideSp4Buttons();
            SortHandTiles();
            var i = 0;
            foreach (var t in TilesHandList)
            {
                ButtonsSp4[i].Content = new Image() { Source = ImagesSrc[t] };
                ButtonsSp4[i].Visibility = Visibility.Visible;
                ++i;
            }
        }

        private void SortHandTiles()
        {
            TilesHandList.Sort((x, y) => x - y);
        }

        private void SortFlowerTiles()
        {
            TilesFlowerList.Sort((x, y) => x - y);
        }
        private void HideSp4Buttons()
        {
            foreach (var button in ButtonsSp4)
            {
                button.Visibility = Visibility.Hidden;
            }
        }

        private void HideSp2Buttons()
        {
            foreach (var button in ButtonsSp2)
            {
                button.Visibility = Visibility.Hidden;
            }
        }
        public void Reset2New()
        {
            HideAllButtons();
            TilesFlowerList.Clear();
            TilesHandList.Clear();
            MeldList.Clear();
            BankerChairId = 0;
            TbRichi.Text = "";
            TbPseudoFlower.Text = "";
        }

        internal void SetPlayer(Player player)
        {
            MyPlayer = player;
            TbName.Text = player.Name;

            Visibility = Visibility.Visible;
        }

        public Player MyPlayer { get; set; }

        public void OnAllowedReActions(MsgAllowReAction msg)
        {
            HideAllActionButtons();
            CurrentAllowPlayerReAction = msg;
            CurrentAllowPlayerAction = null;

            var actions = msg.AllowedActions;


            //if ((actions & (int)ActionType.enumActionType_KONG_Exposed) != 0)
            //{
            //    BtnAction1.Visibility = Visibility.Visible;
            //    BtnAction1.Content = "明杠";
            //    BtnAction1.Tag = (int)ActionType.enumActionType_KONG_Exposed;
            //}

            if ((actions & (int)ActionType.EnumActionTypeDiscard) != 0)
            {
                BtnAction2.Visibility = Visibility.Visible;
                BtnAction2.Content = "出牌";
                BtnAction2.Tag = (int)ActionType.EnumActionTypeDiscard;
            }

            //if ((actions & (int)ActionType.enumActionType_PONG) != 0)
            //{
            //    BtnAction3.Visibility = Visibility.Visible;
            //    BtnAction3.Content = "碰";
            //    BtnAction3.Tag = (int)ActionType.enumActionType_PONG;
            //}

            //if ((actions & (int)ActionType.enumActionType_SKIP) != 0)
            //{
            //    BtnAction4.Visibility = Visibility.Visible;
            //    BtnAction4.Content = "过";
            //    BtnAction4.Tag = (int)ActionType.enumActionType_SKIP;
            //}

            //if ((actions & (int)ActionType.enumActionType_WIN_Chuck) != 0)
            //{
            //    BtnAction5.Visibility = Visibility.Visible;
            //    BtnAction5.Content = "胡";
            //    BtnAction5.Tag = (int)ActionType.enumActionType_WIN_Chuck;
            //}

            if (MyOwner.CheckBoxAutoAction.IsChecked == false && IsAutoX)
            {
                // 自动打牌
                //if ((actions & (int)ActionType.enumActionType_SKIP) != 0)
                //{
                //    OnTakeActionSkip();
                //}
                if ((actions & (int)ActionType.EnumActionTypeDiscard) != 0)
                {
                    int specialCardID = -1;
                    if (CurrentAllowPlayerReAction.TimeoutInSeconds > 255)
                    {
                        specialCardID = (int)CardID.R3H;
                    }

                    //var currents = AgariIndex.FindGreatThanCardHand(CurrentAllowPlayerReAction.PrevActionHand, TilesHandList, specialCardID);
                    //if (null == currents || currents.Count == 0)
                    //{
                    //    MessageBox.Show("auto discard:oh shit, a huge bug");
                    //    throw new System.Exception("huge bug");
                    //}

                    //OnTakeActionDiscardTile(currents[0].cards);

                    HideAllActionButtons();
                }

            }
        }

        public void OnAllowedActions(MsgAllowAction msg)
        {
            HideAllActionButtons();
            CurrentAllowPlayerAction = msg;
            CurrentAllowPlayerReAction = null;

            var actions = msg.AllowedActions;

            if ((actions & (int)ActionType.EnumActionTypeDiscard) != 0)
            {
                BtnAction5.Visibility = Visibility.Visible;
                BtnAction5.Content = "出牌";
                BtnAction5.Tag = (int)ActionType.EnumActionTypeDiscard;

                if (MyOwner.CheckBoxAutoAction.IsChecked == false && IsAutoX)
                {
                    //var handTips = msg.tipsForAction;
                    //// 自动打牌
                    //if ((actions & (int)ActionType.enumActionType_FirstReadyHand) != 0)
                    //{
                    //    if (!IsBandker)
                    //    {
                    //        // 绝对不听牌
                    //        OnNonBankerTakeActionFirstHand(0);
                    //    }
                    //    else
                    //    {
                    //        OnTakeActionDiscardTile(handTips[0].targetTile, 0);
                    //    }
                    //}
                    //else if ((actions & (int)ActionType.enumActionType_DISCARD) != 0)
                    //{
                    //    OnTakeActionDiscardTile(handTips[0].targetTile);
                    //}

                    int specialCardID = -1;
                    if (CurrentAllowPlayerAction.TimeoutInSeconds > 255)
                    {
                        specialCardID = (int)CardID.R3H;
                    }

                    //var current = AgariIndex.SearchLongestDiscardCardHand(TilesHandList, specialCardID);
                    //if (current == null)
                    //{
                    //    MessageBox.Show("auto discard: BIG BIG WORLD");
                    //    throw new System.Exception("Huge bug, darling");
                    //}

                    //OnTakeActionDiscardTile(current.cards);

                    HideAllActionButtons();
                }
            }
        }

        private void HideAllActionButtons()
        {
            foreach (var button in ButtonsAct)
            {
                button.Visibility = Visibility.Hidden;
            }
        }

        public void OnActionResult(MsgActionResultNotify msg)
        {

            // 出牌
            if (msg.Action == (int)ActionType.EnumActionTypeDiscard)
            {
                var discardedHands = msg.ActionHands;
                foreach(var h in discardedHands)
                {
                    foreach (var d in h.Cards)
                    {
                        TilesHandList.Remove(d);
                    }

                    MeldList.Add(h);
                }

                Hand2Buttons();
                MeldList2Buttons();

                //MyOwner.BtnDiscard.Content = new Image() { Source = ImagesSrc[msg.actionTile] };
                //MyOwner.TbDiscard.Text = $"{MyPlayer.Name}->{MyOwner.TileId2Name(msg.actionTile)}";
                //MyOwner.AppendActionLog($"[discard]({MyPlayer.Name}):{MyOwner.TileId2Name(msg.actionTile)}");
                return;
            }
        }

        private void MeldList2Buttons()
        {
            HideSp2Buttons();
            HideSp1Buttons();

            var i = 0;
            foreach (var meld in MeldList)
            {
                if (i > 15)
                {
                    break;
                }

                var j = 0;
                foreach (var card in meld.Cards)
                {
                    var btn = ButtonsSp2[i];
                    btn.Content = new Image() { Source = ImagesSrc[card] };
                    btn.Tag = card;

                    btn.Visibility = Visibility.Visible;
                    if (j == 0)
                    {
                        //SetContributor(ButtonsSp1[i + 1], meld.contributor);
                        SetMeldFlag(ButtonsSp1[i], meld.CardHandType);
                    }
                    j++;
                    i++;

                    if (i > 15)
                    {
                        break;
                    }

                }
            }
        }

        private void SetMeldFlag(Button button, int meldMeldType)
        {
            var ct =(CardHandType)meldMeldType;
            switch(ct)
            {
                case CardHandType.Straight:
                    button.Content = "顺子";
                    break;
                case CardHandType.Flush:
                    button.Content = "同花";
                    break;
                case CardHandType.ThreeOfAkind:
                    button.Content = "三条";
                    break;
                case CardHandType.FullHouse:
                    button.Content = "葫芦";
                    break;
                case CardHandType.HighCard:
                    button.Content = "单";
                    break;
                case CardHandType.OnePair:
                    button.Content = "对牌";
                    break;
                case CardHandType.TwoPairs:
                    button.Content = "两对";
                    break;
                case CardHandType.Four:
                    button.Content = "四条";
                    break;
                case CardHandType.StraightFlush:
                    button.Content = "同花顺";
                    break;
            }

            //if (meldMeldType == (int)MeldType.enumMeldTypeTriplet2Kong)
            //{
            //    button.Content = "加";
            //}
            //else if (meldMeldType == (int)MeldType.enumMeldTypeConcealedKong)
            //{
            //    button.Content = "暗";
            //}
            //else if (meldMeldType == (int)MeldType.enumMeldTypeExposedKong)
            //{
            //    button.Content = "明";
            //}
            //else if (meldMeldType == (int)MeldType.enumMeldTypeTriplet)
            //{
            //    button.Content = "碰";
            //}
            //else if (meldMeldType == (int)MeldType.enumMeldTypeSequence)
            //{
            //    button.Content = "吃";
            //}

            button.Visibility = Visibility.Visible;
        }

        private void HideSp1Buttons()
        {
            foreach (var button in ButtonsSp1)
            {
                button.Visibility = Visibility.Hidden;
            }
        }

        public void SetContributor(Button btn, int contr)
        {
            var chair2Name = new[]
            {
                "A",
                "B",
                "C",
                "D"
            };
            btn.Content = chair2Name[contr];
            btn.Visibility = Visibility.Visible;
        }

        public void CancelAllowedAction()
        {
            HideAllActionButtons();
        }

        public void OnHandScore(MsgHandOver msg)
        {
            if (MyOwner.IsPlaying)
            {
                MyOwner.IsPlaying = false;
                MyOwner.AppendLog("[end]\r\n");
                var handoverType = msg.EndType;
                switch (handoverType)
                {
                    case (int)HandOverType.EnumHandOverTypeNone:
                        MyOwner.AppendLog("平局\r\n");
                        break;
                    case (int)HandOverType.EnumHandOverTypeWin:
                        MyOwner.AppendLog("赢牌\r\n");
                        break;
                    case (int)HandOverType.EnumHandOverTypeLoss:
                        MyOwner.AppendLog("输牌\r\n");
                        break;
                }
            }

            var handScore = msg.Scores;
            if (handScore == null)
            {
                return;
            }

            var myScore = handScore.PlayerScores.FirstOrDefault(playerScore => playerScore.TargetChairID == MyPlayer.ChairId);
            if (myScore == null)
                return;


            TbScore.Text = myScore.TotalScore.ToString();

            if (MyOwner.IsFirstPlayer(MyPlayer))
            {
                var scoreMsg = FormatScore(msg);
                MyOwner.ShowScoreWnd(scoreMsg);
            }
        }

        private static string FormatScore(MsgHandOver msg)
        {
            var sb = new StringBuilder();

            // 结束类型
            sb.AppendLine($"{Enum2StrHelper.EndType2String(msg.EndType)}");

            var handScore = msg.Scores;
            // 每个玩家的得分和赢牌类型
            foreach (var playerScore in handScore.PlayerScores)
            {
                sb.Append(Enum2StrHelper.ChairId2Name(playerScore.TargetChairID));
                sb.Append(":");
                sb.Append(playerScore.TotalScore);

                sb.AppendLine();
            }

            // 每个玩家得分详细信息
            sb.AppendLine("------------------Details:---------------------");
            foreach (var playerScore in handScore.PlayerScores)
            {
                sb.AppendLine($"座位：{Enum2StrHelper.ChairId2Name(playerScore.TargetChairID)}");
                sb.AppendLine($"得分：{playerScore.TotalScore}");
                sb.AppendLine($"是否倒墩：{playerScore.IsInvertedHand}");
                sb.AppendLine($"是否全赢：{playerScore.IsWinAll}");
                
                // 与其他玩家的比较
                foreach(var cc in playerScore.CompareContexts)
                {
                    sb.AppendLine($"与对手(座位：{cc.TargetChairID})的每墩得分:");
                    var i = 0;
                    foreach (var h in cc.HandScores)
                    {
                        sb.AppendLine($"第{i}墩：{h}");
                        i++;
                    }
                    sb.AppendLine();
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void OnEnterTable(MsgEnterTableResult msg)
        {
            if (msg.Status != (int)EnterTableStatus.Success)
            {
                var x = msg.Status;
                MessageBox.Show($"enter table failed:{x.ToString()}");
                return;
            }

            MyPlayer.SendMessage((int)MessageCode.OpplayerReady, null);
        }

        public void OnDisbandNotify(MsgDisbandNotify msg)
        {
            if (msg.Waits != null)
            {
                var me = msg.Waits.Any((x) => x == MyPlayer.ChairId);
                if (me)
                {
                    var result = MessageBox.Show(MyOwner, "有人请求解散房间，是否同意？", "解散房间询问", MessageBoxButton.YesNo);
                    var agree = result == MessageBoxResult.Yes;


                    var msgAnswer = new MsgDisbandAnswer();
                    msgAnswer.Agree = agree;

                    MyPlayer.SendMessage((int)MessageCode.OpdisbandAnswer, msgAnswer.ToBytes());
                }
            }
        }

        public void SendReady2Server()
        {
            MyPlayer.SendMessage((int)MessageCode.OpplayerReady, null);
        }

        public void OnShowTableTips(MsgTableShowTips msg)
        {
            MyOwner.AppendLog($"{MyPlayer.UserId}:  {msg.Tips}\r\n");

            if (string.IsNullOrWhiteSpace(msg.Tips))
            {
                return;
            }

            if (MyOwner.CheckBoxAutoAction.IsChecked == true)
            {
                DoAutoAction(msg.Tips);
            }
        }

        public struct ActionParams
        {
            public string Action;
            public string[] TileStrings;
            public bool HasRichi;
            public int RichiFlags;
        }

        private void DoAutoAction(string msgTips)
        {
            ActionParams actionParams = ParseActionMsgTips(msgTips);
            // var tileId = 0;
            switch (actionParams.Action)
            {
                case "discard":
                    List<int> tiles2Discarded = new List<int>();
                    foreach(var ts in actionParams.TileStrings)
                    {
                        tiles2Discarded.Add(MyOwner.NameIds[ts]);
                    }
                    OnTakeActionDiscardTile(tiles2Discarded);
                    HideAllActionButtons();
                    break;
                case "skip":
                    OnTakeActionSkip();
                    HideAllActionButtons();
                    break;
            }
        }

        private ActionParams ParseActionMsgTips(string msgTips)
        {
            ActionParams actionParams = new ActionParams { Action = "" };

            var pattern = @"^\[(?<action>[^\s]+).*\]$";
            var rgx = new Regex(pattern, RegexOptions.IgnoreCase);
            var matches = rgx.Matches(msgTips);
            if (matches.Count > 0)
            {
                var match = matches[0];
                var action = match.Groups["action"].Value;
                actionParams.Action = action;

                var needTileN = true;
                switch (action)
                {
                    case "discard":
                        break;
                    default:
                        needTileN = false;
                        break;
                }

                if (needTileN)
                {
                    var secondPattern = @"^\[[^\s]+\s(?<paramA>.+)\]$";
                    rgx = new Regex(secondPattern, RegexOptions.IgnoreCase);
                    matches = rgx.Matches(msgTips);
                    match = matches[0];
                    var paramA = match.Groups["paramA"].Value;
                    paramA = paramA.Trim();
                    //Console.WriteLine(paramA);
                    actionParams.TileStrings = paramA.Split(' ');
                }

                if (action == "discard")
                {
                    var secondPattern = @"^\[.*richi\s(?<paramB>[^\s]+).*\]$";
                    rgx = new Regex(secondPattern, RegexOptions.IgnoreCase);
                    matches = rgx.Matches(msgTips);
                    if (matches.Count > 0)
                    {
                        match = matches[0];
                        var paramB = match.Groups["paramB"].Value;
                        //Console.WriteLine(paramB);
                        actionParams.HasRichi = true;
                        actionParams.RichiFlags = int.Parse(paramB);
                    }
                }
            }

            return actionParams;
        }

        private void OnAuoX_Button_Click(object sender, RoutedEventArgs e)
        {
            IsAutoX = !IsAutoX;
            if (!IsAutoX)
            {
                AutoX.Content = "AutoX";
            }
            else
            {
                AutoX.Content = "C-AutoX";
            }
        }

        public bool IsAutoX { get; set; }
    }
}
