using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static StationMeta;

#pragma warning disable 649

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class TimetableController : MonoBehaviour, IObserver<List<Departure>>
{
    private const float UpdateRate = 10f;
    public int stationId;

    public Text textLeft;
    public Text textRight;

    private volatile List<Departure> _departures;

    private TimetableRetriever _retriever;
    private IDisposable _subscription;

    private volatile float _elapsedUpdateTime;

    private void Awake()
    {
        if (!StationIds.Contains(stationId))
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

        _retriever = transform.parent.gameObject.GetComponent<TimetableRetriever>();

        _subscription = _retriever.Subscribe(this);
    }

    private void Update()
    {
        if (_elapsedUpdateTime >= UpdateRate && _departures != null)
        {
            _elapsedUpdateTime = 0;

            PrintTimes();
        }
        else
            _elapsedUpdateTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
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

    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }

    public void OnNext(List<Departure> value)
    {
        var departures = value.Where(departure => departure.id.Equals(stationId.ToString())).ToList();

        if (departures.Any())
        {
            _departures = departures;
            _elapsedUpdateTime = UpdateRate;
        }
    }
}