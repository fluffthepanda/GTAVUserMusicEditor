using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Shell;
using System.ComponentModel;

namespace GTAVUserMusicEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Track> tracks = new List<Track>();

        public MainWindow()
        {
            InitializeComponent();

            CollectionViewSource itemCollectionViewSource;
            itemCollectionViewSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource"));
            itemCollectionViewSource.Source = tracks;

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.db"))
            {
                dbFile.Text = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.db";
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.dbs"))
            {
                dbsFile.Text = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.dbs";
            }
        }

        private void DbBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music";
            fd.Filter = "GTA V Music Database|*.db";
            if (fd.ShowDialog() ?? true)
            {
                dbFile.Text = fd.FileName;
            }
        }

        private void DbsBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music";
            fd.Filter = "GTA V Music Database|*.dbs";
            if (fd.ShowDialog() ?? true)
            {
                dbsFile.Text = fd.FileName;
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(dbFile.Text))
            {
                MessageBox.Show("Please select a valid database file.");
                return;
            }

            if (File.GetAttributes(dbFile.Text).HasFlag(FileAttributes.ReadOnly) && MessageBox.Show("\"" + dbFile.Text + "\" is Read-only! File cannot be modified. Load anyway?", "Warning", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
            {
                return;
            }
            if (!File.GetAttributes(dbFile.Text).HasFlag(FileAttributes.ReadOnly) && File.GetAttributes(dbsFile.Text).HasFlag(FileAttributes.ReadOnly) && MessageBox.Show("\"" + dbsFile.Text + "\" is Read-only! File cannot be modified. Load anyway?", "Warning", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
            {
                return;
            }
            FileStream fs;
            FileStream fsdbs;

            try
            {
                fs = new FileStream(dbFile.Text, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open \"" + dbFile.Text + "\": " + "\n" + ex.Message);
                return;
            }

            try
            {
                fsdbs = new FileStream(dbsFile.Text, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                fs.Close();
                MessageBox.Show("Could not open \"" + dbsFile.Text + "\": " + "\n" + ex.Message);
                return;
            }

            if (tracks.Count > 0)
            {
                if (MessageBox.Show("Are you sure you want to clear the track list? Any unsaved data will be lost.", "Confirm Clear", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
                {
                    return;
                }
                tracks.Clear();
            }

            if (fs.Length % 96 != 0)
            {
                if (MessageBox.Show("\"" + dbsFile.Text + "\" seems to be corrupt (incorrect length). Try to parse anyway?", "Incorrect Length", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
                {
                    fs.Close();
                    fsdbs.Close();
                    return;
                }
            }

            BinaryReader br = new BinaryReader(fs);
            BinaryReader brdbs = new BinaryReader(fsdbs);
            int counter = 0;
            while (fs.CanRead && fs.Position < fs.Length)
            {
                if (fs.Length - fs.Position < 96)
                {
                    break;
                }
                byte[] header = br.ReadBytes(8);
                byte[] chunkArtist = br.ReadBytes(31);
                byte chunkSeparator = br.ReadByte();
                byte[] chunkTitle = br.ReadBytes(31);
                byte chunkSeparator2 = br.ReadByte();
                byte[] duration = br.ReadBytes(4);
                byte[] unknown = br.ReadBytes(12);
                byte[] footer = br.ReadBytes(4);
                byte[] pathByteLength = br.ReadBytes(4);
                int pathLength = BitConverter.ToInt32(pathByteLength) * 2;

                string path = Encoding.Unicode.GetString(brdbs.ReadBytes(pathLength));

                string artist = "";
                string title = "";
                foreach (byte b in chunkArtist)
                {
                    if (b == 0x00)
                    {
                        break;
                    }
                    artist += (char)b;
                }
                foreach (byte b in chunkTitle)
                {
                    if (b == 0x00)
                    {
                        break;
                    }
                    title += (char)b;
                }
                tracks.Add(new Track() { ID = counter, Title = title, Artist = artist, ShortPath = GetShortPath(path.Replace(@"\\", @"\")), Path = System.IO.Path.GetFullPath(path.Replace(@"\\", @"\")), Duration = TimeSpan.FromMilliseconds(BitConverter.ToInt32(duration)) });
                counter++;
            }

            br.Close();
            brdbs.Close();
            fs.Close();
            fsdbs.Close();

            ((ListCollectionView)trackList.ItemsSource).Refresh();
            trackList.Items.Refresh();
        }

        private void WriteFiles_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to overwrite the database?", "Confirm Overwrite", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
            {
                return;
            }
            if (!File.Exists(dbFile.Text))
            {
                MessageBox.Show("Please select a valid database file.");
                return;
            }
            if (File.GetAttributes(dbFile.Text).HasFlag(FileAttributes.ReadOnly))
            {
                MessageBox.Show("\"" + dbFile.Text + "\" is Read-only.", "Write Failed");
                return;
            }
            if (File.GetAttributes(dbsFile.Text).HasFlag(FileAttributes.ReadOnly))
            {
                MessageBox.Show("\"" + dbsFile.Text + "\" is Read-only.", "Write Failed");
                return;
            }

            bool setDbHidden = false;
            bool setDbsHidden = false;

            if (File.GetAttributes(dbFile.Text).HasFlag(FileAttributes.Hidden))
            {
                setDbHidden = true;
                File.SetAttributes(dbFile.Text, File.GetAttributes(dbsFile.Text) & ~FileAttributes.Hidden);
            }

            if (File.GetAttributes(dbsFile.Text).HasFlag(FileAttributes.Hidden))
            {
                setDbsHidden = true;
                File.SetAttributes(dbsFile.Text, File.GetAttributes(dbsFile.Text) & ~FileAttributes.Hidden);
            }

            FileStream fs;
            FileStream fsdbs;

            try
            {
                fs = new FileStream(dbFile.Text, FileMode.Create, FileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not write to \"" + dbFile.Text + "\": " + "\n" + ex.Message);
                return;
            }

            try
            {
                fsdbs = new FileStream(dbsFile.Text, FileMode.Create, FileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                fs.Close();
                MessageBox.Show("Could not write to \"" + dbsFile.Text + "\": " + "\n" + ex.Message);
                return;
            }

            BinaryWriter bw = new BinaryWriter(fs);
            BinaryWriter bwdbs = new BinaryWriter(fsdbs);
            string userMusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Rockstar Games\GTA V\User Music";
            foreach (Track t in tracks)
            {
                byte[] header = BitConverter.GetBytes(t.ID);
                byte[] header2 = { 0x00, 0x00, 0x00, 0x00 };
                byte[] chunkSeparator = { 0x00 };
                byte[] chunkTitle = StrToPaddedChunk(t.Title, 31, 0x00, Encoding.Default);
                byte[] chunkArtist = StrToPaddedChunk(t.Artist, 31, 0x00, Encoding.Default);
                byte[] duration = BitConverter.GetBytes((int)t.Duration.TotalMilliseconds);
                byte[] unknown = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                byte[] footer = { 0x02, 0x00, 0x00, 0x00 };
                
                string final_path = t.Path;
                if(final_path.Contains(userMusicPath))
                {
                    final_path = final_path.Insert(userMusicPath.Length, @"\"); //GTA wants a double slash after User Music for some reason
                }
                else
                {
                    final_path = GetShortPath(final_path);
                }
                byte[] pathLength = BitConverter.GetBytes(final_path.Length);

                byte[] chunk = header.Concat(header2).Concat(chunkArtist).Concat(chunkSeparator).Concat(chunkTitle).Concat(chunkSeparator).Concat(duration).Concat(unknown).Concat(footer).Concat(pathLength).ToArray();

                bw.Write(chunk);
                bwdbs.Write(Encoding.Unicode.GetBytes(final_path));
            }

            bw.Close();
            bwdbs.Close();
            fs.Close();
            fsdbs.Close();

            if (setDbHidden)
            {
                File.SetAttributes(dbFile.Text, File.GetAttributes(dbFile.Text) | FileAttributes.Hidden);
            }
            if (setDbsHidden)
            {
                File.SetAttributes(dbsFile.Text, File.GetAttributes(dbsFile.Text) | FileAttributes.Hidden);
            }
            MessageBox.Show("Be sure to set the files to read-only prior to launching the game.\n\nAlternatively, disable the \"Auto-scan for Music\" setting in-game.", "Database files written");
        }

        private byte[] StrToPaddedChunk(string input, int length, byte padChar, Encoding enc)
        {
            byte[] chunk = new byte[length];
            if (input.Length >= length)
            {
                input = input.Substring(0, length);
            }
            byte[] tmp = enc.GetBytes(input);
            for (int i = 0; i < length; i++)
            {
                if (i < tmp.Length)
                {
                    chunk[i] = tmp[i];
                }
                else
                {
                    chunk[i] = padChar;
                }
            }
            return chunk;
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracks.Count < 1)
            {
                return;
            }
            if (MessageBox.Show("Are you sure you want to clear the track list?", "Confirm Clear", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
            {
                return;
            }
            tracks.Clear();
            ArtistBox.Text = "";
            TitleBox.Text = "";
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            ((ListCollectionView)trackList.ItemsSource).Refresh();
            trackList.Items.Refresh();
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);

        private static string GetShortPath(string longPath)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(longPath, shortPath, 255);
            return shortPath.ToString();
        }

        private void TrackList_Drop(object sender, DragEventArgs e)
        {
            Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    ProgBar.Visibility = Visibility.Visible;
                    ProgBarText.Visibility = Visibility.Visible;
                    trackList.AllowDrop = false;
                    dragTracksMessage.AllowDrop = false;
                });
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    int numFiles = files.Length;

                    bool showFailMessage = false;
                    bool showInvalidExtension = false;
                    bool showMissingMessage = false;
                    int i = 0;
                    foreach (string f in files)
                    {
                        string fileName = f;
                        if (fileName.EndsWith(".lnk"))
                        {
                            try
                            {
                                IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
                                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(fileName);
                                fileName = shortcut.TargetPath;
                            }
                            catch (Exception)
                            {
                                showFailMessage = true;
                                continue;
                            }
                        }
                        if (!File.Exists(fileName) && !Directory.Exists(fileName))
                        {
                            showMissingMessage = true;
                            continue;
                        }
                        if (Directory.Exists(fileName)) //is Folder
                        {
                            string[][] sub = { Directory.GetFiles(fileName, "*.mp3"), Directory.GetFiles(fileName, "*.m4a"), Directory.GetFiles(fileName, "*.aac"), Directory.GetFiles(fileName, "*.wma") };
                            numFiles += sub[0].Length + sub[1].Length + sub[2].Length + sub[3].Length;
                            foreach (string[] type in sub)
                            {
                                foreach (string trk in type)
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        ProgBar.Value = ((double)++i / (double)numFiles) * 100;
                                        ProgBarText.Text = "" + i + " / " + numFiles;
                                    });
                                    showFailMessage = AddFileToTracks(trk);
                                }
                            }
                        }
                        else if (IsValidAudioExt(fileName))
                        {
                            showFailMessage = AddFileToTracks(fileName);
                        }
                        else
                        {
                            showInvalidExtension = true;
                            continue;
                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            ProgBar.Value = ((double)++i / (double)numFiles) * 100;
                            ProgBarText.Text = "" + i + " / " + numFiles;
                        });
                    }
                    if (showFailMessage)
                    {
                        MessageBox.Show("Failed to parse tags in one or more files");
                    }
                    if (showInvalidExtension)
                    {
                        MessageBox.Show("One or more files were skipped due to an invalid extension.");
                    }
                    if (showMissingMessage)
                    {
                        MessageBox.Show("One or more files were missing, so they were skipped.");
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        ((ListCollectionView)trackList.ItemsSource).Refresh();
                        trackList.Items.Refresh();
                    });
                }
                this.Dispatcher.Invoke(() =>
                {
                    ProgBar.Visibility = Visibility.Hidden;
                    ProgBarText.Visibility = Visibility.Hidden;
                    ProgBar.Value = 0;
                    ProgBarText.Text = "";
                    trackList.AllowDrop = true;
                    dragTracksMessage.AllowDrop = true;
                });
            });
        }

        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (trackList.SelectedItem != null && trackList.SelectedItem.GetType() == typeof(Track))
            {
                TitleBox.Text = ((Track)trackList.SelectedItem).Title;
                ArtistBox.Text = ((Track)trackList.SelectedItem).Artist;
                EditButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (trackList.SelectedItem != null && trackList.SelectedItem.GetType() == typeof(Track))
            {
                ((Track)trackList.SelectedItem).Title = TitleBox.Text;
                ((Track)trackList.SelectedItem).Artist = ArtistBox.Text;
                ((ListCollectionView)trackList.ItemsSource).Refresh();
                trackList.Items.Refresh();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (trackList.SelectedItems != null && trackList.SelectedItems.Count > 0 && trackList.SelectedItem.GetType() == typeof(Track))
            {
                foreach (Track t in trackList.SelectedItems)
                {
                    tracks.Remove(t);
                }
                ((ListCollectionView)trackList.ItemsSource).Refresh();
                trackList.Items.Refresh();
            }
        }

        private void DedupeButton_Click(object sender, RoutedEventArgs e)
        {
            if (tracks.Count > 0)
            {
                List<Track> noDupes = new List<Track>(tracks.GroupBy(x => new { x.Artist, x.Title }).Select(x => x.First()).ToList());
                if (tracks.Count != noDupes.Count)
                {
                    tracks.Clear();
                    tracks.AddRange(noDupes);
                    ((ListCollectionView)trackList.ItemsSource).Refresh();
                    trackList.Items.Refresh();
                }
            }
        }

        private static TimeSpan GetAudioDuration(string filePath)
        {
            using (var shell = Microsoft.WindowsAPICodePack.Shell.ShellObject.FromParsingName(filePath))
            {
                Microsoft.WindowsAPICodePack.Shell.PropertySystem.IShellProperty prop = shell.Properties.System.Media.Duration;
                var t = (ulong)prop.ValueAsObject;
                return TimeSpan.FromTicks((long)t);
            }
        }

        private bool IsValidAudioExt(string file)
        {
            file = file.ToLower();
            if(file.EndsWith(".mp3") || file.EndsWith(".m4a") || file.EndsWith(".aac") || file.EndsWith(".wma"))
            {
                return true;
            }
            return false;
        }

        private bool AddFileToTracks(string file)
        {
            string title;
            string artist;
            TimeSpan duration;
            string shortPath = GetShortPath(file);
            bool fail = false;
            try
            {
                var t = TagLib.File.Create(file);
                title = ReplaceProblemChars(t.Tag.Title);
                artist = ReplaceProblemChars(t.Tag.FirstPerformer);
                duration = GetAudioDuration(file);
                t.Dispose();
            }
            catch (Exception)
            {
                title = "<unknown title>";
                artist = "<unknown artist>";
                duration = TimeSpan.FromSeconds(180); //average song length?
                fail = true;
            }

            if (title.Length > 31)
            {
                title = title.Substring(0, 31);
            }
            if (artist.Length > 31)
            {
                artist = artist.Substring(0, 31);
            }

            if (tracks.Count > 0)
            {
                tracks.Add(new Track() { ID = tracks.Max(x => x.ID) + 1, Title = title, Artist = artist, ShortPath = shortPath, Path = file, Duration = duration });
            }
            else
            {
                tracks.Add(new Track() { ID = 0, Title = title, Artist = artist, ShortPath = shortPath, Path = file, Duration = duration });
            }

            return fail;
        }

        private string ReplaceProblemChars(string s)
        {
            if (s.IndexOf('\u2013') > -1) s = s.Replace('\u2013', '-');
            if (s.IndexOf('\u2014') > -1) s = s.Replace('\u2014', '-');
            if (s.IndexOf('\u2015') > -1) s = s.Replace('\u2015', '-');
            if (s.IndexOf('\u2017') > -1) s = s.Replace('\u2017', '_');
            if (s.IndexOf('\u2018') > -1) s = s.Replace('\u2018', '\'');
            if (s.IndexOf('\u2019') > -1) s = s.Replace('\u2019', '\'');
            if (s.IndexOf('\u201a') > -1) s = s.Replace('\u201a', ',');
            if (s.IndexOf('\u201b') > -1) s = s.Replace('\u201b', '\'');
            if (s.IndexOf('\u201c') > -1) s = s.Replace('\u201c', '\"');
            if (s.IndexOf('\u201d') > -1) s = s.Replace('\u201d', '\"');
            if (s.IndexOf('\u201e') > -1) s = s.Replace('\u201e', '\"');
            if (s.IndexOf('\u2026') > -1) s = s.Replace("\u2026", "...");
            if (s.IndexOf('\u2032') > -1) s = s.Replace('\u2032', '\'');
            if (s.IndexOf('\u2033') > -1) s = s.Replace('\u2033', '\"');

            return s;
        }
    }

    public class Track
    {
        private int id;
        private string title;
        private string artist;
        private string shortPath;
        private string path;
        private TimeSpan duration;

        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public string Artist
        {
            get { return artist; }
            set { artist = value; }
        }

        public string ShortPath
        {
            get { return shortPath; }
            set { shortPath = value; }
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set { duration = value; }
        }
    }
}
