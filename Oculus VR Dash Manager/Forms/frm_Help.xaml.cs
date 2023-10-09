﻿using System.Windows;

namespace OVR_Dash_Manager.Forms
{
    /// <summary>
    /// Interaction logic for frm_Help.xaml
    /// </summary>
    public partial class frm_Help : Window
    {
        public frm_Help()
        {
            InitializeComponent();
        }

        private void btn_GitHub_Click(object sender, RoutedEventArgs e)
        {
            Functions_Old.OpenURL("https://github.com/DevOculus-Meta-Quest/Oculus-VR-Dash-Manager");
        }
    }
}