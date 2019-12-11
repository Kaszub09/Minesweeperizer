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
using System.Diagnostics;

namespace Minesweeperizer
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string videoFilepath;
        List<string> imagesFilepaths;
        Dictionary<string, Bitmap> squares;
        int width, height, squareSize, flags;
        Progress<float> progressDrawing;
        bool saveRecognised, addFrame;
        int numberPixelWidth, leftNumber, rightNumber;

        public MainWindow()
        {
            InitializeComponent();
            squares = new Dictionary<string, Bitmap>();
            imagesFilepaths = new List<string>();
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
                    if (Path.GetFileName(d) != "temporaryCatalog")
                    {
                        comboBoxChooseStyle.Items.Add(new ComboBoxItem { Content = Path.GetFileName(d) });
                        if (Path.GetFileName(d) == "default")
                            comboBoxChooseStyle.SelectedIndex = comboBoxChooseStyle.Items.Count - 1;
                    }
                }
                if (comboBoxChooseStyle.SelectedItem == null)
                {
                    comboBoxChooseStyle.SelectedIndex = 0;
                }
            }
            catch (DirectoryNotFoundException e)
            {
                labelInfo.Content = "Couldn't find any themes.";
                ChangeRelevantButtonState(false);
            }
        }

        private void ButtonSourceChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.jpg, *.jpeg, *.bmp, *.gif, *.png) | *.jpg; *.jpeg; *.bmp; *.gif; *.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                imagesFilepaths = new List<string>();
                
                StringBuilder s = new StringBuilder();
                foreach (string filepath in openFileDialog.FileNames)
                {
                    imagesFilepaths.Add(filepath);
                    s.Append(Path.GetFileName(filepath));
                    s.Append("\n"); 
                }
                TextBoxFilepathsImage.Text = s.ToString();
            }
        }

        private void ButtonSourceChooseVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Video files (*.mp4, *.mkv, *.webm, *.avi) | *.mp4; *.mkv; *.webm; *.avi;"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                videoFilepath = openFileDialog.FileName;
                TextBoxFilepathsVideo.Text = videoFilepath.ToString();
            }
        }

        private async void ButtonMinesweeperizeImage_Click(object sender, RoutedEventArgs e)
        {
            if (imagesFilepaths.Count == 0)
            {
                labelInfo.Content = "Choose images to process.";
                return;
            }

            addFrame = (bool)CheckBoxAddFrame.IsChecked;
            flags = comBoxFlag.SelectedIndex;
            saveRecognised = (bool)CheckBoxSaveRecognised.IsChecked;

            if (!Int32.TryParse(textBoxHeight.Text, out height) || !Int32.TryParse(textBoxSize.Text, out squareSize) || height < 1 || squareSize < 1)
            {
                labelInfo.Content = "Incorrect height\\size value.";
                return;
            }
            
            if (!LoadGraphics())
            {
                return;
            }

            ChangeRelevantButtonState(false);
          
            for (int i = 0; i < imagesFilepaths.Count; i++)
            {
                labelInfo.Content = "Processing file " + (i + 1).ToString() + " of " + imagesFilepaths.Count.ToString() + ".";
                await Task.Run(() => Minesweeperize(imagesFilepaths[i],true));
            }
            labelInfo.Content = "Done!";
            ChangeRelevantButtonState(true);
        }

        private void ButtonMinesweeperizeVideo_Click(object sender, RoutedEventArgs e)
        {
            if (videoFilepath == null)
            {
                labelInfo.Content = "Choose video to process.";
                return;
            }

            addFrame = (bool)CheckBoxAddFrame.IsChecked;
            flags = comBoxFlag.SelectedIndex;

            if (!Int32.TryParse(textBoxHeight.Text, out height) || !Int32.TryParse(textBoxSize.Text, out squareSize) || height < 1 || squareSize < 1)
            {
                labelInfo.Content = "Incorrect height\\size value.";
                return;
            }

            if (!LoadGraphics())
            {
                return;
            }

            ChangeRelevantButtonState(false);
            MinesweeperizeVideo();
            ChangeRelevantButtonState(true);
        }

        private async void MinesweeperizeVideo()
        {
            Double fps = int.Parse(TextBoxVideoFps.Text);

            ProcessStartInfo p = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\",
                FileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\ffmpeg.exe",
                Arguments = "-i \"" + videoFilepath + "\"" + " temporaryCatalog\\%05d.png"
            };
            var procIN = System.Diagnostics.Process.Start(p);
            procIN.WaitForExit();

            List<string> videoImagesFilepaths= new List<string>(Directory.GetFiles("minesweeperizer\\temporaryCatalog"));
            leftNumber = 0;
            rightNumber = 0;

            for (int i = 0; i < videoImagesFilepaths.Count; i++)
            {
                leftNumber++;
                rightNumber = leftNumber / (int)(Math.Round(fps));
                labelInfo.Content = "Processing image " + (i + 1).ToString() + " of " + videoImagesFilepaths.Count.ToString() + ".";
                await Task.Run(() => Minesweeperize(videoImagesFilepaths[i], false));
                ((IProgress<float>)progressDrawing).Report(100*i/ videoImagesFilepaths.Count);
            }

            p = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\",
                FileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\ffmpeg.exe",
                Arguments = "-framerate " + fps.ToString() + " -i \"temporaryCatalog\\%05d (minesweeperized).png\" -i \"" + videoFilepath +
                "\" -c:a copy -c:v libx264  \"" +
                videoFilepath.Remove(videoFilepath.Length - 5, 4) + " (minesweeperized).mp4\""
            };
            var procOUT = System.Diagnostics.Process.Start(p);
            procOUT.WaitForExit();

            labelInfo.Content = "Cleaning...";
            ((IProgress<float>)progressDrawing).Report(100);
            await Task.Run(() => {
                System.GC.Collect();
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\temporaryCatalog");
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }
            });

            labelInfo.Content = "Done.";
            ((IProgress<float>)progressDrawing).Report(0);


        }

        private void ChangeRelevantButtonState(bool v)
        {
            TabControlMain.IsEnabled = v;
        }


        private void Minesweeperize(string ImageLocation,bool image)
        {
            Bitmap temp = new Bitmap(ImageLocation);
            double tx = temp.Width, ty = temp.Height, h = height;
            width = (int)(tx*h/ty);
            Bitmap source = new Bitmap(temp, width, height);
            int [,] board = new int[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = source.GetPixel(x, y);
                    board[x, y] = (c.R + c.G + c.B) / 3 > 127 ? 9 : 0;
                }
            }

            board = CalculateBombs(board,flags);

            if (image && saveRecognised)
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
                    outputRecognised.Save(ImageLocation.Remove(ImageLocation.Length - System.IO.Path.GetExtension(ImageLocation).Length) + " (recognised)" + ".png", ImageFormat.Png);
                }
            }

            Bitmap outputWithoutFrame = new Bitmap(width * squareSize, height * squareSize);

            using (Graphics graphics = Graphics.FromImage(outputWithoutFrame))
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        graphics.DrawImage(squares[board[x, y].ToString()], x * squareSize, y * squareSize, squareSize, squareSize);
                    }
                }                
            }

            if (!addFrame)
            {
                outputWithoutFrame.Save(ImageLocation.Remove(ImageLocation.Length - Path.GetExtension(ImageLocation).Length) + " (minesweeperized)" + ".png", ImageFormat.Png);
            }
            else
            {
                if (image)
                {
                    leftNumber = 0;
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (board[x, y] >= 9)
                            {
                                leftNumber++;
                            }
                        }
                    }
                    rightNumber = new Random().Next(1000);
                }
                Bitmap outputWithFrame = new Bitmap((width+2) * squareSize, (height+6) * squareSize);
                using (Graphics graphics = Graphics.FromImage(outputWithFrame))
                {
                    graphics.DrawImage(outputWithoutFrame,  squareSize, 5 * squareSize, width*squareSize, height*squareSize);
                    graphics.DrawImage(squares["top_left_corner"],   0,   0, squareSize, squareSize);
                    graphics.DrawImage(squares["top_right_corner"], (width + 1) * squareSize, 0, squareSize, squareSize);
                    graphics.DrawImage(squares["left_down_corner"], 0, (height + 5) * squareSize, squareSize, squareSize);
                    graphics.DrawImage(squares["right_down_corner"], (width + 1) * squareSize, (height + 5) * squareSize, squareSize, squareSize);
                    for (int x = 1; x < width+1; x++)
                    {
                        graphics.DrawImage(squares["horizontal_wall"], x* squareSize, 0, squareSize, squareSize);
                        graphics.DrawImage(squares["empty"], x * squareSize, 1 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["empty"], x * squareSize, 2 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["empty"], x * squareSize, 3 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["horizontal_wall"], x * squareSize, 4*squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["horizontal_wall"], x * squareSize, (height + 5) * squareSize, squareSize, squareSize);
                    }
                    for (int y = 1; y < height + 5;y++)
                    {
                        graphics.DrawImage(squares["vertical_wall"], 0, y * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["vertical_wall"], (width+1) * squareSize, y*squareSize, squareSize, squareSize);
                    }
                    graphics.DrawImage(squares["left_middle_wall"], 0, 4 * squareSize, squareSize, squareSize);
                    graphics.DrawImage(squares["right_middle_wall"], (width + 1) * squareSize, 4 * squareSize, squareSize, squareSize);
                    graphics.DrawImage(squares["smile"], (width + 2) * squareSize/2 - squareSize, 3*squareSize/2, 2 * squareSize, 2 * squareSize);

                    graphics.DrawImage(GetBitmapWithNumber(leftNumber, true), 3 * squareSize / 2, 3 * squareSize / 2, 4*numberPixelWidth, 2 * squareSize);
                    graphics.DrawImage(GetBitmapWithNumber(rightNumber, false), (width + 2) * squareSize - 3 * squareSize / 2 - 3 * numberPixelWidth, 3 * squareSize / 2, 3*numberPixelWidth, 2 * squareSize);

                    outputWithFrame.Save(ImageLocation.Remove(ImageLocation.Length - Path.GetExtension(ImageLocation).Length) + " (minesweeperized)" + ".png", ImageFormat.Png);
                }
            }
        }

        private Bitmap GetBitmapWithNumber(int number,bool fourNumbers)
        {
            Bitmap BitmapWithNumber = new Bitmap(numberPixelWidth * (fourNumbers?4:3), 2 * squareSize);
            int[] numbers=new int[4];
            numbers[0] = number % 10;
            numbers[1] = (number/10) % 10;
            numbers[2] = (number / 100) % 10;
            numbers[3] = (number / 1000) % 10;
            using (Graphics graphics = Graphics.FromImage(BitmapWithNumber))
            {
                if (fourNumbers)
                {
                    graphics.DrawImage(squares["number" + numbers[3].ToString()], 0, 0, numberPixelWidth, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[2].ToString()], numberPixelWidth, 0, numberPixelWidth, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[1].ToString()], 2 * numberPixelWidth, 0, numberPixelWidth, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[0].ToString()], 3 * numberPixelWidth, 0, numberPixelWidth, 2 * squareSize);
                }
                else
                {
                    graphics.DrawImage(squares["number" + numbers[2].ToString()], 0, 0, numberPixelWidth, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[1].ToString()], numberPixelWidth, 0, numberPixelWidth, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[0].ToString()], 2 * numberPixelWidth, 0, numberPixelWidth, 2 * squareSize);
                }
            }
            return BitmapWithNumber;
        }

        private int [,] CalculateBombs(int [,] board,int flag)
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
            if(flag == 1 || flag == 2)
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
            return board;
        }

        private bool LoadGraphics()
        {
            if (comboBoxChooseStyle.Items.IsEmpty)
            {
                return true;
            }
            else
            {
                squares.Clear();
                try
                {
                    string temp = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                            + "\\minesweeperizer\\" + ((ComboBoxItem)comboBoxChooseStyle.SelectedItem).Content.ToString() + "\\";
                    for (int i = 0; i <= 10; i++)
                    {
                        squares.Add(i.ToString(), new Bitmap(new Bitmap(temp+ i.ToString() + ".png"), squareSize, squareSize));
                    }

                    if (addFrame)
                    {
                        string[] parts = { "left_down_corner", "right_down_corner","vertical_wall","horizontal_wall","top_left_corner","top_right_corner",
                        "empty","right_middle_wall","left_middle_wall"};
                        foreach (string s in parts)
                        {
                            squares.Add(s, new Bitmap(new Bitmap(temp + s + ".png"), squareSize, squareSize));
                        }

                        numberPixelWidth = 2 * squareSize * 13 / 23;
                        string[] numbers = { "number0", "number1", "number2", "number3", "number4", "number5", "number6", "number7", "number8", "number9"};
                        foreach (string s in numbers)
                        {
                            squares.Add(s, new Bitmap(new Bitmap(temp + s + ".png"), numberPixelWidth, 2*squareSize));
                        }

                        squares.Add("smile", new Bitmap(new Bitmap(temp + "smile" + ".png"), 2 * squareSize, 2 * squareSize));
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
