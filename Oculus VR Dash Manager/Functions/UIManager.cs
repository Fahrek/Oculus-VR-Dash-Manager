﻿using System;
using System.IO;
using System.Windows;
using OVR_Dash_Manager.Functions.Oculus;
using OVR_Dash_Manager.Functions.Steam;

namespace OVR_Dash_Manager.Functions
{
    public class UIManager
    {
        private MainWindow _window;

        public UIManager(MainWindow window) => _window = window;

        public void ShowDesktopPlusNotInstalledWarning()
        {
            _window.Dispatcher
                .Invoke(() =>
                {
                    // Temporarily set the main window to not be topmost
                    _window.Topmost = false;

                    // Notify the user or disable certain functionality.
                    MessageBox.Show(_window, "Desktop+ is not installed. Some functionality may be limited.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Set the main window back to topmost
                    _window.Topmost = true;
                });
        }

        public void NotifyNotElevated()
        {
            _window.Dispatcher
                .Invoke(() =>
                    {
                        _window.lbl_CurrentSetting.Content = "Run as Admin Required";

                        MessageBox.Show(_window,
            "This program must be run with Admin Permissions" + Environment.NewLine +
            Environment.NewLine +
            "Right click Program File then click - Run as administrator" +
            Environment.NewLine + Environment.NewLine +
            " or Right Click Program - Properties - Compatibility then Check - Run this program as an administrator",
            "This program must be run with Admin Permissions",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
                    });
        }

        public void UpdateSteamVRBetaStatus()
        {
            _window.Dispatcher
                .Invoke(() =>
                        {
                            // Temporarily set the main window to not be topmost
                            _window.Topmost = false;

                            try
                            {
                                var isSteamVRBeta = SteamAppChecker.IsSteamVRBeta();

                                var betaStatusText = isSteamVRBeta ? "Beta Edition" : "Normal Edition";
                                _window.lbl_SteamVR_BetaStatus.Content = betaStatusText;
                            }
                            catch (Exception ex)
                            {
                                ShowMessageBox($"An error occurred while checking SteamVR beta status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            // Set the main window back to topmost
                            _window.Topmost = true;
                        });
        }

        public void UpdateStatusLabel(string text)
        {
            _window.Dispatcher
                .Invoke(() =>
                            {
                                _window.lbl_CurrentSetting.Content = text;
                            });
        }

        public void UpdateSteamVRStatusLabel(string text)
        {
            _window.Dispatcher
                .Invoke(() =>
                                {
                                    _window.lbl_SteamVR_Status.Content = text;
                                });
        }

        public void UpdateDesktopPlusStatusLabel(bool isInstalled)
        {
            var statusText = isInstalled ? "Installed: True" : "Installed: False";

            _window.Dispatcher
                .Invoke(() =>
                                    {
                                        _window.lbl_DesktopPlusStatus.Content = statusText;
                                    });
        }

        public void EnableButton(string buttonName, bool isEnabled)
        {
            _window.Dispatcher
                .Invoke(() =>
                                        {
                                            switch (buttonName)
                                            {
                                                case "Diagnostics":
                                                    _window.btn_Diagnostics.IsEnabled = isEnabled;
                                                    break;

                                                case "OpenSettings":
                                                    _window.btn_OpenSettings.IsEnabled = isEnabled;
                                                    break;
                                                //... Add cases for other buttons as needed ...
                                                default:
                                                    throw new ArgumentException("Invalid button name", nameof(buttonName));
                                            }
                                        });
        }

        public void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
        {
            _window.Dispatcher
                .Invoke(() =>
                                            {
                                                MessageBox.Show(_window, message, title, buttons, icon);
                                            });
        }

        public void OpenDashLocation()
        {
            _window.Dispatcher
                .Invoke(() =>
                                                {
                                                    if (OculusRunning.Oculus_Is_Installed)
                                                    {
                                                        if (Directory.Exists(OculusRunning.Oculus_Dash_Directory))
                                                        {
                                                            Functions_Old.ShowFileInDirectory(OculusRunning.Oculus_Dash_Directory);
                                                        }
                                                        else
                                                        {
                                                            // Optionally: Handle the case where the directory does not exist
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Optionally: Handle the case where Oculus is not installed
                                                    }
                                                });
        }

        // ... Add other UI management methods as needed ...
    }
}