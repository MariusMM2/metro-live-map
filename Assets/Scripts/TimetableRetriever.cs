using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using static StationMeta;

public class TimetableRetriever : MonoBehaviour, IObservable<List<Departure>>
{
    private const float RetrieveRate = 60f;

    private const int StationsPerRequest = 3;
    private static readonly int Requests = (int) Math.Ceiling((float) StationIds.Length / StationsPerRequest);

    private volatile List<Departure> _departures;
    private List<IObserver<List<Departure>>> _observers;

    private float _elapsedUpdateTime;
    private readonly HttpClient _httpClient = new HttpClient();

    // Start is called before the first frame update
    private void Start()
    {
        _elapsedUpdateTime = RetrieveRate;
        _departures = new List<Departure>();
        _observers = new List<IObserver<List<Departure>>>();
    }

    // Update is called once per frame
    private async void Update()
    {
        if (_elapsedUpdateTime >= RetrieveRate)
        {
            _elapsedUpdateTime = 0;

            // Stuff every RetrieveRate seconds

            var dateTime = DateTime.Now;

            _departures = new List<Departure>();

            for (var i = 0; i < Requests; i++)
            {
                var url = "http://xmlopen.rejseplanen.dk/bin/rest.exe/multiDepartureBoard?";
                for (var j = 0; j < StationsPerRequest && j + i * StationsPerRequest < StationIds.Length; j++)
                {
                    url += $"id{j + 1}={StationIds[i * StationsPerRequest + j]}&";
                }

                url += $"date={dateTime.Day}.{dateTime.Month}.{dateTime.Year}" +
                       $"&time={dateTime.Hour}:{dateTime.Minute}" +
                       "&useTog=0&useBus=0&format=json";

                await GetDepartureBoard(url);
            }
        }
        else
            _elapsedUpdateTime += Time.deltaTime;
    }

    public IEnumerable<Departure> GetDepartures(int stationId)
    {
        if (!StationIds.Contains(stationId))
        {
            var message = $"Attempted to retrieve departures for invalid StationId ('{stationId}')";
            Debug.LogWarning(message);
            throw new ArgumentException(message);
        }

        return _departures.Where(departure => departure.id.Equals(stationId.ToString()));
    }

    private async Task GetDepartureBoard(string url)
    {
        var dataStream = await _httpClient.GetStreamAsync(url);

        var reader = new StreamReader(dataStream);
        var departureData = reader.ReadToEnd();
        Debug.Log($"URL: {url}\n {departureData}");

        reader.Close();
        dataStream.Close();

        AddDepartures(departureData);
    }

    private void AddDepartures(string departureData)
    {
        var contents = JsonConvert.DeserializeObject<MultiDepartureBoardJsonContainer>(departureData);
        var departures = contents.MultiDepartureBoard.Departure;

        _departures.AddRange(departures.Where(departure => MetroNames.Contains(departure.name)));
        Notify(_departures);
    }

    public IDisposable Subscribe(IObserver<List<Departure>> observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);

            observer.OnNext(_departures);
        }

        return new Subscription<List<Departure>>(_observers, observer);
    }

    private void Notify(List<Departure> departures)
    {
        _observers.ForEach(observer => observer.OnNext(departures));
    }

    private class Subscription<T> : IDisposable
    {
        private readonly List<IObserver<T>> _observers;
        private readonly IObserver<T> _observer;

        internal Subscription(List<IObserver<T>> observers, IObserver<T> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}