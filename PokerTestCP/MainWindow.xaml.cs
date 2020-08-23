using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using CsvHelper;
using Xproto;
using Newtonsoft.Json.Linq;

namespace PokerTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string Version = "1.13";
        public const string ToolName = "Chinise Poker";
        public MainWindow()
        {
            InitIdNames();
            DataContext = this;
            InitializeComponent();

            LoadMahjongPics();

            HideTileStackWnds();

            ProgramConfig.LoadConfigFromFile();

            UpdateTile();
        }

        private void UpdateTile()
        {
            Title = $"PokerTestTool-[{ToolName}](ver:{Version})[{ProgramConfig.ServerUrl}]";
        }

        private readonly List<Player> _players = new List<Player>();

        public Dictionary<int, BitmapImage> ImageDict { get;  } = new Dictionary<int, BitmapImage>();

        //private DispatcherTimer _dispatcherTimer;
        private void OnUploadCfgFile_Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "origin"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "CSV documents (.csv)|*.csv"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                try
                {
                    // Open document
                    var filePath = dlg.FileName;
                    HttpHandlers.SendFileContent(filePath, this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OnSinglePlayer_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_players.Count == 4)
            {
                MessageBox.Show("already 4 players");
                return;
            }

            var wnds = new TileStackWnd[4] { Auc, Buc, Cuc, Duc };
            TileStackWnd freeWnd = null;
            foreach (var wnd in wnds)
            {
                var p = _players.Find((x) => x.MyWnd == wnd);
                if (p == null)
                {
                    freeWnd = wnd;
                    break;
                }
            }

            if (freeWnd == null)
            {
                MessageBox.Show("no free player-view to use");
                return;
            }

            var inputWnd = new InputWnd {Owner = this};
            var result = inputWnd.ShowDialog();
            if (result == false)
            {
                return;
            }

            var userId = inputWnd.TextBoxUserId.Text;
            var tableNumber = inputWnd.TextBoxTableId.Text;
            var player = new Player(userId, userId, tableNumber, freeWnd, this);
            player.Connect();

            _players.Add(player);
        }

        public void OnPlayerDisconnected(Player player)
        {
            _players.Remove(player);
        }

        private void HideTileStackWnds()
        {
            var wnds = new TileStackWnd[4] { Auc, Buc, Cuc, Duc };
            foreach (var wnd in wnds)
            {
                wnd.Visibility = Visibility.Collapsed;
            }
        }

        private void OnStartGame_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_players.Count == 0)
            {
                //BuildPlayers();
            }
            else
            {
                foreach (var player in _players)
                {
                    player.SendReady2Server();
                }
            }
            //else if (_players.Count != CurrentDealCfg.PlayerCount)
            //{
            //    _players.ForEach(KillPlayer);
            //    _players.Clear();
            //    BuildPlayers();
            //}

            //if (string.IsNullOrWhiteSpace(CurrentDealCfg.Name))
            //{
            //    MessageBox.Show("no config to start game");
            //    return;
            //}

            //HttpHandlers.SendPostMethod(@"/start", CurrentDealCfg.Name);
        }

        private void OnExportTableCfg_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputWnd = new InputWnd { Owner = this, IsNeedUserId = false };
                var result = inputWnd.ShowDialog();
                if (result == false)
                {
                    return;
                }

                var tableNumber = inputWnd.TextBoxTableId.Text;

                HttpHandlers.SendPostMethod("/resetTable", tableNumber, "&tableNumber=" + tableNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnExportTableOps_Button_Click(object sender, RoutedEventArgs e)
        {
            ExportTableWnd.ShowExportDialog(ExportTableWnd.ExportTableType.Operations, this);
        }

        public ActionListWnd AlWnd { get; private set; }

        private void OnSelectCfg_Button_Click(object sender, RoutedEventArgs e)
        {
            //  选择配置
            //if (DealCfgs.Count < 1)
            //    return;
            //int newSelected;
            //if (CfgSelectWnd.ShowDialog(DealCfgs, TileCfgIndex,out newSelected, this))
            //{
            //    TileCfgIndex = newSelected;
            //    TbCurrentCfg.Text = CurrentDealCfg.Name;
            //}

            var hostWnd = new HostWnd { Owner = this };
            var result = hostWnd.ShowDialog();
            if (result == false)
            {
                return;
            }

            ProgramConfig.ServerUrl = hostWnd.HostUrl;
            ProgramConfig.SaveConfig2File();

            UpdateTile();
        }

        private void OnCreateTable_Button_Click(object sender, RoutedEventArgs e)
        {
            HttpHandlers.SendPostMethod(@"/monkey/create-monkey-table", "", "&tableID=monkey-table");
        }

        private void OnDestroyTable_Button_Click(object sender, RoutedEventArgs e)
        {
            HttpHandlers.SendPostMethod(@"/monkey/destroy-monkey-table", "", "&tableID=monkey-table");
            IsPlaying = false;
        }

        private void LoadMahjongPics()
        {
            var dir = Environment.CurrentDirectory;
            ImageDict.Clear();
            var suitCh = new string[] {"红桃", "方片", "草花", "黑桃", };
            var rankNames = new string[] { "2", "3", "4", "5", "6","7", "8", "9", "10", "J","Q","K","A"};
            // 4张牌
            var index = 0;
            for (var i = 0; i < 13; i++)
            {
                for (var suit = 0; suit < 4; suit++)
                {
                    var suitC = suitCh[suit];
                    var rank = rankNames[i];
                    var fileName = $"{suitC}{rank}.png";
                    var path = System.IO.Path.Combine(dir, "images", fileName);

                    var bitmap = new BitmapImage(new Uri(path));
                    ImageDict.Add(index++, bitmap);

                }
            }

            // 大小wang
            {
                var jokersName = new string[] { "小王", "大王" };
                for (var suit = 0; suit < 2; suit++)
                {
                    var joker = jokersName[suit];
                    
                    var fileName = $"{joker}.png";
                    var path = System.IO.Path.Combine(dir, "images", fileName);

                    var bitmap = new BitmapImage(new Uri(path));
                    ImageDict.Add(index++, bitmap);

                }
            }

            Auc.SetImageSrc(ImageDict, this);
            Buc.SetImageSrc(ImageDict, this);
            Cuc.SetImageSrc(ImageDict, this);
            Duc.SetImageSrc(ImageDict, this);
        }


        public void AppendLog(string logMsg)
        {
            TbLogger.AppendText(logMsg);
            TbLogger.ScrollToEnd();
        }
 
        public Dictionary<int, string> IdNames { get; } = new Dictionary<int, string>();
        public Dictionary<string, int> NameIds { get; } = new Dictionary<string, int>();
        public bool IsPlaying { get; set; }

        public void InitIdNames()
        {
            NameIds["红桃2"] = (int)CardID.R2H;
            NameIds["方块2"] = (int)CardID.R2D;
            NameIds["梅花2"] = (int)CardID.R2C;
            NameIds["黑桃2"] = (int)CardID.R2S;

            NameIds["红桃3"] = (int)CardID.R3H;
            NameIds["方块3"] = (int)CardID.R3D;
            NameIds["梅花3"] = (int)CardID.R3C;
            NameIds["黑桃3"] = (int)CardID.R3S;

            NameIds["红桃4"] = (int)CardID.R4H;
            NameIds["方块4"] = (int)CardID.R4D;
            NameIds["梅花4"] = (int)CardID.R4C;
            NameIds["黑桃4"] = (int)CardID.R4S;

            NameIds["红桃5"] = (int)CardID.R5H;
            NameIds["方块5"] = (int)CardID.R5D;
            NameIds["梅花5"] = (int)CardID.R5C;
            NameIds["黑桃5"] = (int)CardID.R5S;

            NameIds["红桃6"] = (int)CardID.R6H;
            NameIds["方块6"] = (int)CardID.R6D;
            NameIds["梅花6"] = (int)CardID.R6C;
            NameIds["黑桃6"] = (int)CardID.R6S;

            NameIds["红桃7"] = (int)CardID.R7H;
            NameIds["方块7"] = (int)CardID.R7D;
            NameIds["梅花7"] = (int)CardID.R7C;
            NameIds["黑桃7"] = (int)CardID.R7S;

            NameIds["红桃8"] = (int)CardID.R8H;
            NameIds["方块8"] = (int)CardID.R8D;
            NameIds["梅花8"] = (int)CardID.R8C;
            NameIds["黑桃8"] = (int)CardID.R8S;

            NameIds["红桃9"] = (int)CardID.R9H;
            NameIds["方块9"] = (int)CardID.R9D;
            NameIds["梅花9"] = (int)CardID.R9C;
            NameIds["黑桃9"] = (int)CardID.R9S;

            NameIds["红桃10"] = (int)CardID.R10H;
            NameIds["方块10"] = (int)CardID.R10D;
            NameIds["梅花10"] = (int)CardID.R10C;
            NameIds["黑桃10"] = (int)CardID.R10S;

            NameIds["红桃J"] = (int)CardID.Jh;
            NameIds["方块J"] = (int)CardID.Jd;
            NameIds["梅花J"] = (int)CardID.Jc;
            NameIds["黑桃J"] = (int)CardID.Js;

            NameIds["红桃Q"] = (int)CardID.Qh;
            NameIds["方块Q"] = (int)CardID.Qd;
            NameIds["梅花Q"] = (int)CardID.Qc;
            NameIds["黑桃Q"] = (int)CardID.Qs;

            NameIds["红桃K"] = (int)CardID.Kh;
            NameIds["方块K"] = (int)CardID.Kd;
            NameIds["梅花K"] = (int)CardID.Kc;
            NameIds["黑桃K"] = (int)CardID.Ks;

            NameIds["红桃A"] = (int)CardID.Ah; 
            NameIds["方块A"] = (int)CardID.Ad; 
            NameIds["梅花A"] = (int)CardID.Ac; 
            NameIds["黑桃A"] = (int)CardID.As; 

            NameIds["黑小丑"] = (int)CardID.Job;
            NameIds["红小丑"] = (int)CardID.Jor;

            IdNames[(int)CardID.R2H] = "红桃2";
            IdNames[(int)CardID.R2D] = "方块2";
            IdNames[(int)CardID.R2C] = "梅花2";
            IdNames[(int)CardID.R2S] = "黑桃2";

            IdNames[(int)CardID.R3H] = "红桃3";
            IdNames[(int)CardID.R3D] = "方块3";
            IdNames[(int)CardID.R3C] = "梅花3";
            IdNames[(int)CardID.R3S] = "黑桃3";

            IdNames[(int)CardID.R4H] = "红桃4";
            IdNames[(int)CardID.R4D] = "方块4";
            IdNames[(int)CardID.R4C] = "梅花4";
            IdNames[(int)CardID.R4S] = "黑桃4";

            IdNames[(int)CardID.R5H] = "红桃5";
            IdNames[(int)CardID.R5D] = "方块5";
            IdNames[(int)CardID.R5C] = "梅花5";
            IdNames[(int)CardID.R5S] = "黑桃5";

            IdNames[(int)CardID.R6H] = "红桃6";
            IdNames[(int)CardID.R6D] = "方块6";
            IdNames[(int)CardID.R6C] = "梅花6";
            IdNames[(int)CardID.R6S] = "黑桃6";

            IdNames[(int)CardID.R7H] = "红桃7";
            IdNames[(int)CardID.R7D] = "方块7";
            IdNames[(int)CardID.R7C] = "梅花7";
            IdNames[(int)CardID.R7S] = "黑桃7";

            IdNames[(int)CardID.R8H] = "红桃8";
            IdNames[(int)CardID.R8D] = "方块8";
            IdNames[(int)CardID.R8C] = "梅花8";
            IdNames[(int)CardID.R8S] = "黑桃8";

            IdNames[(int)CardID.R9H] = "红桃9";
            IdNames[(int)CardID.R9D] = "方块9";
            IdNames[(int)CardID.R9C] = "梅花9";
            IdNames[(int)CardID.R9S] = "黑桃9";

            IdNames[(int)CardID.R10H] = "红桃10";
            IdNames[(int)CardID.R10D] = "方块10";
            IdNames[(int)CardID.R10C] = "梅花10";
            IdNames[(int)CardID.R10S] = "黑桃10";

            IdNames[(int)CardID.Jh] = "红桃J";
            IdNames[(int)CardID.Jd] = "方块J";
            IdNames[(int)CardID.Jc] = "梅花J";
            IdNames[(int)CardID.Js] = "黑桃J";

            IdNames[(int)CardID.Qh] = "红桃Q";
            IdNames[(int)CardID.Qd] = "方块Q";
            IdNames[(int)CardID.Qc] = "梅花Q";
            IdNames[(int)CardID.Qs] = "黑桃Q";

            IdNames[(int)CardID.Kh] = "红桃K";
            IdNames[(int)CardID.Kd] = "方块K";
            IdNames[(int)CardID.Kc] = "梅花K";
            IdNames[(int)CardID.Ks] = "黑桃K";

            IdNames[(int)CardID.Ah] = "红桃A";
            IdNames[(int)CardID.Ad] = "方块A";
            IdNames[(int)CardID.Ac] = "梅花A";
            IdNames[(int)CardID.As] = "黑桃A";

            IdNames[(int)CardID.Job] = "黑小丑";
            IdNames[(int)CardID.Jor] = "红小丑";

        }

        private ScoreWnd _scoreWnd;
        public void ShowScoreWnd(string msg)
        {
            if (_scoreWnd == null)
                _scoreWnd = new ScoreWnd();

            _scoreWnd.ShowWithMsg(msg, this);
        }

        private void OnAttachDealCfg_Button_Click(object sender, RoutedEventArgs e)
        {
            //TestManualProto();
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "origin"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "CSV documents (.csv)|*.csv"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                try
                {
                    var inputWnd = new InputWnd { Owner = this, IsNeedUserId = false};
                    result = inputWnd.ShowDialog();
                    if (result == false)
                    {
                        return;
                    }

                    var tableNumber = inputWnd.TextBoxTableId.Text;
                    // Open document
                    var filePath = dlg.FileName;
                    HttpHandlers.SendFileContent2(filePath, tableNumber, HttpHandlers.PathAttachDealCfgFile, this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OnAttachTableCfg_Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "table"; // Default file name
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "JSON documents (.json)|*.json"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                try
                {
                    var inputWnd = new InputWnd { Owner = this, IsNeedUserId = false };
                    result = inputWnd.ShowDialog();
                    if (result == false)
                    {
                        return;
                    }

                    var tableNumber = inputWnd.TextBoxTableId.Text;
                    // Open document
                    var filePath = dlg.FileName;
                    HttpHandlers.SendFileContent2(filePath, tableNumber, HttpHandlers.PathAttachTableCfgFile, this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void OnKickAllInTable_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputWnd = new InputWnd { Owner = this, IsNeedUserId = false };
                var result = inputWnd.ShowDialog();
                if (result == false)
                {
                    return;
                }

                var tableNumber = inputWnd.TextBoxTableId.Text;

                HttpHandlers.SendPostMethod("/support/kickAll", tableNumber, "&tableNumber="+tableNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnDealCfg_Button_Click(object sender, RoutedEventArgs e)
        {
            var dealCfgWnd = new DealCfgWnd(this);
            dealCfgWnd.ShowDialog();
        }

        public bool IsFirstPlayer(Player myPlayer)
        {
            if (_players.Count < 1)
            {
                return false;
            }

            return _players[0] == myPlayer;
        }

        private void OnDisbandTable_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var inputWnd = new InputWnd { Owner = this, IsNeedUserId = false };
                //var result = inputWnd.ShowDialog();
                //if (result == false)
                //{
                //    return;
                //}

                //var tableNumber = inputWnd.TextBoxTableId.Text;

                //HttpHandlers.SendPostMethod("/support/disbandTable", tableNumber, "&tableNumber=" + tableNumber);
                if (_players.Count < 1)
                {
                    return;
                }
                var player = _players[0];
                player.SendMessage((int)MessageCode.OpdisbandRequest, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnTableCount_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpHandlers.SendGetMethod("/support/tableCount", null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnUserCount_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpHandlers.SendGetMethod("/support/userCount", null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnExceptionCount_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpHandlers.SendGetMethod("/support/tableException", null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnClearExceptionCount_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HttpHandlers.SendGetMethod("/support/clearTableException", null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
