ABMT is a C# console application that allows you to create backups of media files (photos, videos, audio, documents) from an Android device and restore them from a backup. 
The application uses ADB (Android Debug Bridge) to interact with the device.

Opportunities:
1. Backup media files from an Android device.
2. Restore media files from a backup.
3. Connect to the device via Wi-Fi.
4. Working with the phone in the recovery mode

Requirements:
- .NET Core or .NET Framework 4.6.1 or higher
- ADB (Android Debug Bridge) must be installed in the application folder

Usage:
1. Launch the application.
2. Connect your Android device to your computer.
3. Select an action: "Backup", "Restore from backup" or "Connect via Wi-Fi".
4. Follow the on-screen instructions.

Features:
- Supports backup and recovery of the following file types: photos, videos, audio, documents.
- The application can work with or without a connected device (in continuation mode without a device).
- During backup, the application displays progress and statistics by file type.
- Backups are saved in ZIP archive format in the "backups" folder in the application directory.

Notes:
- Make sure that "USB Debugging" or "Wi-Fi Debugging" mode is enabled on your device.
- The application does not copy files from the "Android/data/" folder as desired.
