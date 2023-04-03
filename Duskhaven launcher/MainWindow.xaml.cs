﻿using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Duskhaven_launcher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        checking,
        install,
        launcherUpdate,
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string rootPath;
        private string clientZip;
        private string gameExe;
        private string launcherExe;
        private string dlUrl;
        private List<string> fileList = new List<string>();
        private List<string> fileUpdateList = new List<string>();
        private LauncherStatus _status;
        private string uri = "https://duskhavenfiles.dev/";
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                setButtonState();
                switch (_status)
                {
                    case LauncherStatus.checking:
                        PlayButton.Content = "Checking For Updates";
                        break;
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.install:
                        PlayButton.Content = "Install";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "DownLoading...";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading update";
                        break;
                    case LauncherStatus.launcherUpdate:
                            PlayButton.Content = "Update Launcher";
                        break;
                    default:
                        break;
                }

            }

        }

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            gameExe = Path.Combine(rootPath, "wow.exe");
            clientZip = Path.Combine(rootPath, "WoW%203.3.5.zip");
            launcherExe = Path.Combine(rootPath, "Duskhaven launcher.exe");


        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            

            if(getLauncherVersion())
            {
                setButtonState();
                CheckForUpdates();
            }
            
        }
        private bool getLauncherVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version assemblyVersion = assembly.GetName().Version;
            Console.WriteLine($"Assembly version: {assemblyVersion}");

            // Replace these values with your own
            string owner = "laurensmarcelis";
            string repo = "Duskhaven-Launcher";

            // Get the latest release information from GitHub API
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            HttpWebRequest apiRequest = WebRequest.CreateHttp(apiUrl);
            apiRequest.UserAgent = "HttpClient";
            apiRequest.Accept = "application/vnd.github.v3+json";
            apiRequest.Method = "GET";

            using (HttpWebResponse apiResponse = (HttpWebResponse)apiRequest.GetResponse())
            {
                using (StreamReader streamReader = new StreamReader(apiResponse.GetResponseStream()))
                {
                    string apiResponseString = streamReader.ReadToEnd();
                    dynamic apiResponseData = JsonConvert.DeserializeObject(apiResponseString);
                    string tagName = apiResponseData["tag_name"];
                    if (tagName == assemblyVersion.ToString() )
                    {
                        return true;
                    }
                    else
                    {
                        AddActionListItem($"Launcher out of date, newest version is {tagName}, your version is {assemblyVersion.ToString()}");
                        Status = LauncherStatus.launcherUpdate;
                        Console.WriteLine(apiResponseData["assets"][0]["browser_download_url"]);
                        dlUrl = apiResponseData["assets"][0]["browser_download_url"];
                        return false;
                    }


                }
            }
        }
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = rootPath;
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
            else if (Status == LauncherStatus.install)
            {
                InstallGameFiles(false);
            }
            else if (Status == LauncherStatus.launcherUpdate)
            {
                UpdateLauncher();
            }
        }
        private void UpdateLauncher()
        {
            // Code to close the application
            Close();

            // Wait for the application to exit
            while (Application.Current != null && Application.Current.MainWindow != null)
            {
                Thread.Sleep(100); // Wait for 0.1 seconds
            }

            // Download the new executable file
            string downloadUrl = dlUrl; // Specify the URL to download the new executable
            string newExePath = Path.Combine(rootPath, "temp.exe"); // Specify the path to save the downloaded file
            using (var client = new WebClient())
            {
                client.DownloadFile(downloadUrl, newExePath);
            }

            // Rename the old executable file
            string oldExePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string backupExePath = Path.Combine(rootPath,"backup-launcher.exe"); ; // Specify the path to save the backup file
            File.Move(oldExePath, backupExePath);

            // Replace the old executable file with the new one
            File.Move(newExePath, oldExePath);

            // Launch the new executable
            Process.Start(oldExePath);
            /*WebClient webClient = new WebClient();
            webClient.DownloadFileCompleted += (sender, e) =>
            {
                AddActionListItem($"Installing new launcher version");
                if (e.Error == null)
                {
                    Application.Current.Shutdown();
                    // Wait for the application to exit
                    while (Application.Current != null && Application.Current.MainWindow != null)
                    {
                        Console.WriteLine("we sleepin");
                        Thread.Sleep(100); // Wait for 0.1 seconds
                    }

                    // Launch the new executable
                    Process.Start(Path.Combine(rootPath, "temp.exe"));
                    string destinationPath = launcherExe;
                    File.Copy(Path.Combine(rootPath, "temp.exe"), destinationPath, true);

                    System.Windows.Application.Current.Shutdown();
                    
                }
                else
                {
                    MessageBox.Show($"Error Downloading game files:{e.Error}");
                    Console.WriteLine(e.Error);
                    MessageBox.Show($"Error Downloading launcher");
                    AddActionListItem($"Error Downloading launcher");
                }
            };
            AddActionListItem($"Downloading new launcher version");
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompleteCallback);
            webClient.DownloadFileAsync(new Uri(dlUrl), Path.Combine(rootPath, "temp.exe" ));*/
        }
        private void CheckForUpdates()
        {
            fileUpdateList.Clear();
            fileList.Clear();   
            Status = LauncherStatus.checking;
            AddActionListItem("checking local files");
            
            WebRequest request = WebRequest.Create(uri);
            WebResponse response = request.GetResponse();
            Regex regex = new Regex("<a href=\"\\.\\/(?<name>.*(mpq|MPQ|wtf|exe))\">");

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string result = reader.ReadToEnd();

                MatchCollection matches = regex.Matches(result);
                if (matches.Count == 0)
                {
                    Console.WriteLine("parse failed.");
                    return;
                }

                foreach (Match match in matches)
                {
                    if (!match.Success) { continue; }
                    fileList.Add(match.Groups["name"].ToString());
                }
            }
            foreach (String file in fileList)
            {
                long remoteFileSize = 0;
                long localFileSize = 0;
                
                // Get the size of the remote file
                var checkRequest = (HttpWebRequest)WebRequest.Create($"{uri}{file}");
                checkRequest.Method = "HEAD";
                Console.WriteLine(file);
                using (var checkResponse = checkRequest.GetResponse())
                {
                    if (checkResponse is HttpWebResponse httpResponse)
                    {
                        remoteFileSize = httpResponse.ContentLength;
                    }
                }
                
                // Get the size of the local file
                if (File.Exists(getFilePath(file)))
                {
                    localFileSize = new FileInfo(getFilePath(file)).Length;
                }
                else 
                {
                    fileUpdateList.Add(file);
                    AddActionListItem($"{file} is not installed, adding to download list");
                    continue;
                }
                // Compare the sizes
                if (remoteFileSize == localFileSize)
                {
                    
                    AddActionListItem($"{file} is up to date, NO update required");
                    Console.WriteLine("The remote file and the local file have the same size.");
                }
                else
                {
                    fileUpdateList.Add(file);
                    AddActionListItem($"{file} is out of date, adding to update list");
                    Console.WriteLine($"{file} is out of date and needs an update.");

                }

            }
            Console.WriteLine(fileUpdateList.Count);
            Console.WriteLine(fileList.Count);
            if (fileUpdateList.Count == 0)
            {
                Status = LauncherStatus.ready;
            }
            
            else if (fileUpdateList.Count == fileList.Count)
            {
                Status = LauncherStatus.install;
            }
            else
            {
                InstallGameFiles(true);
            }


            /* TODO: rewrite this if Versions/checksums are available to reduce load times
             * if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                   
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(""));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);

                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates:{ex}");
                    throw;
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            } */


        }

        private string getFilePath(string file)
        {
            string filePath = rootPath;
            if (file.Contains(".exe"))
            {
                filePath = Path.Combine(filePath, file);
            }
            if (file.Contains(".mpq") || file.Contains(".MPQ"))
            {
                if(!Directory.Exists(Path.Combine(filePath, "data")))
                {
                    Directory.CreateDirectory(Path.Combine(filePath, "data"));
                }
                filePath = Path.Combine(filePath, "data", file);
            }
            if (file.Contains(".wtf"))
            {
                if (Directory.Exists(Path.Combine(filePath, "data", "enGB")))
                {
                    filePath = Path.Combine(filePath, "data", "enGB", file);
                }
                else if (Directory.Exists(Path.Combine(filePath, "data", "enUS")))
                {
                    filePath = Path.Combine(filePath, "data", "enUS", file);
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(filePath, "data", "enGB"));
                    filePath = Path.Combine(filePath, "data", "enGB", file);
                }
            }
            return filePath;
        }
        private void AddActionListItem(string action)
        {
            ActionList.Text += $"{action}\n";
        }
        private void InstallGameFiles(bool _isUpdate)
        {
            
            try
            {
                
                if (_isUpdate)
                {
                    AddActionListItem($"Updating files");
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    AddActionListItem($"Installing files needed to play");
                    Status = LauncherStatus.downloadingGame;
                    //_onlineVersion = new Version(webClient.DownloadString("version file link"));

                }

                DownloadFiles(fileUpdateList,0) ;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error Installing game files:{ex}");
                throw;
            }

        }

        private void DownloadFiles(List<string> files, int index)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFileCompleted += (sender, e) =>
            {
                if (e.Error == null)
                {
                    
                    if (index < files.Count - 1)
                    {
                        DownloadFiles(files, index + 1);
                    }
                    else if( index == files.Count -1)
                    {
                        Status = LauncherStatus.ready;
                        VersionText.Text = "Ready to enjoy Duskhaven";
                    }
                }
                else
                {
                    MessageBox.Show($"Error Downloading game files:{e.Error}");
                    AddActionListItem($"Error Downloading {files[index]}");
                }
            };
            Console.WriteLine($"{uri}{files[index]}");
            AddActionListItem($"Downloading {files[index]}");
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompleteCallback);
            webClient.DownloadFileAsync(new Uri($"{uri}{files[index]}"), getFilePath(files[index]), files[index]);
           
        }


        private void DownloadGameCompleteCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                AddActionListItem($"Installing {e.UserState}");

            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error Installing game files:{ex}");
                throw;
            }
        }


        private void DownloadWotlkClientCompleteCallback(object sender, AsyncCompletedEventArgs e)
        {
            AddActionListItem($"Installing Wotlk 3.3.5 client");
            VersionText.Text = "Extracting Wotlk files to directory...";
            try
            {
                
                ZipFile.ExtractToDirectory(clientZip, rootPath);
                using (var archive = ZipFile.OpenRead(clientZip))
                {
                    // Loop through the archive entries
                    foreach (var entry in archive.Entries)
                    {
                        // Check if the entry is located in the desired directory
                        if (entry.FullName.StartsWith("WoW 3.3.5"))
                        {
                            string newName = entry.FullName.Substring(entry.FullName.IndexOf("/") + 1);
                            Console.WriteLine(newName);
                            // If the entry is a folder, create the folder
                            if (entry.FullName.EndsWith("/"))
                            {
                                Console.WriteLine(entry.FullName);
                                
                                string folderPath = Path.Combine(rootPath, newName);
                                Directory.CreateDirectory(folderPath);
                            }
                            else
                            {
                                // If the entry is a file, extract the file to the target directory
                                string targetFilePath = Path.Combine(@rootPath, newName);
                                entry.ExtractToFile(targetFilePath, true);
                            }
                        }
                    }
                }
                Directory.Delete(Path.Combine(rootPath, "WoW 3.3.5"), true);
                File.Delete(clientZip);
                VersionText.Text = "Extracting Done...";
                AddActionListItem($"Installing done");
                CheckForUpdates();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error Installing game files:{ex}");
                throw;
            }
        }

        private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            string downloadedMBs = Math.Round(e.BytesReceived / 1024.0 / 1024.0, 0).ToString() + " MB";
            string totalMBs = Math.Round(e.TotalBytesToReceive / 1024.0 / 1024.0, 0).ToString() + " MB";
            // Displays the operation identifier, and the transfer progress.
            VersionText.Text = $"{(string)e.UserState}    downloaded {downloadedMBs} of {totalMBs} bytes. {e.ProgressPercentage} % complete...";
            dlProgress.Visibility = Visibility.Visible;
            dlProgress.Value = e.ProgressPercentage;
        }

        private void DLButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddActionListItem($"Downloading Wotlk 3.3.5 client");
                WebClient webClient = new WebClient();
                Status = LauncherStatus.downloadingGame;
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadWotlkClientCompleteCallback);
                webClient.DownloadFileAsync(new Uri($"{uri}WoW%203.3.5.zip"), clientZip);




            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error Installing game files:{ex}");
                throw;
            }
        }
        private void setButtonState()
        {
            if (Status == LauncherStatus.ready || Status == LauncherStatus.failed || Status == LauncherStatus.install ||Status == LauncherStatus.launcherUpdate)
            {
                DLButton.IsEnabled = true;
                PlayButton.IsEnabled = true;
            } else
            {
                DLButton.IsEnabled = false;
                PlayButton.IsEnabled = false;
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://duskhaven.servegame.com/account/register/");
        }

        private void CloseButton_Clicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }


    /* use when a version is available */
    struct LauncherVersion
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal LauncherVersion(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal LauncherVersion(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if (_versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool IsDifferentThan(LauncherVersion _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
