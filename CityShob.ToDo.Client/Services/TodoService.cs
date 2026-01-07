using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using CityShob.ToDo.Contract.DTOs;
using Microsoft.Extensions.Logging;

namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Implementation of the ITodoService that communicates with the backend via Web API (for CRUD)
    /// and SignalR (for real-time events).
    /// </summary>
    public class TodoService : ITodoService
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TodoService> _logger;

        private HubConnection _hubConnection;
        private IHubProxy _hubProxy;

        public string MyConnectionId => _hubConnection?.ConnectionId;

        public event Action<int, string> TaskLocked;
        public event Action<int> TaskUnlocked;
        public event Action<TodoItemDto> ItemCreated;
        public event Action<TodoItemDto> ItemUpdated;
        public event Action<int> ItemDeleted;
        public event Action<bool> ConnectionStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used for REST API communications.</param>
        /// <param name="baseUrl">The base URL of the API/SignalR server.</param>
        /// <param name="logger">The logger instance.</param>
        public TodoService(HttpClient httpClient, string baseUrl, ILogger<TodoService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task ConnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State != ConnectionState.Disconnected)
            {
                return;
            }

            InitHubConnection();
            InitHubProxy();

            try
            {
                await _hubConnection.Start();
                _logger.LogInformation("SignalR Connection Started. ID: {ConnectionId}", _hubConnection.ConnectionId);
                ConnectionStateChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SignalR hub at {BaseUrl}", _baseUrl);
                ConnectionStateChanged?.Invoke(false);
            }
        }

        private void InitHubConnection()
        {
            var queryString = new Dictionary<string, string>
            {
                { "machineName", Environment.MachineName }
            };

            _hubConnection = new HubConnection(_baseUrl, queryString);

            _hubConnection.Closed += () =>
            {
                _logger.LogWarning("SignalR Connection Closed");
                ConnectionStateChanged?.Invoke(false);
            };

            _hubConnection.Reconnecting += () =>
            {
                _logger.LogWarning("SignalR Reconnecting...");
                ConnectionStateChanged?.Invoke(false);
            };

            _hubConnection.Reconnected += () =>
            {
                _logger.LogInformation("SignalR Reconnected");
                ConnectionStateChanged?.Invoke(true);
            };

            _hubConnection.Error += (ex) =>
            {
                _logger.LogError(ex, "SignalR Connection Error");
            };
        }

        private void InitHubProxy()
        {
            _hubProxy = _hubConnection.CreateHubProxy(SignalRConstants.HubName);

            _hubProxy.On<TodoItemDto>(SignalRConstants.TaskCreated, item => ItemCreated?.Invoke(item));
            _hubProxy.On<TodoItemDto>(SignalRConstants.TaskUpdated, item => ItemUpdated?.Invoke(item));
            _hubProxy.On<int>(SignalRConstants.TaskDeleted, id => ItemDeleted?.Invoke(id));
            _hubProxy.On<int, string>(SignalRConstants.TaskLocked, (id, lockedBy) => TaskLocked?.Invoke(id, lockedBy));
            _hubProxy.On<int>(SignalRConstants.TaskUnlocked, (id) => TaskUnlocked?.Invoke(id));
        }

        public async Task<List<TodoItemDto>> GetAllAsync(string tagFilter = null)
        {
            try
            {
                string url = "api/todo";
                if (!string.IsNullOrWhiteSpace(tagFilter))
                {
                    url += $"?tag={Uri.EscapeDataString(tagFilter)}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<TodoItemDto>>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Todo items.");
                throw;
            }
        }

        public async Task AddAsync(TodoItemDto item)
        {
            try
            {
                var json = JsonConvert.SerializeObject(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/todo", content);
                response.EnsureSuccessStatusCode();

                // Deserialize the response to get the server-generated ID
                var responseString = await response.Content.ReadAsStringAsync();
                var createdItem = JsonConvert.DeserializeObject<TodoItemDto>(responseString);

                // Update the local object's ID by reference
                if (createdItem != null)
                {
                    item.Id = createdItem.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add Todo item.");
                throw;
            }
        }

        public async Task UpdateAsync(TodoItemDto item)
        {
            try
            {
                var json = JsonConvert.SerializeObject(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/todo/{item.Id}", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Todo item {Id}.", item?.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/todo/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Todo item {Id}.", id);
                throw;
            }
        }

        public async Task LockTaskAsync(int id)
        {
            try
            {
                if (_hubProxy == null) throw new InvalidOperationException("HubConnection is not initialized.");
                await _hubProxy.Invoke(SignalRConstants.LockTask, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to lock task {Id} via SignalR.", id);
                throw;
            }
        }

        public async Task UnlockTaskAsync(int id)
        {
            try
            {
                if (_hubProxy == null) throw new InvalidOperationException("HubConnection is not initialized.");
                await _hubProxy.Invoke(SignalRConstants.UnlockTask, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unlock task {Id} via SignalR.", id);
                throw;
            }
        }
    }
}