
using System;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevAssistGU;

 public partial class MainWindow : Window
    {
        CommandHandler handler = new CommandHandler();
        AskTool ai = new AskTool();
    HelpTool help = new HelpTool();
        public MainWindow()
        {
        InitializeComponent();
        InputBox.Text = "user: ";
        InputBox.CaretIndex = InputBox.Text.Length;
        }

        // ================= COMMAND PANEL =================
        private async void RunCommand(object sender, RoutedEventArgs e)
        {
            string input = InputBox.Text.Replace("user: ", "");

            if (string.IsNullOrWhiteSpace(input))
                return;

            OutputBox.AppendText("> " + input + "\n");

            // 🔥 Show loading
            OutputBox.AppendText("⏳ Running...\n");

            // 🔥 Run in background
            string result = await Task.Run(() => handler.Handle(input));

            OutputBox.AppendText(result + "\n\n");

            OutputBox.ScrollToEnd();
            InputBox.Clear();
        }

    private async void ShowHelp(object sender, RoutedEventArgs e)
    {
        String input = "help";
        string result = await Task.Run(() => handler.Handle(input));

        OutputBox.AppendText(result + "\n\n");

        OutputBox.ScrollToEnd();

    }
    
    public async void Clear(object sender, RoutedEventArgs e)
    {
        OutputBox.Clear();
    }
        
    
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
        if (InputBox.CaretIndex <= 6 && (e.Key == Key.Back || e.Key == Key.Delete))
        {
            e.Handled = true;
        }
        
        if(e.Key== Key.Enter)
        {
            RunCommand(null, null);
            e.Handled = false;
        }
        }
  
          private void InputBox_TextChanged(object sender, TextChangedEventArgs e)

     {
        if(!InputBox.Text.StartsWith("user: "))
        {
            InputBox.Text = "user: ";
            InputBox.CaretIndex = InputBox.Text.Length;
        }
    }
}

// ================= COMMAND HANDLER =================
class CommandHandler
{
    static ITool[] tools =
    {
        new BuildTool(),
        new GitTool(),
        new ProjectTool(),
        new AskTool(),

    };

    public string Handle(string input)
    {
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
            return "";

        string command = tokens[0].ToLower();
        string[] args = tokens.Skip(1).ToArray();

        foreach (var tool in tools)
        {
            if (tool.CanHandle(command))
                return tool.Execute(args);
        }

        return "Unknown command\n\n "+ Suggest(input);
    }

    string Suggest(string input)
    {
        input = input.ToLower();

        if (input.StartsWith("git"))
        {
            return "Did you mean \n git <project< commit <message>\n- git <project> push";
        }

        if (input.StartsWith("git"))
        {
            return "Usage: \n- build <project>";
        }

        if (input.StartsWith("git"))
        {
            return "\n- new api <name> \n- new consol <name>";
        }

        return "Click  help button to see all commands";
    }
}
// ================= INTERFACE =================
interface ITool
{
    bool CanHandle(string command);
    string Execute(string[] args);
}

// ================= ASK TOOL =================
public class AskTool : ITool
{
    List<(string role, string content)> memory= new List<(string, string )>();
    public bool CanHandle(string command)
    {
        return command == "ask" || command == "explain" || command == "fix";
    }

    public string Execute(string[] args)
    {
        if (args.Length == 0)
            return "Ask something";

        string question = string.Join(" ", args);
        return GetAnswerSafe(question);
    }

   string GetAnswerSafe(string question)
    {
        try
        {
            return GetAnswer(question).GetAwaiter().GetResult();
        }

        catch(Exception e)
        {
            return $"Error : {e.Message}";
        }
    }
    async Task<string> GetAnswer(string question)
    {
        using var client = new HttpClient();

        string prompt = $@"
You are an expert C# developer.

Explain clearly:
- What is the error
- Why it happened
- How to fix it

{question}
";

        var request = new
        {
            model = "mistral",
            prompt = prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "http://localhost:11434/api/generate",
            content
        );

        var text = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Recieved");
        using var doc = JsonDocument.Parse(text);

        if (doc.RootElement.TryGetProperty("response", out var res))
            return res.GetString();

        return "No response";
    }

