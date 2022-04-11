using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Solver
{
    class Solver
    {
        public int NumberOfSecurities { get; set; }
        public int NumberOfDays { get; set; }
        public decimal StartCapital { get; set; }
        public Dictionary<string, Security> SecurityMap { get; set; } = new Dictionary<string, Security>();

        public decimal CurrentCapital { get; set; }
        public Dictionary<string, int> SecurityInStock { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SecurityInPortfolio { get; set; } = new Dictionary<string, int>();

        static void Main()
        {
            var solver = new Solver();
            solver.ParseInputFile("data.in");
            solver.InitializeSolverData();
            if (!solver.HandleOutputFile("data.out", out var score, out var errorMessage))
            {
                Console.WriteLine(errorMessage);
                return;
            }

            Console.WriteLine($"Final score: {score}");
        }

        public void ParseInputFile(string inputFile)
        {
            var streamReader = new StreamReader(inputFile);
            var firstLine = streamReader.ReadLine().Trim().Split();
            NumberOfSecurities = Int32.Parse(firstLine[0]);
            NumberOfDays = Int32.Parse(firstLine[1]);
            StartCapital = Convert.ToDecimal(firstLine[2]);

            for (var i = 0; i < NumberOfSecurities; i++)
            {
                var security = new Security();
                var splitLine = streamReader.ReadLine().Trim().Split();
                security.Name = splitLine[0];
                security.StockAvailable = Convert.ToInt32(splitLine[1]);
                security.StockPrices = streamReader.ReadLine().Trim().Split().Select(s => Convert.ToDecimal(s)).ToArray();
                SecurityMap[security.Name] = security;
            }

            streamReader.Close();
        }

        public void InitializeSolverData()
        {
            CurrentCapital = StartCapital;
            foreach (var securityName in SecurityMap.Keys)
            {
                SecurityInStock[securityName] = SecurityMap[securityName].StockAvailable;
                SecurityInPortfolio[securityName] = 0;
            }
        }

        public bool HandleOutputFile(string outputFile, out decimal score, out string errorMessage)
        {
            var currentLine = 0;
            try
            {
                var streamReader = new StreamReader(outputFile);
                var daySections = Convert.ToInt32(streamReader.ReadLine().Trim());
                currentLine++;
                var previousDay = 0;
                for (var i = 0; i < daySections; i++)
                {
                    var metadataLine = streamReader.ReadLine().Trim().Split().Select(s => Convert.ToInt32(s)).ToArray();
                    currentLine++;
                    var currentDay = metadataLine[0];
                    if (currentDay < 0 || currentDay >= NumberOfDays)
                    {
                        score = 0;
                        errorMessage = $"Line {currentLine}: Can't trade on day {currentDay}";
                        return false;
                    }

                    if (currentDay < previousDay)
                    {
                        score = 0;
                        errorMessage = $"Line {currentLine}: Executed trades for day {previousDay} earlier, which is after day {currentDay}";
                        return false;
                    }

                    previousDay = currentDay;
                    var numberOfTrades = metadataLine[1];
                    for (var j = 0; j < numberOfTrades; j++)
                    {
                        var tradeLine = streamReader.ReadLine().Trim().Split();
                        currentLine++;
                        var securityName = tradeLine[0];
                        var action = tradeLine[1];
                        var amount = Convert.ToInt32(tradeLine[2]);
                        if (!ExecuteTrade(currentDay, securityName, action, amount, out errorMessage))
                        {
                            score = 0;
                            errorMessage = $"Line {currentLine}: {errorMessage}";
                            return false;
                        }
                    }
                }
            } catch (FormatException e)
            {
                score = 0;
                errorMessage = $"Line {currentLine + 1}: Output data was invalid ({e.Message})";
                return false;
            }

            errorMessage = string.Empty;
            score = CurrentCapital;
            return true;
        }

        private bool ExecuteTrade(int currentDay, string securityName, string action, int amount, out string errorMessage)
        {
            if (!SecurityMap.ContainsKey(securityName))
            {
                errorMessage = $"Unknown security name: {securityName}";
                return false;
            }

            if (amount <= 0)
            {
                errorMessage = $"Invalid trade amount: {amount}";
                return false;
            }

            switch (action)
            {
                case "BUY":
                    var capitalNeeded = amount * SecurityMap[securityName].StockPrices[currentDay];
                    if (CurrentCapital < capitalNeeded)
                    {
                        errorMessage = $"Not enough capital to buy {amount} of {securityName}";
                        return false;
                    }

                    if (amount > SecurityInStock[securityName])
                    {
                        errorMessage = $"Not enough {securityName} in stock to buy {amount}";
                        return false;
                    }

                    SecurityInStock[securityName] -= amount;
                    SecurityInPortfolio[securityName] += amount;
                    CurrentCapital -= capitalNeeded;
                    errorMessage = string.Empty;
                    return true;
                case "SELL":
                    if (amount > SecurityInPortfolio[securityName])
                    {
                        errorMessage = $"Not enough {securityName} in portfolio to sell {amount}";
                        return false;
                    }

                    var capitalReceived = amount * SecurityMap[securityName].StockPrices[currentDay];
                    SecurityInStock[securityName] += amount;
                    SecurityInPortfolio[securityName] -= amount;
                    CurrentCapital += capitalReceived;
                    errorMessage = string.Empty;
                    return true;
                default:
                    errorMessage = $"Unknown trade action: {action}";
                    return false;
            }
        }
    }

    public class Security
    {
        public string Name { get; set; }
        public int StockAvailable { get; set; }
        public decimal[] StockPrices { get; set; }
    }
}