using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using WindowsInput;
using System.Text.RegularExpressions;

namespace PlayPauseRemocon
{
    internal class Server
    {
        public bool shutdownSignal = false;
        public HttpListener sv = null;
        public Task task;
        private InputSimulator sim = new InputSimulator();

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
                                    sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.MEDIA_PLAY_PAUSE);
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
