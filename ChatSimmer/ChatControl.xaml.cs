using OpenAI.Chat;
using OpenAI;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ChatSimmer.Configuration;
using NAudio.Wave;
using OpenAI.Audio;

namespace ChatSimmer
{
    public partial class ChatControl : UserControl
    {
        /// <summary>
        /// Collection for storing AI generated chats. Updating collection updates the listview
        /// </summary>
        public ObservableCollection<string> Chats { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Timer for automatically adding another chat to the listview.Tick time can be set from Chat Interval UI component
        /// </summary>
        public DispatcherTimer dispatchChatTimer;

        /// <summary>
        /// Collection for user supplied potential chat options. Updating this auto-updates the listview
        /// </summary>
        public ObservableCollection<string> Topics { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Variable for whether the app is closing or not. Used to help with errors from recording at same time as closing app
        /// </summary>
        private bool closing = false;

        /// <summary>
        /// Used for taking audio input and writing to .wav file
        /// </summary>
        public WaveIn waveSource = null;
        public WaveFileWriter waveFile = null;

        /// <summary>
        /// Gets populated with the CHATGPT transcribed text. Recorder automatically records sends file to OpenAI -> Stores Transcription
        /// in this property
        /// </summary>
        public string ChannelOwnerAITranscribedResponse { get; set; } = "";

        /// <summary>
        /// Timer for automatically recording the player. Happens so that the chat can stay updated with the words coming through audio
        /// </summary>
        public DispatcherTimer dispatchAudioRecordingTimer;


        /// <summary>
        /// Storage for API key after pulling from .env file
        /// </summary>
        private string openAIApiKey = String.Empty;


        public ChatControl()
        {
            InitializeComponent();

            //Get API Key from .env file
            openAIApiKey = TestConfig.GetVarFor<string>("OPENAI_API_KEY");

            //Setup automatic chat timer
            dispatchChatTimer = new System.Windows.Threading.DispatcherTimer();
            dispatchChatTimer.Tick += new EventHandler(ChatIntervalReached);
            dispatchChatTimer.Interval = TimeSpan.FromSeconds(30);
            dispatchChatTimer.Start();

            //Setup automatic mic recording timer
            dispatchAudioRecordingTimer = new System.Windows.Threading.DispatcherTimer();
            dispatchAudioRecordingTimer.Tick += new EventHandler(RecordPlayerAudioForChatEnhancement);
            dispatchAudioRecordingTimer.Interval = TimeSpan.FromSeconds(20);
            dispatchAudioRecordingTimer.Start();

            //Set the Chat Item Source
            ChatList.ItemsSource = Chats;

            //Add 5 chats on startup so it isn't empty
            InitializeChats();
            Application.Current.Exit += CurrentOnExit;
        }

        private void RecordPlayerAudioForChatEnhancement(object? sender, EventArgs e)
        {
            RecordAudioForXTime();
        }

        private void CurrentOnExit(object sender, ExitEventArgs e)
        {
            closing = true;
        }

        private void RecordAudioForXTime()
        {
            var filePath = Directory.GetCurrentDirectory() + "\\UserRecentAudio.wav";
            waveSource = new WaveIn();
            waveSource.WaveFormat = new WaveFormat(44100, 1);
            waveSource.DeviceNumber = 1;

            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);

            waveFile = new WaveFileWriter(filePath, waveSource.WaveFormat);

            waveSource.StartRecording();
        }

        private void waveSource_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }

            SendAudioForTranscription();
        }

