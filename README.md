# MyObsidianMcp

A practical Model Context Protocol (MCP) server implementation in C# that provides tools for interacting with Obsidian vaults. This project demonstrates how to build MCP servers using .NET and was created as a companion to the blog post [Make Copilot Work Your Way: Building MCP Servers in C#](https://blog.nyveldt.com/post/building-mcp-servers-in-csharp).

## Overview

MCP servers are powerful tools that give GitHub Copilot access to your systems, data, and workflows. MyObsidianMcp exposes your Obsidian vault through a standard MCP interface, allowing Copilot to search notes, read content, manage daily notes, create new notes, and track tasks—all without leaving your editor.

The beauty of building your own MCP server is that you're not limited to what someone else thought would be useful. You can make Copilot work the way you want to work. This project solves a real problem: keeping thousands of notes in Obsidian but being able to search and access them without context-switching away from your coding environment.

## Features

### Core Tools

- **SearchNotes** - Searches all markdown files in your vault for content matching a query
- **ReadNote** - Reads the full content of a specific note by file path
- **ListRecentNotes** - Lists the most recently modified notes (default: 10)
- **GetDailyNote** - Retrieves a daily note for a specific date ('today', 'yesterday', or 'YYYY-MM-DD')
- **CreateNote** - Creates a new markdown note with optional tags and folder organization
- **MarkTask** - Marks a task with a status (Completed, InProgress, Forwarded, Scheduled, Open)
- **AddTaskToDaily** - Adds a new task to the Short List section of a daily note

## Getting Started

### Prerequisites

- .NET 10.0 or later
- An Obsidian vault directory

### Quick Start

Create a new MCP server project:

```bash
dotnet new console -n MyMCP
dotnet add package ModelContextProtocol --prerelease
dotnet add package Microsoft.Extensions.Hosting
```

### Building This Project

1. Clone this repository
2. Build the project:
   ```bash
   dotnet build
   ```
3. For production use, create a release build:
   ```bash
   dotnet build -c Release
   ```
   The compiled DLL will be in `bin/Release/net10.0/MyObsidianMcp.dll`

### Configuration

Add the MCP server to your `mcp.json` configuration file. The configuration will depend on your setup:

**Option 1: Using the project directly**
```json
{
  "servers": {
    "myobsidianmcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/MyObsidianMcp/MyObsidianMcp.csproj",
        "/path/to/your/obsidian/vault"
      ]
    }
  }
}
```

**Option 2: Using a published DLL (recommended for multiple machines)**
```json
{
  "servers": {
    "myobsidianmcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "/path/to/MyObsidianMcp.dll",
        "/path/to/your/obsidian/vault"
      ]
    }
  }
}
```

Replace `/path/to/your/obsidian/vault` with the actual path to your Obsidian vault directory.

### Enabling the Tools in Copilot

1. Click the Tools icon in VS Code
2. Click the checkbox next to the MyObsidianMcp tool to enable it
3. Start using the tools in your Copilot conversations

## Project Structure

```
MyObsidianMcp/
├── ObsidianService.cs    # Core service with all MCP tools
├── Program.cs            # MCP server setup and initialization
├── MyObsidianMcp.csproj  # Project configuration
└── README.md             # This file
```

## Usage Examples

### Search for notes containing "project"
```
SearchNotes(query: "project")
```

### Get today's daily note
```
GetDailyNote(date: "today")
```

### Create a new note with tags
```
CreateNote(
  title: "New Idea",
  content: "This is an interesting concept...",
  tags: ["brainstorm", "ideas"],
  folder: "projects/personal"
)
```

### Mark a task as completed
```
MarkTask(
  taskText: "Call MetEd",
  status: "Completed",
  filePath: "dailies/2025-12-06.md"
)
```

### Add a task to today's short list
```
AddTaskToDaily(
  taskText: "Review proposal",
  date: "today"
)
```

## Task Status Values

- **Completed** - Task is done (checkbox: `[x]`)
- **InProgress** - Task is being worked on (checkbox: `[/]`)
- **Forwarded** - Task forwarded to someone else (checkbox: `[>]`)
- **Scheduled** - Task scheduled for later (checkbox: `[<]`)
- **Open** - Task not yet started (checkbox: `[ ]`)

## Project Structure

```
MyObsidianMcp/
├── ObsidianService.cs    # Core service with all MCP tools
├── Program.cs            # MCP server setup and initialization
├── MyObsidianMcp.csproj  # Project configuration
├── .gitignore            # Git ignore file
└── README.md             # This file
```

## Building

### Debug build
```bash
dotnet build
```
Output: `bin/Debug/net10.0/MyObsidianMcp.dll`

### Release build
```bash
dotnet build -c Release
```
Output: `bin/Release/net10.0/MyObsidianMcp.dll`

## Cross-Platform Setup

This project is designed to work across multiple machines. Here's the recommended workflow:

1. **Build for your target platform** using the publish command above
2. **Store the published DLL** in a synced location (e.g., your Obsidian vault)
3. **Update your mcp.json** on each machine to point to the appropriate platform's DLL
4. **Configure your Copilot instructions** to guide how you want the MCP server to be used

## Blog Post

This project accompanies the blog post "Personal MCP with C#" which covers:
- Understanding the Model Context Protocol
- Why you'd want to build your own MCP server
- Building MCP servers in C# step-by-step
- Integrating with GitHub Copilot
- Cross-platform deployment strategies
- Best practices for tool design and error handling

The blog post walks through the journey from a simple "GetGreeting" tool to a fully functional Obsidian integration, demonstrating patterns you can apply to your own projects.

## Author

Created by Al Nyveldt (with GitHub Copilot) as a demonstration of practical MCP server development in C#.
