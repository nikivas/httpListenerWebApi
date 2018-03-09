using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
            avalibleConnection = 0;
            listener = new HttpListener();
        }
        
        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();
                    
                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();
                
                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }
        
        private void Listen()
        {
            //ThreadPool.SetMinThreads(4, 4);
            
            while (true)
            {
                try
                {
                    if (listener.IsListening )
                    {
                        //ConcurrentStack<HttpListenerContext> el = new ConcurrentStack<HttpListenerContext>();
                        var context = listener.GetContext();
                        //Console.WriteLine(avalibleConnection);
                        if (avalibleConnection > maxThreads)
                        {
                            context.Response.StatusCode = 429;
                            context.Response.Close();
                            continue;
                        }
                        else
                        {
                            ++avalibleConnection;
                            ThreadPool.QueueUserWorkItem(HandleContextAsync, context);
                            //Task.Run(() => HandleContextAsync(context));
                        }

                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                }
            }
        }


        private IFilter urlParse(string[] url)
        {
            if (url[1] != @"process")
                return null;
            else if (!Regex.IsMatch(url[3], @"^ *(-?[0-9]*,){3}-?[0-9]* *$"))
                return null;

            url[2] = url[2].ToLower();

            if(url[2] == @"rotate-cw")
                return new FilterRotateCW();

            else if (url[2] == @"rotate-ccw")
                return new FilterRotateCCW();

            else if (url[2] == @"flip-h")
                return new FilterFlipH();

            else if (url[2] == @"flip-v")
                return new FilterFlipV();
                

            return null;
        }

        #region<Testing>
        private void TestHandleContextAsync(object arg)
        {
            var canceller = new CancellationTokenSource();
            canceller.CancelAfter(1000);
            var listenerContext = (HttpListenerContext)arg;
            var streamIn = listenerContext.Request.InputStream;
            var encoding = listenerContext.Request.ContentEncoding;
            var reader = new StreamReader(streamIn, encoding);
            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            
            Thread.Sleep(90);
            var res = reader.ReadToEnd();
            int a;
            int b;
            ThreadPool.GetAvailableThreads(out a, out b);
            //Console.WriteLine("workerThread - {0}, competitionPort - {1}",a,b);

            using (var writer = new StreamWriter(listenerContext.Response.OutputStream, encoding))
            {
                writer.Write(res);
            }
            avalibleConnection--;
        }
        #endregion

        private void BadRequest(HttpListenerContext listenerContext, int httpStatusCode)
        {
            listenerContext.Response.StatusCode = httpStatusCode;
            listenerContext.Response.Close();
            return;
        }

        private async void HandleContextAsync(object arg)
        {
            var listenerContext = (HttpListenerContext)arg;
            

            var url = listenerContext.Request.RawUrl.Split('/');
            var urlParseResult = urlParse(url);
            if (urlParseResult == null)
            {
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
                --avalibleConnection;
                return;
            }
            var streamIn = listenerContext.Request.InputStream;
            var encoding = listenerContext.Request.ContentEncoding;
            var reader = new StreamReader(streamIn, encoding);
            Bitmap img = null;
            try {
                
                img = new Bitmap(streamIn);

            }catch(Exception ee)
            {
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
                --avalibleConnection;
                return;
            }
            
            if (img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
                --avalibleConnection;
                return;
            }

            
            string[] coordList = url[3].Split(',');
            Bitmap resultImage = null;
            try {
                urlParseResult.draw(img);
                resultImage = BitmapCliper.Clip(coordList, img);
            } catch(Exception ee)
            {
                --avalibleConnection;
                BadRequest(listenerContext, (int)HttpStatusCode.NoContent);
                return;
            }
            if (resultImage == null)
            {
                --avalibleConnection;
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
                return;
            }

            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            Console.WriteLine(resultImage.Width);
            Console.WriteLine(resultImage.Height);
            using (var writer = new StreamWriter(listenerContext.Response.OutputStream, encoding))
            {
                resultImage.Save(listenerContext.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Png );
            }
            --avalibleConnection;
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        private int avalibleConnection;
        public int maxTimeResponce = 1000;
        public int maxThreads = 150;
    }
}