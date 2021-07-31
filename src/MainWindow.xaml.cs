using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CountdownTimer
{
    public partial class MainWindow : Window
    {
        DateTime _endTime = new DateTime();
        System.Windows.Threading.DispatcherTimer _dispatcherTimer;
        string _initialTitle = string.Empty;
        string _storagePath => Path.Combine(Directory.GetCurrentDirectory(), "config.txt");
        string _soundPath => Path.Combine(Directory.GetCurrentDirectory(), "resources/Alarm-ringtone.mp3");
        int _soundDuration = 1500;

        public MainWindow()
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            InitializeComponent();
            _initialTitle = Title;

            var selectedDeviceName = GetSelectedDeviceName();

            if (!string.IsNullOrEmpty(selectedDeviceName) && GetDeviceId(selectedDeviceName) == -1)
            {
                var deviceNames = string.Empty;

                foreach (var deviceName in GetDeviceNames())
                    deviceNames += "\n" + deviceName;

                ShowError($"Invalid device name found at {_storagePath}.\n\n" +
                         $"The available device names are: \n{deviceNames}");
            }
        }

        private void StartOrStop_Click(object sender, RoutedEventArgs e)
        {
            if (_dispatcherTimer == null)
                Start();
            else
                Stop();
        }

        private void Start()
        {
            _endTime = DateTime.UtcNow;
            try
            {
                var text = int.Parse(MinutesOrHours.Text);

                if (TimeType.Text == "Seconds")
                    _endTime += TimeSpan.FromSeconds(text);
                else if (TimeType.Text == "Minutes")
                    _endTime += TimeSpan.FromMinutes(text);
                else
                    _endTime += TimeSpan.FromHours(text);

                _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _dispatcherTimer.Tick += OnTick;
                _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
                _dispatcherTimer.Start();

                StartOrStop.Content = "Stop";
                InputContainer.Visibility = Visibility.Collapsed;
                Remaining.Visibility = Visibility.Visible;
                UpdateTimeLeft(DateTime.UtcNow);
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
        }

        async void OnTick(object sender, EventArgs e)
        {
            if (DateTime.UtcNow < _endTime)
            {
                UpdateTimeLeft(DateTime.UtcNow);
                return;
            }

            Stop();
            await PlaySound();
        }

        void Stop()
        {
            _dispatcherTimer?.Stop();
            _dispatcherTimer = null;
            _endTime = default;

            Title = _initialTitle;
            StartOrStop.Content = "Start";
            InputContainer.Visibility = Visibility.Visible;
            Remaining.Visibility = Visibility.Collapsed;
        }

        private async Task PlaySound()
        {
            try
            {
                using var waveOut = new WaveOut();
                using var audioFileReader = new AudioFileReader(_soundPath);
                waveOut.DeviceNumber = GetDeviceId(GetSelectedDeviceName());
                waveOut.Init(audioFileReader);
                waveOut.Play();
                await Task.Delay(_soundDuration);
                waveOut.Stop();
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
        }

        private void UpdateTimeLeft(DateTime now)
        {
            var remaining = FormatRemainingTime(_endTime - now);
            Title = $"Countdown - {remaining}";
            Remaining.Text = remaining;
        }

        string GetSelectedDeviceName()
        {
            try
            {
                return File.ReadAllText(_storagePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, icon: MessageBoxImage.Error);
        }

        private static string FormatRemainingTime(TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
                return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

            if (timeSpan.Hours > 0)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

            return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }

        private static int GetDeviceId(string name)
        {
            // This is needed because the device names
            // returned by GetDeviceNames() are also truncated
            if (name.Length > 31)
                name = name.Substring(0, 31);

            int id = -1;

            foreach (string n in GetDeviceNames())
            {
                ++id;
                if (n.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return id;
            }

            return -1;
        }

        public static string[] GetDeviceNames()
        {
            var deviceCount = WaveOut.DeviceCount;
            var result = new string[deviceCount];

            for (var n = 0; n < deviceCount; n++)
                result[n] = WaveOut.GetCapabilities(n).ProductName;

            return result;
        }
    }
}
