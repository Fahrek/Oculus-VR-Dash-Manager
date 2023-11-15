﻿using System;
using System.Timers;
using System.Windows;

namespace OVR_Dash_Manager.Forms
{
    /// <summary>
    /// Interaction logic for frm_TestWindow.xaml
    /// </summary>
    public partial class frm_TestWindow : Window
    {
        public frm_TestWindow() => InitializeComponent();

        void AddToReadOut(string Text)
        {
            Functions_Old.DoAction(this, new Action(delegate () { txtbx_ReadOut.AppendText(Text + "\r\n"); txtbx_ReadOut.ScrollToEnd(); }));
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer_Functions.CreateTimer("Test_Function", TimeSpan.FromSeconds(1), Test_Function);
            Timer_Functions.StartTimer("Test_Function");
        }

        void Window_Closed(object sender, EventArgs e)
        {
            Timer_Functions.StopTimer("Test_Function");
            Timer_Functions.DisposeTimer("Test_Function");
        }

        void Test_Function(object sender, ElapsedEventArgs args)
        {
        }

        void btn_ChangeSteamRunTime_Click(object sender, RoutedEventArgs e)
        {
            Software.Steam_VR_Settings.Set_SteamVR_Runtime();
        }

        void btn_ChangeOculusRunTime_Click(object sender, RoutedEventArgs e)
        {
            Software.Oculus_Link.SetToOculusRunTime();
        }
    }
}