using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace SirialCommnication
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            getAvailablePorts();
        }

        void getAvailablePorts()
        {
            String[] ports = SerialPort.GetPortNames();
            comboBox2.Items.AddRange(ports);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox2.Text == "" )
                {
                    richTextBox1.Text = "Please select port settings";
                }
                else
                {
                    serialPort1.PortName = comboBox2.Text;
                    serialPort1.BaudRate = 115200;
                    serialPort1.Open();
                    serialPort1.ReadTimeout = 500;
                    progressBar1.Value = 100;
                    button3.Enabled = false;
                    serialPort1.RtsEnable = false;// Enable RTS for flow control
                }
            }
            catch (UnauthorizedAccessException)
            {
                richTextBox1.Text = "Unauthorized Access";
            }
           

            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.RunWorkerAsync();

   
        }

        private void button4_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            serialPort1.Close();
            progressBar1.Value = 0;
            button3.Enabled = true;
            
        }


        byte[] rec = new byte[4096]; 
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            for (; ; )
            {
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                if (RecDataCheck())
                {
                    //データ取得
                    AllgetData();

                    //画像化
                    ConvertJpeg();
                   
                }
            }
        }
        MemoryStream ms;
        ArrayList rcv_data = new ArrayList();
        int pictsize = 0;
        byte[] buf = new byte[0xFFFF];

        private int AllgetData()
        {
            DisposeMs();
            ms = new MemoryStream(0xFFFF);
            StatusText("受信中");
            while (true)
            {
                int read = RecData(0, rec.Length);

                if (read > 0)
                {
                    ms.Write(rec, 0, read);
                    this.Invoke((MethodInvoker)(() => { richTextBox1.AppendText("・"); }));
                    System.Threading.Thread.Sleep(100);
                }
                else if(read == 0)
                {
                    break;
                }
            }
            return 1;
        }


        private int ConvertJpeg()
        {
            byte[] pict_data = new byte[512];
            byte[] p_data = new byte[8];

            DateTime dateNow = DateTime.Now;
            String fileName = dateNow.ToString("yyyyMMddHHmmss");
            string fname = "pict" + fileName + ".jpg";
            FileStream fs = new FileStream(fname, FileMode.Create, FileAccess.Write);

            ms.Seek(0, SeekOrigin.Begin);

            waitInit();
            byte[] temp = new byte[2]{0xFF,0xD8};
            fs.Write(temp, 0, 2);
            int count = rcv_data.Count;
            byte data1 = (byte)rcv_data[count - 3];
            byte data2 = (byte)rcv_data[count - 4];

            pictsize = data1 * 256 + data2;
            ms.Read(pict_data, 0, pictsize);
            //最初に2byte読み込んでるので2引く
            fs.Write(pict_data, 0, pictsize - 2);

            int[] counter = new int[2];
            int cnt = 1;
            int datasize = pictsize;
            for (; ; )
            {
                counter[1] = (cnt & 0xFF00) >> 8;
                counter[0] = (cnt & 0x00FF);

                if (ms.Read(p_data,0, 4) < 0)
                {
                    return -1;
                }
                pictsize = p_data[2] + p_data[3] * 256;
                StatusText("data size:" + pictsize);
                ms.Read(pict_data, 0, pictsize + 2);
                //ファイル書き込み
                fs.Write(pict_data, 0, pictsize);

                if (datasize != pictsize)
                {
                    fs.Dispose();
                    this.Invoke((MethodInvoker)(() =>
                    {

                        //pictureBox1.Image.Dispose();
                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox1.Image = System.Drawing.Image.FromFile(@fname);


                    }));
                    return 1;
                }

            }
        }
        


        private void StatusText(String data)
        {
            this.Invoke((MethodInvoker)(() => { richTextBox1.Focus(); }));
            this.Invoke((MethodInvoker)(() => { richTextBox1.AppendText(data + Environment.NewLine); }));

        }

        private void DisposeMs()
        {
            try
            {
                ms.Dispose();
            }
            catch
            {
            }
        }
        

        private int getData(){
            int datasize = 0;
            if(RecData(0,2) < 0){
                return -1;
            }
            datasize = rec[0] + rec[1] * 256;
            return datasize;
        }




        private int RecData(int offset, int size)
        {
            int i;
            if (serialPort1.IsOpen == false)
            {
                return -2;
            }
            try
            {
                i = serialPort1.Read(rec, offset, size);
            }
            catch (TimeoutException)
            {
                StatusText("完了");
                i = 0;
            }
            catch (InvalidOperationException)
            {
                i = -1;
            }
            catch(Exception){
                i = -1;
            }

            return i;
        }

        private bool RecDataCheck(){
            if(serialPort1.BytesToRead <= 0){
                return false;
            }
            return true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Threading.Thread.Sleep(100);

            while (true)
            {
                if (ms.Length > 0)
                {

                }
            }
        }


        int wait_init_state = 0;

        private int waitInit(){
            byte[] buff = new byte[4];
            byte[] start = new byte[2];
            start[0] = 0xFF;
            start[1] = 0xD8;
            while (true)
            {
                if (ms.Read(buff, 0, 1) != 0)
                {
                    rcv_data.Add(buff[0]);
                    switch (wait_init_state)
                    {
                        case 0:
                            if (buff[0] == start[0])
                            {
                                wait_init_state = 1;
                            }
                            break;

                        case 1:
                            if (buff[0] == start[1])
                            {
                                return 1;
                            }
                            wait_init_state = 0;
                            break;
                    }
                }

            }
        }
    }
}
