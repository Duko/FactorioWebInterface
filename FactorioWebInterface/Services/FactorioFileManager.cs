﻿using FactorioWebInterface.Models;
using FactorioWebInterface.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioWebInterface.Services
{
    public class FactorioFileManager
    {
        private readonly ILogger<FactorioFileManager> _logger;

        public event EventHandler<FactorioFileManager, FilesChangedEventArgs> TempSaveFilesChanged;
        public event EventHandler<FactorioFileManager, FilesChangedEventArgs> LocalSaveFilesChanged;
        public event EventHandler<FactorioFileManager, FilesChangedEventArgs> GlobalSaveFilesChanged;
        public event EventHandler<FactorioFileManager, FilesChangedEventArgs> LogFilesChanged;
        public event EventHandler<FactorioFileManager, FilesChangedEventArgs> ChatLogFilesChanged;
        public event EventHandler<FactorioFileManager, ScenariosChangedEventArgs> ScenariosChanged;

        public FactorioFileManager(ILogger<FactorioFileManager> logger)
        {
            _logger = logger;
        }

        public FileMetaData[] GetTempSaveFiles(FactorioServerData serverData)
        {
            string serverId = serverData.ServerId;

            var path = serverData.TempSavesDirectoryPath;
            var dir = Constants.TempSavesDirectoryName;

            return GetFilesMetaData(path, dir);
        }

        public FileMetaData[] GetLocalSaveFiles(FactorioServerData serverData)
        {
            string serverId = serverData.ServerId;

            var path = serverData.LocalSavesDirectoroyPath;
            var dir = Constants.LocalSavesDirectoryName;

            return GetFilesMetaData(path, dir);
        }

        public FileMetaData[] GetGlobalSaveFiles()
        {
            var path = FactorioServerData.GlobalSavesDirectoryPath;
            var dir = Constants.GlobalSavesDirectoryName;

            return GetFilesMetaData(path, dir);
        }

        public List<FileMetaData> GetLogs(FactorioServerData serverData)
        {
            string serverId = serverData.ServerId;

            List<FileMetaData> logs = new List<FileMetaData>();

            var currentLog = new FileInfo(serverData.CurrentLogPath);
            if (currentLog.Exists)
            {
                logs.Add(new FileMetaData()
                {
                    Name = currentLog.Name,
                    CreatedTime = currentLog.CreationTimeUtc,
                    LastModifiedTime = currentLog.LastWriteTimeUtc,
                    Directory = Path.Combine(serverId),
                    Size = currentLog.Length
                });
            }

            var logsDir = new DirectoryInfo(serverData.LogsDirectoryPath);
            if (logsDir.Exists)
            {
                var logfiles = logsDir.EnumerateFiles("*.log")
                    .Select(x => new FileMetaData()
                    {
                        Name = x.Name,
                        CreatedTime = x.CreationTimeUtc,
                        LastModifiedTime = x.LastWriteTimeUtc,
                        Directory = Path.Combine(serverId, Constants.LogDirectoryName),
                        Size = x.Length
                    })
                    .OrderByDescending(x => x.CreatedTime);

                logs.AddRange(logfiles);
            }

            return logs;
        }

        public ScenarioMetaData[] GetScenarios()
        {
            try
            {
                var dir = new DirectoryInfo(FactorioServerData.ScenarioDirectoryPath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                return dir.EnumerateDirectories().Select(d =>
                    new ScenarioMetaData()
                    {
                        Name = d.Name,
                        CreatedTime = d.CreationTimeUtc,
                        LastModifiedTime = d.LastWriteTimeUtc
                    }
                ).ToArray();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return new ScenarioMetaData[0];
            }
        }

        public List<FileMetaData> GetChatLogs(FactorioServerData serverData)
        {
            string serverId = serverData.ServerId;

            List<FileMetaData> logs = new List<FileMetaData>();

            var logsDir = new DirectoryInfo(serverData.ChatLogsDirectoryPath);
            if (logsDir.Exists)
            {
                var logfiles = logsDir.EnumerateFiles("*.log")
                    .Select(x => new FileMetaData()
                    {
                        Name = x.Name,
                        CreatedTime = x.CreationTimeUtc,
                        LastModifiedTime = x.LastWriteTimeUtc,
                        Directory = Path.Combine(serverId, Constants.ChatLogDirectoryName),
                        Size = x.Length
                    })
                    .OrderByDescending(x => x.CreatedTime);

                logs.AddRange(logfiles);
            }

            return logs;
        }

        public FileInfo GetLogFile(string directoryName, string fileName)
        {
            string safeFileName = Path.GetFileName(fileName);
            string path = Path.Combine(FactorioServerData.baseDirectoryPath, directoryName, safeFileName);
            path = Path.GetFullPath(path);

            if (!path.StartsWith(FactorioServerData.baseDirectoryPath))
            {
                return null;
            }

            var file = new FileInfo(path);
            if (!file.Exists)
            {
                return null;
            }

            if (file.Extension != ".log")
            {
                return null;
            }

            if (file.Directory.Name == Constants.LogDirectoryName)
            {
                return file;
            }
            else if (file.Name == Constants.CurrentLogFileName)
            {
                return file;
            }
            else
            {
                return null;
            }
        }

        public FileInfo GetChatLogFile(string directoryName, string fileName)
        {
            string safeFileName = Path.GetFileName(fileName);
            string path = Path.Combine(FactorioServerData.baseDirectoryPath, directoryName, safeFileName);
            path = Path.GetFullPath(path);

            if (!path.StartsWith(FactorioServerData.baseDirectoryPath))
            {
                return null;
            }

            var file = new FileInfo(path);
            if (!file.Exists)
            {
                return null;
            }

            if (file.Extension != ".log")
            {
                return null;
            }

            if (file.Directory.Name == Constants.ChatLogDirectoryName)
            {
                return file;
            }
            else
            {
                return null;
            }
        }

        private FileMetaData[] GetFilesMetaData(string path, string directory)
        {
            try
            {
                var di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    di.Create();
                }

                var files = di.EnumerateFiles("*.zip")
                    .Select(f => new FileMetaData()
                    {
                        Name = f.Name,
                        Directory = directory,
                        CreatedTime = f.CreationTimeUtc,
                        LastModifiedTime = f.LastWriteTimeUtc,
                        Size = f.Length
                    })
                    .ToArray();

                return files;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return new FileMetaData[0];
            }
        }

        private bool IsSaveDirectory(string dirName)
        {
            switch (dirName)
            {
                case Constants.GlobalSavesDirectoryName:
                case Constants.LocalSavesDirectoryName:
                case Constants.TempSavesDirectoryName:
                    return true;
                default:
                    return false;
            }
        }

        private DirectoryInfo GetSaveDirectory(string dirName)
        {
            try
            {
                if (FactorioServerData.ValidSaveDirectories.Contains(dirName))
                {
                    var dirPath = Path.Combine(FactorioServerData.baseDirectoryPath, dirName);
                    dirPath = Path.GetFullPath(dirPath);

                    if (!dirPath.StartsWith(FactorioServerData.baseDirectoryPath))
                        return null;

                    var dir = new DirectoryInfo(dirPath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }

                    return dir;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public DirectoryInfo GetSaveDirectory(string serverId, string dirName)
        {
            if (dirName == Constants.TempSavesDirectoryName || dirName == Constants.LocalSavesDirectoryName)
            {
                dirName = Path.Combine(serverId, dirName);
            }

            return GetSaveDirectory(dirName);
        }

        private string GetServerIdOfDirectory(DirectoryInfo dir)
        {
            switch (dir.Name)
            {
                case Constants.TempSavesDirectoryName:
                case Constants.LocalSavesDirectoryName:
                    var parent = dir.Parent;
                    return parent.Name;
                default:
                    return "";
            }
        }

        private string SafeFilePath(string dirPath, string fileName)
        {
            fileName = Path.GetFileName(fileName);
            string path = Path.Combine(dirPath, fileName);
            path = Path.GetFullPath(path);

            if (!path.StartsWith(FactorioServerData.baseDirectoryPath))
            {
                return null;
            }

            return path;
        }

        public FileInfo GetSaveFile(string serverId, string directoryName, string fileName)
        {
            var directory = GetSaveDirectory(serverId, directoryName);

            if (directory == null)
            {
                return null;
            }

            string path = SafeFilePath(directory.FullName, fileName);
            if (path == null)
            {
                return null;
            }

            if (Path.GetExtension(fileName) != ".zip")
            {
                return null;
            }

            try
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    return fi;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(GetSaveFile));
                return null;
            }
        }

        public async Task<Result> UploadFiles(string serverId, IList<IFormFile> files)
        {
            var directory = GetSaveDirectory(serverId, Constants.LocalSavesDirectoryName);

            if (directory == null)
            {
                return Result.Failure(new Error(Constants.InvalidDirectoryErrorKey, Path.Combine(serverId, Constants.LocalSavesDirectoryName)));
            }

            var changedFiles = new List<FileMetaData>();
            var errors = new List<Error>();

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.FileName))
                {
                    errors.Add(new Error(Constants.InvalidFileNameErrorKey, file.FileName ?? ""));
                    continue;
                }
                if (file.FileName.Contains(" "))
                {
                    errors.Add(new Error(Constants.InvalidFileNameErrorKey, $"name {file.FileName} cannot contain spaces."));
                    continue;
                }

                string path = SafeFilePath(directory.FullName, file.FileName);
                if (path == null)
                {
                    errors.Add(new Error(Constants.FileErrorKey, $"Error uploading {file.FileName}."));
                    continue;
                }

                try
                {
                    var fi = new FileInfo(path);

                    if (fi.Exists)
                    {
                        errors.Add(new Error(Constants.FileAlreadyExistsErrorKey, $"{file.FileName} already exists."));
                        continue;
                    }

                    using (var writeStream = fi.OpenWrite())
                    using (var readStream = file.OpenReadStream())
                    {
                        await readStream.CopyToAsync(writeStream);
                        await writeStream.FlushAsync();
                    }

                    fi.Refresh();

                    var fileMetaData = new FileMetaData()
                    {
                        Name = fi.Name,
                        CreatedTime = fi.CreationTimeUtc,
                        LastModifiedTime = fi.LastWriteTimeUtc,
                        Size = fi.Length,
                        Directory = Constants.LocalSavesDirectoryName
                    };

                    changedFiles.Add(fileMetaData);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error Uploading file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error uploading {file.FileName}."));
                }
            }

            if (changedFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(serverId, FilesChangedType.Create, changedFiles);
                _ = Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public Result DeleteFiles(string serverId, List<string> filePaths)
        {
            var changedTempFiles = new List<FileMetaData>();
            var changedLocalFiles = new List<FileMetaData>();
            var changedGlobalFiles = new List<FileMetaData>();
            var errors = new List<Error>();

            foreach (string filePath in filePaths)
            {
                var dirName = Path.GetDirectoryName(filePath);
                var dir = GetSaveDirectory(serverId, dirName);

                if (dir == null)
                {
                    errors.Add(new Error(Constants.InvalidDirectoryErrorKey, dirName));
                    continue;
                }

                string path = SafeFilePath(dir.FullName, filePath);
                if (path == null)
                {
                    errors.Add(new Error(Constants.FileErrorKey, $"Error deleting {filePath}."));
                    continue;
                }

                try
                {
                    var fi = new FileInfo(path);

                    if (!fi.Exists)
                    {
                        errors.Add(new Error(Constants.MissingFileErrorKey, $"{filePath} doesn't exists."));
                        continue;
                    }

                    var fileMetaData = new FileMetaData()
                    {
                        Name = fi.Name,
                        CreatedTime = fi.CreationTimeUtc,
                        LastModifiedTime = fi.LastWriteTimeUtc,
                        Size = fi.Length,
                        Directory = dirName
                    };

                    fi.Delete();

                    switch (dirName)
                    {
                        case Constants.TempSavesDirectoryName:
                            changedTempFiles.Add(fileMetaData);
                            break;
                        case Constants.LocalSavesDirectoryName:
                            changedLocalFiles.Add(fileMetaData);
                            break;
                        case Constants.GlobalSavesDirectoryName:
                            changedGlobalFiles.Add(fileMetaData);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error Deleting file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error deleting {filePath}."));
                }
            }

            if (changedTempFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(serverId, FilesChangedType.Delete, null, changedTempFiles);
                _ = Task.Run(() => TempSaveFilesChanged?.Invoke(this, ev));
            }

            if (changedLocalFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(serverId, FilesChangedType.Delete, null, changedLocalFiles);
                _ = Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
            }

            if (changedGlobalFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs("", FilesChangedType.Delete, null, changedGlobalFiles);
                _ = Task.Run(() => GlobalSaveFilesChanged?.Invoke(this, ev));
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        private static HashSet<string> trackMap = new HashSet<string>()
        {
            Constants.TempSavesDirectoryName,
            Constants.LocalSavesDirectoryName,
            Constants.GlobalSavesDirectoryName
        };

        public Result MoveFiles(string serverId, string destination, List<string> filePaths)
        {
            string targetDirPath = Path.Combine(FactorioServerData.baseDirectoryPath, destination);

            var targetDir = GetSaveDirectory(destination);
            if (targetDir == null)
            {
                return Result.Failure(new Error(Constants.InvalidDirectoryErrorKey, destination));
            }

            bool trackDestination = trackMap.Contains(targetDir.Name);

            List<FileMetaData> newFiles = null;
            string destinationId = null;
            if (trackDestination)
            {
                newFiles = new List<FileMetaData>();
                destinationId = GetServerIdOfDirectory(targetDir);
            }

            var oldTempFiles = new List<FileMetaData>();
            var oldLocalFiles = new List<FileMetaData>();
            var oldGlobalFiles = new List<FileMetaData>();

            var errors = new List<Error>();

            foreach (var filePath in filePaths)
            {
                var sourceDirName = Path.GetDirectoryName(filePath);
                var sourceDir = GetSaveDirectory(serverId, sourceDirName);

                if (sourceDir == null)
                {
                    errors.Add(new Error(Constants.InvalidDirectoryErrorKey, sourceDirName));
                    continue;
                }

                string sourceFullPath = SafeFilePath(sourceDir.FullName, filePath);
                if (sourceFullPath == null)
                {
                    errors.Add(new Error(Constants.FileErrorKey, $"Error moveing {filePath}."));
                    continue;
                }

                try
                {
                    var sourceFile = new FileInfo(sourceFullPath);

                    if (!sourceFile.Exists)
                    {
                        errors.Add(new Error(Constants.MissingFileErrorKey, $"{filePath} doesn't exists."));
                        continue;
                    }

                    string destinationFilePath = Path.Combine(targetDir.FullName, sourceFile.Name);

                    var destinationFileInfo = new FileInfo(destinationFilePath);

                    if (destinationFileInfo.Exists)
                    {
                        errors.Add(new Error(Constants.FileAlreadyExistsErrorKey, $"{destination}/{destinationFileInfo.Name} already exists."));
                        continue;
                    }

                    var oldFileMetaDdata = new FileMetaData()
                    {
                        Name = sourceFile.Name,
                        CreatedTime = sourceFile.CreationTimeUtc,
                        LastModifiedTime = sourceFile.LastWriteTimeUtc,
                        Size = sourceFile.Length,
                        Directory = sourceFile.Directory.Name
                    };

                    sourceFile.MoveTo(destinationFilePath);

                    switch (oldFileMetaDdata.Directory)
                    {
                        case Constants.TempSavesDirectoryName:
                            oldTempFiles.Add(oldFileMetaDdata);
                            break;
                        case Constants.LocalSavesDirectoryName:
                            oldLocalFiles.Add(oldFileMetaDdata);
                            break;
                        case Constants.GlobalSavesDirectoryName:
                            oldGlobalFiles.Add(oldFileMetaDdata);
                            break;
                        default:
                            break;
                    }

                    if (trackDestination)
                    {
                        destinationFileInfo.Refresh();

                        var newFileMetaData = new FileMetaData()
                        {
                            Name = destinationFileInfo.Name,
                            CreatedTime = destinationFileInfo.CreationTimeUtc,
                            LastModifiedTime = destinationFileInfo.LastWriteTimeUtc,
                            Size = destinationFileInfo.Length,
                            Directory = destinationFileInfo.Directory.Name
                        };

                        newFiles.Add(newFileMetaData);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error moveing file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error moveing {filePath}."));
                }
            }

            if (oldTempFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(serverId, FilesChangedType.Delete, null, oldTempFiles);
                _ = Task.Run(() => TempSaveFilesChanged?.Invoke(this, ev));
            }

            if (oldLocalFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(serverId, FilesChangedType.Delete, null, oldLocalFiles);
                _ = Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
            }

            if (oldGlobalFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs("", FilesChangedType.Delete, null, oldGlobalFiles);
                _ = Task.Run(() => GlobalSaveFilesChanged?.Invoke(this, ev));
            }

            if (trackDestination && newFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(destinationId, FilesChangedType.Create, newFiles);

                switch (targetDir.Name)
                {
                    case Constants.TempSavesDirectoryName:
                        _ = Task.Run(() => TempSaveFilesChanged?.Invoke(this, ev));
                        break;
                    case Constants.LocalSavesDirectoryName:
                        _ = Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
                        break;
                    case Constants.GlobalSavesDirectoryName:
                        _ = Task.Run(() => GlobalSaveFilesChanged?.Invoke(this, ev));
                        break;
                    default:
                        break;
                }
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public Result CopyFiles(string serverId, string destination, List<string> filePaths)
        {
            string targetDirPath = Path.Combine(FactorioServerData.baseDirectoryPath, destination);

            var targetDir = GetSaveDirectory(destination);
            if (targetDir == null)
            {
                return Result.Failure(new Error(Constants.InvalidDirectoryErrorKey, destination));
            }

            bool trackDestination = trackMap.Contains(targetDir.Name);

            List<FileMetaData> newFiles = null;
            string destinationId = null;
            if (trackDestination)
            {
                newFiles = new List<FileMetaData>();
                destinationId = GetServerIdOfDirectory(targetDir);
            }

            var errors = new List<Error>();

            foreach (var filePath in filePaths)
            {
                var sourceDirName = Path.GetDirectoryName(filePath);
                var sourceDir = GetSaveDirectory(serverId, sourceDirName);

                if (sourceDir == null)
                {
                    errors.Add(new Error(Constants.InvalidDirectoryErrorKey, sourceDirName));
                    continue;
                }

                string sourceFullPath = SafeFilePath(sourceDir.FullName, filePath);
                if (sourceFullPath == null)
                {
                    errors.Add(new Error(Constants.FileErrorKey, $"Error coppying {filePath}."));
                    continue;
                }

                try
                {
                    var sourceFile = new FileInfo(sourceFullPath);

                    if (!sourceFile.Exists)
                    {
                        errors.Add(new Error(Constants.MissingFileErrorKey, $"{filePath} doesn't exists."));
                        continue;
                    }

                    string destinationFilePath = Path.Combine(targetDir.FullName, sourceFile.Name);

                    var destinationFileInfo = new FileInfo(destinationFilePath);

                    if (destinationFileInfo.Exists)
                    {
                        errors.Add(new Error(Constants.FileAlreadyExistsErrorKey, $"{destination}/{destinationFileInfo.Name} already exists."));
                        continue;
                    }

                    sourceFile.CopyTo(destinationFilePath);
                    destinationFileInfo.LastWriteTimeUtc = sourceFile.LastWriteTimeUtc;


                    if (trackDestination)
                    {
                        destinationFileInfo.Refresh();

                        var newFileMetaData = new FileMetaData()
                        {
                            Name = destinationFileInfo.Name,
                            CreatedTime = destinationFileInfo.CreationTimeUtc,
                            LastModifiedTime = destinationFileInfo.LastWriteTimeUtc,
                            Size = destinationFileInfo.Length,
                            Directory = destinationFileInfo.Directory.Name
                        };

                        newFiles.Add(newFileMetaData);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Error copying file.", e);
                    errors.Add(new Error(Constants.FileErrorKey, $"Error coppying {filePath}."));
                }
            }

            if (trackDestination && newFiles.Count > 0)
            {
                var ev = new FilesChangedEventArgs(destinationId, FilesChangedType.Create, newFiles);

                switch (targetDir.Name)
                {
                    case Constants.TempSavesDirectoryName:
                        _ = Task.Run(() => TempSaveFilesChanged?.Invoke(this, ev));
                        break;
                    case Constants.LocalSavesDirectoryName:
                        _ = Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
                        break;
                    case Constants.GlobalSavesDirectoryName:
                        _ = Task.Run(() => GlobalSaveFilesChanged?.Invoke(this, ev));
                        break;
                    default:
                        break;
                }
            }

            if (errors.Count != 0)
            {
                return Result.Failure(errors);
            }
            else
            {
                return Result.OK;
            }
        }

        public Result RenameFile(string serverId, string directoryName, string fileName, string newFileName = "")
        {
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                return Result.Failure(Constants.InvalidFileNameErrorKey, newFileName);
            }
            if (newFileName.Contains(" "))
            {
                return Result.Failure(Constants.InvalidFileNameErrorKey, $"name { newFileName} cannot contain spaces.");
            }

            var directory = GetSaveDirectory(serverId, directoryName);

            if (directory == null)
            {
                return Result.Failure(new Error(Constants.InvalidDirectoryErrorKey, directoryName));
            }

            try
            {
                string actualFileName = Path.GetFileName(fileName);

                if (actualFileName != fileName)
                {
                    return Result.Failure(Constants.FileErrorKey, $"Invalid file name {fileName}");
                }

                string actualNewFileName = Path.GetFileName(newFileName);

                if (actualNewFileName != newFileName)
                {
                    return Result.Failure(Constants.FileErrorKey, $"Invalid file name {newFileName}");
                }

                string filePath = Path.Combine(directory.FullName, fileName);
                var fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists)
                {
                    return Result.Failure(Constants.MissingFileErrorKey, $"File {fileName} doesn't exist.");
                }

                string newFilePath = Path.Combine(directory.FullName, newFileName);
                if (Path.GetExtension(newFilePath) != ".zip")
                {
                    newFilePath += ".zip";
                }

                var newFileInfo = new FileInfo(newFilePath);

                if (newFileInfo.Exists)
                {
                    return Result.Failure(Constants.FileAlreadyExistsErrorKey, $"File {fileName} already exists.");
                }

                string dirName = directory.Name;

                var oldFileMetaDdata = new FileMetaData()
                {
                    Name = fileInfo.Name,
                    CreatedTime = fileInfo.CreationTimeUtc,
                    LastModifiedTime = fileInfo.LastWriteTimeUtc,
                    Size = fileInfo.Length,
                    Directory = dirName
                };

                fileInfo.MoveTo(newFilePath);
                newFileInfo.Refresh();

                var newFileMetaData = new FileMetaData()
                {
                    Name = newFileInfo.Name,
                    CreatedTime = newFileInfo.CreationTimeUtc,
                    LastModifiedTime = newFileInfo.LastWriteTimeUtc,
                    Size = newFileInfo.Length,
                    Directory = dirName
                };

                if (directoryName == Constants.GlobalSavesDirectoryName)
                {
                    serverId = "";
                }

                var ev = new FilesChangedEventArgs(serverId, FilesChangedType.Rename, new[] { newFileMetaData }, new[] { oldFileMetaDdata });

                switch (dirName)
                {
                    case Constants.TempSavesDirectoryName:
                        _ = Task.Run(() => TempSaveFilesChanged?.Invoke(this, ev));
                        break;
                    case Constants.LocalSavesDirectoryName:
                        _ = Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
                        break;
                    case Constants.GlobalSavesDirectoryName:
                        _ = Task.Run(() => GlobalSaveFilesChanged?.Invoke(this, ev));
                        break;
                    default:
                        break;
                }

                return Result.OK;
            }
            catch (Exception e)
            {
                _logger.LogError("Error renaming file.", e);
                return Result.Failure(Constants.FileErrorKey, $"Error renaming files");
            }
        }

        public void RaiseTempFilesChanged(FilesChangedEventArgs ev)
        {
            Task.Run(() => TempSaveFilesChanged?.Invoke(this, ev));
        }

        public void RaiseLocalFilesChanged(FilesChangedEventArgs ev)
        {
            Task.Run(() => LocalSaveFilesChanged?.Invoke(this, ev));
        }

        public void RaiseGlobalFilesChanged(FilesChangedEventArgs ev)
        {
            Task.Run(() => GlobalSaveFilesChanged?.Invoke(this, ev));
        }

        public Task RaiseRecentTempFiles(FactorioServerData serverData)
        {
            return Task.Run(async () =>
            {
                DateTime lastChecked;

                try
                {
                    await serverData.ServerLock.WaitAsync();

                    lastChecked = serverData.LastTempFilesChecked;
                    serverData.LastTempFilesChecked = DateTime.UtcNow;
                }
                finally
                {
                    serverData.ServerLock.Release();
                }

                try
                {
                    var dir = new DirectoryInfo(serverData.TempSavesDirectoryPath);

                    if (!dir.Exists)
                    {
                        dir.Create();
                        return;
                    }

                    var data = dir.EnumerateFiles()
                        .Where(f => f.LastWriteTimeUtc >= lastChecked)
                        .Select(f => new FileMetaData()
                        {
                            Name = f.Name,
                            CreatedTime = f.CreationTimeUtc,
                            LastModifiedTime = f.LastWriteTimeUtc,
                            Size = f.Length,
                            Directory = Constants.TempSavesDirectoryName
                        })
                        .ToArray();

                    var ev = new FilesChangedEventArgs(serverData.ServerId, FilesChangedType.Create, data);

                    TempSaveFilesChanged?.Invoke(this, ev);
                }
                catch (Exception ex)
                {
                    _logger.LogError(nameof(RaiseRecentTempFiles), ex);
                }
            });
        }

        public void NotifyTempFilesChanged(FactorioServerData serverData)
        {
            Task.Run(() =>
            {
                var files = GetTempSaveFiles(serverData);
                var ev = new FilesChangedEventArgs(serverData.ServerId, FilesChangedType.Create, files);

                TempSaveFilesChanged?.Invoke(this, ev);
            });
        }

        public void NotifyScenariosChanged()
        {
            var scenarios = GetScenarios();
            var ev = new ScenariosChangedEventArgs(ScenariosChangedType.Create, scenarios);

            ScenariosChanged?.Invoke(this, ev);
        }
    }
}