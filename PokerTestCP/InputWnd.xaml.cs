using System.Windows;

namespace PokerTest
{
    /// <summary>
    /// InputWnd.xaml 的交互逻辑
    /// </summary>
    public partial class InputWnd : Window
    {
        public InputWnd()
        {
            IsNeedUserId = true;
            IsNeedTableId = true;
            InitializeComponent();

            TextBoxTableId.Text = ProgramConfig.RecentUsedTableNumber;
        }

        private bool _isNeedUserId;
        private bool _isNeedTableId;

        public bool IsNeedUserId
        {
            get { return _isNeedUserId; }
            set
            {
                _isNeedUserId = value;
                if (!_isNeedUserId)
                {
                    TextBoxUserId.IsEnabled = false;
                }
            }
        }

        public bool IsNeedTableId
        {
            get { return _isNeedTableId; }
            set
            {
                _isNeedTableId = value;
                if (!_isNeedTableId)
                {
                    TextBoxTableId.IsEnabled = false;
                }
            }
        }

        private void OnOK_Button_Clicked(object sender, RoutedEventArgs e)
        {
            if (IsNeedUserId && string.IsNullOrWhiteSpace(TextBoxUserId.Text))
            {
                MessageBox.Show("please input a valid userID");
                return;
            }
            if (IsNeedTableId && string.IsNullOrWhiteSpace(TextBoxTableId.Text))
            {
                MessageBox.Show("please input a valid tableID");
                return;
            }

            if (IsNeedTableId && !string.IsNullOrWhiteSpace(TextBoxTableId.Text))
            {
                ProgramConfig.RecentUsedTableNumber = TextBoxTableId.Text;
            }
            
            DialogResult = true;
        }

        private void OnCancel_Button_Clicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
