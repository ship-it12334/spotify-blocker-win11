# Spotify Mute

Automatically mutes Spotify when playing blocked songs or ads (detected by window title).

## Usage
1. Add blocked song titles to `blocklist.txt` (one per line)
2. Run `SpotMute.exe`
3. It will auto-mute/unmute based on Spotify window title

Works by sending WM_APPCOMMAND to Spotify windows.
