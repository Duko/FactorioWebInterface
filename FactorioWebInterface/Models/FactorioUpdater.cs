﻿using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioUpdater
    {
        private static readonly Regex downloadRegex = new Regex(@"/get-download/(\d+\.\d+\.\d+)/headless/linux64", RegexOptions.Compiled);
        private static readonly Regex cacheRegex = new Regex(@"factorio_headless_x64_(\d+\.\d+\.\d+)", RegexOptions.Compiled);

        private readonly SemaphoreSlim downloadLock = new SemaphoreSlim(1);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FactorioUpdater> _logger;

        public FactorioUpdater(IHttpClientFactory httpClientFactory, ILogger<FactorioUpdater> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private bool ExecuteProcess(string filename, string arguments)
        {
            _logger.LogInformation("ExecuteProcess filename: {fileName} arguments: {arguments}", filename, arguments);
            Process proc = Process.Start(filename, arguments);
            proc.WaitForExit();
            return proc.ExitCode > -1;
        }

        private FileInfo GetCachedFile(string version)
        {
            try
            {
                var dir = new DirectoryInfo(FactorioServerData.UpdateCacheDirectoryPath);
                if (!dir.Exists)
                {
                    return null;
                }

                string path = Path.Combine(dir.FullName, $"factorio_headless_x64_{version}.tar.xz");
                FileInfo file = new FileInfo(path);

                if (!file.Exists || file.Directory.FullName != dir.FullName)
                {
                    return null;
                }

                return file;
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(GetCachedFile));
                return null;
            }
        }

        public bool DeleteCachedFile(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            try
            {
                var dir = new DirectoryInfo(FactorioServerData.UpdateCacheDirectoryPath);
                if (!dir.Exists)
                {
                    return false;
                }

                string path = Path.Combine(dir.FullName, $"factorio_headless_x64_{version}.tar.xz");

                FileInfo file = new FileInfo(path);

                if (!file.Exists || file.Directory.FullName != dir.FullName)
                {
                    return false;
                }

                file.Delete();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(DeleteCachedFile));
                return false;
            }
        }

        public List<string> GetCachedVersions()
        {
            List<string> result = new List<string>();

            try
            {
                var dir = new DirectoryInfo(FactorioServerData.UpdateCacheDirectoryPath);
                if (!dir.Exists)
                {
                    return result;
                }

                var files = dir.GetFiles("*.tar.xz");

                foreach (var file in files)
                {
                    var match = cacheRegex.Match(file.Name);

                    if (match.Success)
                    {
                        string version = match.Groups[1].Value;
                        result.Add(version);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(GetCachedVersions));
            }

            return result;
        }

        public async Task<List<string>> GetDownloadableVersions()
        {
            var result = new List<string>();

            var client = _httpClientFactory.CreateClient();
            var download = await client.GetAsync(Constants.DownloadHeadlessExperimentalURL);

            if (!download.IsSuccessStatusCode)
            {
                return result;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(await download.Content.ReadAsStreamAsync());

            var links = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
            foreach (var link in links)
            {
                var attribute = link.GetAttributeValue("href", "");
                var match = downloadRegex.Match(attribute);

                if (match.Success)
                {
                    string version = match.Groups[1].Value;
                    result.Add(version);
                }
            }

            return result;
        }

        public async Task<FileInfo> Download(string version)
        {
            try
            {
                await downloadLock.WaitAsync();

                var cache = new DirectoryInfo(FactorioServerData.UpdateCacheDirectoryPath);
                if (!cache.Exists)
                {
                    cache.Create();
                }

                if (version != "latest")
                {
                    var file = GetCachedFile(version);
                    if (file != null)
                    {
                        return file;
                    }
                }

                var client = _httpClientFactory.CreateClient();
                string url = $"https://factorio.com/get-download/{version}/headless/linux64";
                var download = await client.GetAsync(url);
                if (!download.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Update failed: Error downloading {url}", url);
                    return null;
                }

                var fileName = download.Content.Headers.ContentDisposition.FileName;

                var binariesPath = Path.Combine(FactorioServerData.UpdateCacheDirectoryPath, fileName);
                var binaries = new FileInfo(binariesPath);

                using (var fs = binaries.Open(FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await download.Content.CopyToAsync(fs);
                }

                return binaries;
            }
            finally
            {
                downloadLock.Release();
            }
        }

        public async Task<Result> DoUpdate(FactorioServerData serverData, string version)
        {
            try
            {
                var binaries = await Download(version);

                if (binaries == null)
                {
                    _logger.LogWarning("Update failed: Error downloading file {version}", version);
                    return Result.Failure(Constants.UpdateErrorKey, "Error downloading file.");
                }

                string basePath = serverData.BaseDirectoryPath;
                var extractDirectoryPath = Path.Combine(basePath, "factorio");
                var binDirectoryPath = Path.Combine(basePath, "bin");
                var dataDirectoryPath = Path.Combine(basePath, "data");

                var extractDirectory = new DirectoryInfo(extractDirectoryPath);
                if (extractDirectory.Exists)
                {
                    extractDirectory.Delete(true);
                }

                bool success = ExecuteProcess("/bin/tar", $"-xJf {binaries.FullName} -C {basePath}");

                var binDirectory = new DirectoryInfo(binDirectoryPath);
                if (binDirectory.Exists)
                {
                    binDirectory.Delete(true);
                }
                var dataDirectory = new DirectoryInfo(dataDirectoryPath);
                if (dataDirectory.Exists)
                {
                    dataDirectory.Delete(true);
                }

                if (success)
                {
                    Directory.Move(Path.Combine(extractDirectoryPath, "bin"), binDirectoryPath);
                    Directory.Move(Path.Combine(extractDirectoryPath, "data"), dataDirectoryPath);

                    var configFile = new FileInfo(Path.Combine(basePath, "config-path.cfg"));
                    if (!configFile.Exists)
                    {
                        var extractConfigFile = new FileInfo(Path.Combine(extractDirectoryPath, "config-path.cfg"));
                        if (extractConfigFile.Exists)
                        {
                            extractConfigFile.MoveTo(configFile.FullName);
                        }
                    }
                }

                extractDirectory.Refresh();
                if (extractDirectory.Exists)
                {
                    extractDirectory.Delete(true);
                }

                if (success)
                {
                    return Result.OK;
                }
                else
                {
                    return Result.Failure("UpdateErrorKey", "Error extracting file.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(DoUpdate));
                return Result.Failure(Constants.UnexpctedErrorKey, "Unexpected error installing.");
            }
        }
    }
}
