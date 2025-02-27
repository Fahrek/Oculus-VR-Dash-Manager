﻿using OVR_Dash_Manager.Functions;
using System.Windows;

namespace OVR_Dash_Manager.Forms
{
    /// <summary>
    /// Interaction logic for frm_Help.xaml
    /// </summary>
    public partial class frm_Help : Window
    {
        public frm_Help() => InitializeComponent();

        private void btn_GitHub_Click(object sender, RoutedEventArgs e)
        {
            WebUtilities.OpenURL("https://github.com/DevOculus-Meta-Quest/Oculus-VR-Dash-Manager");
        }

        private void btn_GitIssue_Click(object sender, RoutedEventArgs e)
        {
            WebUtilities.OpenURL("https://github.com/DevOculus-Meta-Quest/Oculus-VR-Dash-Manager/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc");
        }

        private void btn_GitDiscussions_Click(object sender, RoutedEventArgs e)
        {
            WebUtilities.OpenURL("https://github.com/DevOculus-Meta-Quest/Oculus-VR-Dash-Manager/discussions");
        }

        private void btn_GetDesktopPlus_Click(object sender, RoutedEventArgs e)
        {
            WebUtilities.OpenURL("https://store.steampowered.com/app/1494460/Desktop/");
        }
    }
}