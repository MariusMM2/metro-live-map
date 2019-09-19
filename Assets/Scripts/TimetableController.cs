using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using static StationMeta;

#pragma warning disable 649

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class TimetableController : MonoBehaviour
{
    private const float RetrieveRate = 60f;
    private const float UpdateRate = 10f;
    public int stationId;

    private IEnumerable<Departure> _departures;

    public Text textLeft;
    public Text textRight;
    private volatile bool _departuresLock;
    private bool _retrieveThreadStopFlag;
    private float _elapsedUpdateTime;

    // ReSharper disable once UnusedMember.Global
    public static void Main()
    {
        // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
        var controller = new TimetableController
        {
            stationId = 8603307,
//            textLeft = new Text(), 
//            textRight = new Text()
        };

        controller.Awake();
        controller.Start();

        while (true)
        {
//            Time.Update();
            controller.Update();
            Debug.Log(controller.textLeft.text);
            Debug.Log(controller.textRight.text);
            Thread.Sleep(1000);
        }

        // ReSharper disable once FunctionNeverReturns
    }


    private void Awake()
    {
        if (stationId < 8600000)
        {
            Debug.LogWarning(
                $"Potentially invalid StationId ('{stationId}') found at '{gameObject.name}'. Disabling script.");
            enabled = false;
//            Debug.LogWarning($"Potentially invalid StationId ('{stationId}'). Disabling script.");
        }
    }

    private void Start()
    {
        Debug.Log("Retrieving departure board");

        _retrieveThreadStopFlag = false;
        _elapsedUpdateTime = UpdateRate;

        var retrieveThread = new Thread(() =>
        {
            while (!_retrieveThreadStopFlag)
            {
                _departuresLock = true;

                Thread.Sleep(1000);

                var departures = RetrieveDepartures();
                _departures = GetTrains(departures);
                _departuresLock = false;

                Thread.Sleep((int) (RetrieveRate * 1000));
            }

            Debug.Log("Retrieve Thread: Stopping...");

            // ReSharper disable once FunctionNeverReturns
        });

        retrieveThread.Start();
    }

    private void Update()
    {
        if (_elapsedUpdateTime >= UpdateRate)
        {
            _elapsedUpdateTime = 0;

            if (_departuresLock || _departures == null)
            {
                _elapsedUpdateTime = UpdateRate;
                return;
            }

            PrintTimes();
        }
        else
            _elapsedUpdateTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        Debug.Log("Stopping Retrieve thread.");
        _retrieveThreadStopFlag = true;
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
        Debug.Log(response.StatusCode.ToString());

        var dataStream = response.GetResponseStream();
        var reader = new StreamReader(dataStream ?? throw new NullReferenceException());
        var responseFromServer = reader.ReadToEnd();
        Debug.Log(responseFromServer);

        reader.Close();
        dataStream.Close();
        response.Close();

        return responseFromServer;
    }

    private static IEnumerable<Departure> GetTrains(string departures)
    {
        var jsonContent = departures;

        var contents = JsonConvert.DeserializeObject<DepartureBoardJsonContainer>(jsonContent);

        return contents.DepartureBoard.Departure.Where(departure => MetroNames.Contains(departure.name));
    }

    private void PrintTimes()
    {
        var time = DateTime.Now;

        textLeft.text = "";
        textRight.text = "";

        foreach (var departureItem in _departures)
        {
            Text targetText;
            switch (departureItem.track)
            {
                case "1":
                    targetText = textLeft;
                    break;
                case "2":
                    targetText = textRight;
                    break;
                default:
                    targetText = null;
                    break;
            }

            // L: 1min - Metro M1 (Lufthavnen)
            // R: Metro M1 (Vanløse) - 1min
            var arrival = new DateTime(time.Year, time.Month, time.Day,
                int.Parse(departureItem.time.Substring(0, departureItem.time.IndexOf(':'))),
                int.Parse(departureItem.time.Substring(departureItem.time.IndexOf(':') + 1)), 0);
            var now = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);

            var timeSpan = arrival - now;

            if (timeSpan.TotalSeconds <= -30) continue;

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

            var displayText = targetText == textRight
                ? $"{departureItem.name} ({departureItem.direction}) - {minutesLeftString}\n"
                : $"{minutesLeftString} - {departureItem.name} ({departureItem.direction})\n";

            // ReSharper disable once PossibleNullReferenceException
            targetText.text += displayText;
        }
    }
}