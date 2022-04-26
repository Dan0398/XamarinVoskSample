using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinRecognize
{
    public partial class MainPage : ContentPage
    {
        bool isInited;
        bool isRecording;
        bool isRecognizingFromDisk;
        bool isRecognizingRealtime;
        Plugin.AudioRecorder.AudioRecorderService Recorder;
        Plugin.AudioRecorder.AudioPlayer Player;
        Vosk.Model Model;
        Vosk.VoskRecognizer Rec;

        string DataPath => Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        string ModelPathNew => DataPath + "/VoiceModel/";
        const string ModelWebLink = "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip";

        string DownLoadedFileName
        {
            get
            {
                var Preresult = ModelWebLink.Split('/');
                return Preresult[Preresult.Length - 1];
            }
        }
        string ArchivePath => DataPath + DownLoadedFileName;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RequestAllPermissions();
            if (!isInited)
            {
                isInited = true;
                InitRecorder();
                InitPlayer();
                InitRecognizer();
            }
        }
        private void Button_Clicked(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(ModelPathNew))
            {
                System.IO.Directory.Delete(ModelPathNew, true);
            }
            DeleteArchive();
        }

        async Task RequestAllPermissions()
        {
            await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.Microphone>();
            await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageRead>();
            await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.StorageWrite>();
            DependencyService.Resolve<IPermissions>().RequestPermissions();
        }

        void InitRecorder()
        {
            Recorder = new Plugin.AudioRecorder.AudioRecorderService
            {
                PreferredSampleRate = 16000,
                StopRecordingOnSilence = false,
                StopRecordingAfterTimeout = false
            };
        }

        void InitPlayer()
        {
            Player = new Plugin.AudioRecorder.AudioPlayer();
        }

        async void InitRecognizer()
        {
            await CheckModelFolder();
            System.Threading.Thread LaunchAndShowRecogModule = new System.Threading.Thread(() =>
            {
                Model = new Vosk.Model(ModelPathNew);
                Rec = new Vosk.VoskRecognizer(Model, 16000);
            });
            LaunchAndShowRecogModule.Start();
            while (LaunchAndShowRecogModule.IsAlive) await Task.Yield();
            RecogFromDiskColdStack.IsVisible = true;
            RecognizeRealtimeStack.IsVisible = true;
            InitRecogLabel.IsVisible = false;
        }

        async Task CheckModelFolder()
        {
            bool isModelReady = true;
            bool isZipLoaded = true;
            if (!System.IO.Directory.Exists(ModelPathNew))
            {
                System.IO.Directory.CreateDirectory(ModelPathNew);
                isZipLoaded = false;
                isModelReady = false;
            }
            if (System.IO.Directory.GetDirectories(ModelPathNew).Length == 0)
            {
                isModelReady = false;
            }
            if (!System.IO.File.Exists(ArchivePath))
            {
                isZipLoaded = false;
            }
            if (!isModelReady)
            {
                if (!isZipLoaded)
                {
                    bool IsPickedYes = await DisplayAlert("Downloading", "For start voice recognizing you need to download a voice modek (over 50MB)", "Download", "Quit");
                    if (!IsPickedYes) System.Diagnostics.Process.GetCurrentProcess().Kill();
                    await DownloadModel();
                }
                Unzip();
                MoveToModelFolder();
                DeleteArchive();
            }
        }

        async Task DownloadModel()
        {
            try
            {
                using (var Client = new System.Net.WebClient())
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                    await Client.DownloadFileTaskAsync(new Uri(ModelWebLink), ArchivePath);
                    while (Client.IsBusy) await Task.Yield();
                    Client.Dispose();
                }
            }
            catch (System.Net.WebException ex)
            {
                Button_Clicked(null, null);
                await DisplayAlert("Fail...", ex.Message + "\n" + ex.InnerException + "\n" + ex.TargetSite, "Молодец");
                throw ex;
            }
        }

        void Unzip()
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(ArchivePath, ModelPathNew);
        }

        void MoveToModelFolder()
        {
            var ShellFolder = System.IO.Directory.GetDirectories(ModelPathNew)[0];
            var TargetFolders = System.IO.Directory.GetDirectories(ShellFolder);
            for (int i = 0; i < TargetFolders.Length; i++)
            {
                var info = new System.IO.DirectoryInfo(TargetFolders[i]);
                info.MoveTo(ModelPathNew + info.Name);
            }
            string[] TargetFiles = System.IO.Directory.GetFiles(ShellFolder);
            for (int i = 0; i < TargetFiles.Length; i++)
            {
                var info = new System.IO.FileInfo(TargetFiles[i]);
                info.MoveTo(ModelPathNew + info.Name);
            }
            System.IO.Directory.Delete(ShellFolder, true);
        }

        void DeleteArchive()
        {
            if (System.IO.File.Exists(ArchivePath))
            {
                System.IO.File.Delete(ArchivePath);
            }
        }

        private async void SwitchRecord(object sender, EventArgs e)
        {
            isRecording = !isRecording;
            if (isRecording)
            {
                (sender as Button).Text = "Stop Record";
                await Recorder.StartRecording();
            }
            else
            {
                (sender as Button).Text = "Start Record";
                await Recorder.StopRecording();
            }
        }

        private void PlayRecord(object sender, EventArgs e)
        {
            Player.Play(Recorder.FilePath);
        }

        private async void RecognizeFromDisk(object sender, EventArgs e)
        {
            if (isRecognizingFromDisk) return;
            isRecognizingFromDisk = true;
            (sender as Button).Text = "Recognizing in process...";
            using (var Reader = System.IO.File.OpenRead(Recorder.FilePath))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                string Result = String.Empty;
                while ((bytesRead = await Reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    Rec.AcceptWaveform(buffer, bytesRead);
                    Result = Rec.PartialResult();
                    if (Result.Length > 20)
                    {
                        Result = Result.Substring(17, Result.Length - 20);
                        RecognizeFromDiskResult.Text = Result;
                    }
                    else
                    {
                        RecognizeFromDiskResult.Text = String.Empty;
                    }
                }
                Result = Rec.FinalResult();
                if (Result.Length > 20) Result = Result.Substring(14, Result.Length - 17);
                RecognizeFromDiskResult.Text = Result;
                Reader.Close();
                Reader.Dispose();
            }
            (sender as Button).Text = "Recognize recorded sample";
            isRecognizingFromDisk = false;
        }

        void RecognizeRealtime(object sender, EventArgs e)
        {
            isRecognizingRealtime = !isRecognizingRealtime;
            if (isRecognizingRealtime)
            {
                StartRecognizeCycle();
                (sender as Button).Text = "Stop Recognize";
            }
            else
            {
                (sender as Button).Text = "Start Recognize";
            }
        }

        async void StartRecognizeCycle()
        {
            byte[] buffer = new byte[4096];
            int bytesRead;
            string Result;
            await Recorder.StartRecording();
            var Reader = Recorder.GetAudioFileStream();
            while (isRecognizingRealtime)
            {
                bytesRead = await Reader.ReadAsync(buffer, 0, buffer.Length);
                Rec.AcceptWaveform(buffer, bytesRead);
                Result = Rec.PartialResult();
                if (Result.Length > 20)
                {
                    Result = Result.Substring(17, Result.Length - 20);
                    RecognizeRealtimeResult.Text = Result;
                }
                else
                {
                    RecognizeRealtimeResult.Text = String.Empty;
                }
            }
            Result = Rec.FinalResult();
            if (Result.Length > 20) Result = Result.Substring(14, Result.Length - 17);
            RecognizeRealtimeResult.Text = Result;
            await Recorder.StopRecording();
            Reader.Close();
            Reader.Dispose();
        }
    }
}