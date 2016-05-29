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
                if (comboBox2.Text == "" || comboBox3.Text == "")
                {
                    richTextBox1.Text = "Please select port settings";
                }
                else
                {
                    serialPort1.PortName = comboBox2.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox3.Text);
                    serialPort1.Open();
                    progressBar1.Value = 100;
                    button3.Enabled = false;
                    button4.Enabled = true;
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
            button4.Enabled = false;
            button3.Enabled = true;
            
        }


        byte[] rec = new byte[1024]; 
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.Sleep(1000);

            int state = 0;

            for (; ; )
            {
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                if (RecDataCheck())
                {
                    if (state == 0)
                    {
                        if (StartDataWait() == 1)
                        {
                            state = 1;
                        }
                    }
                    else
                    {
                        if (DataNumberWait() != 1)
                        {
                            state = 0;
                        }
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
        MemoryStream ms;
        ArrayList rcv_data = new ArrayList();
        int pictsize = 0;
        private int StartDataWait()
        {
            byte[] start = new byte[2];
            start[0] = 0xFF;
            start[1] = 0xD8;
            
            for (int i = 0; i < 2; i++)
            {

                if (RecData(0, 1) < 0)
                {
                    return -1;
                }
                rcv_data.Add(rec[0]);
                if (rec[0] != start[i])
                {
                    return -1;
                }
            }
            DisposeMs();
            ms = new MemoryStream();
            ms.Write(start, 0, 2);

            int count = rcv_data.Count;

            byte data1 = (byte)rcv_data[count - 3];
            byte data2 = (byte)rcv_data[count - 4];

            pictsize = data1 * 256 + data2;


            if (RecData(0, pictsize) < 0)
            {
                return -1;
            }
            ms.Write(rec, 0, pictsize - 2);
            StatusText("Start");
            return 1;
        }

        private int DataNumberWait(){

            int[] counter = new int[2];
            int cnt = 1;

            for (; ; )
            {
                counter[1] = (cnt & 0xFF00) >> 8;
                counter[0] = (cnt & 0x00FF);

                for (int i = 0; i < 2; i++)
                {

                    if (RecData(0, 1) < 0)
                    {
                        return -1;
                    }

                    if (rec[0] != counter[i])
                    {
                        return -1;
                    }
                }

                int size = getData();
                if (0 > size || size > 1024)
                {
                    return -1;
                }
                if (RecData(0, size + 2) < 0)
                {
                    return -1;
                }
                ms.Write(rec, 0, size);
                StatusText("packet[" + cnt + "] size:" + size);

                if (size != pictsize)
                {
                    DateTime dateNow = DateTime.Now;
                    String fileName = dateNow.ToString("yyyyMMddHHmmss");
                    string fname = "pict" + fileName + ".jpg";
                    FileStream fs = new FileStream(fname,FileMode.Create,FileAccess.Write);
                    int fileSize = (int)ms.Length;
                    byte[] buf = new byte[1024];
                    int remain = fileSize;
                    int readSize;
                    ms.Seek(0, SeekOrigin.Begin);

                    while (remain > 0)
                    {
                        readSize = ms.Read(buf, 0, Math.Min(1024, remain));
                        fs.Write(buf, 0, readSize);
                        remain -= readSize;
                    }
                    fs.Dispose();
                    StatusText("SUCCESS!!");
                    StatusText("Save;"+fname);

                    this.Invoke((MethodInvoker)(() => {

//pictureBox1.Image.Dispose();
                            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                            pictureBox1.Image = System.Drawing.Image.FromFile(@fname);

                        
                    }));
                    return -1;
                }
                cnt++;
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
                return -1;
            }

            try
            {
                i = serialPort1.Read(rec, offset, size);
            }
            catch (TimeoutException)
            {
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

        }


    }
}
