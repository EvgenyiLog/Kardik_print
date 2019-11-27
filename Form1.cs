using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
//using System.Threading;
using System.Runtime.InteropServices;

namespace Kardik_light
{
    public partial class Form1 : Form
    {
        [Serializable]
        struct raw_data
        {

            public Int16 LineA { get; set; }
            public Int16 LineB { get; set; }
            public Int16 LineC { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }
        string Vers = "version 2.6.17";
        byte file_ver = 2;

        Int16[] pointsA;
        Int16[] pointsB;
        Int16[] pointsC;
        Int16 MaxPoint = 1400;
        Int16 pos = 0;
        bool drw = false;
        bool DENA = true;
        Int16 spos = 0;
        UInt16 rej = 0;
      
        int FREQ = 4487;
        double[] Z = new double[3] {0,0,0};
            

        bool rec = false;
        //FileStream raw_file;
        BinaryWriter Creator;
        // Image bmp;
        // Graphics g;
        Image SavePic;

        // Thread thread_draw;

        private void button1_Click(object sender, EventArgs e)
        {
            if (SerialPort1.IsOpen)
            {
                SerialPort1.Close();
                button1.Text = "Start";
                button_record.Enabled = false;
                if (rec == true) button_record_Click(sender, e);
            }
            else
            {
                try
                {
                    SerialPort1.PortName = ((string)comboBox1.SelectedItem);
                    SerialPort1.BaudRate = 921600;
                    SerialPort1.DataBits = 8;
                    SerialPort1.Parity = System.IO.Ports.Parity.None;
                    SerialPort1.StopBits = System.IO.Ports.StopBits.One;
                    SerialPort1.ReadTimeout = -1;
                    SerialPort1.WriteTimeout = 3;
                    SerialPort1.Open();
                    button1.Text = "Stop";
                    button_record.Enabled = true;
                }
                catch
                {
                    MessageBox.Show("Не могу открыть порт: " + comboBox1.Text, "Ошибка",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            this.Text = "KardikPrint 2019    " + Vers;
            try
            {
                foreach (string portName in SerialPort.GetPortNames())
                {
                    comboBox1.Items.Add(portName);
                }
                comboBox1.SelectedIndex = 0;
            }
            catch
            {
                MessageBox.Show("Аппарат не подключен к ПК!", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Text = "KardikPrint 2019    " + Vers + "            -=DEMO=-             Аппарат не обнаружен!";
            }

            pointsA = new Int16[MaxPoint*2];
            pointsB = new Int16[MaxPoint * 2];
            pointsC = new Int16[MaxPoint * 2];

            // numericUpDown1.Maximum = MaxPoint / 1400;

            //  this.Width = pictureBox1.Width + 2*pictureBox1.Left;
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;

            SavePic = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            checkBox1.ForeColor = System.Drawing.Color.DarkRed;
            checkBox2.ForeColor = System.Drawing.Color.DarkOrange;
            checkBox3.ForeColor = System.Drawing.Color.DarkMagenta;

            checkBox4.ForeColor = System.Drawing.Color.DarkBlue;
            checkBox5.ForeColor = System.Drawing.Color.DarkGreen;
            checkBox6.ForeColor = System.Drawing.Color.DarkOrchid;


        }


        private void SerialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string line;
            Int16 value;
            raw_data data = new raw_data();
            int size = Marshal.SizeOf(typeof(raw_data));
            byte[] structureData = new byte[size];

            try
            {
                while (SerialPort1.BytesToRead > 0)
                {

                    try
                    {
                        if (SerialPort1.ReadLine() == "\r*")
                        {
                            line = SerialPort1.ReadLine();
                            value = Int16.Parse(line);
                            data.LineA = value;
                            Z[0] += value;

                            line = SerialPort1.ReadLine();
                            value = Int16.Parse(line);
                            data.LineB = value;
                            Z[1] += value;

                            line = SerialPort1.ReadLine();
                            value = Int16.Parse(line);
                            data.LineC = value;
                            Z[2] += value;

                            rej++;
                            if (rec == true)
                            {
                                /* IntPtr ptr = Marshal.AllocHGlobal(size);
                                 Marshal.StructureToPtr(data, ptr, false);
                                 Marshal.Copy(ptr, structureData, 0, size);
                                 raw_file.Write(structureData, 0, size);
                                 Marshal.FreeHGlobal(ptr);*/
                                Creator.Write(data.LineA);
                                Creator.Write(data.LineB);
                                Creator.Write(data.LineC);
                            }
                        }
                     
                    }
                    catch
                    { value = 0; }
                    if (rej == numericUpDown1.Value)
                    {
                       // points[pos++] = value;
                        pointsA[pos] = (Int16)(Z[0] / rej);
                        pointsB[pos] = (Int16)(Z[1] / rej);
                        pointsC[pos++] = (Int16)(Z[2] / rej);
                        rej = 0;
                        Z[0] = 0;
                        Z[1] = 0;
                        Z[2] = 0;
                    }

                    if (((pos == MaxPoint) || (pos == MaxPoint*2)) && DENA) drw = true;

                    if (pos == MaxPoint * 2) pos = 0;

                    if (pos < MaxPoint) spos = 0; else spos = MaxPoint;
                }
            }
            catch
            { }
        }

        private void GDraw(Graphics g, Pen mainPen, ref double[] data, Int16 offset)
        {
            PointF[] dots = new PointF[1400];
            int i;

            for (i = 0; i < 1400; i++)
            {

                dots[i].X = i;
                dots[i].Y = pictureBox1.Height - (UInt16)(data[i])* trackBar4.Value - offset;

            }
            g.DrawCurve(mainPen, dots);
        }

        public static double fftAbs(alglib.complex z)
        {
            return Math.Sqrt(z.x * z.x + z.y * z.y);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
           int i;
            Graphics g = pictureBox1.CreateGraphics();

            Pen mainPen = new Pen(Brushes.DarkBlue, 1);
            Pen mainPenA = new Pen(Brushes.DarkGreen, 1);
            Pen mainPenB = new Pen(Brushes.DarkOrchid, 1);

            Pen fftPen = new Pen(Brushes.DarkRed, 2);
            Pen fftPenA = new Pen(Brushes.DarkOrange, 2);
            Pen fftPenB = new Pen(Brushes.DarkMagenta, 2);

            Pen NoFFTPenA = new Pen(Brushes.BlueViolet, 1);

            Pen FRQPen = new Pen(Brushes.Black, 1);

            Pen gridPen = new Pen(Brushes.LightCoral, 1);

            double[] mas_4fftA = new double[MaxPoint];
            double[] mas_4fftB = new double[MaxPoint];
            double[] mas_4fftC = new double[MaxPoint];

            //fft[] fft_dataA;
            //fft[] fft_dataB;
            //fft[] fft_dataC;
            alglib.complex[] fft_dataA = new alglib.complex[MaxPoint];
            alglib.complex[] fft_dataB = new alglib.complex[MaxPoint];
            alglib.complex[] fft_dataC = new alglib.complex[MaxPoint];

            double M,F;

            double[] FA = new double[MaxPoint]; // АЧХ до фильтрации
            double[] FFA = new double[MaxPoint]; // АЧХ А после фильтрации
            double[] FFB = new double[MaxPoint]; // АЧХ B после фильтрации
            double[] FFC = new double[MaxPoint]; // АЧХ C после фильтрации

            double[] FRQ = new double[MaxPoint]; // частотные метки
            double[] RDA = new double[MaxPoint]; ; // A после фильтрации
            double[] RDB = new double[MaxPoint]; ; // B после фильтрации
            double[] RDC = new double[MaxPoint]; ; // C после фильтрации

            double p = 50;

            int xgrid = 0; // секундная сетка


            label1.Text = pos.ToString();
            if (SerialPort1.IsOpen)
             label2.Text = SerialPort1.BytesToRead.ToString();

            float mk = (float)(pictureBox1.Height / 4096.0);
            if (drw)
            {
                for (i = 0; i < MaxPoint; i++)
                {
                    mas_4fftA[i] = (double)((float)pointsA[i + spos] * mk);
                    mas_4fftB[i] = (double)((float)pointsB[i + spos] * mk);
                    mas_4fftC[i] = (double)((float)pointsC[i + spos] * mk);
                }
                drw = false;

                //fft_dataA = fft.DFT(mas_4fftA);
                //fft_dataB = fft.DFT(mas_4fftB);
                //fft_dataC = fft.DFT(mas_4fftC);

                alglib.fft.fftr1d(mas_4fftA, (int)MaxPoint, ref fft_dataA, null);
                alglib.fft.fftr1d(mas_4fftB, (int)MaxPoint, ref fft_dataB, null);
                alglib.fft.fftr1d(mas_4fftC, (int)MaxPoint, ref fft_dataC, null);

                //------------------------------
                for (i = 0; i < MaxPoint; i++)
                {
                    M = fftAbs(fft_dataA[i]);
                    F = (double)(FREQ  / numericUpDown1.Value /MaxPoint * i); //12700
                    FA[i] = M;
                    if (F<0.3) { fft_dataA[i].x *= 0; fft_dataA[i].y *= 0; } else
                    if ((F > 33) && (F < 37)) { fft_dataA[i].x = 0; fft_dataA[i].y = 0; }
                    else
                    if ((F > 47) && (F < 53)) { fft_dataA[i].x = 0; fft_dataA[i].y = 0; }
                    else
                    if ((F > 97) && (F < 103)) { fft_dataA[i].x = 0; fft_dataA[i].y = 0; }
                    else
                    if (F > 147) { fft_dataA[i].x = 0; fft_dataA[i].y = 0; }
                    else { fft_dataA[i].x *= 2; }

                    FFA[i] = fftAbs(fft_dataA[i]);
                 
                    if (F>p) { FRQ[i] = 30; p += 50; } else { FRQ[i] = 0; }

                    //-----------------------------

                    if (F < 0.3) { fft_dataB[i].x *= 0; fft_dataB[i].y *= 0; }
                    else
                    if ((F > 33) && (F < 37)) { fft_dataB[i].x = 0; fft_dataB[i].y = 0; }
                    else
                    if ((F > 47) && (F < 53)) { fft_dataB[i].x = 0; fft_dataB[i].y = 0; }
                    else
                    if ((F > 90) && (F < 110)) { fft_dataB[i].x = 0; fft_dataB[i].y = 0; }
                    else
                    if (F > 140) { fft_dataB[i].x = 0; fft_dataB[i].y = 0; }
                    else { fft_dataB[i].x *= 2; }

                    FFB[i] = fftAbs(fft_dataB[i]);
                    //-----------------------------
                    if (F < 0.3) { fft_dataC[i].x *= 0; fft_dataC[i].y *= 0; }
                    else
                   if ((F > 33) && (F < 37)) { fft_dataC[i].x = 0; fft_dataC[i].y = 0; }
                    else
                    if ((F > 47) && (F < 53)) { fft_dataC[i].x = 0; fft_dataC[i].y = 0; }
                    else
                   if ((F > 90) && (F < 110)) { fft_dataC[i].x = 0; fft_dataC[i].y = 0; }
                    else
                   if (F > 140) { fft_dataC[i].x = 0; fft_dataC[i].y = 0; }
                    else { fft_dataC[i].x *= 2; }

                    FFC[i] = fftAbs(fft_dataC[i]);
                    //-----------------------------------
                }
                //RDA = fft.InverseDFT(fft_dataA);
                //RDB = fft.InverseDFT(fft_dataB);
                //RDC = fft.InverseDFT(fft_dataC);
                alglib.fft.fftr1dinv(fft_dataA, (int)MaxPoint, ref RDA, null);
                alglib.fft.fftr1dinv(fft_dataB, (int)MaxPoint, ref RDB, null);
                alglib.fft.fftr1dinv(fft_dataC, (int)MaxPoint, ref RDC, null);
                //-----------------------------

                double minA, maxA, minB, maxB, minC, maxC;
                double FminA, FmaxA, FminB, FmaxB, FminC, FmaxC;
                minA = RDA[0];
                maxA = RDA[0];
                minB = RDB[0];
                maxB = RDB[0];
                minC = RDC[0];
                maxC = RDC[0];

                FminA = FFA[0];
                FmaxA = FFA[0];
                FminB = FFB[0];
                FmaxB = FFB[0];
                FminC = FFC[0];
                FmaxC = FFC[0];

                for (i = 1; i < MaxPoint; i++)
                {
                    if (RDA[i] < minA) minA = RDA[i];
                    if (RDA[i] > maxA) maxA = RDA[i];
                    if (RDB[i] < minB) minB = RDB[i];
                    if (RDB[i] > maxB) maxB = RDB[i];
                    if (RDC[i] < minC) minC = RDC[i];
                    if (RDC[i] > maxC) maxC = RDC[i];

                    if (FFA[i] < FminA) FminA = FFA[i];
                    if (FFA[i] > FmaxA) FmaxA = FFA[i];
                    if (FFB[i] < FminB) FminB = FFB[i];
                    if (FFB[i] > FmaxB) FmaxB = FFB[i];
                    if (FFC[i] < FminC) FminC = FFC[i];
                    if (FFC[i] > FmaxC) FmaxC = FFC[i];

                }

                double dA, dB, dC, d;
                dA = maxA - minA;
                dB = maxB - minB;
                dC = maxC - minC;

                d = dA;

                if (d < dB) d = dB;
                if (d < dC) d = dC;

                double mm = 1;

                mm = pictureBox1.Height / 3 / d;

                double FdA, FdB, FdC, Fd;
                FdA = FmaxA - FminA;
                FdB = FmaxB - FminB;
                FdC = FmaxC - FminC;

                Fd = FdA;

                if (Fd < FdB) Fd = FdB;
                if (Fd < FdC) Fd = FdC;

                double U = pictureBox1.Height*0.9 / Fd;

                for (i = 0; i < MaxPoint; i++)
                {
                    RDA[i] *= mm;
                    RDB[i] *= mm;
                    RDC[i] *= mm;

                    RDA[i] += trackBar1.Value+ pictureBox1.Height/3*2 + pictureBox1.Height / 6;//200;
                    RDB[i] += trackBar1.Value + pictureBox1.Height / 3 + pictureBox1.Height / 6;//100;
                    RDC[i] += trackBar1.Value + pictureBox1.Height / 6;//100;

                    FFA[i] *= U;
                    FFB[i] *= U;
                    FFC[i] *= U;
                    /*   if (RDA[i] > Maximum) Maximum = RDA[i];
                       if (RDA[i] < 0) RDA[i] = 0;
                       if (RDB[i] < 0) RDB[i] = 0;*/


                }

                g.Clear(Color.LightGray);


                

               F = (UInt16)(FREQ / numericUpDown1.Value);
           //    Pulse = 60 * F / Period;
                Font drawFont = new Font("Arial", 16);
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                
 /*                if ((Pulse> 10)&&(Pulse<250))
                g.DrawString("Пульс: " + Convert.ToInt16(Pulse).ToString() + " ударов / c", drawFont, drawBrush, 900, 40);*/

                // расчертим на секунды
                xgrid = (int)F;
                while (xgrid<1400)
                {
                    g.DrawLine(gridPen, xgrid, 40, xgrid, 550);
                    xgrid += (int)F;
                }

                if (checkBox4.Checked) GDraw(g, mainPen, ref RDA, 0);
                if (checkBox5.Checked) GDraw(g, mainPenA, ref RDB, 0);
                if (checkBox6.Checked) GDraw(g, mainPenB, ref RDC, 0);

                if (checkBox1.Checked) GDraw(g, fftPen, ref FFA, 0);
                if (checkBox2.Checked) GDraw(g, fftPenA, ref FFB, 0);
                if (checkBox3.Checked) GDraw(g, fftPenB, ref FFC, 0);
                if (checkBox7.Checked) GDraw(g, FRQPen, ref FRQ, 0);

                if (checkBox8.Checked) GDraw(g, NoFFTPenA, ref mas_4fftA, 0);


                g.Dispose();
                g = Graphics.FromImage(SavePic);
                g.Clear(Color.LightGray);

               // Font drawFont = new Font("Arial", 16);
              //  SolidBrush drawBrush = new SolidBrush(Color.Black);

                g.DrawString(DateTime.Now.ToString(), drawFont, drawBrush, 650,20);

                drawBrush = new SolidBrush(Color.Brown);
                g.DrawString("(c) KARDIK 2019", drawFont, drawBrush, 50, 20);

                if (checkBox4.Checked) GDraw(g, mainPen, ref RDA, (Int16)trackBar1.Value);
                if (checkBox5.Checked) GDraw(g, mainPenA, ref RDB, 0);
                if (checkBox6.Checked) GDraw(g, mainPenB, ref RDC, 0);

                if (checkBox1.Checked) GDraw(g, fftPen, ref FFA, 0);
                if (checkBox2.Checked) GDraw(g, fftPenA, ref FFB, 0);
                if (checkBox3.Checked) GDraw(g, fftPenB, ref FFC, 0);

                if (checkBox7.Checked) GDraw(g, FRQPen, ref FRQ, 0);

                // расчертим на секунды
                xgrid = (int)F;
                while (xgrid < 1400)
                {
                    g.DrawLine(gridPen, xgrid, 40, xgrid, 550);
                    xgrid += (int)F;
                }

            }


            g.Dispose();
            mainPen.Dispose();
            mainPenA.Dispose();
            mainPenB.Dispose();

            fftPen.Dispose();
            fftPenA.Dispose();
            fftPenB.Dispose();

          //  Thread.Sleep(100);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (DENA)
            {
                DENA = false;
                button2.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
               DENA = true;
               button2.ForeColor = System.Drawing.Color.Black;
             }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool need = false;
            need = DENA;
            if (need)
             button2_Click(sender, e);
            if (SavePic != null) //если в pictureBox есть изображение
            {
                //создание диалогового окна "Сохранить как..", для сохранения изображения
                SaveFileDialog savedialog = new SaveFileDialog();
                savedialog.Title = "Сохранить картинку как...";
                //отображать ли предупреждение, если пользователь указывает имя уже существующего файла
                savedialog.OverwritePrompt = true;
                //отображать ли предупреждение, если пользователь указывает несуществующий путь
                savedialog.CheckPathExists = true;
                //список форматов файла, отображаемый в поле "Тип файла"
                savedialog.Filter = "Image Files(*.BMP)|*.BMP|Image Files(*.JPG)|*.JPG|Image Files(*.GIF)|*.GIF|Image Files(*.PNG)|*.PNG|All files (*.*)|*.*";
                //отображается ли кнопка "Справка" в диалоговом окне
                savedialog.ShowHelp = true;
                if (savedialog.ShowDialog() == DialogResult.OK) //если в диалоговом окне нажата кнопка "ОК"
                {
                    try
                    {
                        // pictureBox1.Image.
                        SavePic.Save(savedialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    catch
                    {
                        MessageBox.Show("Невозможно сохранить изображение", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            if (need)
                button2_Click(sender, e);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
        }

        private void button_record_Click(object sender, EventArgs e)
        {
            if (rec == false)
            {
                saveFileDialog1.Filter = "RAW DATA(*.krw)|*.krw|All files(*.*)|*.*";
                if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;
                // получаем выбранный файл
                string filename = saveFileDialog1.FileName;
                // сохраняем  в файл
                //raw_file = new FileStream(filename, FileMode.OpenOrCreate);

                //raw_file = File.OpenWrite(filename);
                Creator = new BinaryWriter(File.Open(filename, FileMode.Create));
                rec = true;
                DateTime Now = DateTime.Now;
                long tik = Now.Ticks;
                //DateTime resum = DateTime.FromBinary(tik);
                //label3.Text = resum.ToLongTimeString();
                //resum.ToLongDateString();
                Creator.Write("KRW");
                Creator.Write(file_ver);
                Creator.Write(tik);
                Creator.Write(FREQ);
            }
            else
            {
                //raw_file.Close();
                Creator.Close();
                rec = false;
            }

            if (rec == true) button_record.Text = "Остановить";
            else button_record.Text = "Запись";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (rec == true) button_record_Click(sender, e);
        }
    }
}


