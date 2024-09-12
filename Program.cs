using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.Marshalling;
using Newtonsoft.Json;


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

        while (true)
        {
            deviceName = GetDeviceName();

            if (deviceName == "UnknownDevice")
            {
                if (!checkMessageShow)
                {
                    Console.WriteLine("Подключите ваше устройство.");
                    Console.WriteLine("Или введите 'C' для продолжения без устройства.");

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

        static void ShowMainMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("ABMT");
            Console.ResetColor();
            Console.WriteLine(" Android Backup Media Tool");
            Console.WriteLine($"Подключенное устройство: {deviceName}");
            DisplayConnectionStatus();
            Console.WriteLine();
            Console.WriteLine("Выберите действие:");
            Console.WriteLine(new string('-', 72));
            Console.WriteLine("1. Резервное копирование");
            Console.WriteLine("2. Восстановление из резервной копии");
            Console.WriteLine("3. Подключиться по Wi-Fi") ;
            Console.WriteLine("0. Выход");
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
                    Console.WriteLine("Некорректный выбор. Нажмите любую клавишу для повторного выбора.");
                    Console.ReadKey();
                    break;
            }
        }

        static void DisplayConnectionStatus()
        {
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
            string status = "Нет подключенного устройства";
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
                            status = "Подключено через Wi-Fi";
                        }
                        else
                        {
                            // Если вывод не содержит префикса `adb-`, считаем подключение по USB
                            status = "Подключено через USB";
                        }
                        break;
                    }
                }
            }

            Console.WriteLine($"Текущее состояние подключения: {status}");
        }



        static void ConnectToDeviceOverWifi()
        {
            Console.WriteLine("Для подключения по Wi-Fi вам нужно включить её и перейти в (Отладка по Wi-Fi -> Подключить устройство с помощью кода подключения) и ввести данные от туда");

            Console.Write("Введите IP-адрес устройства (например, 192.168.1.2): ");
            string ipAddress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string port = Console.ReadLine();
            Console.Write("Введите код подключения по сети Wi-fi: ");
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
                    Console.WriteLine("Устройство подключено по Wi-Fi.");
                }
                else
                {
                    Console.WriteLine("Ошибка подключения по Wi-Fi.");
                }
                Console.WriteLine(output);
            }
            else
            {
                Console.WriteLine("IP-адрес не может быть пустым.");
            }

            Console.WriteLine("Нажмите любую клавишу для возврата в главное меню.");
            Console.ReadKey();
            ShowMainMenu();
        }


        static void BackupMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Резервное копирование");
                Console.ResetColor();
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine(" Начать резервное копирование (1)");
                Console.WriteLine(" Вернуться в главное меню (9)");
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
                        Console.WriteLine("Некорректный выбор. Нажмите любую клавишу для повторного выбора.");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void RestoreMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Восстановление из резервной копии");
                Console.ResetColor();
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine(" Восстановить из резервной копии (1)");
                Console.WriteLine(" Вернуться в главное меню (9)");
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
                        Console.WriteLine("Некорректный выбор. Нажмите любую клавишу для повторного выбора.");
                        Console.ReadKey();
                        break;
                }
            }
        }
        static void BackupMedia()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            string timestamp = DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss");
            string backupFileName = $"{deviceName} {timestamp}.zip";
            string backupPath = Path.Combine(backupFolder, backupFileName);

            Console.WriteLine("Запуск резервного копирования...");

            var findProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "shell find /storage/emulated/0 -type f \\( -name '*.jpg' -o -name '*.png' -o -name '*.mp4' -o -name '*.avi' -o -name '*.mkv' -o -name '*.mov' -o -name '*.mp3' -o -name '*.wav' -o -name '*.flac' -o -name '*.doc' -o -name '*.docx' -o -name '*.pdf' -o -name '*.txt' \\) -exec stat -c '%s %n' {} + 2>/dev/null",
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
                Console.WriteLine($"Ошибка ADB: {errorOutput}");
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

            Console.WriteLine($"Общее количество файлов: {otherFiles.Count + dataFiles.Count}");
            Console.WriteLine($"Количество файлов в Android/data/: {dataFiles.Count}");

            // Инициализация счётчиков для файлов
            var fileCounts = new Dictionary<string, (int Count, int HiddenCount, int DataCount, long Size, long HiddenSize, long DataSize)>();
            long totalSize = 0;

            foreach (var file in otherFiles)
            {
                string extension = Path.GetExtension(file.path).ToLower();
                bool isHidden = Path.GetFileName(file.path).StartsWith(".");

                if (extension == ".jpg" || extension == ".png")
                    fileCounts["photo"] = (fileCounts.GetValueOrDefault("photo").Count + 1,
                                           fileCounts.GetValueOrDefault("photo").HiddenCount + (isHidden ? 1 : 0),
                                           fileCounts.GetValueOrDefault("photo").DataCount,
                                           fileCounts.GetValueOrDefault("photo").Size + file.size,
                                           fileCounts.GetValueOrDefault("photo").HiddenSize + (isHidden ? file.size : 0),
                                           fileCounts.GetValueOrDefault("photo").DataSize);
                else if (extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".mov")
                    fileCounts["video"] = (fileCounts.GetValueOrDefault("video").Count + 1,
                                           fileCounts.GetValueOrDefault("video").HiddenCount + (isHidden ? 1 : 0),
                                           fileCounts.GetValueOrDefault("video").DataCount,
                                           fileCounts.GetValueOrDefault("video").Size + file.size,
                                           fileCounts.GetValueOrDefault("video").HiddenSize + (isHidden ? file.size : 0),
                                           fileCounts.GetValueOrDefault("video").DataSize);
                else if (extension == ".mp3" || extension == ".wav" || extension == ".flac")
                    fileCounts["audio"] = (fileCounts.GetValueOrDefault("audio").Count + 1,
                                           fileCounts.GetValueOrDefault("audio").HiddenCount + (isHidden ? 1 : 0),
                                           fileCounts.GetValueOrDefault("audio").DataCount,
                                           fileCounts.GetValueOrDefault("audio").Size + file.size,
                                           fileCounts.GetValueOrDefault("audio").HiddenSize + (isHidden ? file.size : 0),
                                           fileCounts.GetValueOrDefault("audio").DataSize);
                else if (extension == ".doc" || extension == ".docx" || extension == ".pdf" || extension == ".txt")
                    fileCounts["document"] = (fileCounts.GetValueOrDefault("document").Count + 1,
                                              fileCounts.GetValueOrDefault("document").HiddenCount + (isHidden ? 1 : 0),
                                              fileCounts.GetValueOrDefault("document").DataCount,
                                              fileCounts.GetValueOrDefault("document").Size + file.size,
                                              fileCounts.GetValueOrDefault("document").HiddenSize + (isHidden ? file.size : 0),
                                              fileCounts.GetValueOrDefault("document").DataSize);

                totalSize += file.size;
            }

            // Подсчёт файлов в Android/data/ по категориям и их размеров
            foreach (var file in dataFiles)
            {
                string extension = Path.GetExtension(file.path).ToLower();
                bool isHidden = Path.GetFileName(file.path).StartsWith(".");

                if (extension == ".jpg" || extension == ".png")
                    fileCounts["photo"] = (fileCounts.GetValueOrDefault("photo").Count,
                                           fileCounts.GetValueOrDefault("photo").HiddenCount,
                                           fileCounts.GetValueOrDefault("photo").DataCount + 1,
                                           fileCounts.GetValueOrDefault("photo").Size,
                                           fileCounts.GetValueOrDefault("photo").HiddenSize,
                                           fileCounts.GetValueOrDefault("photo").DataSize + file.size);
                else if (extension == ".mp4" || extension == ".avi" || extension == ".mkv" || extension == ".mov")
                    fileCounts["video"] = (fileCounts.GetValueOrDefault("video").Count,
                                           fileCounts.GetValueOrDefault("video").HiddenCount,
                                           fileCounts.GetValueOrDefault("video").DataCount + 1,
                                           fileCounts.GetValueOrDefault("video").Size,
                                           fileCounts.GetValueOrDefault("video").HiddenSize,
                                           fileCounts.GetValueOrDefault("video").DataSize + file.size);
                else if (extension == ".mp3" || extension == ".wav" || extension == ".flac")
                    fileCounts["audio"] = (fileCounts.GetValueOrDefault("audio").Count,
                                           fileCounts.GetValueOrDefault("audio").HiddenCount,
                                           fileCounts.GetValueOrDefault("audio").DataCount + 1,
                                           fileCounts.GetValueOrDefault("audio").Size,
                                           fileCounts.GetValueOrDefault("audio").HiddenSize,
                                           fileCounts.GetValueOrDefault("audio").DataSize + file.size);
                else if (extension == ".doc" || extension == ".docx" || extension == ".pdf" || extension == ".txt")
                    fileCounts["document"] = (fileCounts.GetValueOrDefault("document").Count,
                                              fileCounts.GetValueOrDefault("document").HiddenCount,
                                              fileCounts.GetValueOrDefault("document").DataCount + 1,
                                              fileCounts.GetValueOrDefault("document").Size,
                                              fileCounts.GetValueOrDefault("document").HiddenSize,
                                              fileCounts.GetValueOrDefault("document").DataSize + file.size);
            }

            // Вычисление общего размера с учётом скрытых и Android/data/ файлов
            long totalRegularSize = fileCounts.Values.Sum(v => v.Size);
            long totalHiddenSize = fileCounts.Values.Sum(v => v.HiddenSize);
            long totalDataSize = fileCounts.Values.Sum(v => v.DataSize);

            long totalCalculatedSize = totalRegularSize + totalHiddenSize + totalDataSize;

            Console.WriteLine($"Общий размер найденных файлов: {FormatFileSize(totalCalculatedSize)}");
            Console.WriteLine($"1. Фото ({fileCounts.GetValueOrDefault("photo").Count} обычных, {fileCounts.GetValueOrDefault("photo").HiddenCount} скрытых, {fileCounts.GetValueOrDefault("photo").DataCount} в Android/data/)");
            Console.WriteLine($"2. Видео ({fileCounts.GetValueOrDefault("video").Count} обычных, {fileCounts.GetValueOrDefault("video").HiddenCount} скрытых, {fileCounts.GetValueOrDefault("video").DataCount} в Android/data/)");
            Console.WriteLine($"3. Аудио ({fileCounts.GetValueOrDefault("audio").Count} обычных, {fileCounts.GetValueOrDefault("audio").HiddenCount} скрытых, {fileCounts.GetValueOrDefault("audio").DataCount} в Android/data/)");
            Console.WriteLine($"4. Документы ({fileCounts.GetValueOrDefault("document").Count} обычных, {fileCounts.GetValueOrDefault("document").HiddenCount} скрытых, {fileCounts.GetValueOrDefault("document").DataCount} в Android/data/)");

            // Выбор файлов для копирования
            Console.WriteLine("Введите номера типов файлов через пробел, которые вы хотите сохранить (например, 1 2 для фото и видео):");
            Console.WriteLine("Введите 'H', чтобы включить скрытые файлы в резервное копирование.");
            Console.WriteLine("Введите 'D', чтобы включить файлы из Android/data/ в резервное копирование.");

            string? choice = Console.ReadLine();
            if (string.IsNullOrEmpty(choice))
            {
                Console.WriteLine("Некорректный выбор. Резервное копирование отменено.");
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
                bool isHidden = Path.GetFileName(file.path).StartsWith(".");

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
                    DisplayProgress(copiedFiles, totalSelectedFiles, "скопировано");
                }
                else
                {
                    Console.WriteLine($"\nОшибка при копировании файла: {file.path}");
                }
            }

            string metadataPath = Path.Combine(tempBackupFolder, "data.json");
            File.WriteAllText(metadataPath, JsonConvert.SerializeObject(mediaInfos));

            Console.WriteLine();
            Console.WriteLine("\nСоздаю архив бекапа.");

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

            Console.WriteLine($"\nРезервная копия успешно создана: {backupPath}");
            Console.WriteLine($"Размер резервной копии: {FormatFileSize(backupSize)}");
            Console.WriteLine("Нажмите любую клавишу для возврата в главное меню.");
            Console.ReadKey();
            ShowMainMenu();
        }


            static void RestoreMedia()
        {
            Console.WriteLine("Выберите файл для восстановления:");
            string? backupFilePath = ShowBackupFilePicker();

            if (string.IsNullOrEmpty(backupFilePath) || !File.Exists(backupFilePath))
            {
                Console.WriteLine("Файл не найден или не выбран.");
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
                Console.WriteLine("Файл метаданных не найден. Восстановление невозможно.");
                return;
            }

            var mediaInfos = JsonConvert.DeserializeObject<List<MediaInfo>>(File.ReadAllText(metadataPath));
            if (mediaInfos == null)
            {
                Console.WriteLine("\nОшибка при чтении файла метаданных.");
                return;
            }

            int photoCount = mediaInfos.Count(m => m.FileName.EndsWith(".jpg") || m.FileName.EndsWith(".png"));
            int videoCount = mediaInfos.Count(m => m.FileName.EndsWith(".mp4") || m.FileName.EndsWith(".avi") || m.FileName.EndsWith(".mkv") || m.FileName.EndsWith(".mov"));
            int audioCount = mediaInfos.Count(m => m.FileName.EndsWith(".mp3") || m.FileName.EndsWith(".wav") || m.FileName.EndsWith(".flac"));
            int documentCount = mediaInfos.Count(m => m.FileName.EndsWith(".doc") || m.FileName.EndsWith(".docx") || m.FileName.EndsWith(".pdf") || m.FileName.EndsWith(".txt"));

            Console.WriteLine($"1. Фото ({photoCount})");
            Console.WriteLine($"2. Видео ({videoCount})");
            Console.WriteLine($"3. Аудио ({audioCount})");
            Console.WriteLine($"4. Документы ({documentCount})");
            Console.WriteLine("Введите номера типов файлов через пробел, которые вы хотите восстановить (например, 1 2 для фото и видео):");

            string? choice = Console.ReadLine();
            if (string.IsNullOrEmpty(choice))
            {
                Console.WriteLine("Некорректный выбор. Восстановление отменено.");
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
                        DisplayProgress(restoredFiles, totalFiles, "восстановлено");
                    }
                    else
                    {
                        Console.WriteLine($"\nОшибка при восстановлении файла: {sourcePath}");
                    }
                }
            }

            Directory.Delete(extractPath, true);
            Console.WriteLine("\nВосстановление завершено.");
            Console.WriteLine("Нажмите любую клавишу для возврата в главное меню.");
            Console.ReadKey();
            ShowMainMenu();
        }

        static string? ShowBackupFilePicker()
        {
            if (!Directory.Exists(backupFolder))
            {
                Console.WriteLine("Папка с бэкапами не найдена.");
                return null;
            }

            var files = Directory.GetFiles(backupFolder, "*.zip");
            if (files.Length == 0)
            {
                Console.WriteLine("Нет доступных бэкапов для восстановления.");
                return null;
            }

            for (int i = 0; i < files.Length; i++)
            {
                var fileInfo = new FileInfo(files[i]);
                Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])} | (Размер: {FormatFileSize(fileInfo.Length)})");
            }

            Console.Write("Введите номер файла для восстановления: ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= files.Length)
            {
                return files[choice - 1];
            }

            return null;
        }

        static void DisplayProgress(int completed, int total, string operation)
        {
            int progressBarWidth = 30;
            int progressChars = (int)((completed / (double)total) * progressBarWidth);
            string progressBar = new string('#', progressChars) + new string('-', progressBarWidth - progressChars);

            Console.CursorLeft = 0;
            Console.Write($"Копирование: [{progressBar}] {(completed / (double)total):P0} ({completed}/{total}) Файлов {operation}");
        }

        static void DisplayArchiveProgress(long archivedSize, long totalSize)
        {
            int progressBarWidth = 30;
            int progressChars = (int)((archivedSize / (double)totalSize) * progressBarWidth);
            string progressBar = new string('#', progressChars) + new string('-', progressBarWidth - progressChars);

            Console.CursorLeft = 0;
            Console.Write($"Архивация: [{progressBar}] {(archivedSize / (double)totalSize):P0} ({FormatFileSize(archivedSize)}/{FormatFileSize(totalSize)})");
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
