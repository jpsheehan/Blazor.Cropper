# Blazor.Cropper

A fork of [Chronostasys's Blazor.Cropper](https://github.com/Chronostasys/Blazor.Cropper) using [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT licensed) for image processing.

![GitHub](https://img.shields.io/github/license/jpsheehan/Blazor.Cropper)
![Nuget](https://img.shields.io/nuget/v/Sheehan.Blazor.Cropper)

A blazor library provide a component to crop image  
![](imgs/base.gif)=>  
![](imgs/1.gif) ![](imgs/2.gif) ![](imgs/3.gif)  
  
It is:
- almost full c#
- fast
- mobile compatible
- lightweight
- support proportion
- PNG, JPEG and WebP output
- support [maui(blazor)](https://github.com/dotnet/maui)
- open source on [github](https://github.com/jpsheehan/Blazor.Cropper)  

If you find Blazor.Cropper helpful, you could **star this repo**, it's really important to me.  

For a long time, crop image in blazor bother me a lot. That's why I tried to implement a cropper in blazor.    

## Users
- [CnGal](https://www.cngal.org/) CnGal是一个非营利性的，立志于收集整理国内制作组创作的中文Galgame/AVG的介绍、攻略、评测、感想等内容的资料性质的网站。

## Maui Usage (new)
Blazor.Cropper now supports [maui(blazor)](https://github.com/dotnet/maui)!  
In some platforms, you may need to use `FilePicker` to get the input image
rather than using html input element. You can find details in the [maui sample project](https://github.com/Chronostasys/CropperMaui)

## dotnet 6 changes
Although most of apis remains the same, there're some new apis which provide better 
performance in .Net 6.  
In dotnet 5, using `ImageCroppedResult.GetBase64Async();` to get the base64 result is fine. 
In dotnet 6, it should be replaced with `ImageCroppedResult.GetDataAsync();`, which may combined 
with new `SetImageAsync(this IJSRuntime js,byte[] bin,string imgid)` api to display the crop result.  
You may find the current sample [here](CropperSample).

## Server-side Usage
Blazor.Cropper is designed to be a client side library. However, [it can be used on server side blazor when setting `PureCSharpProcessing="true"`](https://github.com/Chronostasys/Blazor.Cropper/issues/30).  
Please note that using Blazor.Cropper on server side could consume remarkable amount of server resources(including bandwidth, cpu and memory).  
[Sample project](https://github.com/Chronostasys/ServerSideCropperExample)  

## Image processing and migration

ImageSharp has been replaced with SkiaSharp. Browser canvas processing remains the default; set `PureCSharpProcessing="true"` to use SkiaSharp.

- `CropInfo.Rectangle` now uses `System.Drawing.Rectangle` from the cross-platform primitives library; this does not require `System.Drawing.Common`.
- `ImageCroppedResult.Img` is now a `SkiaSharp.SKBitmap`. Each result owns its bitmap, so dispose the result after use.
- `ImageCroppedResult.Format` now uses `Blazor.Cropper.ImageFormat`, with `Png`, `Jpeg` and `Webp` instances. `Format.DefaultMimeType` and `Format.Name` remain available.
- Replace `SaveAsync(stream, imageSharpEncoder)` with `SaveAsync(stream, SKEncodedImageFormat.Png)` or another supported format. An optional third argument controls JPEG/WebP quality from 0 to 100.
- JPEG and WebP retain their input format. Other supported inputs are saved as PNG. Animated GIF output is no longer supported; GIF inputs produce static crops. `AnimeGifEnable` is obsolete and has no effect.
- SkiaSharp's decoder supports common formats including PNG, JPEG, WebP, BMP and GIF. ImageSharp-only formats such as TIFF, TGA and PBM are no longer supported in pure C# mode.

Windows, macOS, Linux and WebAssembly native assets are included through the package dependencies. Blazor WebAssembly applications using pure C# processing must set `<WasmBuildNative>true</WasmBuildNative>` in their project and install the WebAssembly build tools to link SkiaSharp's native library:

```sh
dotnet workload install wasm-tools
```

The library, sample, and tests target .NET 10. Build the sample with the .NET 10 SDK and its `wasm-tools` workload. Applications that only use browser canvas processing can set `<ShouldIncludeNativeSkiaSharp>false</ShouldIncludeNativeSkiaSharp>` in their project to skip native linking.

For MAUI, add the matching `SkiaSharp.NativeAssets.Android`, `SkiaSharp.NativeAssets.iOS` or `SkiaSharp.NativeAssets.MacCatalyst` package to the host application as needed.

Run the regression checks with `dotnet test Blazor.Cropper.Tests/Blazor.Cropper.Tests.csproj` and `node --test Blazor.Cropper.Tests/CropHelper.test.cjs`.


## Quick Start
Only 4 steps to use Blazor.Cropper
### Step0. Add nuget  pkg
Install our nuget pkg at [nuget.org](https://www.nuget.org/packages/Sheehan.Blazor.Cropper). 
Add namespace to `_import.razor`:  
```razor
@using Blazor.Cropper
```
### Step1. Add script referrence
Then, you should paste following code into your index.html:  
```html
<script src="_content/Sheehan.Blazor.Cropper/CropHelper.js"></script>
```
### Step2. Add cropper
Just add cropper to your code. We recommend you to use it inside a modal card.  


Example:
```razor
@* .... some code ...*@
<InputFile OnChange="OnInputFileChange"></InputFile>
<Cropper ImageFile="file" ></Cropper>
@* .... some code ...*@
    async Task OnInputFileChange(InputFileChangeEventArgs args)
    {
        file = args.File;
    }
@* .... some code ...*@
@* Example below shows how to bind Cropper with an InputFile component using InputId.
    Please note that you must set ImageFile to use PureCSharpProcessing.
    <InputFile id="input1"></InputFile>
    <Cropper InputId="input1" ></Cropper>
*@
```

### Step3. Get result
To get the crop result, you need to get the reference of the `Cropper`, then call the `Cropper.GetCropedResult()` method.  
Example:  
```razor
@* .... some code ...*@
<Cropper ImageFile="file" @ref="cropper"></Cropper>
@* .... some code ...*@
@code{
    Cropper cropper;
    @* .... some code ...*@
    void GetCropResult()
    {
        var re = cropper.GetCropedResult();

        // in dotnet 6 or later
        var buffer = await re.GetDataAsync();
        // donot transfer bytes to base64 in dotnet 6.
        // if you want to display the crop result, use 
        // SetImageAsync(this IJSRuntime js,byte[] bin,string imgid) to
        // do the job



        // otherwise, get base64 directly
        // var base64 = await re.GetBase64Async();
    }
    @* .... some code ...*@
}
```


## Api referrence
We have detailed xml comments on Cropper's properties & methods, simply read it while use it!  
On the other hand, you can go to [the sample project](CropperSample) for usage examples.
To build it, simply clone it and run it in visual studio. The running result should be like this:  
![](2020-09-26-12-29-30.png)  

