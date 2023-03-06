using iptb;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayPauseRemocon
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 開始状況: 0→無操作、1→ホストとして、2→クライアントとして
        /// </summary>
        private int mode = 0;
        private string serverUrl = "";

        private Server server = new Server();

        public Form1()
        {
            InitializeComponent();

            try
            {
                string read = File.ReadAllText("url.txt");
                string[] ar = read.Split(':');
                ipTextBox1.Text = ar[0];
                port.Value = int.Parse(ar[1]);
                mode = 1;
                button1_Click(null, null);

            }
            catch { 
            }
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Form1_FormClosing(null, null);
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mode = 0;
            label3.Text = "";

            if (e != null && e.CloseReason != System.Windows.Forms.CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                this.Visible = false;
            }
            else {
                notifyIcon1.Visible = false;
                notifyIcon1.Dispose();
                server.Stop();
            }
        }

        private void connect_Click(object sender, EventArgs e)
        {
            if (serverUrl == "")
            {
                Text = "Connect To...";
                mode = 1;
                ipTextBox1.Enabled = true;
            }
            else {
                label3.Text = "Disconedted.";
                serverUrl = "";
                connect.Text = "Connect";
            }

            Show();
            Activate();
        }

        private void startServer_Click(object sender, EventArgs e)
        {
            Text = "Start Server...";
            mode = 2;
            ipTextBox1.Enabled = false;

            if (server.IsListening())
            {
                server.Stop();
                startServer.Text = "Start server";

            }
            else {
                Show();
                Activate();
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            switch (mode)
            {
                case 1:
                    if (!ipTextBox1.IsValid())
                    {
                        Show();
                        Activate();
                        label3.Text = "Invalid IP.";
                        return;
                    }
                    try
                    {
                        var client = new HttpClient();
                        var result = await client.GetAsync("http://" + ipTextBox1.Text + ":" + port.Value.ToString() + "/");
                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            Show();
                            Activate();
                            label3.Text = "Cannnot connect.";
                            return;
                        }
                    }
                    catch {
                        Show();
                        Activate();
                        label3.Text = "Cannnot connect.";
                        return;
                    }
                    serverUrl = "http://" + ipTextBox1.Text + ":" + port.Value.ToString() + "/";
                    connect.Text = "Disconnect (" + ipTextBox1.Text + ":" + port.Value.ToString() + ")";
                    try
                    {
                        File.WriteAllText(@".\url.txt", ipTextBox1.Text + ":" + port.Value.ToString());
                    }
                    catch {
                        MessageBox.Show("設定ファイルの保存に失敗しました。", "エラー", MessageBoxButtons.OK);
                    }
                    break;
                case 2:
                    var stresult = server.Start((int)port.Value);
                    if (!stresult) {
                        label3.Text = "Cannnot start.";
                        return;
                    }
                    startServer.Text = "Stop server (" + port.Value.ToString() + ")";
                    break;
                default:
                    break;
            }
            Hide();
            
        }

        private async void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (serverUrl != "")
            {
                var client = new HttpClient();
                var result = await client.GetAsync("http://" + ipTextBox1.Text + ":" + port.Value.ToString() + "/playpause");
                if (result.StatusCode != HttpStatusCode.OK)
                {
                    label3.Text = "Cannnot connect.";
                    Show();
                    Activate();
                    return;
                }
            }
            else
            {
                Text = "Connect To...";
                mode = 1;
                ipTextBox1.Enabled = true;
                label3.Text = "Not connected.";
                Show();
                Activate();
            }
        }
    }
}
