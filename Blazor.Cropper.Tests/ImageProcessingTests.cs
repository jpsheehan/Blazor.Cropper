using System.Drawing;
using System.Text;
using Microsoft.JSInterop;
using SkiaSharp;
using Xunit;

namespace Blazor.Cropper.Tests;

public class ImageProcessingTests
{
    [Fact]
    public void CropSelectsRequestedPixelsWithoutChangingSource()
    {
        using var source = CreateImage();
        using var crop = ImageProcessing.Crop(source, new Rectangle(1, 1, 2, 2), 2, 2);
        Assert.Equal(source.GetPixel(1, 1), crop.GetPixel(0, 0));
        Assert.Equal(source.GetPixel(2, 2), crop.GetPixel(1, 1));
        Assert.Equal(4, source.Width);
        Assert.Equal(4, source.Height);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(9, 6)]
    public void CropResizesToFinalDimensions(int width, int height)
    {
        using var source = CreateImage();
        using var crop = ImageProcessing.Crop(source, new Rectangle(1, 1, 2, 2), width, height);
        Assert.Equal(width, crop.Width);
        Assert.Equal(height, crop.Height);
    }

    [Theory]
    [InlineData(ImageEncoding.Png)]
    [InlineData(ImageEncoding.Jpeg)]
    [InlineData(ImageEncoding.Webp)]
    public async Task ResultsEncodeAndSaveInRequestedFormat(ImageEncoding encoding)
    {
        var format = GetFormat(encoding);
        using var result = new ImageCroppedResult(CreateImage(), format, 90);
        byte[] bytes = await result.GetDataAsync();
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        Assert.Equal(format.EncodedFormat, codec.EncodedFormat);
        Assert.Equal(4, codec.Info.Width);
        Assert.Equal(4, codec.Info.Height);
        Assert.Same(bytes, await result.GetDataAsync());
        using var stream = new MemoryStream();
        await result.SaveAsync(stream);
        Assert.Equal(bytes, stream.ToArray());
        stream.SetLength(0);
        await result.SaveAsync(stream, SKEncodedImageFormat.Png);
        using var pngData = SKData.CreateCopy(stream.ToArray());
        using var pngCodec = SKCodec.Create(pngData);
        Assert.Equal(SKEncodedImageFormat.Png, pngCodec.EncodedFormat);
    }

    [Theory]
    [InlineData(ImageEncoding.Jpeg)]
    [InlineData(ImageEncoding.Webp)]
    public async Task LossyEncodingHonorsQuality(ImageEncoding encoding)
    {
        using var bitmap = new SKBitmap(100, 100);
        var random = new Random(42);
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
        var format = GetFormat(encoding);
        using var low = new ImageCroppedResult(bitmap.Copy(), format, 10);
        using var high = new ImageCroppedResult(bitmap.Copy(), format, 95);
        Assert.True((await high.GetDataAsync()).Length > (await low.GetDataAsync()).Length);
    }

