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
        int  height, squareSize, flags,treshold;
        Progress<float> progressConverting;
        bool addFrame;
        int numberPixelWidth, leftNumber, rightNumber;
        public string ffmpeg;

        public MainWindow()
        {
            InitializeComponent();
            squares = new Dictionary<string, Bitmap>();
            imagesFilepaths = new List<string>();
            progressConverting = new Progress<float>(percent => progressBarDrawing.Value = percent);
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
            ffmpeg = "ffmpeg";
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

            if (!Int32.TryParse(textBoxHeight.Text, out height) || !Int32.TryParse(textBoxSize.Text, out squareSize) || height < 1 || squareSize < 1)
            {
                labelInfo.Content = "Incorrect height or size value.";
                return;
            }

            if (!Int32.TryParse(textBoxTreshold.Text, out treshold)) {
                labelInfo.Content = "Incorrect threshold value.";
                return;
            }

            if (!LoadGraphics())
            {
                labelInfo.Content = "Can't find all pictures of " + ((ComboBoxItem)comboBoxChooseStyle.SelectedItem).Content.ToString() + " theme.";
                return;
            }

            ChangeRelevantButtonState(false);
          
            for (int i = 0; i < imagesFilepaths.Count; i++)
            {
                labelInfo.Content = "Processing file " + (i + 1).ToString() + " of " + imagesFilepaths.Count.ToString() + ".";
                await Task.Run(() =>  {
                    Bitmap output = MinesweeperizerClass.Minesweeperize(new Bitmap(imagesFilepaths[i]), squares, squareSize, treshold, height, flags, addFrame);
                    output.Save(imagesFilepaths[i].Remove(imagesFilepaths[i].Length - Path.GetExtension(imagesFilepaths[i]).Length) + " (minesweeperized)" + ".png", ImageFormat.Png);
                });
                ((IProgress<float>)progressConverting).Report(100 * i / imagesFilepaths.Count);
            }
            labelInfo.Content = "Done!";
            ((IProgress<float>)progressConverting).Report(0);
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

            if (!Int32.TryParse(textBoxTreshold.Text, out treshold)) {
                labelInfo.Content = "Incorrect threshold value.";
                return;
            }

            if (!LoadGraphics())
            {
                return;
            }

            MinesweeperizeVideo();
        }

        private async void MinesweeperizeVideo()
        {
            ChangeRelevantButtonState(false);
            labelInfo.Content = "Cleaning temporaryCatalog...";
            double fps = 0;
            await Task.Run(() => {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\temporaryCatalog");
                foreach (FileInfo file in di.EnumerateFiles()) {
                    file.Delete();
                }
            });

            labelInfo.Content = "Reading fps info...";
            await Task.Run(() => {
                var p = new Process {
                    StartInfo = new ProcessStartInfo {
                        WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\",
                        FileName = ffmpeg,
                        Arguments = " -i \"" + videoFilepath + "\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                try {
                    p.Start();
                }
                catch (Exception e) {
                    WindowFFMPEG w = new WindowFFMPEG();
                    w.ShowDialog();
                    p.StartInfo.FileName = ffmpeg;
                    p.Start();
                }
                String line = p.StandardError.ReadToEnd();
                line = System.Text.RegularExpressions.Regex.Match(line, $", [0-9.]+? fps,").Value;
                line = line.Remove(line.Length - 5);
                line = line.Substring(2, line.Length - 2);
                p.WaitForExit();
                fps = Double.Parse(line);
            });

            labelInfo.Content = "Extracting frames from video...";
            await Task.Run(() => {
                ProcessStartInfo pExtract = new ProcessStartInfo {
                    WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\",
                    FileName = ffmpeg,
                    Arguments = " -i \"" + videoFilepath + "\" temporaryCatalog\\%05d.png"
                };
                    Process.Start(pExtract).WaitForExit();
            });

            List<string> videoImagesFilepaths= new List<string>(Directory.GetFiles("minesweeperizer\\temporaryCatalog"));
            leftNumber = 0;
            rightNumber = 0;

            for (int i = 0; i < videoImagesFilepaths.Count; i++)
            {
                leftNumber++;
                rightNumber = leftNumber / (int)(Math.Round(fps));
                labelInfo.Content = "Processing image " + (i + 1).ToString() + " of " + videoImagesFilepaths.Count.ToString() + ".";
                await Task.Run(() => {
                    Bitmap output = MinesweeperizerClass.Minesweeperize(new Bitmap(videoImagesFilepaths[i]), squares, squareSize, treshold, height, flags, addFrame,leftNumber,rightNumber);
                    output.Save(videoImagesFilepaths[i].Remove(videoImagesFilepaths[i].Length - Path.GetExtension(videoImagesFilepaths[i]).Length) + " (minesweeperized)" + ".png", ImageFormat.Png);

                });
                ((IProgress<float>)progressConverting).Report(100*i/ videoImagesFilepaths.Count);
            }

            labelInfo.Content = "Rendering video...";
            await Task.Run(() => {
                ProcessStartInfo pCompile = new ProcessStartInfo {
                    WorkingDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\minesweeperizer\\",
                    FileName = ffmpeg,
                    Arguments = "-framerate " + fps.ToString() + " -i \"temporaryCatalog\\%05d (minesweeperized).png\" -i \"" + videoFilepath +
                    "\" -c:a copy -c:v libx264  \"" +  videoFilepath.Remove(videoFilepath.Length - 5, 4) + " (minesweeperized).mp4\""
                };
                Process.Start(pCompile).WaitForExit();
            });

            labelInfo.Content = "Done.";
            ((IProgress<float>)progressConverting).Report(0);
            ChangeRelevantButtonState(true);
        }

        private void ChangeRelevantButtonState(bool v)
        {
            TabControlMain.IsEnabled = v;
            buttonMinesweeperizeImage.IsEnabled = v;
            buttonMinesweeperizeVideo.IsEnabled = v;
        }


        private void Minesweeperize(string ImageLocation)
        {
            Bitmap output = MinesweeperizerClass.Minesweeperize(new Bitmap(ImageLocation), squares, squareSize, treshold, height, flags, addFrame);
            output.Save(ImageLocation.Remove(ImageLocation.Length - Path.GetExtension(ImageLocation).Length) + " (minesweeperized)" + ".png", ImageFormat.Png);
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
                    for (int i = 0; i <= 9; i++)
                    {
                        squares.Add(i.ToString(), new Bitmap(new Bitmap(temp+ i.ToString() + ".png"), squareSize, squareSize));
                    }

                    if (flags != 0) {
                        squares.Add("10", new Bitmap(new Bitmap(temp + "10" + ".png"), squareSize, squareSize));
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
                    return false;
                }
                return true;
            }        
        }

    }
}
