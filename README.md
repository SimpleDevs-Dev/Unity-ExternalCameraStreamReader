# Unity-ExternalCameraStreamReader

A basic example of a Unity script that is enabled to read a video stream outputted by a PiCamera 2, attached to a Rasberry Pi (see [PiCamera2Stream](https://github.com/SimpleDevs-Dev/PiCamera2Stream)).

## Dependencies:

* [PiCamera2Stream](https://github.com/SimpleDevs-Dev/PiCamera2Stream)

## Resources & Guides

* [Initial Inspiration](https://stackoverflow.com/questions/39494986/streaming-live-footage-from-camera-to-unity3d)
* [`MpegProcessor.cs`: Basic web HTTP logc](https://github.com/DanielArnett/SampleUnityMjpegViewer/blob/master/Assets/Scripts/MjpegProcessor.cs)
* [Coroutine logic of Start/Stop/Restarting the stream listener](https://discussions.unity.com/t/c-unity-stream-from-ip-camera-over-3d-plane-object/737783/3)

## Installation

1. The repo comes with a custom Unity package that can be installed into any Unity project. It is located in the root directory, as `Unity-ExternalCameraStreamReader.unitypackage`. Simply import this package into your project.
2. You must enable your project to allow HTTP requests. To do this, follow these steps:
  1. "Edit" > "Project Settings" > "Player" > "Other Settings"
  2. Locate "Configuration" > "Allow downloads over HTTP*". Change from `Not allowed` to either `Allowed in Developer Builds` or `Always Allowed` (depending on your needs).

## Usage

_The scene included in the custom package, `Demo.unity`, contains an example of how to use the provided scripts. A prefab is also provided for easy drag-and-drop._

At the minimum, you need:

* A GameObject that has the `CameraStreamReader.cs` componet attached.
* A `RawImage` component or GameObject somewhere in the scene.

You need to set up `CameraStreamReader` to receive the video stream.

1. `ScreenImage`: The `RawImage` that the footage will be drawn onto. By default, this is set to the `RawImage` component that comes within the provided prefab.
2. `URL`: The local HTTP url that the camera feed is streamed from. By default, this is set to the URL that was used in debugging.
3. `Chunk Size`: A stream contains endless data while it is running. You need to parse the stream data in chunks, and process each chunk as they come. This lets you determine how big of a chunk you want to process.
   * A chunk size that is too small will improve your game's FPS, with the cost of taking longer to receive images from the stream.
   * A chunk size that's too large will increase the chance of receiving full images per chunk, at the cost of FPS in your game.
   * A recommendated chunk size is provided as `25600`. This is based on the idea that the [PiCamera2Stream](https://github.com/SimpleDevs-Dev/PiCamera2Stream) stream outputs a 640 x 480 frame and `640 * 40 = 25600`. This number is arbitrary and completely up to you, though.
  
The component comes with Inspector buttons that you can control, which are tied to public functions in the component. Upon starting the scene, the reader will NOT be reading the stream by default. 

* To start the reader, either click the **"Start Listening"** button or call `StartListening()`.
* To stop listening, either click the **"Stop Listening"** button or call `StopListening()`.
* If you want to restart the listener while it is active, either click the **"Restart Listening"** button or call `RestartListening(null)`. You can also manually pass an `Exception` with a message, if you want to print any eror messages in response to the request to restart the reader.
