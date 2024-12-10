using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsInput;
using System.Collections.Generic;
using System.Diagnostics; // For opening the browser

class Program
{
    private const int WM_HOTKEY = 0x0312; // Hotkey message ID
    private const int MOD_ALT = 0x0001; // ALT modifier
    private const int MOD_CONTROL = 0x0002; // CTRL modifier
    private const int MOD_SHIFT = 0x0004; // SHIFT modifier
    private const int MOD_NOREPEAT = 0x4000; // Disable hotkey repeat

    private static InputSimulator _simulator = new InputSimulator();
    private static List<string> _messageLog = new List<string>();
    private static readonly int _maxMessages = 5; // Max messages to store in the log

    // Define the delay ranges
    private static int _minDelay = 20; // Default to short delay
    private static int _maxDelay = 30;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    public static void Main()
    {
        // Print header
        PrintHeader();

        // Register hotkeys for ALT+1 to ALT+0, CTRL+SHIFT+U (update), CTRL+SHIFT+L (long delay), CTRL+SHIFT+S (short delay)
        for (int i = 1; i <= 10; i++)
        {
            RegisterHotKey(IntPtr.Zero, i, MOD_ALT | MOD_NOREPEAT, (int)(ConsoleKey.D0 + (i % 10))); // ALT+1 to ALT+0
        }
        RegisterHotKey(IntPtr.Zero, 11, MOD_CONTROL | MOD_SHIFT, (int)ConsoleKey.U); // CTRL+SHIFT+U
        RegisterHotKey(IntPtr.Zero, 12, MOD_CONTROL | MOD_SHIFT, (int)ConsoleKey.L); // CTRL+SHIFT+L
        RegisterHotKey(IntPtr.Zero, 13, MOD_CONTROL | MOD_SHIFT, (int)ConsoleKey.S); // CTRL+SHIFT+S

        // Listen for hotkeys
        ListenForHotkeys();
    }

