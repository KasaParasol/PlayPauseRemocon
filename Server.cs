using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using WindowsInput;

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

                Process proc = new Process();
                proc.StartInfo.FileName = "netsh";
                proc.StartInfo.Arguments = "http add urlacl url=" + url + " user=Everyone";
                proc.StartInfo.Verb = "runas";
                proc.Start();
                proc.WaitForExit();

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
