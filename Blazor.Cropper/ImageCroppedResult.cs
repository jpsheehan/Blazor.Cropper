using System;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

namespace Blazor.Cropper
{
    internal sealed class BrowserCropResult
    {
        public byte[] Data { get; set; }
        public string MimeType { get; set; }
    }

    public class ImageCroppedResult : IDisposable
    {
        private readonly string _base64;
        private byte[] _data;
        private bool _disposed;

        /// <summary>The cropped bitmap in pure C# mode. The result owns this bitmap.</summary>
        public SKBitmap Img { get; init; }
        public ImageFormat Format { get; init; }
        public int Quality { get; init; }

        public ImageCroppedResult(SKBitmap img, ImageFormat format, int quality)
        {
            ArgumentNullException.ThrowIfNull(img);
            ArgumentNullException.ThrowIfNull(format);
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality));
            Img = img;
            Format = format;
            Quality = quality;
        }

        public ImageCroppedResult(string base64, ImageFormat format)
        {
            ArgumentNullException.ThrowIfNull(base64);
            ArgumentNullException.ThrowIfNull(format);
            _base64 = base64;
            Format = format;
        }

        public ImageCroppedResult(byte[] bytes, ImageFormat format)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentNullException.ThrowIfNull(format);
            _data = bytes;
            Format = format;
        }

        [Obsolete("Use GetDataAsync to avoid base64 conversion overhead.")]
        public async Task<string> GetBase64Async()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _base64 ?? Convert.ToBase64String(await GetDataAsync());
        }

        /// <summary>Get the encoded image bytes.</summary>
        public Task<byte[]> GetDataAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _data ??= _base64 != null
                ? Convert.FromBase64String(_base64)
                : Encode(Format.EncodedFormat, Quality);
            return Task.FromResult(_data);
        }

        /// <summary>Save the encoded image to a stream.</summary>
        public async Task SaveAsync(Stream s)
        {
            ArgumentNullException.ThrowIfNull(s);
            await s.WriteAsync(await GetDataAsync());
        }

        /// <summary>
        /// Encode the cropped bitmap with a specified Skia format and quality.
        /// This method requires pure C# processing and supports PNG, JPEG and WebP.
        /// </summary>
        public async Task SaveAsync(Stream s, SKEncodedImageFormat format, int quality = 100)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(s);
            if (Img == null)
                throw new InvalidOperationException("This method requires pure C# processing.");
            await s.WriteAsync(Encode(format, quality));
        }

        private byte[] Encode(SKEncodedImageFormat format, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality));
            if (format != SKEncodedImageFormat.Png && format != SKEncodedImageFormat.Jpeg && format != SKEncodedImageFormat.Webp)
                throw new NotSupportedException("Cropped images can be encoded as PNG, JPEG or WebP.");
            using var image = SKImage.FromBitmap(Img);
            using var data = image.Encode(format, quality);
            return data?.ToArray() ?? throw new InvalidOperationException("The cropped image could not be encoded.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Img?.Dispose();
        }
    }
}
