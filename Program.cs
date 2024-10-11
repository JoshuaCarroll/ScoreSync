using System;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using ScoreSync;
using System.Net.Sockets;
using Spectre.Console;
using Spectre.Console.Json;
using System.Xml.Linq;
using System.Security.AccessControl;
using System.Diagnostics;

class Program
{
    static Dictionary<Regex, Action<Match, ScoreboardOCRData>> regexHandlers;
    static string EndPointUrl = "";
    static int EndPointPort;
    static string ComPortName = "";
    static ScoreboardOCRData scoreboardData = new ScoreboardOCRData();
    static string LastSentJson = "";

    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: ScoreSync <COM port> <serverAddress> <serverPort>\r\n");

            ComPortName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which COM port is the ESPN box?")
                    .PageSize(10)
                    .AddChoices(SerialPort.GetPortNames()));

            EndPointUrl = AnsiConsole.Prompt(new TextPrompt<string>("What is the address of the scorebug generator?"));

            EndPointPort = AnsiConsole.Prompt(new TextPrompt<int>("What is the server port?"));
        }
        else
        {
            ComPortName = args[0];
            EndPointUrl = args[1];

            if (!int.TryParse(args[2], out EndPointPort))
            {
                Console.WriteLine("Server port must be numeric.");
                return;
            }
        }

        InitializeRegexHandlers();
        
        int baudRate = 9600;
        Parity parity = Parity.None;
        int dataBits = 8;
        StopBits stopBits = StopBits.One;

        // Check if the specified COM port is available
        if (!Array.Exists(SerialPort.GetPortNames(), port => port.Equals(ComPortName, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"COM port {ComPortName} is not available.");
            return;
        }

        using (SerialPort serialPort = new SerialPort(ComPortName, baudRate, parity, dataBits, stopBits))
        {
            serialPort.Open();
            Console.Clear();

            AnsiConsole.WriteLine("");

            while (true)
            {
                string data = ReadData(serialPort);
                if (!string.IsNullOrEmpty(data))
                {
                    if (TransformToScoreboardData(data))
                    {
                        Console.Clear();

                        AnsiConsole.WriteLine($"Period: {scoreboardData.Period}  Clock: {scoreboardData.GameClock}  Home: {scoreboardData.ScoreHome}  Away: {scoreboardData.ScoreAway}");

                        string jsonData = scoreboardData.ToJson();
                        await SendJsonData(EndPointUrl, EndPointPort, jsonData);

                        AnsiConsole.WriteLine(jsonData);
 
                    }
                    else
                    {
                        Debug.WriteLine($"Unrecognized: {data}");
                    }
                }
            }


        }
    }

    static void InitializeRegexHandlers()
    {
        regexHandlers = new Dictionary<Regex, Action<Match, ScoreboardOCRData>>
        {
            {
                new Regex(@"F(.{10})(.{2})(.{1})(.{10})(.{2})(.{1})(.{1})(.{2})(.{2})(.{1})"),
                (match, data) =>
                {
                    data.ScoreHome = match.Groups[2].Value.Trim();
                    data.ScoreAway = match.Groups[5].Value.Trim();
                    data.TimeoutsHome = match.Groups[3].Value;
                    data.TimeoutsAway = match.Groups[6].Value;
                    data.Downs = match.Groups[7].Value;
                    data.Yards = match.Groups[8].Value;
                    data.LOS = match.Groups[9].Value;
                    data.Possession = match.Groups[10].Value;
                }
            },
            {
                new Regex(@"C(.{5})(.{1})(.{2})"),
                (match, data) =>
                {
                    data.GameClock = $"{match.Groups[1].Value.Substring(0, 2)}:{match.Groups[1].Value.Substring(2, 2)}";//.{match.Groups[1].Value.Substring(4, 1)}";
                    data.Period = match.Groups[2].Value;
                    data.ShotClock = match.Groups[3].Value.Trim();
                }
            }
        };
    }

    static string ReadData(SerialPort serialPort)
    {
        StringBuilder dataBuffer = new StringBuilder();
        bool inFrame = false;

        while (true)
        {
            try
            {
                int byteRead = serialPort.ReadByte();

                if (byteRead == 0x02) // STX character
                {
                    inFrame = true;
                    dataBuffer.Clear();
                }
                else if (byteRead == 0x03) // ETX character
                {
                    inFrame = false;
                    break;
                }
                else if (inFrame)
                {
                    dataBuffer.Append((char)byteRead);
                }
            }
            catch (OperationCanceledException ex)
            {
                throw ex;
            }
        }

        return dataBuffer.ToString();
    }

    static bool TransformToScoreboardData(string data)
    {
        foreach (var regexHandler in regexHandlers)
        {
            Match match = regexHandler.Key.Match(data);
            if (match.Success)
            {
                regexHandler.Value(match, scoreboardData);
                return true;
            }
        }

        return false;
    }

    static async Task SendJsonData(string serverAddress, int port, string jsonData)
    {
        if (serverAddress.ToLower() != "none" && jsonData != LastSentJson)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverAddress, port))
                using (NetworkStream networkStream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes(jsonData);
                    await networkStream.WriteAsync(data, 0, data.Length);
                }
                Console.Write(".");
                LastSentJson = jsonData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send data over TCP to {serverAddress}:{port.ToString()}");
                Console.WriteLine($"Exception: {ex.Message}\r\n{ex.StackTrace}");
                Console.WriteLine(jsonData);
            }
        }
    }
}
