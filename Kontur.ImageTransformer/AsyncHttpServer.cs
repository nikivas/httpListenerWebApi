using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.RegularExpressions;
namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
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
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
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

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {

            // TODO: implement request handling
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

            var img1 = BitmapCliper.bpp_32Argb_Clip(img, 0, 0, 200, 200);
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
    }
}