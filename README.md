# Real-Time Data Synchronization To-Do System

This repository contains a full-stack To-Do application built with the .NET Framework 4.8 ecosystem. The solution is designed to demonstrate high-performance, real-time data synchronization across multiple WPF desktop clients using a centralized ASP.NET Web API and SignalR.

---

## Communication Protocols and Reasoning

The system utilizes a hybrid communication architecture to balance authoritative state management with low-latency updates:

### RESTful API (HTTPS/JSON)
Standard CRUD operations (Create, Read, Update, Delete) are handled via a RESTful Web API.
* **Reasoning**: HTTP is the industry standard for persistent data operations. It provides a stateless, reliable request-response cycle with clear status codes, ensuring that data persistence in the SQL database is confirmed before the UI reflects a permanent change.

### SignalR (WebSockets)
Real-Time broadcasting and concurrency control are handled via SignalR.
* **Reasoning**: SignalR provides a full-duplex persistent connection, allowing the server to push updates to clients instantly.

---

## Design Patterns (Examples)

### 1. Distributed Pub/Sub (Cross-Process)
The server acts as a message broker for the entire ecosystem.
* **Implementation**: When a change is persisted to the database, the server "Publishes" an event via the SignalR Hub. All connected WPF clients are "Subscribers" to these specific events.

### 2. Internal Observer Pattern (Client-Side)
While SignalR handles network-level notifications, the client uses the Observer pattern for in-memory UI updates.
* **Implementation**: ViewModels implement `INotifyPropertyChanged`, and collections use `ObservableCollection<T>`. The WPF View acts as the Observer, automatically re-rendering components when the underlying Subject changes due to network events or user input.

### 3. Repository Pattern
All database interactions are abstracted through the `SqlTodoRepository`.
* **Reasoning**: This encapsulates Entity Framework logic, preventing database-specific code from leaking into the Web API controllers and facilitating easier unit testing through mocking.

### 4. Singleton Pattern & Dependency Injection
Critical services like `TodoService` (Client) and `ITodoBroadcaster` (Server) are registered as Singletons via Dependency Injection.
* **Concurrency Reasoning**: This prevents "Split Brain" scenarios where different parts of the application might be listening to different instances of a service. By enforcing a single instance:
    * **Event Consistency**: We guarantee that a SignalR event received from the network is broadcast to *all* active ViewModels simultaneously.
    * **Resource Management**: It ensures only one physical socket connection is maintained per client, regardless of how many views are open.

### 5. Command Pattern
User interactions are handled via `RelayCommand`.
* **Reasoning**: This removes logic from the View's code-behind and allows UI actions to be tested as discrete units of logic within the ViewModels.

### 6. Adapter Pattern
The `WpfDispatcherService` adapts the framework-specific WPF threading model to a generic interface (`IDispatcherService`).
* **Reasoning**: This decouples the ViewModels from WPF libraries (`System.Windows`), allowing unit tests to run in a headless environment by injecting a mock adapter that runs synchronously.

### 7. Proxy Pattern
The client communicates with the server via the SignalR `IHubProxy`.
* **Reasoning**: The proxy acts as a local representative for the remote `TodoHub`. The client calls methods on this local object, and the proxy handles the complex serialization and network transmission to execute the logic on the server.

---

## Features and API Documentation

### API Exploration with Swagger
The server includes **Swagger (Swashbuckle)** integration to provide a high-level technical interface for the REST API.
* **Usage**: Developers can navigate to `/swagger` on the server URL to view all available endpoints, schemas, and perform manual testing of the CRUD operations.

### Real-Time Data Synchronization
When a user adds or deletes a task, the change is propagated to all other clients instantly. The client uses a `WpfDispatcherService` to ensure that updates received on the background SignalR thread are safely marshaled back to the UI thread.

### Concurrency Control: Edit Locking
To prevent simultaneous edits, the app implements row-level locking.
* **Mechanism**: When a user focuses a task, a lock signal is sent. Other clients receive a `taskLocked` signal and use an `IsLockedByOtherConverter` to visually dim and disable that specific task in their UI.

---

## Testing Infrastructure

The solution includes a test suite using xUnit, Moq, and FluentAssertions:
* **ViewModel Testing**: Ensures UI logic functions correctly when services are mocked.
* **Logic Validation**: Tests verify that updating a task property triggers the correct `UpdateAsync` call.
* **Offline Recovery**: Validates that the application gracefully handles server connection failures.