    private static void PrintHeader()
    {
        // Clear console and print the ASCII art header
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("░░░░░░░░░░░░░░░░░░░░ ░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░░░░");
        Console.WriteLine("░░░░░░░░░  ░░░░░░░░░      ░░░░      ░░░        ░░░░░░");
        Console.WriteLine("▒▒▒▒▒▒▒▒▒  ▒▒▒▒▒▒▒▒  ▒▒▒▒  ▒▒  ▒▒▒▒  ▒▒▒▒▒  ▒▒▒▒▒▒▒▒▒");
        Console.WriteLine("▓▓▓▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓  ▓▓▓▓▓▓▓▓  ▓▓▓▓  ▓▓▓▓▓  ▓▓▓▓▓▓▓▓▓");
        Console.WriteLine("█████████  ████████  ████  ██        █████  █████████");
        Console.WriteLine("█████████        ███      ███  ████  █████  █████████");
        Console.Write("                      LameCode's Auto Typer Lite ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("v0.1");
        Console.ResetColor();
        Console.WriteLine(new string('-', 53)); // Divider
        Console.Write("Application is now listening for a hotkey.\nTo check for updates, press "); // Divider between header and log
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write(" CTRL "); // Divider between header and log
        Console.ResetColor();
        Console.Write(" + ");
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write(" SHIFT ");
        Console.ResetColor();
        Console.Write(" + ");
        Console.BackgroundColor = ConsoleColor.Cyan;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write(" U ");
        Console.ResetColor();
        Console.WriteLine(".");
        Console.WriteLine(new string('-', 53)); // Divider
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Press hotkeys to Auto Type your message anywhere.");
        Console.ResetColor();
        Console.WriteLine("Hotkeys:");

        // Print hotkeys in two columns
        const int firstColumnWidth = 40; // Adjust width for the first column
        const int secondColumnOffset = 25; // Adjust offset for second column to align properly

        for (int i = 1; i <= 5; i++)
        {
            Console.Write($"Macro {i} is [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"ALT");
            Console.ResetColor();
            Console.Write(" + ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{i % 10}");
            Console.ResetColor();
            Console.WriteLine("]");

            // Add space to create the second column
            Console.SetCursorPosition(secondColumnOffset, Console.CursorTop - 1);

            Console.Write($"Macro {i + 5} is [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"ALT");
            Console.ResetColor();
            Console.Write(" + ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{(i + 5) % 10}");
            Console.ResetColor();
            Console.WriteLine("]");
        }

        // Print the current delay at the end of the intro
        Console.WriteLine(new string('-', 53)); // Divider
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Current delay: {_minDelay} - {_maxDelay} ms");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("If your system can't keep up with the typing speed,");
        Console.WriteLine("press CTRL+SHIT+L for long delays (50 to 70 ms).");
        Console.WriteLine("Press CTRL+SHIFT+S to revert to short (20 to 30 ms).");
        Console.ResetColor();
        Console.WriteLine(new string('-', 53)); // Divider
    }

    private static void ListenForHotkeys()
    {
        MSG msg;

        while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
        {
            if (msg.message == WM_HOTKEY)
            {
                int id = (int)msg.wParam;

                if (id == 11) // CTRL+SHIFT+U
                {
                    OpenUpdateWebsite();
                }
                else if (id == 12) // CTRL+SHIFT+L (Long delay)
                {
                    SetLongDelay();
                }
                else if (id == 13) // CTRL+SHIFT+S (Short delay)
                {
                    SetShortDelay();
                }
                else
                {
                    string fileName = $"{id:D2}.txt"; // Format as 01.txt, 02.txt, ..., 10.txt
                    TypeTextFromFile(fileName);
                }
            }
        }

        // Unregister hotkeys when exiting
        for (int i = 1; i <= 10; i++)
        {
            UnregisterHotKey(IntPtr.Zero, i);
        }
        UnregisterHotKey(IntPtr.Zero, 11); // Unregister CTRL+SHIFT+U
        UnregisterHotKey(IntPtr.Zero, 12); // Unregister CTRL+SHIFT+L
        UnregisterHotKey(IntPtr.Zero, 13); // Unregister CTRL+SHIFT+S
    }

    private static void SetLongDelay()
    {
        _minDelay = 50;
        _maxDelay = 70;
        // Do not log the delay change message anymore
        // Just reprint the header with updated delay
        LogMessage(""); // Empty message just to refresh the header with updated delay
    }

    private static void SetShortDelay()
    {
        _minDelay = 20;
        _maxDelay = 30;
        // Do not log the delay change message anymore
        // Just reprint the header with updated delay
        LogMessage(""); // Empty message just to refresh the header with updated delay
    }


    private static void OpenUpdateWebsite()
    {
        string url = "http://lamecode.eu/aplikace/lcat"; // Replace with your actual update URL
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            LogMessage("Opening update website...");
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to open website: {ex.Message}");
        }
    }

    private static void LogMessage(string message)
    {
        // Clear the console to reprint the intro with updated delay
        Console.Clear();

        // Reprint the header with updated delay information
        PrintHeader();

        // Only keep the latest message
        _messageLog.Clear(); // Clear previous log entries
        if (!string.IsNullOrEmpty(message))
        {
            _messageLog.Add(message); // Add the new message
        }

        // Define where the log will start, after the intro section
        const int logStartLine = 20; // Line after the intro section (adjust if needed)

        // Move the cursor to the start of the log section
        Console.SetCursorPosition(0, logStartLine);

        // Print the last log message (only the most recent message)
        if (_messageLog.Count > 0)
        {
            Console.WriteLine(_messageLog[0]);
        }
        else
        {
            // If there is no message, print an empty line
            Console.WriteLine("");
        }
    }



    private static void TypeTextFromFile(string fileName)
    {
        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        string macrosDirectory = Path.Combine(exePath, "Your Macros");  // Path to the 'Your Macros' folder
        string filePath = Path.Combine(macrosDirectory, fileName);  // Combine the directory with the filename

        if (File.Exists(filePath))
        {
            Console.Clear();
            PrintHeader();

            string text = File.ReadAllText(filePath);

            // Log the content that's going to be typed, with colors for both file name and content
            Console.ForegroundColor = ConsoleColor.Yellow; // File name in yellow
            string logMessage = $"Now typing the content of file {fileName}: ";
            Console.Write(logMessage); // Print the file name in yellow
            Console.ResetColor(); // Reset to default for the content

            // Print the file content in cyan
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
            Console.ResetColor(); // Reset color after content

            // Introduce a 250ms delay before starting to type
            Thread.Sleep(250);

            // Type each character with delay
            foreach (char c in text)
            {
                _simulator.Keyboard.TextEntry(c);
                Thread.Sleep(new Random().Next(_minDelay, _maxDelay)); // Use current delay range
            }
        }
        else
        {
            string message = $"File not found: {fileName}";
            LogMessage(message); // Log and print the message if file doesn't exist
        }
    }


    // Structure for handling messages
    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }
}
