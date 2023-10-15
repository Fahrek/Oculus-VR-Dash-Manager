﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OVR_Dash_Manager.Forms.Settings
{
    public partial class frm_Settings_v2 : Window
    {
        private bool Audio_DevicesChanged = false;
        private bool FireEvents = false;

        public frm_Settings_v2()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the ItemsSource for the combo boxes and select the appropriate speaker for each
            cbo_NormalSpeaker.ItemsSource = Software.Windows_Audio_v2.Speakers;
            cbo_QuestSpeaker.ItemsSource = Software.Windows_Audio_v2.Speakers;

            cbo_NormalSpeaker.SelectedItem = Software.Windows_Audio_v2.Speakers.FirstOrDefault(a => a.Normal_Speaker);
            cbo_QuestSpeaker.SelectedItem = Software.Windows_Audio_v2.Speakers.FirstOrDefault(a => a.Quest_Speaker);

            // Reset flags
            Audio_DevicesChanged = false;
            FireEvents = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check if audio devices have changed and confirm with the user if they want to save the changes
            if (Audio_DevicesChanged)
            {
                if (MessageBox.Show(this, "Automatic Audio Devices Changed - Are you sure you want to save this ?", "Confirm Automatic Audio Device Change", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var NormalSpeaker = Software.Windows_Audio_v2.Speakers.FirstOrDefault(a => a.Normal_Speaker);
                    var QuestSpeaker = Software.Windows_Audio_v2.Speakers.FirstOrDefault(a => a.Quest_Speaker);

                    Properties.Settings.Default.Normal_Speaker_GUID = NormalSpeaker != null ? NormalSpeaker.ID : new System.Guid();
                    Properties.Settings.Default.Quest_Speaker_GUID = QuestSpeaker != null ? QuestSpeaker.ID : new System.Guid();
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void CheckSpeaker(Software.Windows_Audio_v2.IDevice_Ext speaker, bool isChecked, bool isNormal, bool isQuest)
        {
            // If FireEvents is false, exit the method early to avoid unnecessary processing
            if (!FireEvents)
                return;

            // Temporarily disable event firing to prevent potential recursive calls
            FireEvents = false;

            // Reset speaker statuses
            speaker.Quest_Speaker = false;
            speaker.Normal_Speaker = false;

            // Update the speaker statuses based on the parameters passed to the method
            if (isNormal)
            {
                // Unset Normal_Speaker for all speakers, then set it for the current speaker if isChecked is true
                Software.Windows_Audio_v2.Speakers.ForEach(a => a.Normal_Speaker = false);
                speaker.Normal_Speaker = isChecked;
            }

            if (isQuest)
            {
                // Unset Quest_Speaker for all speakers, then set it for the current speaker if isChecked is true
                Software.Windows_Audio_v2.Speakers.ForEach(a => a.Quest_Speaker = false);
                speaker.Quest_Speaker = isChecked;
            }

            // Re-enable event firing
            FireEvents = true;
        }

        private void btn_Set_Default_Normal_Click(object sender, RoutedEventArgs e)
        {
            // Find the speaker marked as the normal speaker and set it as the default playback device
            var normalSpeaker = Software.Windows_Audio_v2.Speakers.FirstOrDefault(a => a.Normal_Speaker);

            if (normalSpeaker != null)
                Software.Windows_Audio_v2.Set_Default_PlaybackDevice(normalSpeaker.ID);
            else
                MessageBox.Show(this, "Normal Speaker Not Found", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void btn_Set_Default_Quest_Click(object sender, RoutedEventArgs e)
        {
            // Find the speaker marked as the quest speaker and set it as the default playback device
            var questSpeaker = Software.Windows_Audio_v2.Speakers.FirstOrDefault(a => a.Quest_Speaker);

            if (questSpeaker != null)
                Software.Windows_Audio_v2.Set_Default_PlaybackDevice(questSpeaker.ID);
            else
                MessageBox.Show(this, "Quest Speaker Not Set", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void cbo_NormalSpeaker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbo_NormalSpeaker.SelectedItem is Software.Windows_Audio_v2.IDevice_Ext Speaker)
            {
                CheckSpeaker(Speaker, true, true, false);
            }
        }

        private void cbo_QuestSpeaker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbo_QuestSpeaker.SelectedItem is Software.Windows_Audio_v2.IDevice_Ext Speaker)
            {
                CheckSpeaker(Speaker, true, false, true);
            }
        }

        private void btn_Open_Auto_Launch_Settings_Click(object sender, RoutedEventArgs e)
        {
            Auto_Program_Launch.frm_Auto_Program_Launch_Settings pShow = new Auto_Program_Launch.frm_Auto_Program_Launch_Settings();
            pShow.ShowDialog();
        }
    }
}