        private void waveSource_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                //Stop recording after x seconds
                if (waveFile.Position > waveSource.WaveFormat.AverageBytesPerSecond * 10)
                {
                    waveSource.StopRecording();
                }
                waveFile.Flush();
            }
        }

        private async Task<string> GetNextChat(string activity)
        {
            OpenAIClient client = new OpenAIClient(openAIApiKey);

            if (UseRandomTopicFromListCheckbox.IsChecked.HasValue)
            {
                if (UseRandomTopicFromListCheckbox.IsChecked.Value)
                {
                    activity = GetRandomTopic();


                    Random random = new Random();
                    var randomizerInt = random.Next(0, 3);

                    switch (randomizerInt)
                    {
                        case 0:
                            activity = GetRandomTopic();
                            break;
                        case 1:
                            activity = GetRandomTopic() + " " +  GetRandomTopic();
                            break;
                        case 2:
                        {
                            if (aiSpeechRecognitionCheckbox.IsChecked.HasValue)
                            {
                                if (aiSpeechRecognitionCheckbox.IsChecked.Value)
                                {
                                    activity = ChannelOwnerAITranscribedResponse;
                                }
                                else
                                {
                                    activity = GetRandomTopic();
                                }
                            }
                        }
                        break;
                    }
                }
            }

            var prompt = ChatMessage.CreateUserMessage($"I want you to return a new generated twitch chat related to {activity}, don't include ANY extra text other than username and chat message. Don't repeat the same message twice! You can also ask random questions someone might ask a streamer. I want 1 single chat at a time when I send the text NewChat. Include a realistic gamertag.");
            var chat = ChatMessage.CreateUserMessage("NewChat");

            var openAiResponse = await client.GetChatClient("gpt-4o").CompleteChatAsync([prompt, chat]);

            return openAiResponse.Value.Content[0].Text;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var chat = await GetNextChat(CurrentActivityTextbox.Text);
            AddChat(chat);
        }

        public void AddChat(string chat)
        {
            Chats.Add(chat);
            ChatList.ScrollIntoView(chat);

            var path = Directory.GetCurrentDirectory();
            var modifiedPath = path + "\\notifications.wav";

            //System.Media.SoundPlayer player = new System.Media.SoundPlayer(modifiedPath);
            //player.Play();
        }

        private async void ChatIntervalReached(object? sender, EventArgs e)
        {
            int.TryParse(ChatInterval.Text, out int delay);

            if (delay > 0)
            {
                dispatchChatTimer.Interval = TimeSpan.FromSeconds(delay);
            }

            //Update view here
            var chat = await GetNextChat(CurrentActivityTextbox.Text);
            AddChat(chat);
        }

        async void InitializeChats()
        {
            var chat = await GetNextFiveChat(CurrentActivityTextbox.Text);
            AddChat(chat);
        }

        private async Task<string> GetNextFiveChat(string activity)
        {
            OpenAIClient client = new OpenAIClient(openAIApiKey);
            var prompt = ChatMessage.CreateUserMessage($"I want you to return a new generated twitch chat related to {activity}, don't include ANY extra text other than username and chat message. I want 5 chats at a time when I send the text NewChat. Include a realistic gamer tag.");
            var chat = ChatMessage.CreateUserMessage("NewChat");

            var openAiResponse = await client.GetChatClient("gpt-3.5-turbo").CompleteChatAsync([prompt, chat]);

            return openAiResponse.Value.Content[0].Text;
        }

        private string GetRandomTopic()
        {
            LoadTopicFile();
            Random random = new Random();
            var randomIndex = random.Next(0, Topics.Count);
            return Topics[randomIndex];
        }

        private async void LoadTopicFile()
        {
            Topics.Clear();
            var fileExists = File.Exists("activities.txt");

            if(fileExists)
            {
                using StreamReader reader = new StreamReader("activities.txt");

                while (reader.Peek() >= 0)
                {
                    var topic = await reader.ReadLineAsync();
                    if(topic!=null)
                        Topics.Add(topic);
                }
            }
        }

        private async void SendAudioForTranscription()
        {
            var filePath = Directory.GetCurrentDirectory() + "\\UserRecentAudio.wav";
            var client = new AudioClient("whisper-1", openAIApiKey);

            var response = await client.TranscribeAudioAsync(filePath);
            ChannelOwnerAITranscribedResponse = response.Value.Text;
            CurrentActivityTextbox.Text = ChannelOwnerAITranscribedResponse;
        }
    }
}
