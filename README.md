# Temp Desktop
This is a program for temporarily changing the folder that windows uses for the Desktop UI. I find this useful when working in a particular folder for long periods of time, where normally I would need to keep the File explorer open. 

![Preview Image](temp_desktop_preview.png "Preview Image")

It works by simply editing the Registry key that sets your desktop folder and then copying all desktop shortcuts to a backup folder to hide them temporarily. 

## Usage
1. Install [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. Download the [latest release](https://github.com/giplgwm/Temporary-Desktop/releases/latest) of Temp Desktop
3. Extract the folder
4. Run `Temp Desktop.exe`
5. Press `Change Desktop Folder` and select the folder you'd like to use. Consider adding a shortcut to `Temp Desktop.exe` in the folder you'll be using as your desktop to make it more convenient to switch back.
6. You will be signed out of Windows - sign back in and the Desktop folder will be changed.
7. When you're done working in the temporary desktop folder, re-open the application and choose `Reset Desktop Folder`

### Notes
- When changing the desktop folder your desktop shortcuts will be moved from C:\Users\Public\Desktop to C:\Users\Public_Desktop_Backup. This keeps them from showing on the new desktop screen, they are moved back to the appropriate folder when `Reset Desktop Folder` is selected.
- The program requires admin access due to the above note, since adding files to C:\Users\Public\Desktop requires elevated priveledges.
