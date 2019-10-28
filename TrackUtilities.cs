using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GTAVUserMusicEditor
{
    public static class TrackUtilities
    {
        public static bool AddFileToTracks(string file, ref List<Track> tracks)
        {
            if (!File.Exists(file))
            {
                return false;
            }
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

            return !fail;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);

        public static string GetShortPath(string longPath)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(longPath, shortPath, 255);
            return shortPath.ToString();
        }

        public static byte[] StrToPaddedChunk(string input, int length, byte padChar, Encoding enc)
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

        public static TimeSpan GetAudioDuration(string filePath)
        {
            using var shell = Microsoft.WindowsAPICodePack.Shell.ShellObject.FromParsingName(filePath);
            Microsoft.WindowsAPICodePack.Shell.PropertySystem.IShellProperty prop = shell.Properties.System.Media.Duration;
            var t = (ulong)prop.ValueAsObject;
            return TimeSpan.FromTicks((long)t);
        }

        public static bool IsValidAudioExt(string file)
        {
            file = file.ToLower();
            if (file.EndsWith(".mp3") || file.EndsWith(".m4a") || file.EndsWith(".aac") || file.EndsWith(".wma"))
            {
                return true;
            }
            return false;
        }

        public static string ReplaceProblemChars(string s)
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
}