**Note on Coverage:** The current test suite is intended to demonstrate the testability and architectural integrity of the application rather than providing 100% line coverage. It focuses on high-value logic where threading, synchronization, and state management are most critical.

---

## Features and Usage

### Creating a New Task
The creation bar is located at the bottom of the main window.
* **Title**: Enter the task description in the main text field.
* **Tags**: Enter categories (e.g., "Work, Urgent") in the tag field; separate multiple tags with commas.
* **Options**:
    * Click the **Flag** icon to set the Priority (Low, Medium, High).
    * Click the **Calendar** icon to select a Due Date.
* **Submit**: Press `Enter` or click the **Arrow Button** to add the task.

### Editing Tasks
* **Edit Text**: Click directly on any task title to edit it. Changes are **auto-saved** immediately when you click away (lose focus).
* **Mark Complete**: Click the checkbox on the left side of a row to toggle its completion status.
* **Update Details**: Click the Flag or Calendar icons within any row to instantly change the Priority or Due Date.

### Deleting a Task
* **Hover to Reveal**: To keep the interface clean, the delete button is hidden by default. **Hover your mouse** over a specific task row to reveal the red **"X"** button on the far right, then click it to remove the item.

### Real-Time Locking
* **Visual Lock**: If another user selects a task to edit, it will instantly appear **dimmed (grayed out)** on your screen. This indicates the task is locked to prevent conflicting edits.

---

## Setup Instructions

### 1. Database Configuration
1. Open the solution in Visual Studio.
2. Navigate to `Web.config` in the `CityShob.ToDo.Server` project.
3. Update the `connectionString` to point to your local MS SQL Server instance (if needed).
* **Note on Database**: You do not need to run Update-Database manually; the system is configured for a seamless "First Run" experience.

### 2. Running the Server
1. Set `CityShob.ToDo.Server` as the Startup Project.
2. Rebuild Solution (Build->Rebuild Solution)
3. Press F5 to launch the Server.
4. (Optional) Visit `/swagger` to verify the API is running.

### Troubleshooting

### "Could not find a part of the path ... roslyn\csc.exe"

If you see a `DirectoryNotFoundException` related to **roslyn\csc.exe** when starting the Server, it means Visual Studio failed to copy the compiler binaries to the output folder.

**To fix this immediately:**

1. In Visual Studio, go to **Tools** > **NuGet Package Manager** > **Package Manager Console**.
2. In the console window, ensure **Default project** (dropdown) is set to `CityShob.ToDo.Server`.
3. Paste and run the following command:

```powershell
Update-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -Reinstall
```
 4.Rebuild the CityShob.ToDo.Server project. The application will now start correctly.

 *This error is a notorious "ghost" in .NET 4.8 Web API projects because the compiler binaries sometimes don't copy correctly during a fresh clone

### 3. Running the Client
1. Ensure the server URL in the client's configuration matches your local port (`CityShob.ToDo.Client.App.config`.ServerUrl).
2. Launch the `CityShob.ToDo.Client` project.
3. To observe real-time sync, launch multiple instances of the application executable from the `/bin/Debug` folder.

## Developer Tools & Stress Testing

The application includes a hidden "Developer Backdoor" to simulate high-throughput scenarios and test concurrency handling without manual data entry.

### How to Run the Stress Test
1. Launch the Client and ensure it is **Online** (Green Wi-Fi icon).
2. **Right-Click** directly on the **Wi-Fi / Connection Status** badge in the top-right corner.
3. The system will immediately trigger the simulation.

### What the Test Does
The simulation executes a burst of asynchronous operations on background threads to test the robustness of the Server, Database Locking, and SignalR broadcasts.

For **each of the 50 simulated users**, the client performs the following sequence in parallel:
1. **Create**: POSTs a new task with unique data.
2. **Update**: Immediately modifies the task (flagging it as completed) to test optimistic concurrency.
3. **Delete**: Removes the task to clean up the database.

### Verifying Results
* **Visual**: You will see the UI flood with new tasks, see them turn "Completed" (strike-through), and then vanish.
* **Logs**: Check the log file (C:\Users\USERNAME\AppData\Local\CityShob.ToDo\logs) for the "Stress Test Summary," which provides performance metrics:
  ```text
  --- STRESS TEST COMPLETE ---
  Total Time: 2.50s
  Success: 50/50
  Throughput: 60.00 ops/sec```