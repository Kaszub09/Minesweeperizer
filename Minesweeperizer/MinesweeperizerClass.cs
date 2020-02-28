using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Minesweeperizer {
    static public class MinesweeperizerClass {
        static ColorMatrix grayMatrix;
        static ImageAttributes attributes;
        static int flag, width, height,squareSize;
        static int[,] board;
        static Dictionary<string, Bitmap> squares;
        static MinesweeperizerClass() {
            grayMatrix = new ColorMatrix(
                new float[][] {
                    new float[] {0.299f, 0.299f, 0.299f,0,0},
                    new float[] {0.587f, 0.587f, 0.587f, 0,0},
                    new float[] {0.114f, 0.114f, 0.114f, 0,0},
                    new float[] {0, 0, 0,1,0},
                    new float[] {0, 0, 0,0,1},
                });
            attributes = new ImageAttributes();
            attributes.SetColorMatrix(grayMatrix);
        }

        static public Bitmap Minesweeperize(Bitmap source, Dictionary<string, Bitmap> squaresArgu, int squareSizeArgu, int threshold, int heightArgu,int flagsArgu,bool addFrame,int leftNumber=-1, int rightNumber = -1) {
            height = heightArgu;
            flag = flagsArgu;
            width = (int)(source.Width*height/source.Height);
            Bitmap sourceSmall = new Bitmap(source, width, height);
            board = new int[width, height];
            squares = squaresArgu;
            squareSize = squareSizeArgu;
            //using(Graphics grap = Graphics.FromImage(source)) {
            //    attributes.SetThreshold(((float)threshold) / 255);
            //    grap.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            //}

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Color c = sourceSmall.GetPixel(x, y);
                    board[x, y] = (c.R + c.G + c.B) / 3 > threshold ? 9 : 0;
                }
            }

            sourceSmall.Dispose();

            CalculateBombs();

            Bitmap outputWithoutFrame = new Bitmap(width * squareSize, height * squareSize);

            using (Graphics graphics = Graphics.FromImage(outputWithoutFrame)) {
                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        graphics.DrawImage(squares[board[x, y].ToString()], x * squareSize, y * squareSize, squareSize, squareSize);
                    }
                }
            }

            if (!addFrame) {
                return outputWithoutFrame;
            }
            else {
                if(leftNumber < 0)
                    for (int x = 0; x < width; x++) {
                        for (int y = 0; y < height; y++) {
                            if (board[x, y] >= 9) {
                                leftNumber++;
                            }
                        }
                    }
                if(rightNumber < 0 )
                    rightNumber = new Random().Next(1000);
                
                Bitmap outputWithFrame = new Bitmap((width + 2) * squareSize, (height + 6) * squareSize);

                using (Graphics graphics = Graphics.FromImage(outputWithFrame)) {
                    graphics.DrawImage(outputWithoutFrame, squareSize, 5 * squareSize, width * squareSize, height * squareSize);
                    graphics.DrawImage(squares["top_left_corner"], 0, 0, squareSize, squareSize);
                    graphics.DrawImage(squares["top_right_corner"], (width + 1) * squareSize, 0, squareSize, squareSize);
                    graphics.DrawImage(squares["left_down_corner"], 0, (height + 5) * squareSize, squareSize, squareSize);
                    graphics.DrawImage(squares["right_down_corner"], (width + 1) * squareSize, (height + 5) * squareSize, squareSize, squareSize);
                    for (int x = 1; x < width + 1; x++) {
                        graphics.DrawImage(squares["horizontal_wall"], x * squareSize, 0, squareSize, squareSize);
                        graphics.DrawImage(squares["empty"], x * squareSize, 1 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["empty"], x * squareSize, 2 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["empty"], x * squareSize, 3 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["horizontal_wall"], x * squareSize, 4 * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["horizontal_wall"], x * squareSize, (height + 5) * squareSize, squareSize, squareSize);
                    }
                    for (int y = 1; y < height + 5; y++) {
                        graphics.DrawImage(squares["vertical_wall"], 0, y * squareSize, squareSize, squareSize);
                        graphics.DrawImage(squares["vertical_wall"], (width + 1) * squareSize, y * squareSize, squareSize, squareSize);
                    }
                    graphics.DrawImage(squares["left_middle_wall"], 0, 4 * squareSize, squareSize, squareSize);
                    graphics.DrawImage(squares["right_middle_wall"], (width + 1) * squareSize, 4 * squareSize, squareSize, squareSize);
                    graphics.DrawImage(squares["smile"], (width + 2) * squareSize / 2 - squareSize, 3 * squareSize / 2, 2 * squareSize, 2 * squareSize);

                    graphics.DrawImage(GetBitmapWithNumber(leftNumber, true), 3 * squareSize / 2, 3 * squareSize / 2, 4 * squareSize, 2 * squareSize);
                    graphics.DrawImage(GetBitmapWithNumber(rightNumber, false), (width + 2) * squareSize - 3 * squareSize / 2 - 3 * squareSize, 3 * squareSize / 2, 3 * squareSize, 2 * squareSize);                 
                }
                outputWithoutFrame.Dispose();
                return outputWithFrame;
            }
        }

        private static Bitmap GetBitmapWithNumber(int number, bool left) {
            Bitmap BitmapWithNumber = new Bitmap(squareSize * (left ? 4 : 3), 2 * squareSize);
            int[] numbers = new int[4];
            numbers[0] = number % 10;
            numbers[1] = (number / 10) % 10;
            numbers[2] = (number / 100) % 10;
            numbers[3] = (number / 1000) % 10;
            using (Graphics graphics = Graphics.FromImage(BitmapWithNumber)) {
                if (left) {
                    graphics.DrawImage(squares["number" + numbers[3].ToString()], 0, 0, squareSize, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[2].ToString()], squareSize, 0, squareSize, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[1].ToString()], 2 * squareSize, 0, squareSize, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[0].ToString()], 3 * squareSize, 0, squareSize, 2 * squareSize);
                }
                else {
                    graphics.DrawImage(squares["number" + numbers[2].ToString()], 0, 0, squareSize, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[1].ToString()], squareSize, 0, squareSize, 2 * squareSize);
                    graphics.DrawImage(squares["number" + numbers[0].ToString()], 2 * squareSize, 0, squareSize, 2 * squareSize);
                }
            }
            return BitmapWithNumber;
        }

        private static void CalculateBombs() {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    int count = 0;
                    if (board[x, y] != 9) {
                        for (int i = -1; i <= 1; i++) {
                            for (int j = -1; j <= 1; j++) {
                                if (i + x >= 0 && i + x < width && j + y >= 0 && j + y < height) {
                                    if (board[i + x, j + y] == 9) {
                                        count++;
                                    }
                                }

                            }
                        }
                        board[x, y] = count;
                    }
                }
            }
            if (flag == 1 || flag == 2) {
                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        if (board[x, y] == 9) {
                            if (flag == 2) {
                                board[x, y] = 10;
                            }
                            else {
                                for (int i = -1; i <= 1; i++) {
                                    for (int j = -1; j <= 1; j++) {
                                        if (i + x >= 0 && i + x < width && j + y >= 0 && j + y < height) {
                                            if (board[i + x, j + y] < 9) {
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
    }
}
