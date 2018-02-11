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
                            ++avalibleConnection;
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

            else if (Regex.IsMatch(url[2], "^threshold/(/d{1,}/)$"))
            {
                var el = new FilterThreshold();
                var valueString = Regex.Match(url[2], "(/d*)").Value;
                var valueNumber = int.Parse(valueString.Substring(1, valueString.Length - 1));

                el.value = valueNumber;
                return el;
            }
                

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
                listenerContext.Response.Close();
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

            // TODO обрабатывать прямоугольник картинки правильно
            // TODO убедиться в распараллеливании и устойчивости сервера к нагрузке
            var coordList = url[4].Split(',');
            int x, w, y, h;

            int.TryParse(coordList[0], out x);
            int.TryParse(coordList[1], out y);
            int.TryParse(coordList[2], out w);
            int.TryParse(coordList[3], out h);

            int x1 = w + x;
            int y1 = h + y;

            BitmapCliper.getCorrectCoords(ref img, ref x, ref y, ref x1, ref y1);

            int dy = Math.Abs(y1 - y);
            int dx = Math.Abs(x1 - x);
            if(dy == 0 && dx == 0)
            {
                listenerContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }

            int x0 = Math.Min(x, x1);
            int y0 = Math.Min(y, y1);

            var img1 = BitmapCliper.Clip(img, x0, y0, dx, dy, urlParseResult); 
            // tut vse ostal'noe<Filters>


            // </Filters>

            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
            //var result =  reader.ReadToEndAsync();


            using (var writer = new StreamWriter(listenerContext.Response.OutputStream, encoding))
            {
                img1.Save(listenerContext.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Png );
            }
            --avalibleConnection;
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        private int avalibleConnection;
        public int maxTimeResponce = 1000;
    }
}