using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.Resources;

using System.Windows.Shapes;
using System.Reflection;
using Path = System.IO.Path;

namespace Minesweeperizer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string currentFilepath;
        List<string> allFilepaths;
        int[,] board;
        Bitmap[] squares;
        int width, height, squareSize, flags;
        bool saveRecognised;
        Progress<float> progressDrawing;

        public MainWindow()
        {
            InitializeComponent();
            squares = new Bitmap[11];
            allFilepaths = new List<string>();
            progressDrawing = new Progress<float>(percent => progressBarDrawing.Value = percent);
            try
            {
                string[] themes = Directory.GetDirectories("minesweeperizer");
                if (themes.Length == 0)
                {
                    throw new DirectoryNotFoundException();
                }
                foreach (string d in themes)
                {
                    comboBoxChooseStyle.Items.Add(new ComboBoxItem { Content = Path.GetFileName(d) });
                    if (Path.GetFileName(d) == "default")
                        comboBoxChooseStyle.SelectedIndex = comboBoxChooseStyle.Items.Count - 1;
                }
                if (comboBoxChooseStyle.SelectedItem == null)
                {
                    comboBoxChooseStyle.SelectedIndex = 0;
                }
            }
            catch (DirectoryNotFoundException e)
            {
                comboBoxChooseStyle.IsEnabled = false;
                for (int i = 0; i <=10; i++)
                {
                    var bitmapImage = new BitmapImage(new Uri(@"pack://application:,,,/" 
                        + Assembly.GetExecutingAssembly().GetName().Name
                        + ";component/"
                        + "Images/"+i.ToString()+".png", UriKind.Absolute));
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapImage)bitmapImage));
                    var stream = new MemoryStream();
                    encoder.Save(stream);
                    stream.Flush();
                    squares[i] = new Bitmap(new Bitmap(stream),40,40);

                }
                labelInfo.Content = "Couldn't find any themes. Only default theme avaible.";
            }
        }

        private void ButtonSourceChoose_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.jpg, *.jpeg, *.bmp, *.gif, *.png) | *.jpg; *.jpeg; *.bmp; *.gif; *.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                allFilepaths = new List<string>();
                
                StringBuilder s = new StringBuilder();
                foreach (string filepath in openFileDialog.FileNames)
                {
                    allFilepaths.Add(filepath);
                    s.Append(Path.GetFileName(filepath));
                    s.Append("\n"); 
                }
                TextBoxFilepaths.Text = s.ToString();
            }
        }

        private async void ButtonMinesweeperize_Click(object sender, RoutedEventArgs e)
        {
            if (allFilepaths.Count == 0)
                labelInfo.Content = "Choose images to process.";
            if (!Int32.TryParse(textBoxWidth.Text, out width) || !Int32.TryParse(textBoxHeight.Text, out height) ||
                    !Int32.TryParse(textBoxSize.Text, out squareSize) || width < 1 || height < 1 || squareSize < 1)
            {
                labelInfo.Content = "Incorrect width\\height\\size value.";
                return;
            }
            if (!LoadGraphics())
            {
                return;
            }
            ChangeRelevantButtonState(false);
            board = new int[width, height];
            if (CheckBoxSaveRecognised.IsChecked == true)
            {
                saveRecognised = true;
            }
            else
            {
                saveRecognised = false;
            }
            flags = comBoxFlag.SelectedIndex;
            for (int i=0;i< allFilepaths.Count; i++)
            {
                currentFilepath = allFilepaths[i];
                labelInfo.Content = "Processing file " + (i+1).ToString() + " of " + allFilepaths.Count.ToString()+".";
                await Task.Run(()=> Minesweeperize());
            }
            labelInfo.Content = "Done!";
            ChangeRelevantButtonState(true);
        }

        private void ChangeRelevantButtonState(bool v)
        {
            buttonMinesweeperize.IsEnabled = v;
            buttonSourceChoose.IsEnabled = v;
        }

        private void Minesweeperize()
        {  
            Bitmap source = new Bitmap(new Bitmap(currentFilepath), width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = source.GetPixel(x, y);
                    board[x, y] = (c.R + c.G + c.B) / 3 > 127 ? 9 : 0;
                }
            }

            CalculateBombs();

            if (saveRecognised == true)
            {
                Bitmap outputRecognised = new Bitmap(width, height);
                using (Graphics graphics = Graphics.FromImage(outputRecognised))
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            outputRecognised.SetPixel(x, y, board[x, y] >8 ? Color.White : Color.Black);
                        }                     
                    }
                    outputRecognised.Save(currentFilepath.Remove(currentFilepath.Length - System.IO.Path.GetExtension(currentFilepath).Length) + " (recognised)" + ".png", ImageFormat.Png);
                }
            }

            Bitmap output = new Bitmap(width * squareSize, height * squareSize);
            using (Graphics graphics = Graphics.FromImage(output))
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        graphics.DrawImage(squares[board[x, y]], x * squareSize, y * squareSize, squareSize, squareSize);
                    }
                    ((IProgress<float>)progressDrawing).Report(((float)x) * 100 / width);
                }
                ((IProgress<float>)progressDrawing).Report(100);
                output.Save(currentFilepath.Remove(currentFilepath.Length - Path.GetExtension(currentFilepath).Length) + " (minesweeperized)" + ".png", ImageFormat.Png);
            }          
        }

        private void CalculateBombs()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int count = 0;
                    if (board[x, y] != 9)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (i + x >= 0 && i + x < width && j + y >= 0 && j + y < height)
                                {
                                    if (board[i + x, j + y] == 9)
                                    {
                                        count++;
                                    }
                                }

                            }
                        }
                        board[x, y] = count;
                    }
                }
            }
            if(flags == 1 || flags == 2)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (board[x, y] == 9)
                        {
                            if (flags == 2)
                            {
                                board[x, y] = 10;
                            }
                            else
                            {
                                for (int i = -1; i <= 1; i++)
                                {
                                    for (int j = -1; j <= 1; j++)
                                    {
                                        if (i + x >= 0 && i + x < width && j + y >= 0 && j + y < height)
                                        {
                                            if (board[i + x, j + y] < 9)
                                            {
                                                board[x, y] = 10;
                                            }
                                        }

                                    }
                                }
                               
                            }

                        }
                    }
                }
            }

        }

        private bool LoadGraphics()
        {
            if (comboBoxChooseStyle.Items.IsEmpty)
            {
                return true;
            }
            else
            {
                try
                {
                    for (int i = 0; i <= 10; i++)
                    {
                        squares[i] = new Bitmap(new Bitmap(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                            + "\\minesweeperizer\\" + ((ComboBoxItem)comboBoxChooseStyle.SelectedItem).Content.ToString() + "\\" + i.ToString() + ".png"), squareSize, squareSize);
                    }
                }
                catch (ArgumentException e)
                {
                    labelInfo.Content = "Can't find all pictures of "+ ((ComboBoxItem)comboBoxChooseStyle.SelectedItem).Content.ToString()+" theme.";
                    return false;
                }
                return true;
            }        
        }

    }
}
