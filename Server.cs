using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using WindowsInput;
using System.Text.RegularExpressions;
using System;
using System.Runtime.InteropServices;

namespace PlayPauseRemocon
{
    internal class Server
    {
        //送信するためのメソッド(数値)
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hwnd, uint message, int wParam, int lParam);

        private const uint WM_APPCOMMAND = 0x0319;

        private const int APPCOMMAND_MEDIA_PLAY_PAUSE    = 0x000E0000;
        private const int APPCOMMAND_MEDIA_PLAY          = 0x002E0000;
        private const int APPCOMMAND_MEDIA_PAUSE         = 0x002F0000;
        private const int APPCOMMAND_MEDIA_STOP          = 0x000D0000;
        private const int APPCOMMAND_VOLUME_MUTE         = 0x00080000;
        private const int APPCOMMAND_VOLUME_UP           = 0x000A0000;
        private const int APPCOMMAND_VOLUME_DOWN         = 0x00090000;
        private const int APPCOMMAND_MEDIA_NEXTTRACK     = 0x000B0000;
        private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 0x000C0000;

        public bool shutdownSignal = false;
        public HttpListener sv = null;
        public Task task;
        private InputSimulator sim = new InputSimulator();
        private string[][] regExps;
        private bool useHWid;

        public Server(string[] strs)
        {
            useHWid = strs.Length != 0;

            if (useHWid)
            {

                regExps = new string[strs.Length][];
                for(int i = 0; i < strs.Length; i++)
                {
                    regExps[i] = strs[i].Split('\t');
                }
            }
        }

        public bool Start(int port)
        {
            try
            {
                shutdownSignal = false;
                string url = "http://+:" + port.ToString() + "/";

                Process procShow = new Process();
                procShow.StartInfo.RedirectStandardOutput = true;
                procShow.StartInfo.CreateNoWindow = true;
                procShow.StartInfo.UseShellExecute = false;
                procShow.StartInfo.FileName = "netsh";
                procShow.StartInfo.Arguments = "http show urlacl url=" + url;
                procShow.StartInfo.Verb = "runas";
                procShow.Start();
                procShow.WaitForExit();
                string netshResult = procShow.StandardOutput.ReadToEnd();
                if (!netshResult.Contains(url))
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = "netsh";
                    proc.StartInfo.Arguments = "http add urlacl url=" + url + " user=Everyone";
                    proc.StartInfo.Verb = "runas";
                    proc.Start();
                    proc.WaitForExit();
                }

                Process procFWShow = new Process();
                procFWShow.StartInfo.RedirectStandardOutput = true;
                procFWShow.StartInfo.CreateNoWindow = true;
                procFWShow.StartInfo.UseShellExecute = false;
                procFWShow.StartInfo.FileName = "netsh";
                procFWShow.StartInfo.Arguments = "advfirewall firewall show rule name=\"PlayPauseRemocon\"";
                procFWShow.StartInfo.Verb = "runas";
                procFWShow.Start();
                procFWShow.WaitForExit();
                string netshFWResult = procFWShow.StandardOutput.ReadToEnd();
                if (!netshFWResult.Contains("PlayPauseRemocon"))
                {
                    Process procFW = new Process();
                    procFW.StartInfo.FileName = "netsh";
                    procFW.StartInfo.Arguments = "advfirewall firewall add rule name=\"PlayPauseRemocon\" dir=in action=allow";
                    procFW.StartInfo.Verb = "runas";
                    procFW.Start();
                    procFW.WaitForExit();
                }

                if (!netshFWResult.Contains(port.ToString())) {
                    Process procPort = new Process();
                    procPort.StartInfo.FileName = "netsh";
                    procPort.StartInfo.Arguments = "advfirewall firewall set rule name=\"PlayPauseRemocon\" new program=system profile=private protocol=tcp localport=" + port.ToString();
                    procPort.StartInfo.Verb = "runas";
                    procPort.Start();
                    procPort.WaitForExit();
                }
                

                sv = new HttpListener();
                sv.Prefixes.Add(url);
                task = new Task(
                    () =>
                    {
                        sv.Start();
                        while (!shutdownSignal)
                        {
                            try
                            {
                                var cont = sv.GetContext();
                                var req = cont.Request;
                                var res = cont.Response;

                                if (req != null && req.RawUrl.IndexOf("playpause") != -1)
                                {
                                    // TODO: ここに処理
                                    if (!useHWid)
                                    {
                                        sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_PLAY_PAUSE);

                                    }
                                    else messaging();
                                    byte[] text = Encoding.UTF8.GetBytes("<html><head><meta charset='utf-8'/></head><body>OK</body></html>");
                                    res.OutputStream.Write(text, 0, text.Length);
                                }
                                else if (req != null)
                                {
                                    byte[] text = Encoding.UTF8.GetBytes("<html><head><meta charset='utf-8'/></head><body>hallo</body></html>");
                                    res.OutputStream.Write(text, 0, text.Length);
                                }
                                else
                                {
                                    res.StatusCode = 404;
                                }
                                res.Close();
                            }
                            catch { 
                                // 特に手立ては必要ない
                            }
                        }
                        sv.Stop();
                    }
                );
                task.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void messaging() {
            foreach (string[] reg in regExps)
            {
                foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
                {
                    //メインウィンドウのタイトルがある時だけ列挙する
                    if (p.MainWindowTitle.Length != 0)
                    {
                        if (reg.Length >= 2 && reg[1] == "exe")
                        {
                            if (!Regex.IsMatch(p.ProcessName, reg[0]))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (!Regex.IsMatch(p.MainWindowTitle, reg[0]))
                            {
                                continue;
                            }
                        }

                        IntPtr hWind = p.MainWindowHandle;

                        if (reg.Length >= 3) switch (reg[2])
                        {
                                case "handle":
                                    {
                                        hWind = p.Handle;
                                        break;
                                    }
                                default:
                                    {
                                        hWind = p.MainWindowHandle;
                                        break;
                                    }
                        }

                        SendMessage(p.MainWindowHandle, WM_APPCOMMAND, 0, APPCOMMAND_MEDIA_PLAY_PAUSE);
                        return;
                    }
                }
            }
        }

        public void Stop()
        {
            if (sv == null) return;
            sv.Stop();
            shutdownSignal = true;
        }

        public bool IsListening()
        {
            return sv != null && sv.IsListening;
        }
    }
}
