﻿using OVR_Dash_Manager.Functions;
using OVR_Dash_Manager.Software;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace OVR_Dash_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceManager _serviceManager = new ServiceManager();
        private UIManager _uiManager;
        // Declare _hoverButtonManager at the class level
        private HoverButtonManager _hoverButtonManager;
        private bool Elevated = false;
        private bool FireUIEvents = false;
        private InputSimulator Keyboard_Simulator;
        public bool Debug_EmulateReleaseMode = false;

        public MainWindow()
        {
            InitializeComponent();
            _uiManager = new UIManager(this);
            // Temporarily create HoverButtonManager without ActivateDash action
            _hoverButtonManager = new HoverButtonManager(this, pb_Normal, pb_Exit, null);

            // Now that _hoverButtonManager is created, assign the ActivateDash action
            _hoverButtonManager.SetActivateDashAction(_hoverButtonManager.ActivateDash);


            Application _This = Application.Current;
            _This.DispatcherUnhandledException += AppDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;

            Title += " v" + typeof(MainWindow).Assembly.GetName().Version;
            Topmost = Properties.Settings.Default.AlwaysOnTop;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await WindowLoadedAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                // Handle exception or rethrow if necessary
                ErrorLog(ex);
            }
        }

        private async Task WindowLoadedAsync()
        {
            btn_Diagnostics.IsEnabled = false;
            btn_OpenSettings.IsEnabled = false;
            _uiManager.UpdateStatusLabel("Starting Up");
            Elevated = Functions.Process_Functions.IsCurrentProcess_Elevated();

            Disable_Dash_Buttons();
            LinkDashesToButtons();
            _hoverButtonManager.GenerateHoverButtons();

            Dashes.Dash_Manager.PassMainForm(this);
            Software.Steam.Steam_VR_Running_State_Changed_Event += Steam_Steam_VR_Running_State_Changed_Event;

            Software.Auto_Launch_Programs.Generate_List();

            await StartupAsync();
        }

        private void Steam_Steam_VR_Running_State_Changed_Event()
        {
            // Assuming Software.Steam.Steam_VR_Server_Running is a boolean, 
            // you might want to convert it to a string message to display in the UI.
            string statusText = Software.Steam.Steam_VR_Server_Running ? "Running" : "Not Running";
            _uiManager.UpdateSteamVRStatusLabel(statusText);
            // Note: If you have multiple buttons to enable/disable based on SteamVR status, consider adding a method in UIManager to handle this.
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Software.Windows_Audio_v2.Set_To_Normal_Speaker_Auto();
            Software.ADB.Stop();
            Functions.ProcessWatcher.Stop();

            Timer_Functions.StopTimer("Hover Checker");
            Timer_Functions.DisposeTimer("Hover Checker");

            Hide();
            Software.Oculus.StopOculusServices();

            Software.Auto_Launch_Programs.Run_Closing_Programs();
        }

        private void RefreshUI()
        {
            CheckRunTime();
            _hoverButtonManager.Exit_Link.Hovered_Seconds_To_Activate = Properties.Settings.Default.Hover_Activation_Time;
            _hoverButtonManager.Oculus_Dash.Hovered_Seconds_To_Activate = Properties.Settings.Default.Hover_Activation_Time;
        }

        private async Task StartupAsync()
        {
            try
            {
                Functions.ProcessWatcher.Start();

                Functions.DeviceWatcher.DeviceConnected += Oculus_Link.StartLinkOnDevice;
                Functions.DeviceWatcher.Start();
                ADB.Start();

                Functions.ProcessWatcher.IgnoreExeName("cmd.exe");
                Functions.ProcessWatcher.IgnoreExeName("conhost.exe");
                Functions.ProcessWatcher.IgnoreExeName("reg.exe");
                Functions.ProcessWatcher.IgnoreExeName("SearchFilterHost.exe");

                if (Elevated)
                {
                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Checking Installed Dashes & Updates"; }));

                    await Dashes.Dash_Manager.GenerateDashesAsync();

                    if (!Software.Oculus.Oculus_Is_Installed)
                    {
                        Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Oculus Directory Not Found"; }));
                        return;
                    }

                    if (!Dashes.Dash_Manager.Oculus_Official_Dash_Installed())
                    {
                        Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Official Oculus Dash Not Found, Replace Original Oculus Dash"; }));
                        return;
                    }

                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Steam Watcher"; }));
                    Software.Steam.Setup();

                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Hover Buttons"; }));

                    Timer_Functions.CreateTimer("Hover Checker", TimeSpan.FromMilliseconds(250), _hoverButtonManager.CheckHover);
                    Timer_Functions.StartTimer("Hover Checker");

                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Service Manager"; }));

                    Service_Manager.RegisterService("OVRLibraryService");
                    Service_Manager.RegisterService("OVRService");

                    if (Properties.Settings.Default.RunOculusClientOnStartup)
                    {
                        Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Oculus Client"; }));
                        Software.Oculus.StartOculusClient();
                    }

                    CheckRunTime();

                    Software.Windows_Audio_v2.Setup();
                    Software.Windows_Audio_v2.Set_To_Quest_Speaker_Auto();

                    Software.Auto_Launch_Programs.Run_Startup_Programs();

                    Functions_Old.DoAction(this, new Action(delegate ()
                    {
                        btn_Diagnostics.IsEnabled = true;
                        btn_OpenSettings.IsEnabled = true;
                        lbl_SteamVR_Status.Content = "Installed: " + Software.Steam.Steam_VR_Installed;
                        lbl_CurrentSetting.Content = Software.Oculus.Current_Dash_Name;
                        _hoverButtonManager.UpdateDashButtons();
                    }));

                    FireUIEvents = true;
                }
                else
                    NotElevated();
            }
            catch (Exception ex)
            {
                ErrorLog(ex);
            }
            bool isDesktopPlusInstalled = SteamAppChecker.IsAppInstalled("DesktopPlus");

            // Update UI
            _uiManager.UpdateDesktopPlusStatusLabel(isDesktopPlusInstalled);

            // Notify the user if Desktop+ is not installed.
            if (!isDesktopPlusInstalled)
            {
                _uiManager.ShowDesktopPlusNotInstalledWarning();
            }
        }

        private void NotElevated()
        {
            _uiManager.NotifyNotElevated();
        }

        #region Dash Buttons

        private void LinkDashesToButtons()
        {
            btn_ExitOculusLink.Tag = Dashes.Dash_Type.Exit;
            btn_Normal.Tag = Dashes.Dash_Type.Normal;
            btn_SteamVR.Tag = Dashes.Dash_Type.OculusKiller;
        }

        #region Hover Buttons Enter/Leave

        private void btn_Normal_MouseEnter(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Oculus_Dash.SetHovering();
        }

        private void btn_Normal_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Oculus_Dash.StopHovering();
        }

        private void btn_ExitOculusLink_MouseEnter(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Exit_Link.SetHovering();
        }

        private void btn_ExitOculusLink_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Exit_Link.StopHovering();
        }

        #endregion Hover Buttons Enter/Leave

        private void btn_ActivateDash_Click(object sender, RoutedEventArgs e)
        {
            Disable_Dash_Buttons();

            if (sender is Button button)
                ActivateDash(button);

            Thread ReactivateButtons = new Thread(Thread_ReactivateButtons);
            ReactivateButtons.IsBackground = true;
            ReactivateButtons.Start();

            CheckRunTime();
        }

        private void Thread_ReactivateButtons()
        {
            Thread.Sleep(5000);
            Functions_Old.DoAction(this, new Action(delegate () { _hoverButtonManager.UpdateDashButtons(); }));
        }

        private void btn_OpenDashLocation_Click(object sender, RoutedEventArgs e)
        {
            _uiManager.OpenDashLocation();
        }

        #endregion Dash Buttons

        #region URL Links

        private void lbl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Functions_Old.OpenURL("https://github.com/DevOculus-Meta-Quest/OculusKiller");
        }

        private void lbl_Title_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Functions_Old.OpenURL("https://github.com/DevOculus-Meta-Quest/Oculus-VR-Dash-Manager");
        }

        #endregion URL Links

        #region Dynamic Functions

        private void ActivateDash(Button Clicked)
        {
            MoveMouseToElement(lbl_CurrentSetting);
            _hoverButtonManager.ResetHoverButtons();

            if (Clicked.Tag is Dashes.Dash_Type Dash)
            {
                if (Dashes.Dash_Manager.IsInstalled(Dash))
                {
                    if (Properties.Settings.Default.FastSwitch)
                        Dashes.Dash_Manager.Activate_FastTransition(Dash);
                    else
                        Dashes.Dash_Manager.Activate(Dash);

                    Software.Oculus.Check_Current_Dash();

                    lbl_CurrentSetting.Content = Software.Oculus.Current_Dash_Name;
                }
                else
                    lbl_CurrentSetting.Content = Dashes.Dash_Manager.GetDashName(Dash) + " Not Installed";
            }
        }

        private void Disable_Dash_Buttons()
        {
            foreach (UIElement item in gd_DashButtons.Children)
            {
                if (item is Button button)
                    button.IsEnabled = false;
            }
        }

        private void MoveMouseToElement(FrameworkElement Element)
        {
            Point relativePoint = Element.TransformToAncestor(this).Transform(new Point(0, 0));
            Point pt = new Point(relativePoint.X + Element.ActualWidth / 2, relativePoint.Y + Element.ActualHeight / 2);
            Point windowCenterPoint = pt;//new Point(125, 80);
            Point centerPointRelativeToSCreen = this.PointToScreen(windowCenterPoint);
            Functions_Old.MoveCursor((int)centerPointRelativeToSCreen.X, (int)centerPointRelativeToSCreen.Y);
        }

        #endregion Dynamic Functions

        #region Forms

        private void OpenForm(Window Form, bool DialogMode = true)
        {
            Topmost = false;

            if (DialogMode)
            {
                Form.ShowDialog();
                Topmost = Properties.Settings.Default.AlwaysOnTop;
                RefreshUI();
            }
            else
                Form.Show();
        }

        private void btn_OculusServiceManager_Click(object sender, RoutedEventArgs e)
        {
            if (!FireUIEvents)
                return;

            Forms.frm_Oculus_Service_Control ServiceControl = new Forms.frm_Oculus_Service_Control();
            OpenForm(ServiceControl);
        }

        private void btn_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            Forms.Settings.frm_Settings_v2 Settings = new Forms.Settings.frm_Settings_v2();
            //Forms.frm_Settings Settings = new Forms.frm_Settings();
            OpenForm(Settings);
        }

        private bool Get_Properties_Setting(string SettingName)
        {
            bool Setting = false;

            try
            {
                Setting = (bool)Properties.Settings.Default[SettingName];
            }
            catch (Exception)
            {
                return false;
            }

            return Setting;
        }

        private void btn_Diagnostics_Click(object sender, RoutedEventArgs e)
        {
            Forms.frm_Diagnostics Settings = new Forms.frm_Diagnostics();
            OpenForm(Settings);
        }

        private void btn_CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (!FireUIEvents)
                return;

            Forms.frm_UpdateChecker Settings = new Forms.frm_UpdateChecker();
            OpenForm(Settings);
        }

        private void btn_Help_Click(object sender, RoutedEventArgs e)
        {
            Forms.frm_Help Settings = new Forms.frm_Help();
            OpenForm(Settings);
        }

        private void lbl_TestAccess_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    Forms.frm_TestWindow TestWindow = new Forms.frm_TestWindow();
                    OpenForm(TestWindow, false);
                }
            }
        }

        private void btn_OpenSteamVRSettings_Click(object sender, RoutedEventArgs e)
        {
            Forms.frm_SteamVR_Settings Settings = new Forms.frm_SteamVR_Settings();
            OpenForm(Settings);
        }

        #endregion Forms

        public void Cancel_TaskView_And_Focus()
        {
            if (Keyboard_Simulator == null)
                Keyboard_Simulator = new InputSimulator();

            Keyboard_Simulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);

            this.Topmost = true;
            this.BringIntoView();
            this.Topmost = Properties.Settings.Default.AlwaysOnTop;
        }

        #region OpenXR Runtime

        private void btn_RunTime_SteamVR_Checked(object sender, RoutedEventArgs e)
        {
            if (!FireUIEvents)
                return;

            Software.Steam_VR_Settings.Set_SteamVR_Runtime();
        }

        private void btn_RunTime_Oculus_Checked(object sender, RoutedEventArgs e)
        {
            if (!FireUIEvents)
                return;

            Software.Oculus_Link.SetToOculusRunTime();
        }

        public void CheckRunTime()
        {
            Software.Steam_VR_Settings.OpenXR_Runtime CurrentRuntime = Software.Steam_VR_Settings.Read_Runtime();

            if (CurrentRuntime == Software.Steam_VR_Settings.OpenXR_Runtime.Oculus)
                Functions_Old.DoAction(this, new Action(delegate () { btn_RunTime_Oculus.IsChecked = true; }));

            if (CurrentRuntime == Software.Steam_VR_Settings.OpenXR_Runtime.SteamVR)
                Functions_Old.DoAction(this, new Action(delegate () { btn_RunTime_SteamVR.IsChecked = true; }));
        }

        #endregion OpenXR Runtime

        private void ErrorLog(Exception e)
        {
            try
            {
                File.AppendAllText("ErrorLog.txt", Environment.NewLine +
                                                   Environment.NewLine +
                                                   " ------ " +
                                                   DateTime.Now.ToString(CultureInfo.InvariantCulture) +
                                                   " ------" +
                                                   Environment.NewLine +
                                                   e.Message +
                                                   Environment.NewLine +
                                                   e.StackTrace);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to write to ErrorLog.txt: " + ex.Message);
            }
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorLog(e.Exception);
            e.Handled = true;
        }

        private void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ErrorLog((Exception)e.ExceptionObject);
        }

        private void btn_StartSteamVR_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Assuming Steam is installed in the default location
                Process.Start(@"C:\Program Files (x86)\Steam\steam.exe", "-applaunch 250820");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start SteamVR: " + ex.Message);
            }
        }

        private void btn_ExitSteamVR_Click(object sender, RoutedEventArgs e)
        {
            Steam.Close_SteamVR_Server();
        }
    }
}