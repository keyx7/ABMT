using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Resources;
using System.Runtime.InteropServices.Marshalling;
using Newtonsoft.Json;
using System.Threading;


class MediaBackup
{
    static string deviceName = "";
    static string backupFolder = "backups";
    static string adbPath = Path.Combine("adb", "adb"); // для использования adb  из дериктории приложения
    static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("ABMT");
        Console.ResetColor();
        Console.WriteLine(" Android Backup Media Tool");

        bool checkMessageShow = false;
        bool continueWithoutDevice = false;

        SelectLanguage();

        while (true)
        {
            ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
            deviceName = GetDeviceName();

            if (deviceName == "UnknownDevice")
            {
                if (!checkMessageShow)
                {
                    Console.WriteLine(new string('-', 72));
                    Console.WriteLine(rm.GetString("ConnectDevice"));
                    Console.WriteLine(rm.GetString("PressC"));

                    checkMessageShow = true;
                }

                // Проверяем, хочет ли пользователь продолжить без устройства
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.C) // Если пользователь ввел 'C'
                    {
                        continueWithoutDevice = true;
                        break; // Выходим из цикла
                    }
                }
            }
            else
            {
                ShowMainMenu();
                break; // Выходим из цикла при подключении устройства
            }
        }

        // Если пользователь решил продолжить без устройства
        if (continueWithoutDevice)
        {
            ShowMainMenu();
        }

        static string GetDeviceName()
        {
            var deviceNameProcess = new Process();
            deviceNameProcess.StartInfo.FileName = adbPath;
            deviceNameProcess.StartInfo.Arguments = "shell getprop ro.product.model";
            deviceNameProcess.StartInfo.UseShellExecute = false;
            deviceNameProcess.StartInfo.RedirectStandardOutput = true;
            deviceNameProcess.StartInfo.CreateNoWindow = true;
            deviceNameProcess.Start();
            string output = deviceNameProcess.StandardOutput.ReadToEnd().Trim();
            deviceNameProcess.WaitForExit();
            return string.IsNullOrEmpty(output) ? "UnknownDevice" : output;
        }

        static void SelectLanguage()
        {
            Console.WriteLine("Select language / Выберите язык:");
            Console.WriteLine("1. English");
            Console.WriteLine("2. Русский");

            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            switch (key.KeyChar)
            {
                case '1':
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;
                case '2':
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Using default language (English).");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;
            }
        }

        static void ShowMainMenu()
        { 
            while (true)
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("ABMT");
                Console.ResetColor();
                Console.WriteLine(" Android Backup Media Tool C#");
                Console.WriteLine($"{rm.GetString("ConnectedDevice")} {deviceName}");
                DisplayConnectionStatus();
                Console.WriteLine();
                Console.WriteLine(rm.GetString("SelectAction"));
                Console.WriteLine(new string('-', 72));
                Console.WriteLine(rm.GetString("Backup"));
                Console.WriteLine(rm.GetString("Restore"));
                Console.WriteLine(rm.GetString("WI-Fi Connect"));
                Console.WriteLine(rm.GetString("Exit"));
                Console.WriteLine(new string('-', 72));

                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.KeyChar)
                {
                    case '1':
                        BackupMenu();
                        break;
                    case '2':
                        RestoreMenu();
                        break;
                    case '3':
                        ConnectToDeviceOverWifi();
                        break;
                    case '0':
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine(rm.GetString("IncorrectSelection"));
                        Console.ReadKey();
                        break;
                }
            }

            static void DisplayConnectionStatus()
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = "devices",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Определяем тип подключения
                string status = rm.GetString("ConnectionStatus.status");
                if (!string.IsNullOrEmpty(output))
                {
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    // Игнорируем первую строку заголовка
                    var deviceLines = lines.Skip(1);

                    foreach (var line in deviceLines)
                    {
                        if (line.Contains("device"))
                        {
                            // Проверяем, если устройство подключено через Wi-Fi
                            if (line.StartsWith("adb-"))
                            {
                                status = rm.GetString("ConnectionStatus.Wifi");
                            }
                            else
                            {
                                // Если вывод не содержит префикса `adb-`, считаем подключение по USB
                                status = rm.GetString("ConnectionStatus.USB");
                            }
                            break;
                        }
                    }
                }

                Console.WriteLine($"{rm.GetString("ConnectionStatus.State")} {status}");
            }



            static void ConnectToDeviceOverWifi()
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.StartMSG"));
                Console.Write(rm.GetString("ConnectToDeviceOverWifi.EnterIP"));
                string ipAddress = Console.ReadLine();
                Console.Write(rm.GetString("ConnectToDeviceOverWifi.EnterPORT"));
                string port = Console.ReadLine();
                Console.Write(rm.GetString("ConnectToDeviceOverWifi.EnterCODE"));
                string key = Console.ReadLine();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    var connectProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = adbPath,
                            Arguments = $"pair {ipAddress}:{port} {key}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    connectProcess.Start();
                    connectProcess.WaitForExit();

                    string output = connectProcess.StandardOutput.ReadToEnd();
                    if (connectProcess.ExitCode == 0)
                    {
                        Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.Connected"));
                    }
                    else
                    {
                        Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.ConnectERROR"));
                    }
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.EmptyIP"));
                }

                Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.PressAnyKey"));
                Console.ReadKey();
                ShowMainMenu();
            }


            static void BackupMenu()
            {
                while (true)
                {
                    ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(rm.GetString("BackupMenu"));
                    Console.ResetColor();
                    Console.WriteLine(rm.GetString("SelectAction"));
                    Console.WriteLine("--------------------------------------------------------");
                    Console.WriteLine(rm.GetString("StartBackup"));
                    Console.WriteLine(rm.GetString("Back"));
                    Console.WriteLine("--------------------------------------------------------");

                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    switch (key.KeyChar)
                    {
                        case '1':
                            BackupMedia();
                            break;
                        case '9':
                            ShowMainMenu();
                            return;
                        default:
                            Console.WriteLine(rm.GetString("IncorrectSelection"));
                            Console.ReadKey();
                            break;
                    }
                }
            }

            static void RestoreMenu()
            {
                while (true)
                {
                    ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(rm.GetString("RestoreMenu"));
                    Console.ResetColor();
                    Console.WriteLine(rm.GetString("SelectAction"));
                    Console.WriteLine("--------------------------------------------------------");
                    Console.WriteLine(rm.GetString("StartRestore"));
                    Console.WriteLine(rm.GetString("Back"));
                    Console.WriteLine("--------------------------------------------------------");

                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    switch (key.KeyChar)
                    {
                        case '1':
                            RestoreMedia();
                            break;
                        case '9':
                            ShowMainMenu();
                            return;
                        default:
                            Console.WriteLine(rm.GetString("IncorrectSelection"));
                            Console.ReadKey();
                            break;
                    }
                }
            }
            static void BackupMedia()
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string timestamp = DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss");
                string backupFileName = $"{deviceName} {timestamp}.zip";
                string backupPath = Path.Combine(backupFolder, backupFileName);

                Console.WriteLine(rm.GetString("BackupMedia.Start"));

                var findProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = "shell find /storage/emulated/0 -type f \\( " +
                                    "-name '*.jpg' -o -name '*.jpeg' -o -name '*.png' -o -name '*.gif' -o " +
                                    "-name '*.mp4' -o -name '*.avi' -o -name '*.mkv' -o -name '*.mov' -o " +
                                    "-name '*.mp3' -o -name '*.wav' -o -name '*.flac' -o -name '*.aac' -o -name '*.ogg' -o " +
                                    "-name '*.doc' -o -name '*.docx' -o -name '*.pdf' -o -name '*.txt' -o -name '*.xls' -o -name '*.xlsx' -o -name '*.ppt' -o -name '*.pptx' " +
                                    "\\) -exec stat -c '%s %n' {} + 2>/dev/null",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    }
                };
                findProcess.Start();

                var output = findProcess.StandardOutput.ReadToEnd();
                var errorOutput = findProcess.StandardError.ReadToEnd();
                findProcess.WaitForExit();

                if (!string.IsNullOrEmpty(errorOutput))
                {
                    Console.WriteLine($"{rm.GetString("AdbError")} {errorOutput}");
                    return;
                }

                var files = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line =>
                    {
                        var parts = line.Split(' ', 2);
                        return (size: long.Parse(parts[0]), path: parts[1]);
                    }).ToList();

                // Подсчёт файлов в Android/data/ и других путях
                var dataFiles = files.Where(file => file.path.StartsWith("/storage/emulated/0/Android/data/")).ToList();
                var otherFiles = files.Where(file => !file.path.StartsWith("/storage/emulated/0/Android/data/")).ToList();

                Console.WriteLine($"{rm.GetString("BackupMedia.TotalFiles")} {otherFiles.Count + dataFiles.Count}");
                Console.WriteLine($"{rm.GetString("BackupMedia.Android/data Count")} {dataFiles.Count}");

                // Инициализация счётчиков для файлов
                var fileCounts = new Dictionary<string, (int Count, int HiddenCount, int DataCount, long Size, long HiddenSize, long DataSize)>();
                long totalSize = 0;

                foreach (var file in otherFiles)
                {
                    string extension = Path.GetExtension(file.path).ToLower();
                    bool isHidden = Path.GetFileName(file.path).StartsWith(".") || file.path.Split('/').Any(part => part.StartsWith("."));

                    if (extension == ".jpg" || extension == ".png")
                        UpdateFileCounts("photo", file, isHidden, ref fileCounts);
                    else if (extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".mov")
                        UpdateFileCounts("video", file, isHidden, ref fileCounts);
                    else if (extension == ".mp3" || extension == ".wav" || extension == ".flac")
                        UpdateFileCounts("audio", file, isHidden, ref fileCounts);
                    else if (extension == ".doc" || extension == ".docx" || extension == ".pdf" || extension == ".txt")
                        UpdateFileCounts("document", file, isHidden, ref fileCounts);

                    totalSize += file.size;
                }

                // Подсчёт файлов в Android/data/ по категориям и их размеров
                foreach (var file in dataFiles)
                {
                    string extension = Path.GetExtension(file.path).ToLower();
                    bool isHidden = Path.GetFileName(file.path).StartsWith(".") || file.path.Split('/').Any(part => part.StartsWith("."));

                    if (extension == ".jpg" || extension == ".png")
                        UpdateFileCounts("photo", file, isHidden, ref fileCounts, isDataFile: true);
                    else if (extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".mov")
                        UpdateFileCounts("video", file, isHidden, ref fileCounts, isDataFile: true);
                    else if (extension == ".mp3" || extension == ".wav" || extension == ".flac")
                        UpdateFileCounts("audio", file, isHidden, ref fileCounts, isDataFile: true);
                    else if (extension == ".doc" || extension == ".docx" || extension == ".pdf" || extension == ".txt")
                        UpdateFileCounts("document", file, isHidden, ref fileCounts, isDataFile: true);
                }

                // Вычисление общего размера с учётом скрытых и Android/data/ файлов
                long totalRegularSize = fileCounts.Values.Sum(v => v.Size);
                long totalHiddenSize = fileCounts.Values.Sum(v => v.HiddenSize);
                long totalDataSize = fileCounts.Values.Sum(v => v.DataSize);

                long totalCalculatedSize = totalRegularSize + totalHiddenSize + totalDataSize;

                Console.WriteLine($"{rm.GetString("BackupMedia.TotalFileSize")} {FormatFileSize(totalCalculatedSize)}");
                Console.WriteLine($"{rm.GetString("BackupMedia.Photo")} ({fileCounts.GetValueOrDefault("photo").Count} {rm.GetString("BackupMedia.Conventional")}, {fileCounts.GetValueOrDefault("photo").HiddenCount} {rm.GetString("BackupMedia.Hidden")}, {fileCounts.GetValueOrDefault("photo").DataCount} {rm.GetString("BackupMedia.In android/data")})");
                Console.WriteLine($"{rm.GetString("BackupMedia.Video")} ({fileCounts.GetValueOrDefault("video").Count} {rm.GetString("BackupMedia.Conventional")}, {fileCounts.GetValueOrDefault("video").HiddenCount} {rm.GetString("BackupMedia.Hidden")}, {fileCounts.GetValueOrDefault("video").DataCount} {rm.GetString("BackupMedia.In android/data")})");
                Console.WriteLine($"{rm.GetString("BackupMedia.Audio")} ({fileCounts.GetValueOrDefault("audio").Count} {rm.GetString("BackupMedia.Conventional")}, {fileCounts.GetValueOrDefault("audio").HiddenCount}  {rm.GetString("BackupMedia.Hidden")}, {fileCounts.GetValueOrDefault("audio").DataCount} {rm.GetString("BackupMedia.In android/data")})");
                Console.WriteLine($"{rm.GetString("BackupMedia.Documents")} ({fileCounts.GetValueOrDefault("document").Count}  {rm.GetString("BackupMedia.Conventional")} , {fileCounts.GetValueOrDefault("document").HiddenCount} {rm.GetString("BackupMedia.Hidden")}, {fileCounts.GetValueOrDefault("document").DataCount} {rm.GetString("BackupMedia.In android/data")})");

                // Выбор файлов для копирования
                Console.WriteLine(rm.GetString("BackupMedia.EnterNumb"));
                Console.WriteLine(rm.GetString("BackupMedia.PressH"));
                Console.WriteLine(rm.GetString("BackupMedia.PressD"));

                string? choice = Console.ReadLine();
                if (string.IsNullOrEmpty(choice))
                {
                    Console.WriteLine(rm.GetString("BackupMedia.IncorrectSelect"));
                    return;
                }

                bool includeHiddenFiles = choice.Contains('H') || choice.Contains('Н');
                bool includeDataFiles = choice.Contains('D') || choice.Contains('Д');
                choice = choice.Replace("H", "").Replace("Н", "").Replace("D", "").Replace("Д", "").Trim();
                var selectedTypes = choice.Split(' ').Select(int.Parse).ToHashSet();

                // Определяем выбранные файлы с учетом выбора скрытых файлов из Android/data/
                var selectedFiles = otherFiles.Concat(
                    includeDataFiles ? dataFiles : Enumerable.Empty<(long size, string path)>()
                ).Where(file =>
                {
                    string extension = Path.GetExtension(file.path).ToLower();
                    bool isHidden = Path.GetFileName(file.path).StartsWith(".") || file.path.Split('/').Any(part => part.StartsWith("."));

                    if (!includeHiddenFiles && isHidden) return false;

                    if (selectedTypes.Contains(1) && (extension == ".jpg" || extension == ".png"))
                        return true;
                    else if (selectedTypes.Contains(2) && (extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".mov"))
                        return true;
                    else if (selectedTypes.Contains(3) && (extension == ".mp3" || extension == ".wav" || extension == ".flac"))
                        return true;
                    else if (selectedTypes.Contains(4) && (extension == ".doc" || extension == ".docx" || extension == ".pdf" || extension == ".txt"))
                        return true;

                    return false;
                }).ToList();

                // Создаём временную папку для резервного копирования
                string tempBackupFolder = Path.Combine(Path.GetTempPath(), "media_backup_temp");
                if (Directory.Exists(tempBackupFolder))
                {
                    Directory.Delete(tempBackupFolder, true);
                }
                Directory.CreateDirectory(tempBackupFolder);

                var mediaInfos = new List<MediaInfo>();
                int copiedFiles = 0;
                int totalSelectedFiles = selectedFiles.Count;

                // Копируем файлы в временную папку
                foreach (var file in selectedFiles)
                {
                    string localPath;
                    if (Path.GetExtension(file.path).ToLower() == ".jpg" || Path.GetExtension(file.path).ToLower() == ".png")
                        localPath = "photos";
                    else if (Path.GetExtension(file.path).ToLower() == ".mp4" || Path.GetExtension(file.path).ToLower() == ".avi" || Path.GetExtension(file.path).ToLower() == ".mkv" || Path.GetExtension(file.path).ToLower() == ".mov")
                        localPath = "videos";
                    else if (Path.GetExtension(file.path).ToLower() == ".mp3" || Path.GetExtension(file.path).ToLower() == ".wav" || Path.GetExtension(file.path).ToLower() == ".flac")
                        localPath = "audio";
                    else if (Path.GetExtension(file.path).ToLower() == ".doc" || Path.GetExtension(file.path).ToLower() == ".docx" || Path.GetExtension(file.path).ToLower() == ".pdf" || Path.GetExtension(file.path).ToLower() == ".txt")
                        localPath = "documents";
                    else
                        continue;

                    Directory.CreateDirectory(Path.Combine(tempBackupFolder, localPath));

                    string filePath = Path.Combine(tempBackupFolder, localPath, Path.GetFileName(file.path));

                    var pullProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = adbPath,
                            Arguments = $"pull \"{file.path}\" \"{filePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    pullProcess.Start();
                    pullProcess.WaitForExit();

                    if (File.Exists(filePath))
                    {
                        mediaInfos.Add(new MediaInfo(file.path, Path.GetFileName(filePath)));
                        copiedFiles++;
                        DisplayProgress(copiedFiles, totalSelectedFiles, rm.GetString("BackupMedia.Copied"));
                    }
                    else
                    {
                        Console.WriteLine($"\n{rm.GetString("BackupMedia.CopyError")} {file.path}");
                    }
                }

                string metadataPath = Path.Combine(tempBackupFolder, "data.json");
                File.WriteAllText(metadataPath, JsonConvert.SerializeObject(mediaInfos));

                Console.WriteLine();
                Console.WriteLine("\n" + rm.GetString("BackupMedia.BackupArchive"));

                // Архивирование с отображением прогресса
                long totalSizeForArchiving = new DirectoryInfo(tempBackupFolder).GetFiles("*", SearchOption.AllDirectories)
                                                      .Sum(f => f.Length);
                long archivedSize = 0;

                using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
                {
                    foreach (var file in Directory.EnumerateFiles(tempBackupFolder, "*", SearchOption.AllDirectories))
                    {
                        string entryName = file.Substring(tempBackupFolder.Length + 1);
                        archive.CreateEntryFromFile(file, entryName);

                        // Обновляем прогресс архивирования
                        archivedSize += new FileInfo(file).Length;
                        DisplayArchiveProgress(archivedSize, totalSizeForArchiving);
                    }
                }

                Directory.Delete(tempBackupFolder, true);

                // Получаем размер архива
                long backupSize = new FileInfo(backupPath).Length;

                Console.WriteLine($"\n {rm.GetString("BackupMedia.BackupCreated")} {backupPath}");
                Console.WriteLine($"{rm.GetString("BackupMedia.Size")} {FormatFileSize(backupSize)}");
                Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.PressAnyKey"));
                Console.ReadKey();
                ShowMainMenu();
            }

            static void UpdateFileCounts(string fileType, (long size, string path) file, bool isHidden, ref Dictionary<string, (int Count, int HiddenCount, int DataCount, long Size, long HiddenSize, long DataSize)> fileCounts, bool isDataFile = false)
            {
                var currentCount = fileCounts.GetValueOrDefault(fileType);
                if (isDataFile)
                {
                    fileCounts[fileType] = (currentCount.Count,
                                            currentCount.HiddenCount,
                                            currentCount.DataCount + 1,
                                            currentCount.Size,
                                            currentCount.HiddenSize,
                                            currentCount.DataSize + file.size);
                }
                else if (isHidden)
                {
                    fileCounts[fileType] = (currentCount.Count,
                                            currentCount.HiddenCount + 1,
                                            currentCount.DataCount,
                                            currentCount.Size,
                                            currentCount.HiddenSize + file.size,
                                            currentCount.DataSize);
                }
                else
                {
                    fileCounts[fileType] = (currentCount.Count + 1,
                                            currentCount.HiddenCount,
                                            currentCount.DataCount,
                                            currentCount.Size + file.size,
                                            currentCount.HiddenSize,
                                            currentCount.DataSize);
                }
            }

            static void RestoreMedia()
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                Console.WriteLine(rm.GetString("RestoreMedia.SelectRestoreFile"));
                string? backupFilePath = ShowBackupFilePicker();

                if (string.IsNullOrEmpty(backupFilePath) || !File.Exists(backupFilePath))
                {
                    Console.WriteLine(rm.GetString("RestoreMedia.FileNotSelected"));
                    return;
                }

                string extractPath = Path.Combine(Path.GetTempPath(), "media_backup_extract");
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                ZipFile.ExtractToDirectory(backupFilePath, extractPath);

                string metadataPath = Path.Combine(extractPath, "data.json");
                if (!File.Exists(metadataPath))
                {
                    Console.WriteLine(rm.GetString("RestoreMedia.MetadataNotFind"));
                    return;
                }

                var mediaInfos = JsonConvert.DeserializeObject<List<MediaInfo>>(File.ReadAllText(metadataPath));
                if (mediaInfos == null)
                {
                    Console.WriteLine("\n" + rm.GetString("RestoreMedia.ReadingError"));
                    return;
                }

                int photoCount = mediaInfos.Count(m => m.FileName.EndsWith(".jpg") || m.FileName.EndsWith(".png"));
                int videoCount = mediaInfos.Count(m => m.FileName.EndsWith(".mp4") || m.FileName.EndsWith(".avi") || m.FileName.EndsWith(".mkv") || m.FileName.EndsWith(".mov"));
                int audioCount = mediaInfos.Count(m => m.FileName.EndsWith(".mp3") || m.FileName.EndsWith(".wav") || m.FileName.EndsWith(".flac"));
                int documentCount = mediaInfos.Count(m => m.FileName.EndsWith(".doc") || m.FileName.EndsWith(".docx") || m.FileName.EndsWith(".pdf") || m.FileName.EndsWith(".txt"));

                Console.WriteLine($"{rm.GetString("BackupMedia.Photo")} ({photoCount})");
                Console.WriteLine($"{rm.GetString("BackupMedia.Video")} ({videoCount})");
                Console.WriteLine($"{rm.GetString("BackupMedia.Audio")} ({audioCount})");
                Console.WriteLine($"{rm.GetString("BackupMedia.Documents")} ({documentCount})");
                Console.WriteLine(rm.GetString("RestoreMedia.SelectRestoreFiles"));

                string? choice = Console.ReadLine();
                if (string.IsNullOrEmpty(choice))
                {
                    Console.WriteLine(rm.GetString("RestoreMedia.IncorrectSelection"));
                    return;
                }

                var selectedTypes = choice.Split(' ').Select(int.Parse).ToHashSet();

                int totalFiles = mediaInfos.Count;
                int restoredFiles = 0;

                foreach (var mediaInfo in mediaInfos)
                {
                    bool shouldRestore = false;

                    if ((selectedTypes.Contains(1) && (mediaInfo.FileName.EndsWith(".jpg") || mediaInfo.FileName.EndsWith(".png"))))
                        shouldRestore = true;
                    else if ((selectedTypes.Contains(2) && (mediaInfo.FileName.EndsWith(".mp4") || mediaInfo.FileName.EndsWith(".avi") || mediaInfo.FileName.EndsWith(".mkv") || mediaInfo.FileName.EndsWith(".mov"))))
                        shouldRestore = true;
                    else if ((selectedTypes.Contains(3) && (mediaInfo.FileName.EndsWith(".mp3") || mediaInfo.FileName.EndsWith(".wav") || mediaInfo.FileName.EndsWith(".flac"))))
                        shouldRestore = true;
                    else if ((selectedTypes.Contains(4) && (mediaInfo.FileName.EndsWith(".doc") || mediaInfo.FileName.EndsWith(".docx") || mediaInfo.FileName.EndsWith(".pdf") || mediaInfo.FileName.EndsWith(".txt"))))
                        shouldRestore = true;

                    if (shouldRestore)
                    {
                        string localPath = mediaInfo.FileName.EndsWith(".mp4") || mediaInfo.FileName.EndsWith(".avi") || mediaInfo.FileName.EndsWith(".mkv") || mediaInfo.FileName.EndsWith(".mov") ? "videos" :
                                           mediaInfo.FileName.EndsWith(".mp3") || mediaInfo.FileName.EndsWith(".wav") || mediaInfo.FileName.EndsWith(".flac") ? "audio" :
                                           mediaInfo.FileName.EndsWith(".doc") || mediaInfo.FileName.EndsWith(".docx") || mediaInfo.FileName.EndsWith(".pdf") || mediaInfo.FileName.EndsWith(".txt") ? "documents" :
                                           "photos";

                        string sourcePath = Path.Combine(extractPath, localPath, Path.GetFileName(mediaInfo.FileName));

                        if (!File.Exists(sourcePath))
                        {
                            Console.WriteLine($"Файл не найден: {sourcePath}. Пропуск...");
                            continue;
                        }

                        var pushProcess = new Process();
                        pushProcess.StartInfo.FileName = adbPath;
                        pushProcess.StartInfo.Arguments = $"push \"{sourcePath}\" \"{mediaInfo.OriginalPath}\"";
                        pushProcess.StartInfo.UseShellExecute = false;
                        pushProcess.StartInfo.RedirectStandardOutput = true;
                        pushProcess.StartInfo.RedirectStandardError = true;
                        pushProcess.StartInfo.CreateNoWindow = true;
                        pushProcess.Start();
                        pushProcess.WaitForExit();

                        if (pushProcess.ExitCode == 0)
                        {
                            restoredFiles++;
                            DisplayProgress(restoredFiles, totalFiles, rm.GetString("Restored"));
                        }
                        else
                        {
                            Console.WriteLine($"\n{rm.GetString("RestoreMedia.ErrorRestoring")} {sourcePath}");
                        }
                    }
                }

                Directory.Delete(extractPath, true);
                Console.WriteLine("\n" + rm.GetString("RestoreMedia.RestoreDone"));
                Console.WriteLine(rm.GetString("ConnectToDeviceOverWifi.PressAnyKey"));
                Console.ReadKey();
                ShowMainMenu();
            }

            static string? ShowBackupFilePicker()
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                if (!Directory.Exists(backupFolder))
                {
                    Console.WriteLine(rm.GetString("FolderNotFind"));
                    return null;
                }

                var files = Directory.GetFiles(backupFolder, "*.zip");
                if (files.Length == 0)
                {
                    Console.WriteLine(rm.GetString("NoAvailableBackups"));
                    return null;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])} | (Размер: {FormatFileSize(fileInfo.Length)})");
                }

                Console.Write(rm.GetString("SelectFileNumb"));
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= files.Length)
                {
                    return files[choice - 1];
                }

                return null;
            }

            static void DisplayProgress(int completed, int total, string operation)
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                int progressBarWidth = 30;
                int progressChars = (int)((completed / (double)total) * progressBarWidth);
                string progressBar = new string('#', progressChars) + new string('-', progressBarWidth - progressChars);

                Console.CursorLeft = 0;
                Console.Write($"{rm.GetString("Copy")} [{progressBar}] {(completed / (double)total):P0} ({completed}/{total}) {rm.GetString("Files")} {operation}");
            }

            static void DisplayArchiveProgress(long archivedSize, long totalSize)
            {
                ResourceManager rm = new ResourceManager("ABMT_v1.Strings", typeof(MediaBackup).Assembly);
                int progressBarWidth = 30;
                int progressChars = (int)((archivedSize / (double)totalSize) * progressBarWidth);
                string progressBar = new string('#', progressChars) + new string('-', progressBarWidth - progressChars);

                Console.CursorLeft = 0;
                Console.Write($"{rm.GetString("Archiving")} [{progressBar}] {(archivedSize / (double)totalSize):P0} ({FormatFileSize(archivedSize)}/{FormatFileSize(totalSize)})");
            }

            static string FormatFileSize(long bytes)
            {
                if (bytes >= 1073741824)
                {
                    return $"{bytes / 1073741824.0:F2} GB";
                }
                else if (bytes >= 1048576)
                {
                    return $"{bytes / 1048576.0:F2} MB";
                }
                else if (bytes >= 1024)
                {
                    return $"{bytes / 1024.0:F2} KB";
                }
                else
                {
                    return $"{bytes} bytes";
                }
            }

        }
    }

    class MediaInfo
    {
        public string OriginalPath { get; set; }
        public string FileName { get; set; }

        public MediaInfo(string originalPath, string fileName)
        {
            OriginalPath = originalPath;
            FileName = fileName;
        }
    }
}