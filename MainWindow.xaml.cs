using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        private readonly byte[] dbChunkBegin = { 0xC0, 0x5D, 0x30, 0x60,     0x4B, 0x01, 0x00, 0x00};
        private readonly byte[] dbChunkSeparator = { 0x00 };
        private readonly byte[] dbChunkEnd = { 0x00,     0xA7, 0x32, 0x02, 0x00,     0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,     0x00, 0x00, 0x00, 0x00,     0x02, 0x00, 0x00, 0x00,     0x1C, 0x00, 0x00, 0x00};

        public MainWindow()
        {
            InitializeComponent();

            CollectionViewSource itemCollectionViewSource;
            itemCollectionViewSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource"));
            itemCollectionViewSource.Source = tracks;

            if(File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.db"))
            {
                dbFile.Text = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.db";
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.dbs"))
            {
                dbsFile.Text = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music\\usertracks.dbs";
            }
        }

        private void dbBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Rockstar Games\\GTA V\\User Music";
            fd.Filter = "GTA V Music Database|*.db";
            if(fd.ShowDialog() ?? true)
            {
                dbFile.Text = fd.FileName;
            }
        }

        private void dbsBrowse_Click(object sender, RoutedEventArgs e)
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
            if(!File.Exists(dbFile.Text))
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
            catch(Exception ex)
            {
                MessageBox.Show("Could not open \"" + dbFile.Text + "\": " + "\n" + ex.Message);
                return;
            }

            try
            {
                fsdbs = new FileStream(dbsFile.Text, FileMode.Open, FileAccess.Read);
            }
            catch(Exception ex)
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
                if(MessageBox.Show("\"" + dbsFile.Text + "\" seems to be corrupt (incorrect length). Try to parse anyway?", "Incorrect Length", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
                {
                    fs.Close();
                    fsdbs.Close();
                    return;
                }
            }

            BinaryReader bw = new BinaryReader(fs);
            int counter = 0;
            while (fs.CanRead && fs.Position < fs.Length)
            {
                if(fs.Length - fs.Position < 96)
                {
                    break;
                }
                byte[] chunkBegin = bw.ReadBytes(8);
                byte[] chunkArtist = bw.ReadBytes(31);
                byte chunkSeparator = bw.ReadByte();
                byte[] chunkTitle = bw.ReadBytes(31);
                byte[] chunkEnd = bw.ReadBytes(25);

                string artist = "";
                string title = "";
                foreach(byte b in chunkArtist)
                {
                    if(b == 0x00)
                    {
                        break;
                    }
                    artist += (char)b;
                }
                foreach(byte b in chunkTitle)
                {
                    if(b == 0x00)
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
            foreach(Match m in matches)
            {
                try
                {
                    tracks.Find(x => x.ID == i).ShortPath = m.Value;
                }
                catch(Exception)
                {
                    MessageBox.Show("Could not find ID: " + i);
                    break;
                }
                i++;
            }

            bw.Close();
            sr.Close();
            fs.Close();
            fsdbs.Close();

            ((ListCollectionView)trackList.ItemsSource).Refresh();
            trackList.Items.Refresh();
        }

        private void writeFiles_Click(object sender, RoutedEventArgs e)
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
                byte[] chunkTitle = strToPaddedChunk(t.Title, 31, 0x00, Encoding.Default);
                byte[] chunkArtist = strToPaddedChunk(t.Artist, 31, 0x00, Encoding.Default);
                byte[] chunk = dbChunkBegin.Concat(chunkArtist).Concat(dbChunkSeparator).Concat(chunkTitle).Concat(dbChunkEnd).ToArray();
                bw.Write(chunk);
                bwdbs.Write(Encoding.Unicode.GetBytes(t.ShortPath));
            }

            bw.Close();
            bwdbs.Close();
            fs.Close();
            fsdbs.Close();
            MessageBox.Show("Be sure to set the files to read-only prior to launching the game.\n\nAlternatively, disable the \"Auto-scan for Music\" setting in-game.","Database files written");
        }

        private byte[] strToPaddedChunk(string input, int length, byte padChar, Encoding enc)
        {
            byte[] chunk = new byte[length];
            if(input.Length >= length)
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
            if(tracks.Count < 1)
            {
                return;
            }
            if(MessageBox.Show("Are you sure you want to clear the track list?", "Confirm Clear", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes)
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

        private void trackList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                bool showFailMessage = false;
                bool showInvalidExtension = false;
                foreach(string f in files)
                {

                    if(f.LastIndexOf('.') >= 0)
                    {
                        string ext;
                        try
                        {
                            ext = f.Substring(f.LastIndexOf('.'), 4).ToLower();
                        }
                        catch(Exception)
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
                    catch(Exception)
                    {
                        title = "<unknown title>";
                        artist = "<unknown artist>";
                        showFailMessage = true;
                    }

                    if(title.Length > 31)
                    {
                        title = title.Substring(0, 31);
                    }
                    if(artist.Length > 31)
                    {
                        artist = artist.Substring(0, 31);
                    }

                    if (tracks.Count > 0)
                    {
                        tracks.Add(new Track() { ID = tracks.Max(x => x.ID) + 1, Title = title, Artist = artist, ShortPath = shortPath });
                    }
                    else
                    {
                        tracks.Add(new Track() { ID = 0, Title = title, Artist = artist, ShortPath = shortPath });
                    }
                }

                if(showFailMessage)
                {
                    MessageBox.Show("Failed to parse tags in one or more files");
                }
                if(showInvalidExtension)
                {
                    MessageBox.Show("One or more files were skipped due to an invalid extension.");
                }

                ((ListCollectionView)trackList.ItemsSource).Refresh();
                trackList.Items.Refresh();
            }
        }

        private void trackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(trackList.SelectedItem != null && trackList.SelectedItem.GetType() == typeof(Track))
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
                foreach(Track t in trackList.SelectedItems)
                {
                    tracks.Remove(t);
                }
                ((ListCollectionView)trackList.ItemsSource).Refresh();
                trackList.Items.Refresh();
            }
        }

        private void dedupeButton_Click(object sender, RoutedEventArgs e)
        {
            if(tracks.Count > 0)
            {
                List<Track> noDupes = new List<Track>(tracks.GroupBy(x => new { x.Artist, x.Title }).Select(x => x.First()).ToList());
                if(tracks.Count != noDupes.Count)
                {
                    tracks.Clear();
                    tracks.AddRange(noDupes);
                    ((ListCollectionView)trackList.ItemsSource).Refresh();
                    trackList.Items.Refresh();
                }
            }
        }
    }

    public class Track
    {
        private int id;
        private string title;
        private string artist;
        private string shortPath;

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
    }
}
