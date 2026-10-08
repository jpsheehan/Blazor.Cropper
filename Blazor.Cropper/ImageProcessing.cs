using System;
using System.Drawing;
using System.IO;
using SkiaSharp;

namespace Blazor.Cropper
{
    internal static class ImageProcessing
    {
        internal static SKBitmap Decode(byte[] data, out ImageFormat format)
        {
            using var encoded = SKData.CreateCopy(data);
            using var codec = SKCodec.Create(encoded);
            if (codec == null)
                throw new InvalidDataException("The image format is unsupported or the image data is invalid.");

            format = ImageFormat.FromEncodedFormat(codec.EncodedFormat);
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var decoded = new SKBitmap(info);
            if (codec.GetPixels(info, decoded.GetPixels()) != SKCodecResult.Success)
                throw new InvalidDataException("The image could not be decoded.");

            // Match the browser's EXIF orientation before applying crop coordinates.
            var origin = codec.EncodedOrigin;
            bool swapDimensions = origin >= SKEncodedOrigin.LeftTop;
            var oriented = new SKBitmap(new SKImageInfo(
                swapDimensions ? info.Height : info.Width,
                swapDimensions ? info.Width : info.Height,
                info.ColorType, info.AlphaType));
            try
            {
                using var canvas = new SKCanvas(oriented);
                switch (origin)
                {
                    case SKEncodedOrigin.TopRight:
                        canvas.Translate(info.Width, 0);
                        canvas.Scale(-1, 1);
                        break;
                    case SKEncodedOrigin.BottomRight:
                        canvas.Translate(info.Width, info.Height);
                        canvas.RotateDegrees(180);
                        break;
                    case SKEncodedOrigin.BottomLeft:
                        canvas.Translate(0, info.Height);
                        canvas.Scale(1, -1);
                        break;
                    case SKEncodedOrigin.LeftTop:
                        canvas.RotateDegrees(90);
                        canvas.Scale(1, -1);
                        break;
                    case SKEncodedOrigin.RightTop:
                        canvas.Translate(info.Height, 0);
                        canvas.RotateDegrees(90);
                        break;
                    case SKEncodedOrigin.RightBottom:
                        canvas.Translate(info.Height, info.Width);
                        canvas.RotateDegrees(90);
                        canvas.Scale(-1, 1);
                        break;
                    case SKEncodedOrigin.LeftBottom:
                        canvas.Translate(0, info.Width);
                        canvas.RotateDegrees(270);
                        break;
                }
                canvas.DrawBitmap(decoded, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
                return oriented;
            }
            catch
            {
                oriented.Dispose();
                throw;
            }
        }

        internal static SKBitmap Crop(SKBitmap source, Rectangle rectangle, int width, int height)
        {
            if (rectangle.Width <= 0 || rectangle.Height <= 0 || rectangle.X < 0 || rectangle.Y < 0
                || rectangle.Right > source.Width || rectangle.Bottom > source.Height)
                throw new ArgumentOutOfRangeException(nameof(rectangle), "The crop must be within the image bounds.");
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "The output dimensions must be positive.");

            using var cropped = new SKBitmap();
            if (!source.ExtractSubset(cropped, new SKRectI(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)))
                throw new InvalidOperationException("The image could not be cropped.");

            return cropped.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType),
                new SKSamplingOptions(SKCubicResampler.Mitchell))
                ?? throw new InvalidOperationException("The cropped image could not be resized.");
        }
    }
}
