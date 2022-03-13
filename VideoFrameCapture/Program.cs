using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace VideoFrameCapture
{
    public class Program
    {
        private static async void ColorFrameReader_FrameArrivedAsync(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            Console.WriteLine(DateTime.Now.Millisecond.ToString() + " " + Thread.CurrentThread.ManagedThreadId + " ==> Frame received");

            MediaFrameReference mediaFrameReference;
            if ((mediaFrameReference = sender.TryAcquireLatestFrame()) != null)
            {
                MediaFrameFormat frameFormat = mediaFrameReference.Format;
                Console.WriteLine(frameFormat.VideoFormat.MediaFrameFormat.Subtype);
                VideoMediaFrame videoFrame = mediaFrameReference.VideoMediaFrame;

                if (videoFrame != null)
                {
                    byte[] array;
                    using (var ms = new InMemoryRandomAccessStream())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                        encoder.SetSoftwareBitmap(videoFrame.SoftwareBitmap);

                        try
                        {
                            await encoder.FlushAsync();
                        }
                        catch (Exception ex) { return; }

                        array = new byte[ms.Size];
                        await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);

                        // array now contains the JPEG data; write it out!
                    }

                }
            }
        }

        static async Task Main(string[] args)
        {
            //string cameraName = "C615";
            string cameraName = "Surface";

            var videoFrameReaderBuilder = new FrameReaderBuilder(cameraName, 1280, 720, MediaEncodingSubtypes.Mjpg);

            MediaFrameReader? frameReader = await videoFrameReaderBuilder.Build();

            if (frameReader != null)
            {
                Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + ": Beginning processing of video frames");
                frameReader.FrameArrived += ColorFrameReader_FrameArrivedAsync;
                await frameReader.StartAsync();

                // Let the main thread sleep while events are handled for 10 seconds
                Thread.Sleep(1000);
            }

        }

    }
}