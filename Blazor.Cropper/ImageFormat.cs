using SkiaSharp;

namespace Blazor.Cropper
{
    /// <summary>Describes the format used to encode a cropped image.</summary>
    public sealed class ImageFormat
    {
        public static ImageFormat Png { get; } = new ImageFormat(SKEncodedImageFormat.Png, "PNG", "image/png");
        public static ImageFormat Jpeg { get; } = new ImageFormat(SKEncodedImageFormat.Jpeg, "JPEG", "image/jpeg");
        public static ImageFormat Webp { get; } = new ImageFormat(SKEncodedImageFormat.Webp, "WebP", "image/webp");

        public SKEncodedImageFormat EncodedFormat { get; }
        public string Name { get; }
        public string DefaultMimeType { get; }

        private ImageFormat(SKEncodedImageFormat encodedFormat, string name, string mimeType)
        {
            EncodedFormat = encodedFormat;
            Name = name;
            DefaultMimeType = mimeType;
        }

        internal static ImageFormat FromEncodedFormat(SKEncodedImageFormat format)
        {
            // Other decoded formats are saved as PNG because Skia cannot encode them.
            return format switch
            {
                SKEncodedImageFormat.Jpeg => Jpeg,
                SKEncodedImageFormat.Webp => Webp,
                _ => Png
            };
        }

        internal static ImageFormat FromFileExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                "jpg" or "jpeg" or "jpe" => Jpeg,
                "webp" => Webp,
                _ => Png
            };
        }

        internal static ImageFormat FromMimeType(string mimeType)
        {
            return mimeType switch
            {
                "image/jpeg" => Jpeg,
                "image/webp" => Webp,
                _ => Png
            };
        }
    }
}
