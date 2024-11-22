using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatSimmer
{
    /// <summary>
    /// Interaction logic for ActivitiesControl.xaml
    /// </summary>
    public partial class ActivitiesControl : UserControl
    {
        public ObservableCollection<string> Topics { get; set; } = new ObservableCollection<string>();

        public ActivitiesControl()
        {
            InitializeComponent();
            ActivitiesList.ItemsSource = Topics;
            LoadTopicFile();
        }

        private async void LoadTopicFile()
        {

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //Add Topic
            Topics.Add(CurrentActivityTextbox.Text);
            WriteTopicsToFile();
            CurrentActivityTextbox.Clear();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Remove Topic
            if(ActivitiesList.SelectedItem != null)
                Topics.Remove(ActivitiesList.SelectedItem.ToString());

            WriteTopicsToFile();
        }


        private async void WriteTopicsToFile()
        {
            //Write all records to file
            await using StreamWriter writer = new StreamWriter("activities.txt");

            foreach (var topic in Topics)
            {
                await writer.WriteLineAsync(topic);
            }
        }
    }
}
