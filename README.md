DevAssist GUI



DevAssist is a lightweight developer tool that combines a command-based interface with an AI assistant to help automate development workflows and explain errors in real time.



\---



&#x20;Features



\-  Command-based interface (CLI inside GUI)

\-  Create projects ("new api", "new console")

\-  Build projects ("build MyApp")

\-  Git automation (commit, push)

\-  AI-powered error explanation (local Mistral model)

\-  Memory-aware AI assistant

\-  Smart command suggestions

\-  Built-in help system



\---



&#x20;How It Works



User enters a command:



build MyApp



↓



System runs the command



↓



If error occurs → AI explains it automatically



\---



&#x20;Tech Stack



\- C# (.NET)

\- WPF (Windows Presentation Foundation)

\- Local AI (Mistral 7B via Ollama)

\- Process execution (dotnet, git)



\---



Example Commands



build MyApp

new api MyApp

git MyApp commit initial commit

ask what is dependency injection



\---



&#x20;AI Integration



This project uses a local AI model (Mistral 7B) to:



\- Explain build errors

\- Suggest fixes

\- Answer development questions



No API key required.



\---

&#x20;Why This Project?



This project demonstrates:



\- Real-world tool development

\- Clean architecture (UI + Core separation)

\- Process automation

\- AI integration into developer workflows



\---





