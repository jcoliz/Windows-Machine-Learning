# SqueezeNet Object Detection Module

This is a sample module showing how to run Windows ML inferencing in an Azure IoT Edge module running on Windows. 
Images are supplied by a connected camera, inferenced against the SqueezeNet model, and sent to IoT Hub.

This code is derived from the 
[NetCore SqueezeNetObjectDetection](https://github.com/Microsoft/Windows-Machine-Learning/tree/master/Samples/SqueezeNetObjectDetection/NETCore/cs) sample published in the [Windows ML Repo](https://github.com/Microsoft/Windows-Machine-Learning).

## Install Azure IoT Edge

These instructions work with the 1.0.5 release of [Azure IoT Edge for Windows](https://docs.microsoft.com/en-us/azure/iot-edge/).

## Peripheral Hardware

For this sample, a camera is required. I recommend a [LifeCam Cinema](https://www.microsoft.com/accessories/en-us/webcams).

## Prerequisites

- [Windows 10 - Build 17763 or higher](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewiso) to build the solution
- [Windows 10 IoT Core - Build 17763 or higher](https://docs.microsoft.com/en-us/windows/iot-core/windows-iot-core) to run the solution. Currently, this solution only runs on Windows 10 IoT Core hardware.
- [Windows SDK - Build 17763 or higher](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewSDK)

## Build the sample

To get access to Windows.AI.MachineLearning and various other Windows classes an assembly reference needs to be added for Windows.winmd
For this project the assembly reference is parametrized by the environment variable WINDOWS_WINMD, so you need to set this environment variable before building.
The file path for the Windows.winmd file may be: ```C:\Program Files (x86)\Windows Kits\10\UnionMetadata\[version]\Windows.winmd```

1. If you download the samples ZIP, be sure to unzip the entire archive, not just the folder with the sample you want to build.
2. Open a PowerShell window.
3. Change directory to the folder where you unzipped the samples, go to the **Samples** subfolder, then the subfolder for this sample (**SqueezeNetObjectDetection**).
3. Build and publish the sample using dotnet command line:

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SqueezeNetObjectDetection\cs> dotnet publish -r win-x64

Microsoft (R) Build Engine version 15.8.169+g1ccb72aefa for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 34.7 ms for C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs\SqueezeNetObjectDetectionNC.csproj.
  SqueezeNetObjectDetectionNC -> C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs\bin\Debug\netcoreapp2.1\win-x64\SqueezeNetObjectDetectionNC.dll
  SqueezeNetObjectDetectionNC -> C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs\bin\Debug\netcoreapp2.1\win-x64\publish\
```

## Run the sample on your development machine

As a first initial step, you can run the sample natively on your development machine to ensure it's working.

First, run the app with the "--list" parameter to show the cameras on your PC:

```
PS C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs> dotnet run -- --list

Found 5 Cameras
Microsoft Camera Rear
Microsoft IR Camera Front
Microsoft Camera Front
Microsoftr LifeCam Studio(TM)
IntelIRCameraSensorGroup
```

From this list, we will choose the camera to use as input, as pass that into the next call with the --device parameter:

```
PS C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs> dotnet run -- --model="C:\Users\J\source\repos\Windows-Machine-Learning\SharedContent\models\SqueezeNet.onnx" -dLifeCam

Using launch settings from C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs\Properties\launchSettings.json...
Loading modelfile 'C:\Users\J\source\repos\Windows-Machine-Learning\SharedContent\models\SqueezeNet.onnx' on the 'default' device...
...OK 265 ticks
Color
Enumerating Frame Source Info
Selecting Source
Enumerating Frame Sources
capture initialized
have frame sources
have frame source that matches chosen source info id
have formats
hunting for format
selected videoformat -- major Video sub YUY2 w 1920 h 1080
set format complete
frame reader retrieved

Retrieving image from camera...
...OK 484 ticks
Running the model...
...OK 63 ticks

"saltshaker, salt shaker" with confidence of 0.5000542
"soap dispenser" with confidence of 0.08704852
"toaster" with confidence of 0.02377551
```

Here we can see that the sample is successfully running on the development machine, found the camera, and recognized that the camera was possibly
looking at a salt shaker.

## Create a personal container repository

In order to deploy modules to your device, you will need access to a container respository. 
Refer to [Quickstart: Create a private container registry using the Azure portal](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal).

When following the sample, replace any "{ACR_*}" values with the correct values for your container repository.

Be sure to log into the container respository from your device.

```
PS C:\data\modules\SerialWin32> docker login {ACR_NAME}.azurecr.io {ACR_USER} {ACR_PASSWORD}
```

## License

MIT. See [LICENSE file](https://github.com/Microsoft/Windows-Machine-Learning/blob/master/LICENSE).
