using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsTest4
{
    public partial class Form1 : Form
    {
        bool to_close = false;

        bool[] connection = { false, false, false };

        int cl_num = 3;

        string[] client = { "93.123.150.67", "77.88.21.11", "8.8.8.8" }; // "10.52.179.240"; // "176.59.33.29"; // "93.123.150.67";

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        Label[] text;

        Ping[] ping;

        AutoResetEvent[] waiter;

        public Form1()
        {
            InitializeComponent();

            timer.Tick += Timer_Tick;
        }

        private void Inits()
        {
            text = new Label[cl_num];
            ping = new Ping[cl_num];
            waiter = new AutoResetEvent[cl_num];

            timer.Interval = 1000;

            for (int i = 0; i < cl_num; i++)
            {
                text[i] = new Label();
                text[i].AutoSize = true;
                text[i].Font = new Font("Harlow Solid Italic", 14.25F, FontStyle.Italic);
                text[i].ForeColor = Color.FromArgb(255, 255, 255);
                text[i].Location = new Point(5, 5 + i * 32);
                text[i].Name = "text" + i;
                text[i].Size = new Size(0, 27);
                text[i].TabIndex = i * 2;
                text[i].UseCompatibleTextRendering = true;
                Controls.Add(text[i]);

                ping[i] = new Ping();
                ping[i].PingCompleted += new PingCompletedEventHandler(Received_reply);

                waiter[i] = new AutoResetEvent(false);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timer.Enabled)
                timer.Stop();

            for(int i = 0; i < cl_num; i++)
                if (!connection[i])
                    PingCl(ping[i], client[i], waiter[i]);

            if (connection.All(c => c))
                timer.Stop();
        }

        private void ShowReply(int cl, IPStatus status, long time)
        {
            switch(cl)
            {
                case 0:
                    text[cl].Text = "Client: ";
                    break;
                case 1:
                    text[cl].Text = "Yandex: ";
                    break;
                case 2:
                    text[cl].Text = "Google: ";
                    break;
            }

            if (status == IPStatus.Success)
            {
                text[cl].ForeColor = Color.FromArgb(0, 192, 0);
                text[cl].Text += time + " ms";
            }
            else
            {
                text[cl].ForeColor = Color.FromArgb(192, 0, 0);
                text[cl].Text += "No connection";
            }

            connection[cl] = true;

            if (!to_close)
                PingCl(ping[cl], client[cl], waiter[cl]);
        }

        private void PingCl(Ping ping, string address, AutoResetEvent are)
        {
            try
            {
                ping.SendAsync(address, 5000, are);
            }
            catch
            {
                if (!timer.Enabled)
                    timer.Start();

                for(int i = 0; i < cl_num; i++)
                    connection[i] = false;

                text[0].ForeColor = Color.FromArgb(192, 192, 192);
                text[0].Text = "No network connection";
                text[1].Text = "";
                text[2].Text = "";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Inits();

            for(int i = 0; i < cl_num; i++)
                PingCl(ping[i], client[i], waiter[i]);
        }

        private void Received_reply(object sender, PingCompletedEventArgs e)
        {
            if (e.Cancelled)
                ((AutoResetEvent)e.UserState).Set();

            if (e.Error != null)
                ((AutoResetEvent)e.UserState).Set();

            // Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();

            if (e.Reply != null)
            {
                if (e.Reply.Address.ToString() == "77.88.21.11")
                    ShowReply(1, e.Reply.Status, e.Reply.RoundtripTime);
                else if (e.Reply.Address.ToString() == "8.8.8.8")
                    ShowReply(2, e.Reply.Status, e.Reply.RoundtripTime);
                else
                    ShowReply(0, e.Reply.Status, e.Reply.RoundtripTime);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            to_close = true;

            for(int i = 0; i < cl_num; i++)
                ping[i].SendAsyncCancel();

            FormClosing -= new FormClosingEventHandler(Form1_FormClosing);

            Dispose();
            Close();
        }
    }
}
