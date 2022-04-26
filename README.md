### Xamarin Recognizer
A simple and self-sufficient example application for speech recognition on the Xamarin platform. The example is only available for the Android platform.
The basic syntax and logic are in the file `Xamarin.Recognize/MainPage.xaml.cs`.
The application consists of 3 logical parts:
1. Audio part. Provided by the plugin Plugin.AudioRecorder from [NUGET Library](https://www.nuget.org/packages/Plugin.AudioRecorder"). A separate menu item "Recording Test" is available in the application, inside which you can start / stop recording ("Start Record" / StopRecord"), as well as listen to the recording ("Play Sample").
2. Preparing model. Provided by method CheckModelFolder and called under methods:
	 - Checks the presence of the model in the folder. If there is, then it gives the folder to Vosk.
    - If not, checks if the archive has been downloaded. If downloaded, it unzips, and moves the result to the target folder of the model. The archive is being deleted
    - If not, then it download.

3.Recognize module. Provided by [Vosk Library](https://alphacephei.com/vosk/). The application has 2 blocks of working with the application:
 - RecognizeRecordedSample. Recognizes a previously recorded audio file.
 - Recognize Real time. Simultaneously starts audio recordings, and throws the actual audio data into Vosk.

### Connect Vosk
First you need to connect the library. In the current project, everything is already connected for armv7. How to do it yourself?
1. Go to [Github page](https://github.com/alphacep/vosk-api/releases).
2. In the actual/liked version, select `vosk-linux-armv7l-*****.zip` for android devices and `vosk-linux-x86_64-******.zip` for emulators.
3. Files contained in the archives `libvosk.so` put in `[Android Project folder]/libs/`
    - `armeabi-v7a/` from the archive `vosk-linux-armv7l...`
    - `x86_64/` from the archive `vosk-linux-x86_64...`
4. Next , you need to connect .dll to the main Xamarin project. You can take ` XamarinVoskSample/Vosk.dll`, this is the result of the build version 0.3.32 => SourceCode.zip, in the archive `vosk-api-0.3.32/csharp/nuget/src'. You can even throw them into a folder separately, it won't change much. If it is still a library, then next in the project itself, select Dependencies => Add Project Reference => Browse... and choose .dll file
5. Choose the right [model from list](https://alphacephei.com/vosk/models). You add a link to the model to your application.
6. I haven't figured out how to ship an app with a pre-installed model. Therefore, you will have to download runtime. Sample methods can be taken in `Xamarin.Recognize/MainPage.xaml.cs => CheckModelFolder() and called under methods`.


### Problems and solutions
* Android Permissions. To use the microphone, you need the `Record_audio` and `Capture_audio_output` permissions in the manifest, as well as request permission to use it in the application itself. Xamarin.Essentials solves this problem in the `RequestAllPermissions()` method of the `Xamarin' file.Recognize/MainPage.xaml.cs`
* The start of the module causes the application to hang. The Vosk developers in the examples/library itself do not provide an opportunity to load the model into RAM asynchronously. In my application, the module is initialized in a separate thread. This approach works fast enough.