    public string GetReply(string message)
    {
       memory.Add(("user", message));
       string response= GetLocalAIWithMemory().Result;
       memory.Add(("assistant", message));

       return response;
    }

async Task<string> GetLocalAIWithMemory()
{
        using var client = new HttpClient();

        string prompt = $@"
You are an expert C# developer.
Answer clearly and briefly.
";

foreach(var msg in memory)
{
    prompt += $"{msg.role} :  {msg.content}\n";
}

        var request = new
        {
            model = "mistral",
            prompt = prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "http://localhost:11434/api/generate",
            content
        );

        var text = await response.Content.ReadAsStringAsync();
        
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("response").GetString();
}
}

// ================= BUILD TOOL =================
public class BuildTool : ITool
{
    public bool CanHandle(string command)
    {
        return command == "build";
    }

    public string Execute(string[] args)
    {
        AskTool ai = new AskTool();

        if (args.Length == 0)
            return "Specify project to build";

        string projectName = args[0];
        string path = Path.Combine(Directory.GetCurrentDirectory(), projectName);

        if (!Directory.Exists(path))
            return $"Project not found: {projectName}";

        string output = new RunClass().Run("dotnet", "build", path);

        if (output.ToLower().Contains("error"))
        {
           string shortError= output.Length>2000 ?output.Substring(0,2000):output;
            string aiResponse = ai.GetReply(shortError);

            return $"❌ Build Failed\n\n{shortError}\n\n🤖 Fix:\n{aiResponse}";
        }

        return $"✅ Build Success\n\n{output}";
    }
}

// ================= GIT TOOL =================
public class GitTool : ITool
{
    public bool CanHandle(string command)
    {
        return command == "git";
    }

    public string Execute(string[] args)
    {
        if (args.Length < 2)
            return "Usage: git <project> <action>";

        string projectName = args[0];
        string action = args[1];
        string path = Path.Combine(Directory.GetCurrentDirectory(), projectName);

        if (!Directory.Exists(path))
            return $"Project not found: {projectName}";

        if (action == "commit")
        {
            string message = string.Join(" ", args.Skip(2));

            string addOutput = new RunClass().Run("git", "add .", path);
            string commitOutput = new RunClass().Run("git", $"commit -m \"{message}\"", path);

            return $"Git Commit:\n{addOutput}\n{commitOutput}";
        }
        else if (action == "push")
        {
            string pushOutput = new RunClass().Run("git", "push", path);
            return $"Git Push:\n{pushOutput}";
        }

        return "Unknown git action";
    }
}

// ================= PROJECT TOOL =================
public class ProjectTool : ITool
{
    public bool CanHandle(string command)
    {
        return command == "new";
    }

    public string Execute(string[] args)
    {
        if (args.Length < 2)
            return "Usage: new <type> <name>";

        string type = args[0];
        string name = args[1];
        string path = Directory.GetCurrentDirectory();

        string output = "";

        if (type == "api")
            output = new RunClass().Run("dotnet", $"new webapi -n {name}", path);
        else if (type == "console")
            output = new RunClass().Run("dotnet", $"new console -n {name}", path);
        else
            return "Unknown project type";

        return $"Project created at: {Path.Combine(path, name)}\n\n{output}";
    }
}

// ================= HELP TOOL =================
public class HelpTool : ITool
{
    public bool CanHandle(string command)
    {
        return command == "help";
    }
public string Execute(string[] args)
    {
        return
@"Available Commands:

build <project>
  → Builds a project
  Example: build MyApp

new <type> <name>
  → Creates a new project
  Example: new api MyApp
           new console TestApp

git <project> commit <message>
  → Commit changes
  Example: git MyApp commit initial commit

git <project> push
  → Push to remote

ask <question>
  → Ask AI for help
  Example: ask what is dependency injection

help
  → Show this help menu
";
    }
}

// ================= RUN CLASS =================
public class RunClass
{
    public string Run(string cmd, string arg, string path)
    {
        var process = new Process();

        process.StartInfo.FileName = cmd;
        process.StartInfo.Arguments = arg;
        process.StartInfo.WorkingDirectory = path;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return output + "\n" + error;
    }
}
