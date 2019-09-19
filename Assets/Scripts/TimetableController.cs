using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 649

//public class TimetableController
public class TimetableController : MonoBehaviour
{
    private const int RetrieveRate = 60 * 1000;
    public int stationId;

    private IEnumerable<Departure> _departures;

//    private string _text;
    public Text textLeft;
    public Text textRight;
    private volatile bool _departuresLock;

    // ReSharper disable once UnusedMember.Global
    public static void Main(string[] args)
    {
        // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
        var controller = new TimetableController {stationId = 8603307};
        controller.Awake();
        controller.Start();

        while (true)
        {
            controller.Update();
//            Console.WriteLine(controller._text);
            Thread.Sleep(500);
        }

        // ReSharper disable once FunctionNeverReturns
    }


    private void Awake()
    {
        if (stationId < 8600000)
        {
            Debug.LogWarningFormat("Potentially invalid StationId ('{0}') found at '{1}'. Disabling script.", stationId,
                gameObject.name);
            enabled = false;
        }
    }

    private void Start()
    {
        Debug.Log("Retrieving departure board");
//        Console.WriteLine("Retrieving departure board");

        var retrieveThread = new Thread(() =>
        {
            while (true)
            {
                _departuresLock = true;

                Thread.Sleep(1000);

                var departures = RetrieveDepartures();
                _departures = GetTrains(departures);
                _departuresLock = false;

                Thread.Sleep(RetrieveRate);
            }

            // ReSharper disable once FunctionNeverReturns
        });

        retrieveThread.Start();
    }

    private void Update()
    {
        if (_departuresLock || _departures == null) return;

        PrintTimes();
    }

    private string RetrieveDepartures()
    {
        var dateTime = DateTime.Now;

        var url = "http://xmlopen.rejseplanen.dk/bin/rest.exe/departureBoard" +
                  $"?id={stationId}" +
                  $"&date={dateTime.Day}.{dateTime.Month}.{dateTime.Year}" +
                  $"&time={dateTime.Hour}:{dateTime.Minute}" +
                  "&useTog=0&useBus=0&format=json";

        Debug.Log($"URL: {url}");
        var request = WebRequest.Create(url);

        var response = (HttpWebResponse) request.GetResponse();
        Debug.Log(response.StatusCode);
//        Console.WriteLine(response.StatusCode);

        var dataStream = response.GetResponseStream();
        var reader = new StreamReader(dataStream ?? throw new NullReferenceException());
        var responseFromServer = reader.ReadToEnd();
        Debug.Log(responseFromServer);
//        Console.WriteLine(responseFromServer);

        reader.Close();
        dataStream.Close();
        response.Close();

        return responseFromServer;
    }

    private static IEnumerable<Departure> GetTrains(string departures)
    {
        var jsonContent = departures;

        var contents = JsonConvert.DeserializeObject<DepartureBoardJsonContainer>(jsonContent);

        return Array.FindAll(contents.DepartureBoard.Departure,
            departure => ((IList) StationMeta.MetroNames).Contains(departure.name));
    }

    private void PrintTimes()
    {
        var time = DateTime.Now;

//        _text = "";
        textLeft.text = "";
        textRight.text = "";

        foreach (var departureItem in _departures)
        {
//            string targetText = null;
            Text targetText;
            switch (departureItem.direction)
            {
                case StationMeta.Direction.Lufthavnen:
                case StationMeta.Direction.Vestamager:
                    targetText = textLeft;
                    break;
                case StationMeta.Direction.Vanløse:
                    targetText = textRight;
                    break;
                default:
                    targetText = null;
                    break;
            }

            // L: 1min - Metro M1 (Lufthavnen)
            // R: Metro M1 (Vanlose) - 1min
            var arrival = new DateTime(time.Year, time.Month, time.Day,
                int.Parse(departureItem.time.Substring(0, departureItem.time.IndexOf(':'))),
                int.Parse(departureItem.time.Substring(departureItem.time.IndexOf(':') + 1)), 0);
            var now = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);

            var timeSpan = arrival - now;

            if (timeSpan.TotalSeconds <= 0) continue;

            var minutesLeft = timeSpan.TotalSeconds / 60f;

            string minutesLeftString;

            var flooredDeltaMinutes = minutesLeft - (int) minutesLeft;
            if (flooredDeltaMinutes < 0.25 || minutesLeft > 4)
                minutesLeftString = minutesLeft < 0.25f ? "Now" : $"{(int) minutesLeft:00} min";
            else if (flooredDeltaMinutes < 0.75)
                minutesLeftString = minutesLeft < 1 ? "½ min" : $"{(int) minutesLeft:00} ½ min";
            else
                minutesLeftString = $"{(int) minutesLeft + 1:00} min";

            minutesLeftString += $"({minutesLeft:#.##})";

//            var displayText = false
            var displayText = targetText == textRight
                ? $"{departureItem.name} ({departureItem.direction}) - {minutesLeftString}\n"
                : $"{minutesLeftString} - {departureItem.name} ({departureItem.direction})\n";
//                : $"{minutesLeftString}({minutesLeft:#.##}) - {departureItem.name} ({departureItem.direction})\n";

            if (targetText != null)
                targetText.text += displayText;

//            _text += displayText;
        }
    }

    private struct DepartureBoardJsonContainer
    {
        public DepartureBoard DepartureBoard;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private struct DepartureBoard
    {
        public string noNamespaceSchemaLocation;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public Departure[] Departure;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private struct Departure
    {
        public string name;
        public string type;
        public string stop;
        public string time;
        public string date;
        public string id;
        public string line;
        public string messages;
        public string track;
        public string finalStop;
        public string direction;

        public override string ToString()
        {
            return $"{{name: {name}, type: {type}, stop: {stop}, time: {time}, track: {track}, " +
                   $"direction: {direction}}}";
        }
    }
}