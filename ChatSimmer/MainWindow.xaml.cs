using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OpenAI;
using OpenAI.Chat;

namespace ChatSimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DotNetEnv.Env.TraversePath().Load();
        }

        private void OpenChatControl(object sender, RoutedEventArgs e)
        {
            SetActiveUserControlWindow(ChatControl, ChatTabButtton);
        }

        private void OpenActivitiesControl(object sender, RoutedEventArgs e)
        {
            SetActiveUserControlWindow(ActivitiesControl, activitiesTabButtton);
        }

        /// <summary>
        /// Changes the active control window visibility
        /// </summary>
        /// <param name="control">the user control that should be active</param>
        private void SetActiveUserControlWindow(UserControl control, Button actionButton)
        {
            //Sets all control visibilities
            ChatControl.Visibility = Visibility.Collapsed;
            ActivitiesControl.Visibility = Visibility.Collapsed;


            var bc = new BrushConverter();
            ChatTabButtton.Background = (Brush)bc.ConvertFrom("#FF2F2F2F");
            activitiesTabButtton.Background = (Brush)bc.ConvertFrom("#FF2F2F2F");


            //Sets the passed in control parameter visibility to ON.
            control.Visibility = Visibility.Visible;
            actionButton.Background = (Brush)bc.ConvertFrom("#FF7D0202");
        }
    }
}