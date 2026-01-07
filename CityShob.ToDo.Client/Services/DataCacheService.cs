using CityShob.ToDo.Contract.DTOs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Service responsible for persisting Todo items to local storage (AppData)
    /// to support offline capabilities and state persistence.
    /// </summary>
    public class DataCacheService : IDataCacheService
    {
        private readonly string _cacheFilePath;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly ILogger<DataCacheService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCacheService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public DataCacheService(ILogger<DataCacheService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CityShob.ToDo");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                _cacheFilePath = Path.Combine(folder, "cache.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize local cache directory.");
                // If we can't create the directory, _cacheFilePath might be invalid,
                // but subsequent methods handle exceptions gracefully.
            }
        }

        /// <summary>
        /// Asynchronously saves the list of Todo items to the local cache file.
        /// </summary>
        /// <param name="items">The list of items to save.</param>
        public async Task SaveAsync(List<TodoItemDto> items)
        {
            if (string.IsNullOrEmpty(_cacheFilePath)) return;

            await _fileLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(items);
                using (var writer = new StreamWriter(_cacheFilePath))
                {
                    await writer.WriteAsync(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to local cache file at {Path}", _cacheFilePath);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Asynchronously loads the list of Todo items from the local cache file.
        /// </summary>
        /// <returns>The list of items, or null if the cache does not exist or fails to load.</returns>
        public async Task<List<TodoItemDto>> LoadAsync()
        {
            if (string.IsNullOrEmpty(_cacheFilePath)) return null;

            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_cacheFilePath))
                {
                    _logger.LogInformation("No local cache file found.");
                    return null;
                }

                using (var reader = new StreamReader(_cacheFilePath))
                {
                    var json = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<List<TodoItemDto>>(json);
                }
            }
            catch (JsonException jEx)
            {
                _logger.LogError(jEx, "Local cache file is corrupted.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read from local cache file.");
                return null;
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}