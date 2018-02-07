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
                        if (avalibleConnection > 150)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            context.Response.Close();
                            continue;
                        }
                        else
                        {
                            avalibleConnection++;
                            //ThreadPool.QueueUserWorkItem(HandleContextAsync, context);
                            Task.Run(() => HandleContextAsync(context));
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
            if (url[1] != "process")
                return null;
            else if (!Regex.IsMatch(url[3], "^([0-9,-]*,){3}[0-9,-]*$"))
                return null;

            if(url[2] == "grayscale")
                return new FilterGrayScale();

            else if (url[2] == "sepia")
                return new FilterSepia();

            else if (Regex.IsMatch(url[2], "^threshold/(/d*/)$"))
                return new FilterThreshold();

            return null;
        }

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

        private void BadRequest(object arg)
        {
            var listenerContext = (HttpListenerContext) arg;
            listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
            {
                writer.Write(" ");
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {

            // TODO: implement request handling
            //var listenerContext = arg as HttpListenerContext;
            var canceller = new CancellationTokenSource();
            canceller.CancelAfter(maxTimeResponce);

            var url = listenerContext.Request.RawUrl.Split('/');
            var urlParseResult = urlParse(url);

            if (urlParseResult == null)
            {
                listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
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
                Console.WriteLine("Error text not a picture");
                listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }
            int imageWidth = img.Width;
            int imageHeight = img.Height;
            // obrezanie nado sna4ala sdelat'

            Console.WriteLine(img.PixelFormat);
            Console.WriteLine(urlParseResult.GetType().ToString());
            // TODO добавить в threshold цифры 
            // TODO обрабатывать прямоугольник картинки правильно
            // TODO убедиться в распараллеливании и устойчивости сервера к нагрузке
            var img1 = BitmapCliper.Clip(img, 0, 0, 250, 170, urlParseResult); 
            // tut vse ostal'noe<Filters>


            // </Filters>

            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            //var result =  reader.ReadToEndAsync();


            using (var writer = new StreamWriter(listenerContext.Response.OutputStream, encoding))
            {
                img1.Save(listenerContext.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Png );
            }

        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        private int avalibleConnection;
        public int maxTimeResponce = 1000;
    }
}