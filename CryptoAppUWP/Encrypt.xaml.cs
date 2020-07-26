using CryptoAppUWP.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CryptoAppUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Encrypt : Page
    {
        private Windows.Storage.StorageFile fileData = null;
        public Encrypt()
        {
            this.InitializeComponent();
            PasswordSwitch.IsOn = true;
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TextInputEditor.Text))
            {
                fileData = null;
                await new ContentDialog() { Title = "Error", Content = "You can encrypt either file or text at a time. First, clear the text!", CloseButtonText = "Okay" }.ShowAsync();
                return;
            }
            try
            {
                OpenFileButton.IsEnabled = false;
                EncryptButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                fileData = null;
                FileOpenPicker openPicker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail
                };
                string[] SupportedFilterTypes = new string[] { ".jpeg", ".png", ".pdf", ".txt", ".mp4" ,".mkv",".mp4" };
                foreach (var item in SupportedFilterTypes)
                {
                    openPicker.FileTypeFilter.Add(item);
                }
                fileData = await openPicker.PickSingleFileAsync();
                if(fileData==null)
                {
                    SelectedFileNameLabel.Text = "No file selected";
                    RemoveButton.IsEnabled = false;
                    EncryptButton.IsEnabled = false;
                    OpenFileButton.IsEnabled = true;
                    return;
                }
                string fileName = fileData.Name;
                if (!String.IsNullOrEmpty(fileName) && fileName.Length > 50)
                {
                    SelectedFileNameLabel.Text = fileName.Substring(0, 50);
                }
                else
                {
                    SelectedFileNameLabel.Text = fileName;
                }
                OpenFileButton.IsEnabled = true;
                EncryptButton.IsEnabled = true;
                RemoveButton.IsEnabled = true;
                await new ContentDialog() { Title = "Success", Content = "You selected \"" + fileName + "\"", CloseButtonText = "Okay" }.ShowAsync();
            }
            catch (Exception)
            {
                fileData = null;
                OpenFileButton.IsEnabled = true;
                EncryptButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                SelectedFileNameLabel.Text = "No file selected";
                await new ContentDialog() { Title = "Error", Content = "Can not open files", CloseButtonText = "Okay" }.ShowAsync();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            fileData = null;
            SelectedFileNameLabel.Text = "No file selected";
            EncryptButton.IsEnabled = false;
            OpenFileButton.IsEnabled = true;
            RemoveButton.IsEnabled = false;
        }

        private async void TextInputEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (fileData != null)
            {
                this.Focus(FocusState.Programmatic);
                TextInputEditor.Text = String.Empty;
                try
                {
                    await new ContentDialog() { Title = "Error", Content = "You can encrypt either file or text at a time. First, remove selected file!", CloseButtonText = "Okay" }.ShowAsync();
                }
                catch (Exception)
                {
                    return;
                }
                return;
            }
            if (!String.IsNullOrEmpty(TextInputEditor.Text))
            {
                if (TextInputEditor.Text.Length > 51200)
                {
                    try
                    {
                        await new ContentDialog() { Title = "Error", Content = "Text size is larger than 50KB, copy the text to a text file and then encrypt that file.", CloseButtonText = "Okay" }.ShowAsync();
                    }
                    catch (Exception)
                    {
                    }
                    TextInputEditor.Text = TextInputEditor.Text.Substring(0, 51200 - 1);
                }
            }
            if (String.IsNullOrEmpty(TextInputEditor.Text) && fileData == null)
            {
                EncryptButton.IsEnabled = false;
            }
            else
            {
                EncryptButton.IsEnabled = true;
            }
        }

        private void TextInputEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(TextInputEditor.Text) && fileData == null)
            {
                EncryptButton.IsEnabled = false;
            }
            else
            {
                EncryptButton.IsEnabled = true;
            }
        }

        private void PasswordSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (PasswordSwitch.IsOn)
            {
                CustomPasswordStack.Visibility = Visibility.Collapsed;
                CustomPasswordEntry.Text = String.Empty;
            }
            else
            {
                CustomPasswordStack.Visibility = Visibility.Visible;
                CustomPasswordEntry.Text = String.Empty;
            }
        }

        private async void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            bool RemoveButtonState = DisableAllControls();
            if (String.IsNullOrEmpty(TextInputEditor.Text) && fileData == null)
            {
                RestoreAllControls(RemoveButtonState);
                await new ContentDialog() { Title = "Error", Content = "Choose file or write/paste text to encrypt!", CloseButtonText = "Okay" }.ShowAsync();
                EncryptButton.IsEnabled = false;
                return;
            }
            string password = "$ude$h1611";
            if (!PasswordSwitch.IsOn)
            {
                if (String.IsNullOrEmpty(CustomPasswordEntry.Text) || CustomPasswordEntry.Text.Length < 6 || CustomPasswordEntry.Text.Length > 16)
                {
                    RestoreAllControls(RemoveButtonState);
                    CustomPasswordEntry.Focus(FocusState.Programmatic);
                    await new ContentDialog() { Title = "Error", Content = "Custom Password length should be at least 6 and at most 16", CloseButtonText = "Okay" }.ShowAsync();
                    return;
                }
                password = CustomPasswordEntry.Text;
            }
            if (fileData == null)
            {
                EncryptionResult encryptionResult = null;
                string plainText = TextInputEditor.Text;
                await Task.Run( () =>
                {
                    encryptionResult = EncryptionService.EncryptText(plainText, password);
                });
                if (encryptionResult.Result)
                {
                    var DP = new DataPackage();
                    DP.SetText(encryptionResult.EncryptedString);
                    Clipboard.SetContent(DP);
                    fileData = null;
                    RestoreAllControls(false);
                    RemoveButton_Click(null, null);
                    TextInputEditor.Text = String.Empty;
                    CustomPasswordEntry.Text = String.Empty;
                    PasswordSwitch.IsOn = true;
                    var FormatedTextSize = EncryptionService.GetFormatedSize(System.Text.Encoding.UTF8.GetByteCount(encryptionResult.EncryptedString));
                    await new ContentDialog() { Title = "Success", Content = String.Format("Text encrypted and copied to clipboard. Encrypted Text size is {0:n1}{1}", FormatedTextSize.Item1, FormatedTextSize.Item2), CloseButtonText = "Okay" }.ShowAsync();
                    
                }
                else
                {
                    await new ContentDialog() { Title = "Failed", Content = "Encryption failed. This text can not be encrypted. Error: \"" + encryptionResult.Error + "\"", CloseButtonText = "Okay" }.ShowAsync();
                    RestoreAllControls(RemoveButtonState);
                }

            }
            else
            {
                EncryptionResult encryptionResult = null;
                await Task.Run(async () =>
                {
                    encryptionResult = await EncryptionService.EncryptFile(fileData, password);
                });
                if (encryptionResult.Result)
                {
                    ActivityText.Text = "Do not close the application. Saving encrypted file.";
                    FileSavePicker savePicker = new FileSavePicker();
                    savePicker.FileTypeChoices.Add("Crypto App Encrypted File", new List<string>() { ".senc" });
                    savePicker.SuggestedFileName = System.IO.Path.GetFileName(encryptionResult.WritePath);
                    var writeFile = await savePicker.PickSaveFileAsync();
                    if (writeFile != null)
                    {
                        CachedFileManager.DeferUpdates(writeFile);
                        await FileIO.WriteBytesAsync(writeFile, encryptionResult.EncryptedContents);
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(writeFile);
                        if (status == FileUpdateStatus.Complete)
                        {
                            await new ContentDialog() { Title = "Success", Content = "File is encrypted and saved as " + writeFile.Name, CloseButtonText = "Okay" }.ShowAsync();
                        }
                        else
                        {
                            await new ContentDialog() { Title = "Failure", Content = "File could not be saved! ", CloseButtonText = "Okay" }.ShowAsync();
                        }
                    }
                    else
                    {
                        await new ContentDialog() { Title = "Cancelled", Content = "File was not saved! ", CloseButtonText = "Okay" }.ShowAsync();
                    }
                    RestoreAllControls(false);
                    fileData = null;
                    RemoveButton_Click(null, null);
                    TextInputEditor.Text = String.Empty;
                    CustomPasswordEntry.Text = String.Empty;
                    PasswordSwitch.IsOn = true;
                }
                else
                {
                    await new ContentDialog() { Title = "Failed", Content = "File encryption failed. This file can not be encrypted.Error: \"" + encryptionResult.Error + "\"", CloseButtonText = "Okay" }.ShowAsync();
                    RestoreAllControls(RemoveButtonState);
                }
            }
        }

        private bool DisableAllControls()
        {
            StackOne.Visibility = Visibility.Collapsed;
            StackTwo.Visibility = Visibility.Collapsed;
            StackThree.Visibility = Visibility.Collapsed;
            ActivityIndicatorLayout.Visibility = Visibility.Visible;
            EncryptButton.Content = "Encrypting...";
            var RemoveButtonState = RemoveButton.IsEnabled;
            EncryptButton.IsEnabled = false;
            OpenFileButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            TextInputEditor.IsEnabled = false;
            PasswordSwitch.IsEnabled = false;
            CustomPasswordEntry.IsEnabled = false;
            return RemoveButtonState;
        }

        private void RestoreAllControls(bool RemoveButtonState)
        {
            StackOne.Visibility = Visibility.Visible;
            StackTwo.Visibility = Visibility.Visible;
            StackThree.Visibility = Visibility.Visible;
            ActivityIndicatorLayout.Visibility = Visibility.Collapsed;
            EncryptButton.IsEnabled = true;
            OpenFileButton.IsEnabled = true;
            RemoveButton.IsEnabled = RemoveButtonState;
            TextInputEditor.IsEnabled = true;
            PasswordSwitch.IsEnabled = true;
            CustomPasswordEntry.IsEnabled = true;
            ActivityText.Text = "Do not close the application. Encryption in progress.";
            EncryptButton.Content = "Start Encryption";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}
