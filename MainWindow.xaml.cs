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
            int counter = 0;
            while (fs.CanRead && fs.Position < fs.Length)
            {
                if (fs.Length - fs.Position < 96)
                {
                    break;
                }
                byte[] chunkBegin = br.ReadBytes(8);
                byte[] chunkArtist = br.ReadBytes(31);
                byte chunkSeparator = br.ReadByte();
                byte[] chunkTitle = br.ReadBytes(31);
                byte[] chunkEnd = br.ReadBytes(25);

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
                tracks.Add(new Track() { ID = counter, Title = title, Artist = artist });
                counter++;
            }

            StreamReader sr = new StreamReader(fsdbs, Encoding.Unicode);
            string raw = sr.ReadToEnd();
            MatchCollection matches = Regex.Matches(raw, "\\G([A-Z]:.+?(?:\\.MP3|\\.M4A|\\.AAC|\\.WMA))", RegexOptions.IgnoreCase);
            int i = 0;
            foreach (Match m in matches)
            {
                try
                {
                    if (m.Value == GetShortPath(m.Value))
                    {
                        tracks.Find(x => x.ID == i).Path = System.IO.Path.GetFullPath(m.Value.Replace(@"\\", @"\"));
                        tracks.Find(x => x.ID == i).ShortPath = m.Value.Replace(@"\\", @"\");
                    }
                    else
                    {
                        tracks.Find(x => x.ID == i).Path = m.Value.Replace(@"\\", @"\");
                        tracks.Find(x => x.ID == i).ShortPath = GetShortPath(m.Value.Replace(@"\\", @"\"));
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not find ID: " + i);
                    break;
                }
                i++;
            }

            br.Close();
            sr.Close();
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

            if(ConfigurationManager.AppSettings.Get("key") == null || ConfigurationManager.AppSettings.Get("path") == null || ConfigurationManager.AppSettings.Get("doubleSlash") == null)
            {
                MessageBox.Show("Cannot write files until a key is saved.");
                return;
            }

            byte[] key = StringToByteArray(ConfigurationManager.AppSettings.Get("key"));
            byte[] chunkStart = key.AsSpan(0, 8).ToArray();
            byte[] chunkSeparator = key.AsSpan(8, 1).ToArray();
            byte[] chunkEnd = key.AsSpan(9, 25).ToArray();

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
            foreach (Track t in tracks)
            {
                byte[] chunkTitle = StrToPaddedChunk(t.Title, 31, 0x00, Encoding.Default);
                byte[] chunkArtist = StrToPaddedChunk(t.Artist, 31, 0x00, Encoding.Default);
                byte[] chunk = chunkStart.Concat(chunkArtist).Concat(chunkSeparator).Concat(chunkTitle).Concat(chunkEnd).ToArray();
                bw.Write(chunk);
                if (ConfigurationManager.AppSettings.Get("path") == "short")
                {
                    bwdbs.Write(Encoding.Unicode.GetBytes(GetShortPath(t.Path)));
                }
                else
                {
                    if (bool.Parse(ConfigurationManager.AppSettings.Get("doubleSlash")))
                    {
                        if(!t.Path.Contains(@"\\"))
                        {
                            t.Path.Insert(t.Path.LastIndexOf(@"\"), @"\");
                        }
                        bwdbs.Write(Encoding.Unicode.GetBytes(t.Path));
                    }
                    else
                    {
                        if (t.Path.Contains(@"\\"))
                        {
                            bwdbs.Write(Encoding.Unicode.GetBytes(t.Path.Replace(@"\\", @"\")));
                        }
                        else
                        {
                            bwdbs.Write(Encoding.Unicode.GetBytes(t.Path));
                        }
                    }
                }
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
            PathText.Text = "";
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
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                bool showFailMessage = false;
                bool showInvalidExtension = false;
                foreach (string f in files)
                {

                    if (f.LastIndexOf('.') >= 0)
                    {
                        string ext;
                        try
                        {
                            ext = f.Substring(f.LastIndexOf('.'), 4).ToLower();
                        }
                        catch (Exception)
                        {
                            showInvalidExtension = true;
                            continue;
                        }
                        if (ext != ".mp3" && ext != ".m4a" && ext != ".aac" && ext != ".wma")
                        {
                            showInvalidExtension = true;
                            continue;
                        }
                    }
                    else
                    {
                        showInvalidExtension = true;
                        continue;
                    }
                    string title;
                    string artist;
                    string shortPath = GetShortPath(f);
                    try
                    {
                        var t = TagLib.File.Create(f);
                        title = t.Tag.Title;
                        artist = t.Tag.FirstPerformer;
                    }
                    catch (Exception)
                    {
                        title = "<unknown title>";
                        artist = "<unknown artist>";
                        showFailMessage = true;
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
                        tracks.Add(new Track() { ID = tracks.Max(x => x.ID) + 1, Title = title, Artist = artist, ShortPath = shortPath, Path = f });
                    }
                    else
                    {
                        tracks.Add(new Track() { ID = 0, Title = title, Artist = artist, ShortPath = shortPath, Path = f });
                    }
                }

                if (showFailMessage)
                {
                    MessageBox.Show("Failed to parse tags in one or more files");
                }
                if (showInvalidExtension)
                {
                    MessageBox.Show("One or more files were skipped due to an invalid extension.");
                }

                ((ListCollectionView)trackList.ItemsSource).Refresh();
                trackList.Items.Refresh();
            }
        }

        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (trackList.SelectedItem != null && trackList.SelectedItem.GetType() == typeof(Track))
            {
                TitleBox.Text = ((Track)trackList.SelectedItem).Title;
                ArtistBox.Text = ((Track)trackList.SelectedItem).Artist;
                PathText.Text = ((Track)trackList.SelectedItem).ShortPath;
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

        private void SaveKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if(ConfigurationManager.AppSettings.Get("key") != null && ConfigurationManager.AppSettings.Get("path") != null && ConfigurationManager.AppSettings.Get("doubleSlash") != null && MessageBox.Show("You already have a key saved. Overwrite the key?", "Overwrite Key", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
            {
                return;
            }
            if (!File.Exists(dbFile.Text))
            {
                MessageBox.Show("Please select a valid database file.");
                return;
            }
            if (!File.Exists(dbsFile.Text))
            {
                MessageBox.Show("Please select a valid database file.");
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
                MessageBox.Show("Could not open \"" + dbsFile.Text + "\": " + "\n" + ex.Message);
                return;
            }

            if (fs.Length % 96 != 0)
            {
                MessageBox.Show("The usertracks.db file is corrupt!","Cannot Read Key");
            }

            BinaryReader br = new BinaryReader(fs);
            byte[] chunkStart = br.ReadBytes(8);
            br.BaseStream.Position = 39;
            byte[] chunkSeparator = { br.ReadByte() };
            br.BaseStream.Position = 71;
            byte[] chunkEnd = br.ReadBytes(25);

            byte[] key = chunkStart.Concat(chunkSeparator).Concat(chunkEnd).ToArray();

            AddUpdateAppSettings("key", BitConverter.ToString(key).Replace("-", ""));

            StreamReader sr = new StreamReader(fsdbs, Encoding.Unicode);
            string raw = sr.ReadToEnd();
            MatchCollection matches = Regex.Matches(raw, "\\G([A-Z]:.+?(?:\\.MP3|\\.M4A|\\.AAC|\\.WMA))", RegexOptions.IgnoreCase);
            Match m = matches[0];
            if(GetShortPath(m.Value) == m.Value)
            {
                AddUpdateAppSettings("path", "short");
                AddUpdateAppSettings("doubleSlash", "false");
            }
            else
            {
                AddUpdateAppSettings("path", "long");
                if(m.Value.Contains(@"\\"))
                {
                    AddUpdateAppSettings("doubleSlash", "true");
                }
                else
                {
                    AddUpdateAppSettings("doubleSlash", "false");
                }
            }


            MessageBox.Show("Key saved!", "Success");
        }

        static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("Error writing app settings");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings.Get("key") == null || appSettings.Get("path") == null || appSettings.Get("doubleSlash") == null)
            {
                MessageBox.Show("No key data found. Please generate a playlist with GTA V, then click Save Key.", "No Key Data");
            }
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }

    public class Track
    {
        private int id;
        private string title;
        private string artist;
        private string shortPath;
        private string path;

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
    }
}
