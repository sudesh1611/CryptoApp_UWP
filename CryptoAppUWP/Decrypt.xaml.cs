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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls; 
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CryptoAppUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Decrypt : Page
    {
        private Windows.Storage.StorageFile fileData = null;
        public Decrypt()
        {
            this.InitializeComponent();
            PasswordSwitch.IsOn = true;
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(TextInputEditor.Text))
            {
                fileData = null;
                await new ContentDialog() { Title = "Error", Content = "You can decrypt either file or text at a time. First, clear the text!", CloseButtonText = "Okay" }.ShowAsync();
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
                string[] SupportedFilterTypes = new string[] { ".senc" };
                foreach (var item in SupportedFilterTypes)
                {
                    openPicker.FileTypeFilter.Add(item);
                }
                fileData = await openPicker.PickSingleFileAsync();
                if (fileData == null)
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
                        await new ContentDialog() { Title = "Error", Content = "Text size is larger than 50KB, copy the text to a text file and then decrypt that file.", CloseButtonText = "Okay" }.ShowAsync();
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
                await new ContentDialog() { Title = "Error", Content = "Choose file or paste text to decrypt!", CloseButtonText = "Okay" }.ShowAsync();
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
                DecryptionResult decryptionResult = null;
                string plainText = TextInputEditor.Text;
                await Task.Run(() =>
                {
                    decryptionResult = DecryptionService.DecryptText(plainText, password);
                });
                if (decryptionResult.Result)
                {
                    var FormatedTextSize = DecryptionService.GetFormatedSize(System.Text.Encoding.UTF8.GetByteCount(decryptionResult.DecryptedString));
                    var result = await new ContentDialog() { Title = "Decrypted Text", Content = decryptionResult.DecryptedString, PrimaryButtonText="Copy decrypted text" ,CloseButtonText = "Do not copy" }.ShowAsync();
                    if(result == ContentDialogResult.Primary)
                    {
                        var DP = new DataPackage();
                        DP.SetText(decryptionResult.DecryptedString);
                        Clipboard.SetContent(DP);
                    }
                    fileData = null;
                    RestoreAllControls(false);
                    RemoveButton_Click(null, null);
                    TextInputEditor.Text = String.Empty;
                    CustomPasswordEntry.Text = String.Empty;
                    PasswordSwitch.IsOn = true;
                }
                else
                {
                    await new ContentDialog() { Title = "Failed", Content = "Decryption failed. This text can not be decrypted. Error: \"" + decryptionResult.Error + "\"", CloseButtonText = "Okay" }.ShowAsync();
                    RestoreAllControls(RemoveButtonState);
                }
            }
            else
            {
                DecryptionResult decryptionResult = null;
                await Task.Run(async () =>
                {
                    decryptionResult = await DecryptionService.DecryptFile(fileData, password);
                });
                if (decryptionResult.Result)
                {
                    ActivityText.Text = "Do not close the application. Saving decrypted file.";
                    FileSavePicker savePicker = new FileSavePicker();
                    var FileNameExtension = System.IO.Path.GetExtension(decryptionResult.WritePath);
                    savePicker.FileTypeChoices.Add("Crypto App Decrypted File", new List<string>() { FileNameExtension });
                    savePicker.SuggestedFileName = System.IO.Path.GetFileName(decryptionResult.WritePath);
                    var writeFile = await savePicker.PickSaveFileAsync();
                    if (writeFile != null)
                    {
                        CachedFileManager.DeferUpdates(writeFile);
                        await FileIO.WriteBytesAsync(writeFile, decryptionResult.DecryptedContents);
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(writeFile);
                        if (status == FileUpdateStatus.Complete)
                        {
                            await new ContentDialog() { Title = "Success", Content = "File is decrypted and saved as " + writeFile.Name, CloseButtonText = "Okay" }.ShowAsync();
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
                else if (decryptionResult.Result == false && decryptionResult.Error == "Password")
                {
                    await new ContentDialog() { Title = "Error", Content = "Password is wrong. Enter correct password to decrypt.", CloseButtonText = "Okay" }.ShowAsync();
                    RestoreAllControls(RemoveButtonState);
                    PasswordSwitch.IsOn = false;
                    CustomPasswordStack.Visibility = Visibility.Visible;
                    CustomPasswordEntry.Focus(FocusState.Programmatic);
                    return;
                }
                else
                {
                    await new ContentDialog() { Title = "Failed", Content = "Decryption failed. This file can not be decrypted. Error: \"" + decryptionResult.Error + "\"", CloseButtonText = "Okay" }.ShowAsync();
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
            EncryptButton.Content = "Decrypting...";
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
            ActivityText.Text = "Do not close the application. Decryption in progress.";
            EncryptButton.Content = "Start Decryption";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}
