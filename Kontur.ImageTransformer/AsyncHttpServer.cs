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
            if (url[1] != @"process")
                return null;
            else if (!Regex.IsMatch(url[3], @"^([0-9,-]*,){3}[0-9,-]*$"))
                return null;

            if(url[2] == @"grayscale")
                return new FilterGrayScale();

            else if (url[2] == @"sepia")
                return new FilterSepia();

            else if (Regex.IsMatch(url[2], @"^threshold\(\d*\)$"))
            {
                var el = new FilterThreshold();
                Console.WriteLine("in");
                
                var valueString = Regex.Match(url[2], @"\(\d*\)").Value;
                Console.WriteLine(valueString);
                var valueNumber = int.Parse(valueString.Substring(1, valueString.Length - 2));

                el.value = valueNumber;
                return el;
            }
                

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

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            //var canceller = new CancellationTokenSource();
            //canceller.CancelAfter(maxTimeResponce);
            var url = listenerContext.Request.RawUrl.Split('/');
            var urlParseResult = urlParse(url);
            if (urlParseResult == null)
            {
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
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
                return;
            }

            int imageWidth = img.Width;
            int imageHeight = img.Height;

            // TODO обрабатывать прямоугольник картинки правильно
            // TODO убедиться в распараллеливании и устойчивости сервера к нагрузке
            
            string[] coordList = url[3].Split(',');
            long polygonX0 = 0;
            long polygonWeight = 0;
            long polygonY0 = 0;
            long polygonHeight = 0;
            if( !Int64.TryParse(coordList[0], out polygonX0) || !Int64.TryParse(coordList[1], out polygonY0) ||
                !Int64.TryParse(coordList[2], out polygonWeight) || !Int64.TryParse(coordList[3], out polygonHeight))
            {
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
            }

            long polygonX1 = polygonWeight + polygonX0;
            long polygonY1 = polygonHeight + polygonY0;
            BitmapCliper.getCorrectCoords(ref img, ref polygonX0, ref polygonY0, ref polygonX1, ref polygonY1);

            long resultImageHeight = Math.Abs(polygonY1 - polygonY0);
            long resultImageWidth = Math.Abs(polygonX1 - polygonX0);

            if(resultImageHeight == 0 && resultImageWidth == 0)
            {
                BadRequest(listenerContext, (int)HttpStatusCode.NoContent);
                return;
            }
            Int64 resultImagePointX0 = Math.Min(polygonX0, polygonX1);
            Int64 resultImagePointY0 = Math.Min(polygonY0, polygonY1);
            if(img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                BadRequest(listenerContext, (int)HttpStatusCode.BadRequest);
                return;
            }

            
            var resultImage = BitmapCliper.Clip(img, (int) resultImagePointX0, (int) resultImagePointY0, 
                                                (int) resultImageWidth, (int) resultImageHeight, urlParseResult); 

            listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

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
    }
}