using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Content_Aware_Image_Scaling_Seam_Carving_
{
    public partial class Viewer : Form
    {
        int SCALE_RATE;
        Bitmap userImage;
        int[,] energyImage, minTable;
        Color[,] imgColors;
        int newWidth, newHeight;
        MethodInvoker updateGUI;

        public Viewer(Bitmap userImage, int width, int height, int rate)
        {
            InitializeComponent();
            SCALE_RATE = rate;
            this.userImage = userImage;
            newHeight = height;
            newWidth = width;
            backgroundWorker1.RunWorkerAsync();
        }
        
        private void fillColors()
        {
            imgColors = new Color[userImage.Width, userImage.Height];
            LockBitmap lImg = new LockBitmap(userImage);
            lImg.LockBits();
            for (int x = 0; x < userImage.Width; ++x)
                for (int y = 0; y < userImage.Height; ++y)
                    imgColors[x, y] = getCopy(lImg.GetPixel(x, y));
            lImg.UnlockBits();
        }
        public void commitScale()
        {
            fillColors();
            computeVert();
            computeHori();
            userImage = createNewImage();
        }
        private Bitmap createNewImage()
        {
            Bitmap resultImg = new Bitmap(imgColors.GetLength(0), imgColors.GetLength(1));
            int w = resultImg.Width, h = resultImg.Height;
            LockBitmap lockRes = new LockBitmap(resultImg);
            lockRes.LockBits();
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    lockRes.SetPixel(x, y, getCopy(imgColors[x, y]));
            lockRes.UnlockBits();
            return resultImg;
        }
        private Color[,] createNewColor(int w, int h)
        {
            Color[,] tmp = new Color[w, h];
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    tmp[x, y] = getCopy(imgColors[x, y]);
            return tmp;
        }
        public Bitmap getImage()
        {
            return userImage;
        }
        private void minSumTableVert()
        {
            int w = energyImage.GetLength(0);
            int h = energyImage.GetLength(1);
            minTable = new int[w, h];
            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    minTable[i, j] = energyImage[i, j];
                    if (j != 0)
                        if (i == 0)
                            minTable[i, j] += Math.Min(minTable[i, j - 1], minTable[i + 1, j - 1]);
                        else if (i == w - 1)
                            minTable[i, j] += Math.Min(minTable[i, j - 1], minTable[i - 1, j - 1]);
                        else
                            minTable[i, j] += Math.Min(minTable[i - 1, j - 1], Math.Min(minTable[i, j - 1], minTable[i + 1, j - 1]));
                }
            }
        }
        private void minSumTableHori()
        {
            int w = energyImage.GetLength(0);
            int h = energyImage.GetLength(1);
            minTable = new int[w, h];
            for (int i = 0; i < w; ++i)
            {
                for (int j = 0; j < h; ++j)
                {
                    minTable[i, j] = energyImage[i, j];
                    if (i != 0)
                        if (j == 0)
                            minTable[i, j] += Math.Min(minTable[i - 1, j], minTable[i - 1, j + 1]);
                        else if (j == h - 1)
                            minTable[i, j] += Math.Min(minTable[i - 1, j], minTable[i - 1, j - 1]);
                        else
                            minTable[i, j] += Math.Min(minTable[i - 1, j - 1], Math.Min(minTable[i - 1, j], minTable[i - 1, j + 1]));
                }
            }
        }
        private void decreaseHeight(int rate)
        {
            computeEnergy();
            minSumTableHori();
            LockBitmap lockImg = new LockBitmap(userImage);
            lockImg.LockBits();
            for (int r = 0; r < rate; ++r)
            {
                int minCol = 0;
                int w = minTable.GetLength(0);
                int h = minTable.GetLength(1) - r;
                for (int y = 0; y < h; ++y)
                    if (minTable[w - 1, minCol] > minTable[w - 1, y])
                        minCol = y;

                for (int x = w - 1; x >= 0; --x)
                {
                    int inc = 0;
                    for (int y = 0; y < h - 1; ++y)
                    {
                        if (minCol == y)
                        {
                            inc = 1;
                            lockImg.SetPixel(x, y, Color.Red);
                        }
                        imgColors[x, y] = imgColors[x, y + inc];
                        minTable[x, y] = minTable[x, y + inc];
                    }
                    if (x > 0)
                    {
                        if (minCol > 0 && minTable[x - 1, minCol] > minTable[x - 1, minCol - 1])
                            minCol = minCol - 1;
                        if (minCol < h - 1 && minTable[x - 1, minCol] > minTable[x - 1, minCol + 1])
                            minCol = minCol + 1;
                    }
                }
            }
            lockImg.UnlockBits();
            imgColors = createNewColor(imgColors.GetLength(0), imgColors.GetLength(1) - rate);

            updateGUI = delegate { pictureBox1.Image = userImage; };
            pictureBox1.BeginInvoke(updateGUI);
            userImage = createNewImage();
        }
        private void decreaseWidth(int rate)
        {
            computeEnergy();
            minSumTableVert();
            LockBitmap lockImg = new LockBitmap(userImage);
            lockImg.LockBits();
            for (int r = 0; r < rate; ++r)
            {
                int minRow = 0;
                int w = minTable.GetLength(0) - r;
                int h = minTable.GetLength(1);
                for (int x = 0; x < w; ++x)
                    if (minTable[minRow, h - 1] > minTable[x, h - 1])
                        minRow = x;

                for (int y = h - 1; y >= 0; --y)
                {
                    int inc = 0;
                    for (int x = 0; x < w - 1; ++x)
                    {
                        if (minRow == x)
                        {
                            inc = 1;
                            lockImg.SetPixel(x, y, Color.Red);
                        }
                        imgColors[x, y] = imgColors[x + inc, y];
                        minTable[x, y] = minTable[x + inc, y];
                    }
                    if (y > 0)
                    {
                        if (minRow > 0 && minTable[minRow, y - 1] > minTable[minRow - 1, y - 1])
                            minRow = minRow - 1;
                        if (minRow < w - 1 && minTable[minRow, y - 1] > minTable[minRow + 1, y - 1])
                            minRow = minRow + 1;
                    }
                }
            }
            lockImg.UnlockBits();
            imgColors = createNewColor(imgColors.GetLength(0) - rate, imgColors.GetLength(1));
            /*userImage.Save("C:\\Users\\Tarek\\Desktop\\test.jpg");
            Process.Start("C:\\Users\\Tarek\\Desktop\\test.jpg");*/
            updateGUI = delegate { pictureBox1.Image = userImage; };
            pictureBox1.BeginInvoke(updateGUI);
            userImage = createNewImage();
        }

        ///******///

        private void increaseHeight(int rate)
        {
            computeEnergy();
            minSumTableHori();
            Color[,] tmpColor = new Color[imgColors.GetLength(0), imgColors.GetLength(1) + rate];
            int[,] tmpMin= new int[imgColors.GetLength(0), imgColors.GetLength(1) + rate];
            bool[,] visited = new bool[imgColors.GetLength(0), imgColors.GetLength(1) + rate];
            for (int i = 0; i < visited.GetLength(0); ++i) for (int j = 0; j < visited.GetLength(1); ++j) visited[i,j] = false;
            LockBitmap lockImg = new LockBitmap(userImage);
            lockImg.LockBits();
            for (int r = 0; r < rate; ++r)
            {
                int minCol = 0;
                int w = minTable.GetLength(0);
                int h = minTable.GetLength(1) + r;
                for (int y = 0; y < h; ++y)
                    if (!visited[w - 1, y] && minTable[w - 1, minCol] > minTable[w - 1, y])
                        minCol = y;
                visited[w - 1, minCol]=true;
                for (int x = w - 1; x >= 0; --x)
                {
                    int inc = 0;
                    for (int y = 0; y < h - 1; ++y)
                    {
                        if (minCol == y)
                        {
                            inc = 1;
                            tmpColor[x, y + inc] = getAvg(imgColors[x, y], imgColors[x, y + inc]);
                            minTable[x, y + inc] = (minTable[x, y + inc] + minTable[x, y]) >> 1;
                            lockImg.SetPixel(x, y, Color.Red);
                        }
                        else
                        {
                            tmpColor[x, y + inc] = imgColors[x, y];
                            minTable[x, y + inc] = minTable[x, y];
                        }
                    }
                    if (x > 0)
                    {
                        if (visited[x - 1, minCol])
                            if (minCol > 0 && !visited[x - 1, minCol - 1])
                                minCol -= 1;
                            else
                                minCol += 1;
                        if (minCol > 0 && !visited[x - 1, minCol - 1] && minTable[x - 1, minCol] > minTable[x - 1, minCol - 1])
                            minCol = minCol - 1;
                        if (minCol < h - 1 && !visited[x - 1, minCol + 1] && minTable[x - 1, minCol] > minTable[x - 1, minCol + 1])
                            minCol = minCol + 1;
                        visited[x-1, minCol] = true;
                    }
                }
            }
            lockImg.UnlockBits();
            imgColors = tmpColor;

            updateGUI = delegate { pictureBox1.Image = userImage; };
            pictureBox1.BeginInvoke(updateGUI);
            userImage = createNewImage();
        }
        private void increaseWidth(int rate)
        {
            computeEnergy();
            minSumTableVert();
            Color[,] tmpColor = new Color[imgColors.GetLength(0)+ rate, imgColors.GetLength(1)];
            LockBitmap lockImg = new LockBitmap(userImage);
            lockImg.LockBits();
            for (int r = 0; r < rate; ++r)
            {
                int minRow = 0;
                int w = minTable.GetLength(0) + r;
                int h = minTable.GetLength(1);
                for (int x = 0; x < w; ++x)
                    if (minTable[minRow, h - 1] > minTable[x, h - 1])
                        minRow = x;

                for (int y = h - 1; y >= 0; --y)
                {
                    int inc = 0;
                    for (int x = 0; x < w - 1; ++x)
                    {
                        if (minRow == x)
                        {
                            inc = 1;
                            tmpColor[x + inc, y] = getAvg(imgColors[x, y], imgColors[x + inc, y]);
                            minTable[x + inc, y] = (minTable[x + inc, y] + minTable[x, y]) >> 1;
                            lockImg.SetPixel(x, y, Color.Red);
                        }
                        else
                        {
                            tmpColor[x + inc, y] = imgColors[x, y];
                            minTable[x + inc, y] = minTable[x, y];
                        }
                    }
                    if (y > 0)
                    {
                        if (minRow > 0 && minTable[minRow, y - 1] > minTable[minRow - 1, y - 1])
                            minRow = minRow - 1;
                        if (minRow < w - 1 && minTable[minRow, y - 1] > minTable[minRow + 1, y - 1])
                            minRow = minRow + 1;
                    }
                }
            }
            lockImg.UnlockBits();
            imgColors = createNewColor(tmpColor.GetLength(0), tmpColor.GetLength(1));

            updateGUI = delegate { pictureBox1.Image = userImage; };
            pictureBox1.BeginInvoke(updateGUI);
            userImage = createNewImage();
        }
        ///******///
        
        private void computeHori()
        {
            int H = newHeight;
            int prevHeight= userImage.Height;
            if (prevHeight > H)
            {
                if (prevHeight < SCALE_RATE)
                { decreaseHeight(prevHeight - H); return; }
                while (prevHeight > H)
                {
                    H += SCALE_RATE;
                    decreaseHeight(SCALE_RATE);
                }
                decreaseHeight(H - prevHeight);
            }
            else
            {
                increaseHeight(H - prevHeight);
                /*if (prevHeight > SCALE_RATE)
                { increaseHeight(H - prevHeight); return; }
                while (prevHeight < H)
                {
                    H -= SCALE_RATE;
                    increaseHeight(SCALE_RATE);
                }
                increaseHeight(prevHeight - H);*/
            }
        }
        private void computeVert()
        {
            int W = newWidth;
            int prevWidth = userImage.Width;
            if (prevWidth > W)
            {
                if (prevWidth < SCALE_RATE)
                { decreaseWidth(prevWidth - W); return; }
                while (prevWidth > W)
                {
                    W += SCALE_RATE;
                    decreaseWidth(SCALE_RATE);
                }
                decreaseWidth(W - prevWidth);
            }
            else
            {
                increaseWidth(W - prevWidth);
                /*if (prevWidth > SCALE_RATE)
                { increaseWidth(W - prevWidth); return; }
                while (prevWidth < W)
                {
                    W -= SCALE_RATE;
                    increaseWidth(SCALE_RATE);
                }
                increaseWidth(prevWidth - W);*/
            }
        }


        private void computeEnergy()
        {
            int w = imgColors.GetLength(0);
            int h = imgColors.GetLength(1);
            energyImage = new int[w, h];
            for (int i = 0; i < w; ++i)
                for (int j = 0; j < h; ++j)
                {
                    int val = 0;
                    if (j == 0)
                        val += colorDist(imgColors[i, j], imgColors[i, j + 1]);
                    else if (j == h - 1)
                        val += colorDist(imgColors[i, j], imgColors[i, j - 1]);
                    else
                        val += colorDist(imgColors[i, j - 1], imgColors[i, j + 1]);

                    if (i == 0)
                        val += colorDist(imgColors[i, j], imgColors[i + 1, j]);
                    else if (i == w - 1)
                        val += colorDist(imgColors[i, j], imgColors[i - 1, j]);
                    else
                        val += colorDist(imgColors[i - 1, j], imgColors[i + 1, j]);
                    energyImage[i, j] = val;
                }
        }
        public Bitmap getEnergyImage()
        {
            double maxVal = 0;
            int w = energyImage.GetLength(0);
            int h = energyImage.GetLength(1);
            for (int i = 0; i < w; ++i)
                for (int j = 0; j < h; ++j)
                    maxVal = Math.Max(maxVal, energyImage[i, j]);

            Bitmap energyResult = new Bitmap(w, h);
            LockBitmap lockEnergy = new LockBitmap(energyResult);
            lockEnergy.LockBits();
            for (int i = 0; i < w; ++i)
                for (int j = 0; j < h; ++j)
                {
                    int map = (int)((energyImage[i, j] / maxVal) * 255.0);
                    lockEnergy.SetPixel(i, j, Color.FromArgb(map, map, map));
                }
            lockEnergy.UnlockBits();
            return energyResult;
        }
        private int colorDist(Color A, Color B)
        {
            int red = (A.R - B.R);
            int green = (A.G - B.G);
            int blue = (A.B - B.B);
            double sum = red * red + green * green + blue * blue;
            return (int)Math.Sqrt(sum);
        }
        private Color getAvg(Color A, Color B)
        {
            return Color.FromArgb((A.R + B.R) >> 1, (A.G + B.G) >> 1, (A.B + B.B) >> 1);
        }
        private Color getCopy(Color A)
        {
            int r = A.R, g = A.G, b = A.B;
            return Color.FromArgb(r, g, b);
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            commitScale();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            userImage = createNewImage();
            this.Close();
        }
    }
}

