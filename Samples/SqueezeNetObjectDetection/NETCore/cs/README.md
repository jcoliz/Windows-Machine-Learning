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
PS C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs> dotnet run -- --model=SqueezeNet.onnx --device=LifeCam

Using launch settings from C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs\Properties\launchSettings.json...
Loading modelfile 'SqueezeNet.onnx' on the 'default' device...
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

## Copy bits to target device

Currently, the container image must be built on an IoT Core device. At this point, we will copy the bits over to our device. 
In this case, I have mapped the Q: drive on my development PC to the C: drive on my IoT Core device.

```
PS C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs> robocopy bin\Debug\netcoreapp2.1\win-x64\publish\ q:\data\modules\squeezenet

-------------------------------------------------------------------------------
   ROBOCOPY     ::     Robust File Copy for Windows
-------------------------------------------------------------------------------

  Started : Friday, December 21, 2018 4:20:48 PM
   Source : C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs\bin\Debug\netcoreapp2.1\win-x64\publish\
     Dest : q:\data\modules\squeezenet\

    Files : *.*

  Options : *.* /DCOPY:DA /COPY:DAT /R:1000000 /W:30

------------------------------------------------------------------------------
```

Also, I'll need to copy over the Dockerfile, the model, and the tags:

```
PS C:\Users\J\source\repos\Windows-Machine-Learning\Samples\SqueezeNetObjectDetection\NETCore\cs> Copy-Item .\Dockerfile.windows-x64 , .\SqueezeNet.onnx , .\Labels.json -Destination q:\data\modules\squeezenet
```

## Containerize the sample app

Build the container on the device. For the remainder of this sample, we will use the environment variable $Container
to refer to the address of our container.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> $Container = "{ACR_NAME}.azurecr.io/squeezenet:1.0.0-x64"

PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker build . -f .\Dockerfile.windows-x64 -t $Container

Sending build context to Docker daemon  81.89MB
Step 1/5 : FROM mcr.microsoft.com/windows/nanoserver/insider:10.0.17763.55
 ---> 91da8a971b53
Step 2/5 : ARG EXE_DIR=bin/Debug/netcoreapp2.1/win-x64/publish
 ---> Running in b537bd4962d6
Removing intermediate container b537bd4962d6
 ---> 6d6281589c30
Step 3/5 : WORKDIR /app
 ---> Running in b8f3943ab2e5
Removing intermediate container b8f3943ab2e5
 ---> 37f5488097e5
Step 4/5 : COPY $EXE_DIR/ ./
 ---> 49f265682955
Step 5/5 : CMD [ "SerialWin32.exe", "-rte", "-dPID_6001" ]
 ---> Running in 1aedd449ffa4
Removing intermediate container 1aedd449ffa4
 ---> d6cbd51600e3
Successfully built d6cbd51600e3
    Successfully tagged {ACR_NAME}.azurecr.io/serialwin32:1.0.0-x64
```

## License

MIT. See [LICENSE file](https://github.com/Microsoft/Windows-Machine-Learning/blob/master/LICENSE).
