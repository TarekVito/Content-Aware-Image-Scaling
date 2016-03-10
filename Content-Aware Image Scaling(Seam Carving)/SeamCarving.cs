using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Content_Aware_Image_Scaling_Seam_Carving_
{
    public partial class SeamCarving : Form
    {

        Bitmap userImage, savedCopy;

        public SeamCarving()
        {
            InitializeComponent();
            btnView.Enabled = false;
        }

        private bool correctFile(string st)
        {
            string[] check;
            check = st.Split('.');
            int i = check.Length - 1;
            check[i] = check[i].ToLower();
            if (check[i] == "bmp" || check[i] == "jpg" || check[i] == "jpeg" || check[i] == "tif" || check[i] == "tiff" || check[i] == "gif")
                return true;
            return false;
        }

        private void initWHOutput(int w, int h)
        {
            lblWidth.Text = "Width : " + w;
            lblHeight.Text = "Height : " + h;
        }
        public static Bitmap ResizeBitmap(Bitmap sourceBMP, int Start, int End, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(sourceBMP, Start, End, width, height);
            }
            sourceBMP.Dispose();
            sourceBMP = null;
            return result;
        }

        public Bitmap resizeImg(Bitmap img)
        {
            //can be viewed in pictureBox1 .. else then it needs to be resized
            if (img.Width < 600 && img.Height < 420)
                return img;
            double ratio = img.Width / (double)img.Height;
            int newWidth, newHeight;
            if (img.Width > img.Height)
            {
                newWidth = 600;
                newHeight = (int)(600.0 / ratio);
            }
            else
            {
                newHeight = 420;
                newWidth = (int)(420.0 * ratio);
            }
            return ResizeBitmap(img, 0, 0, newWidth, newHeight);
        }

        private void btnLoadImg_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            if (correctFile(openFileDialog1.FileName))
            {
                userImage = new Bitmap(openFileDialog1.FileName);
                userImage = resizeImg(userImage);
                savedCopy = new Bitmap(userImage);
                initWHOutput(userImage.Width, userImage.Height);
                pictureBox1.Image = userImage;
                pictureBox1.Refresh();
                btnView.Enabled = true;
            }
        }

        private void btnView_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = userImage;
            int w, h, rate;
            if (int.TryParse(textBoxWidth.Text, out w) && int.TryParse(textBoxHeight.Text, out h) && w<=userImage.Width && h<=userImage.Height
                && int.TryParse(textBoxRate.Text, out rate))
            {
                if(rate>10 || rate<1)
                { MessageBox.Show("Rate value is between 1 and 10"); return; }
                pictureBox1.Enabled = false;
                Viewer instanceView = new Viewer(userImage, w, h, rate);
                instanceView.ShowDialog();
                pictureBox1.Image = instanceView.getImage();
                initWHOutput(pictureBox1.Image.Width, pictureBox1.Image.Height);
                btnView.Enabled = false;
            }
            else
                MessageBox.Show("Check Input Data");
        }

        private void btnBefore_Click(object sender, EventArgs e)
        {
            savedCopy.Save(Application.ExecutablePath.Remove(Application.ExecutablePath.Length - 4) + "tmp.jpg");
            Process.Start(Application.ExecutablePath.Remove(Application.ExecutablePath.Length - 4) + "tmp.jpg");
           
        }

        private void btnAfter_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.Save(Application.ExecutablePath.Remove(Application.ExecutablePath.Length-4)+ "tmp2.jpg");
            Process.Start(Application.ExecutablePath.Remove(Application.ExecutablePath.Length - 4) + "tmp2.jpg");
        }


    }
}
