# GTA V User Music Editor

## IMPORTANT:
In order to prevent GTA V from overwriting your track data, please set the usertracks.db and usertracks.dbs files to "read-only." You will have to clear the read-only attribute in order to use them with this application again.
Alternatively, in-game you can disable "Auto-scan for Music" in Settings > Audio.
 
Features:
- Browsing for database files
- Loading and parsing database files
- Adding/Editing/Deleting entries
- Removing duplicate entries
- Writing database files to disk

Usage:
- Browse your computer for your GTA V usertracks files. They will be located in the Local AppData folder, in /Rockstar Games/GTA V/User Music.
- Load the tracks from the database.
- Drag and drop supported audio files (.mp3, .m4a, .aac, .wma) to add them to the track list.
- Make any changes to the track list, then write them to the database.

Notes:
- The "Remove Duplicates" feature will remove any entries with the same Artist and Title. This can be problematic if the first 31 characters of the title/artist of two entries match, since the database cuts the fields at 31 characters. Only one of those entries will be kept.