    [Theory]
    [InlineData(1, 0, 1, 2, 3)]
    [InlineData(2, 1, 0, 3, 2)]
    [InlineData(3, 3, 2, 1, 0)]
    [InlineData(4, 2, 3, 0, 1)]
    [InlineData(5, 0, 2, 1, 3)]
    [InlineData(6, 2, 0, 3, 1)]
    [InlineData(7, 3, 1, 2, 0)]
    [InlineData(8, 1, 3, 0, 2)]
    public void DecodeAppliesAllExifOrientations(int orientation, int tl, int tr, int bl, int br)
    {
        using var source = new SKBitmap(40, 20);
        var colors = new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Yellow };
        for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++)
                source.SetPixel(x, y, colors[(y < 10 ? 0 : 2) + (x < 20 ? 0 : 1)]);
        using var image = SKImage.FromBitmap(source);
        using var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        byte[] bytes = jpeg.ToArray();
        using var original = ImageProcessing.Decode(bytes, out _);
        var expected = CornerColors(original);
        using var oriented = ImageProcessing.Decode(AddExifOrientation(bytes, orientation), out var format);
        Assert.Equal(orientation >= 5 ? 20 : 40, oriented.Width);
        Assert.Equal(orientation >= 5 ? 40 : 20, oriented.Height);
        Assert.Same(ImageFormat.Jpeg, format);
        var expectedCorners = new[] { expected[tl], expected[tr], expected[bl], expected[br] };
        var actualCorners = CornerColors(oriented);
        for (int i = 0; i < expectedCorners.Length; i++)
        {
            // JPEG rounding varies slightly between neighboring pixels after a flip.
            Assert.InRange(Math.Abs(expectedCorners[i].Red - actualCorners[i].Red), 0, 2);
            Assert.InRange(Math.Abs(expectedCorners[i].Green - actualCorners[i].Green), 0, 2);
            Assert.InRange(Math.Abs(expectedCorners[i].Blue - actualCorners[i].Blue), 0, 2);
        }
    }

    [Fact]
    public void InvalidImagesAndCropBoundsAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => ImageProcessing.Decode(new byte[] { 1, 2, 3 }, out _));
        using var source = CreateImage();
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageProcessing.Crop(source, new Rectangle(-1, 0, 2, 2), 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageProcessing.Crop(source, new Rectangle(3, 3, 2, 2), 2, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageProcessing.Crop(source, new Rectangle(0, 0, 2, 2), 0, 2));
    }

    [Fact]
    public async Task ComponentCropsAreIndependentAndHonorFinalSize()
    {
        var cropper = CreateCropper();
        cropper._sourceImage = CreateImage();
        cropper.FinalCropWidth = 6;
        cropper.FinalCropHeight = 3;
        using var first = await cropper.GetCropedResult();
        Assert.Equal(6, first.Img.Width);
        Assert.Equal(3, first.Img.Height);
        first.Dispose();
        using var second = await cropper.GetCropedResult();
        Assert.Equal(4, cropper._sourceImage.Width);
        cropper.Dispose();
        Assert.NotEmpty(await second.GetDataAsync());
    }

    [Fact]
    public async Task LoadingReadsWholeStreamAndClearsPreviousBitmap()
    {
        var runtime = new RecordingRuntime();
        var cropper = CreateCropper();
        cropper.JSRuntime = runtime;
        cropper.PureCSharpProcessing = true;
        using var bitmap = CreateImage();
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var stream = new PartialReadStream(png.ToArray());
        await cropper.LoadImage("png", new StreamFile(stream, "image.png"));
        Assert.Equal(4, cropper._sourceImage.Width);
        Assert.True(stream.WasDisposed);
        var previous = cropper._sourceImage;
        cropper.PureCSharpProcessing = false;
        cropper.InputId = "input";
        await cropper.LoadImage("jpg", new StreamFile(new MemoryStream(), "image.jpg"));
        Assert.Null(cropper._sourceImage);
        Assert.Equal(IntPtr.Zero, previous.Handle);
        Assert.Same(ImageFormat.Jpeg, cropper._format);
        Assert.Equal("setImg", runtime.Identifier);
        cropper.Dispose();
    }

    [Fact]
    public async Task AsyncComponentDisposalReleasesNativeBitmap()
    {
        var cropper = CreateCropper();
        var runtime = new RecordingRuntime();
        cropper.JSRuntime = runtime;
        var source = CreateImage();
        cropper._sourceImage = source;
        await cropper.DisposeAsync();
        Assert.Equal("rmCropperEventListeners", runtime.Identifier);
        Assert.Null(cropper._sourceImage);
        Assert.Equal(IntPtr.Zero, source.Handle);
    }

    [Fact]
    public async Task BrowserResultUsesActualEncodedMimeType()
    {
        var cropper = CreateCropper();
        var runtime = new RecordingRuntime
        {
            Result = new BrowserCropResult { Data = new byte[] { 1, 2, 3 }, MimeType = "image/png" }
        };
        cropper.JSRuntime = runtime;
        cropper._format = ImageFormat.Webp;
        using var result = await cropper.GetCropedResult();
        Assert.Equal("cropAsync", runtime.Identifier);
        Assert.Same(ImageFormat.Png, result.Format);
        Assert.Equal(new byte[] { 1, 2, 3 }, await result.GetDataAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => result.SaveAsync(new MemoryStream(), SKEncodedImageFormat.Png));
        cropper.Dispose();
    }

    [Fact]
    public async Task PngEncodingPreservesTransparency()
    {
        var bitmap = CreateImage();
        bitmap.SetPixel(1, 1, new SKColor(120, 60, 30, 128));
        using var result = new ImageCroppedResult(bitmap, ImageFormat.Png, 100);
        using var decoded = SKBitmap.Decode(await result.GetDataAsync());
        Assert.Equal(128, decoded.GetPixel(1, 1).Alpha);
        Assert.InRange(Math.Abs(120 - decoded.GetPixel(1, 1).Red), 0, 1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task InvalidQualityAndUnsupportedEncodingAreRejected(int quality)
    {
        using var bitmap = CreateImage();
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageCroppedResult(bitmap, ImageFormat.Png, quality));
        using var result = new ImageCroppedResult(bitmap.Copy(), ImageFormat.Png, 100);
        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => result.SaveAsync(stream, SKEncodedImageFormat.Jpeg, quality));
        await Assert.ThrowsAsync<NotSupportedException>(() => result.SaveAsync(stream, SKEncodedImageFormat.Bmp));
    }

    [Fact]
    public async Task ByteAndBase64ResultsSaveWithoutNativeEncoding()
    {
        var bytes = new byte[] { 1, 2, 3 };
        using var binary = new ImageCroppedResult(bytes, ImageFormat.Png);
        using var base64 = new ImageCroppedResult(Convert.ToBase64String(bytes), ImageFormat.Png);
        foreach (var result in new[] { binary, base64 })
        {
            Assert.Equal(bytes, await result.GetDataAsync());
            using var stream = new MemoryStream();
            await result.SaveAsync(stream);
            Assert.Equal(bytes, stream.ToArray());
        }
        binary.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => binary.GetDataAsync());
    }

    private static Cropper CreateCropper() => new Cropper
    {
        _image = new ImageData { Width = 4, Height = 4 },
        _imgContainerWidth = 4,
        _imgContainerHeight = 4,
        initCropWidth = 2,
        initCropHeight = 2,
        _prevPosX = 1,
        _prevPosY = 1
    };

    private static SKBitmap CreateImage()
    {
        var bitmap = new SKBitmap(4, 4);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 60), (byte)(y * 60), 100));
        return bitmap;
    }

    private static SKColor[] CornerColors(SKBitmap bitmap) => new[]
    {
        bitmap.GetPixel(bitmap.Width / 4, bitmap.Height / 4),
        bitmap.GetPixel(bitmap.Width * 3 / 4, bitmap.Height / 4),
        bitmap.GetPixel(bitmap.Width / 4, bitmap.Height * 3 / 4),
        bitmap.GetPixel(bitmap.Width * 3 / 4, bitmap.Height * 3 / 4)
    };

    private static byte[] AddExifOrientation(byte[] jpeg, int orientation)
    {
        using var exif = new MemoryStream();
        using var writer = new BinaryWriter(exif, Encoding.UTF8, leaveOpen: true);
        writer.Write(new byte[] { (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0 });
        writer.Write((ushort)0x4949); // Little-endian TIFF header.
        writer.Write((ushort)42);
        writer.Write(8u);
        writer.Write((ushort)1); // One IFD entry: orientation.
        writer.Write((ushort)0x0112);
        writer.Write((ushort)3);
        writer.Write(1u);
        writer.Write((ushort)orientation);
        writer.Write((ushort)0);
        writer.Write(0u);
        var segment = exif.ToArray();
        using var result = new MemoryStream();
        result.Write(jpeg, 0, 2);
        result.Write(new byte[] { 0xff, 0xe1, 0, (byte)(segment.Length + 2) });
        result.Write(segment);
        result.Write(jpeg, 2, jpeg.Length - 2);
        return result.ToArray();
    }

    public enum ImageEncoding { Png, Jpeg, Webp }

    private static ImageFormat GetFormat(ImageEncoding encoding) => encoding switch
    {
        ImageEncoding.Jpeg => ImageFormat.Jpeg,
        ImageEncoding.Webp => ImageFormat.Webp,
        _ => ImageFormat.Png
    };

    private sealed class RecordingRuntime : IJSRuntime
    {
        public string Identifier { get; private set; }
        public object Result { get; set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            Identifier = identifier;
            return ValueTask.FromResult(Result is TValue value ? value : default);
        }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class PartialReadStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }
        public PartialReadStream(byte[] bytes) : base(bytes) { }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => base.ReadAsync(buffer[..Math.Min(buffer.Length, 7)], cancellationToken);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => CopyInChunks(destination, cancellationToken);
        private async Task CopyInChunks(Stream destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[7];
            int read;
            while ((read = await ReadAsync(buffer, cancellationToken)) > 0)
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
