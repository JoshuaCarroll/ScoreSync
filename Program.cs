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

class Program
{
    static Dictionary<Regex, Action<Match, ScoreboardOCRData>> regexHandlers;
    static string EndPointUrl;
    static ScoreboardOCRData scoreboardData = new ScoreboardOCRData();
    static string LastSentJson = "";

    static async Task Main(string[] args)
    {
        // Check if the correct number of command line arguments is passed
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: ScoreSync <COM port> <serverAddress> <serverPort>");
            return;
        }

        string portName = args[0];
        string EndPointUrl = args[1];
        int EndPointPort;

        if (!int.TryParse(args[2], out EndPointPort))
        {
            Console.WriteLine("Server port must be numeric.");
            return;
        }

        InitializeRegexHandlers();
        
        int baudRate = 9600;
        Parity parity = Parity.None;
        int dataBits = 8;
        StopBits stopBits = StopBits.One;

        // Check if the specified COM port is available
        if (!Array.Exists(SerialPort.GetPortNames(), port => port.Equals(portName, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"COM port {portName} is not available.");
            return;
        }

        using (SerialPort serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits))
        {
            serialPort.Open();
            Console.WriteLine("Waiting for data...");

            while (true)
            {
                string data = ReadData(serialPort);
                if (!string.IsNullOrEmpty(data))
                {
                    if (TransformToScoreboardData(data))
                    {
                        string jsonData = scoreboardData.ToJson();
                        await SendJsonData(EndPointUrl, EndPointPort, jsonData);
                    }
                    else
                    {
                        Console.WriteLine($"|{data}|");
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
                    data.ScoreHome = match.Groups[2].Value;
                    data.ScoreAway = match.Groups[5].Value;
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
                    data.GameClock = $"{match.Groups[1].Value.Substring(0, 2)}:{match.Groups[1].Value.Substring(2, 2)}.{match.Groups[1].Value.Substring(4, 1)}";
                    data.Period = match.Groups[2].Value;
                    data.ShotClock = match.Groups[3].Value;
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
        if (jsonData != LastSentJson)
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
