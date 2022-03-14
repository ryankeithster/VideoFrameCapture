using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
        /// <summary>
        /// Grab the latest video frame, convert it and write it to disk as a JPEG image.
        /// </summary>
        /// <seealso cref="https://stackoverflow.com/questions/34291291/how-to-get-byte-array-from-softwarebitmap"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader"/>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static async void ColorFrameReader_FrameArrivedAsync(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // Grab the current timestamp immediately. This will be used to generate a unique
            // output file name, and waiting until later in the method to grab the timestamp
            // can result in resource contention as other threads grab the same timestamp and
            // try and create a file that has already been created by previous threads
            DateTime dtNow = DateTime.UtcNow;

            //Console.WriteLine(DateTime.Now.Millisecond.ToString() + " " + Thread.CurrentThread.ManagedThreadId + " ==> Frame received");

            StringBuilder filePath = new StringBuilder(@"C:\temp\video_frames\");
            MediaFrameReference mediaFrameReference;
            if ((mediaFrameReference = sender.TryAcquireLatestFrame()) != null)
            {
                VideoMediaFrame videoFrame = mediaFrameReference.VideoMediaFrame;

                if (videoFrame != null)
                {
                    byte[] frameBytes;
                    using (var ms = new InMemoryRandomAccessStream())
                    {
                        // Get encoder to convert the frame to JPEG
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                        
                        // The video frame will likely be in NV12 format. NV12 uses a YUV color space instead of an RGB color space.
                        // BitmapEncoder will only work with imagery in RGB color space, so convert to RGB first
                        SoftwareBitmap interimRgbSwBmp = SoftwareBitmap.Convert(videoFrame.SoftwareBitmap, BitmapPixelFormat.Rgba8);
                        
                        encoder.SetSoftwareBitmap(interimRgbSwBmp);

                        try
                        {
                            // We don't want to encode any more frames so commit it
                            await encoder.FlushAsync();
                        }
                        catch (Exception ex) { return; }

                        frameBytes = new byte[ms.Size];
                        await ms.ReadAsync(frameBytes.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);

                        // frameBytes now contains the JPEG data; write it out!
                        string fileName = @"C:\temp\video_frames\" + dtNow.Hour.ToString() + "h_" + dtNow.Minute.ToString() + "m_" + dtNow.Second.ToString() + "s_" + dtNow.Millisecond.ToString() + "ms.jpg";
                        File.WriteAllBytes(fileName, frameBytes);
                    }

                }
            }
        }

        static async Task Main(string[] args)
        {
            //string cameraName = "C615";
            string cameraName = "Surface";

            // Build a video frame reader to capture 720p video.
            // NV12 seems to be the preferred format. Attempting to use MJPG instead for instance still results in NV12 imagery.
            // NV12 is similar to YUV420: YUV color space with 420 chroma subsampling
            var videoFrameReaderBuilder = new FrameReaderBuilder(cameraName, 1280, 720, MediaEncodingSubtypes.Nv12);

            MediaFrameReader? frameReader = await videoFrameReaderBuilder.Build();

            if (frameReader != null)
            {
                frameReader.FrameArrived += ColorFrameReader_FrameArrivedAsync;
                await frameReader.StartAsync();

                // Let the main thread sleep while events are handled for 10 seconds
                Thread.Sleep(10000);
            }

        }

    }
}