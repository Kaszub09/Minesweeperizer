using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net;

namespace Minesweeperizer {
    /// <summary>
    /// Logika interakcji dla klasy Window1.xaml
    /// </summary>
    public partial class WindowFFMPEG : Window {
        public WindowFFMPEG() {
            InitializeComponent();
        }

        private void ButtonIHave_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Multiselect = false,
                Filter = "Exe | *.exe;"
            };
            if (openFileDialog.ShowDialog() == true) {
                MainWindow w = (MainWindow)Application.Current.MainWindow;
                w.ffmpeg = openFileDialog.FileName;
                Close();
            }
        }
    }
}
