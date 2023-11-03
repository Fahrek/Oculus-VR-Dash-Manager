﻿using OVR_Dash_Manager.Forms;
using OVR_Dash_Manager.Functions;
using OVR_Dash_Manager.Software;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsInput;
using WindowsInput.Native;
using YOVR_Dash_Manager.Functions;

namespace OVR_Dash_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool Debug_EmulateReleaseMode = false;

        // Declare _hoverButtonManager at the class level
        private HoverButtonManager _hoverButtonManager;

        private ServiceManager _serviceManager = new ServiceManager();
        private UIManager _uiManager;
        private bool Elevated = false;
        private bool FireUIEvents = false;
        private InputSimulator Keyboard_Simulator;

        public MainWindow()
        {
            InitializeComponent();

            // Start the watchdog
            WatchdogManager.StartWatchdog();

            // Initialize UI Manager
            _uiManager = new UIManager(this);

            // Initialize HoverButtonManager without ActivateDash action
            _hoverButtonManager = new HoverButtonManager(this, pb_Normal, pb_Exit, null);

            // Assign the ActivateDash action now that _hoverButtonManager is created
            _hoverButtonManager.SetActivateDashAction(_hoverButtonManager.ActivateDash);

            // Handle unhandled exceptions
            Application.Current.DispatcherUnhandledException += AppDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;

            // Set window properties
            Title += " v" + typeof(MainWindow).Assembly.GetName().Version;
            Topmost = Properties.Settings.Default.AlwaysOnTop;
        }

        public void Cancel_TaskView_And_Focus()
        {
            if (Keyboard_Simulator == null)
                Keyboard_Simulator = new InputSimulator();

            Keyboard_Simulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);

            this.Topmost = true;
            this.BringIntoView();
            this.Topmost = Properties.Settings.Default.AlwaysOnTop;
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception using ErrorLogger
            ErrorLogger.LogError(e.Exception, "An unhandled exception occurred in the dispatcher.");

            // Handle the exception to prevent the application from terminating
            e.Handled = true;
        }

        private void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log the exception using ErrorLogger
            ErrorLogger.LogError((Exception)e.ExceptionObject, "An unhandled exception occurred in the application domain.");
        }

        private void btn_ExitSteamVR_Click(object sender, RoutedEventArgs e)
        {
            Steam.Close_SteamVR_Server();
        }

        private void btn_OtherTools_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of frm_OtherTools window
            var otherToolsWindow = new frm_OtherTools();

            // Set the main window as the owner of otherToolsWindow
            otherToolsWindow.Owner = this;

            // Show the window as a modal dialog box
            otherToolsWindow.ShowDialog();
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
                // Log the exception using ErrorLogger
                ErrorLogger.LogError(ex, "Failed to start SteamVR.");

                // Display a message box to inform the user
                MessageBox.Show("Failed to start SteamVR: " + ex.Message);
            }
        }

        private void HandleWindowLoadingException(Exception ex)
        {
            // Log the exception using ErrorLogger
            ErrorLogger.LogError(ex, "An error occurred during window loading.");

            // Inform the user about the error
            MessageBox.Show("An error occurred while loading the window. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Additional logging or actions can be added here if needed
            Debug.WriteLine($"Exception occurred: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }

        private void NotElevated()
        {
            _uiManager.NotifyNotElevated();
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
                // Start the ProcessWatcher
                Functions.ProcessWatcher.Start();

                // Set up DeviceWatcher and start it
                Functions.DeviceWatcher.DeviceConnected += Oculus_Link.StartLinkOnDevice;
                Functions.DeviceWatcher.Start();

                // Start ADB
                ADB.Start();

                // Ignore specific executables in ProcessWatcher
                Functions.ProcessWatcher.IgnoreExeName("cmd.exe");
                Functions.ProcessWatcher.IgnoreExeName("conhost.exe");
                Functions.ProcessWatcher.IgnoreExeName("reg.exe");
                Functions.ProcessWatcher.IgnoreExeName("SearchFilterHost.exe");

                // Check if elevated permissions are granted
                if (Elevated)
                {
                    // Check for installed dashes and updates
                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Checking Installed Dashes & Updates"; }));

                    // Generate dashes asynchronously
                    await Dashes.Dash_Manager.GenerateDashesAsync();

                    // Check if Oculus is installed
                    if (!Software.Oculus.Oculus_Is_Installed)
                    {
                        Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Oculus Directory Not Found"; }));
                        return;
                    }

                    // Check if the official Oculus Dash is installed
                    if (!Dashes.Dash_Manager.Oculus_Official_Dash_Installed())
                    {
                        Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Official Oculus Dash Not Found, Replace Original Oculus Dash"; }));
                        return;
                    }

                    // Start Steam Watcher
                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Steam Watcher"; }));
                    Software.Steam.Setup();

                    // Initialize hover buttons
                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Hover Buttons"; }));

                    // Set up and start the hover checker timer
                    Timer_Functions.CreateTimer("Hover Checker", TimeSpan.FromMilliseconds(250), _hoverButtonManager.CheckHover);
                    Timer_Functions.StartTimer("Hover Checker");

                    // Start the service manager
                    Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Service Manager"; }));
                    Service_Manager.RegisterService("OVRLibraryService");
                    Service_Manager.RegisterService("OVRService");

                    // Start Oculus Client if set to run on startup
                    if (Properties.Settings.Default.RunOculusClientOnStartup)
                    {
                        Functions_Old.DoAction(this, new Action(delegate () { lbl_CurrentSetting.Content = "Starting Oculus Client"; }));
                        Software.Oculus.StartOculusClient();
                    }

                    // Check runtime
                    CheckRunTime();

                    // Set up Windows audio and set to Quest speaker
                    Software.Windows_Audio_v2.Setup();
                    Software.Windows_Audio_v2.Set_To_Quest_Speaker_Auto();

                    // Run startup programs
                    Software.Auto_Launch_Programs.Run_Startup_Programs();

                    // Update UI elements
                    Functions_Old.DoAction(this, new Action(delegate ()
                    {
                        btn_Diagnostics.IsEnabled = true;
                        btn_OpenSettings.IsEnabled = true;
                        lbl_SteamVR_Status.Content = "Installed: " + Software.Steam.Steam_VR_Installed;
                        lbl_CurrentSetting.Content = Software.Oculus.Current_Dash_Name;
                        _hoverButtonManager.UpdateDashButtons();
                    }));

                    // Enable UI events
                    FireUIEvents = true;
                }
                else
                {
                    // Handle non-elevated scenario
                    NotElevated();
                }
            }
            catch (Exception ex)
            {
                // Log the exception using ErrorLogger
                ErrorLogger.LogError(ex, "An error occurred during startup.");

                // Inform the user about the error
                MessageBox.Show("An error occurred during startup. Please check the error log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Additional logging for debugging
                Debug.WriteLine($"Exception occurred: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }

            // Check if Desktop+ is installed
            bool isDesktopPlusInstalled = SteamAppChecker.IsAppInstalled("DesktopPlus");

            // Update UI based on Desktop+ installation status
            _uiManager.UpdateDesktopPlusStatusLabel(isDesktopPlusInstalled);

            // Notify the user if Desktop+ is not installed
            if (!isDesktopPlusInstalled)
            {
                _uiManager.ShowDesktopPlusNotInstalledWarning();
            }

            // Check and update the UI for SteamVR beta status
            _uiManager.UpdateSteamVRBetaStatus();
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
            try
            {
                // Set Windows audio to normal speaker automatically
                Software.Windows_Audio_v2.Set_To_Normal_Speaker_Auto();

                // Stop ADB
                Software.ADB.Stop();

                // Stop the ProcessWatcher
                Functions.ProcessWatcher.Stop();

                // Stop and dispose the "Hover Checker" timer
                Timer_Functions.StopTimer("Hover Checker");
                Timer_Functions.DisposeTimer("Hover Checker");

                // Hide the window
                Hide();

                // Stop Oculus services
                Software.Oculus.StopOculusServices();

                // Run programs that are set to execute upon closing
                Software.Auto_Launch_Programs.Run_Closing_Programs();
            }
            catch (Exception ex)
            {
                // Log the exception using ErrorLogger
                ErrorLogger.LogError(ex, "An error occurred while closing the window.");

                // Optionally: Inform the user about the error
                MessageBox.Show("An error occurred while closing the application. Please check the error log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await WindowLoadedAsync();
            }
            catch (Exception ex)
            {
                HandleWindowLoadingException(ex);
            }
        }

        private async Task WindowLoadedAsync()
        {
            // Disable buttons and update status label
            btn_Diagnostics.IsEnabled = false;
            btn_OpenSettings.IsEnabled = false;
            _uiManager.UpdateStatusLabel("Starting Up");

            // Check if the current process is elevated
            Elevated = Functions.Process_Functions.IsCurrentProcess_Elevated();

            // Configure dash buttons and hover buttons
            Disable_Dash_Buttons();
            LinkDashesToButtons();
            _hoverButtonManager.GenerateHoverButtons();

            // Pass the main form and subscribe to events
            Dashes.Dash_Manager.PassMainForm(this);
            Software.Steam.Steam_VR_Running_State_Changed_Event += Steam_Steam_VR_Running_State_Changed_Event;

            // Generate the list of auto-launch programs
            Software.Auto_Launch_Programs.Generate_List();

            // Perform startup actions
            await StartupAsync();

            frm_UpdateChecker updateChecker = new frm_UpdateChecker();

            // Check if updates are available for Dash Manager
            bool isDashManagerNewVersionAvailable = await updateChecker.CheckDashManagerUpdates();

            // Check if updates are available for OculusKiller
            bool isOculusKillerNewVersionAvailable = await updateChecker.CheckOculusKillerUpdates();

            // Temporarily disable Always on Top property of the main window
            this.Topmost = false;

            // If a new version of Dash Manager is available, show a MessageBox
            if (isDashManagerNewVersionAvailable)
            {
                MessageBox.Show(this, "A new update for Oculus VR Dash Manager is available. Please open the update checker for more details.", "Update Available", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // If a new version of OculusKiller is available, show a MessageBox
            if (isOculusKillerNewVersionAvailable)
            {
                MessageBox.Show(this, "A new update for OculusKiller is available. Please open the update checker for more details.", "Update Available", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Re-enable Always on Top property of the main window
            this.Topmost = true;
        }

        #region Dash Buttons

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

        private void btn_OpenDashLocation_Click(object sender, RoutedEventArgs e)
        {
            _uiManager.OpenDashLocation();
        }

        private void LinkDashesToButtons()
        {
            btn_ExitOculusLink.Tag = Dashes.Dash_Type.Exit;
            btn_Normal.Tag = Dashes.Dash_Type.Normal;
            btn_SteamVR.Tag = Dashes.Dash_Type.OculusKiller;
        }

        #region Hover Buttons Enter/Leave

        private void btn_ExitOculusLink_MouseEnter(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Exit_Link.SetHovering();
        }

        private void btn_ExitOculusLink_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Exit_Link.StopHovering();
        }

        private void btn_Normal_MouseEnter(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Oculus_Dash.SetHovering();
        }

        private void btn_Normal_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoverButtonManager.Oculus_Dash.StopHovering();
        }

        #endregion Hover Buttons Enter/Leave

        private void Thread_ReactivateButtons()
        {
            Thread.Sleep(5000);
            Functions_Old.DoAction(this, new Action(delegate () { _hoverButtonManager.UpdateDashButtons(); }));
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

        private void btn_CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!FireUIEvents)
                    return;

                Forms.frm_UpdateChecker Settings = new Forms.frm_UpdateChecker();
                OpenForm(Settings);
            }
            catch (Exception ex)
            {
                // Log the exception using ErrorLogger
                ErrorLogger.LogError(ex, "An error occurred while checking for updates.");

                // Optionally: Inform the user about the error
                MessageBox.Show("An error occurred while trying to check for updates. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_Diagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Forms.frm_Diagnostics Settings = new Forms.frm_Diagnostics();
                OpenForm(Settings);
            }
            catch (Exception ex)
            {
                // Log the exception using ErrorLogger
                ErrorLogger.LogError(ex, "An error occurred while opening the Diagnostics window.");

                // Optionally: Inform the user about the error
                MessageBox.Show("An error occurred while trying to open the Diagnostics window. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_Help_Click(object sender, RoutedEventArgs e)
        {
            Forms.frm_Help Settings = new Forms.frm_Help();
            OpenForm(Settings);
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

        private void btn_OpenSteamVRSettings_Click(object sender, RoutedEventArgs e)
        {
            Forms.frm_SteamVR_Settings Settings = new Forms.frm_SteamVR_Settings();
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

        #endregion Forms

        #region OpenXR Runtime

        public void CheckRunTime()
        {
            Software.Steam_VR_Settings.OpenXR_Runtime CurrentRuntime = Software.Steam_VR_Settings.Read_Runtime();

            if (CurrentRuntime == Software.Steam_VR_Settings.OpenXR_Runtime.Oculus)
                Functions_Old.DoAction(this, new Action(delegate () { btn_RunTime_Oculus.IsChecked = true; }));

            if (CurrentRuntime == Software.Steam_VR_Settings.OpenXR_Runtime.SteamVR)
                Functions_Old.DoAction(this, new Action(delegate () { btn_RunTime_SteamVR.IsChecked = true; }));
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggledButton = (ToggleButton)sender;

            if (toggledButton == btn_RunTime_SteamVR)
            {
                btn_RunTime_Oculus.IsChecked = false;
                Software.Steam_VR_Settings.Set_SteamVR_Runtime();
            }
            else if (toggledButton == btn_RunTime_Oculus)
            {
                btn_RunTime_SteamVR.IsChecked = false;
                Software.Oculus_Link.SetToOculusRunTime();
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggledButton = (ToggleButton)sender;

            if (toggledButton == btn_RunTime_SteamVR && btn_RunTime_Oculus.IsChecked == false)
            {
                btn_RunTime_SteamVR.IsChecked = true;
            }
            else if (toggledButton == btn_RunTime_Oculus && btn_RunTime_SteamVR.IsChecked == false)
            {
                btn_RunTime_Oculus.IsChecked = true;
            }
        }

        #endregion OpenXR Runtime
    }
}