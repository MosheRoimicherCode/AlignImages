using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using ReadMetadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlignImages
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private CameraOrientationReader reader;
        public MainWindow()
        {
            InitializeComponent();
            reader = new();
        }

        private async void PickMultipleFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                //disable the button to avoid double-clicking
                button.IsEnabled = false;

                var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);

                picker.CommitButtonText = "Pick Files";

                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                picker.ViewMode = PickerViewMode.List;

                // Show the picker dialog window
                var files = await picker.PickMultipleFilesAsync();

                if (files.Count > 0)
                {
                    PickedMultipleFilesTextBlock.Text = "";
                    foreach (var file in files)
                    {
                        var obj = CameraOrientationReader.GetOrientation(file.Path);
                        //if (obj.Pitch <= -91 || obj.Pitch >= -89)
                        PickedMultipleFilesTextBlock.Text += "Image: " + file.Path + " Pitch: " + obj.Pitch + " Yaw: " + obj.Yaw + " Roll: " + obj.Roll + " Ratatons: " + obj.rotation.RotationSteps + " Mode: " + obj.rotation.ModeDetected + Environment.NewLine;
                    }

                }
                else
                {
                    PickedMultipleFilesTextBlock.Text = "No files selected.";
                }

                //re-enable the button
                button.IsEnabled = true;
            }
        }
    }
}
