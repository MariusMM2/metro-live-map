using System.Diagnostics.CodeAnalysis;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

public static class StationMeta
{
    public static readonly string[] MetroNames = {"Metro M1", "Metro M2"};

    public static readonly MetroLine[] M1 =
    {
        new MetroLine {To = Direction.Vanløse, From = Direction.Vestamager, Name = "M1"},
        new MetroLine {To = Direction.Vestamager, From = Direction.Vanløse, Name = "M1"}
    };

    public static readonly MetroLine[] M2 =
    {
        new MetroLine {To = Direction.Vanløse, From = Direction.Lufthavnen, Name = "M2"},
        new MetroLine {To = Direction.Lufthavnen, From = Direction.Vanløse, Name = "M2"}
    };

    public static class Direction
    {
        public const string Vanløse = "Vanløse",
            Vestamager = "Vestamager",
            Lufthavnen = "Lufthavnen";
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public struct MetroLine
    {
        public string To,
            From,
            Name;
    }

    public struct DepartureBoardJsonContainer
    {
        // ReSharper disable once UnassignedField.Global
        public DepartureBoard DepartureBoard;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct DepartureBoard
    {
        public string noNamespaceSchemaLocation;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        // ReSharper disable once UnassignedField.Global
        public Departure[] Departure;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnassignedField.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct Departure
    {
        public string name,
            type,
            stop,
            time,
            date,
            id,
            line,
            messages,
            track,
            finalStop,
            direction;

        public override string ToString()
        {
            return $"{{name: {name}, type: {type}, stop: {stop}, time: {time}, track: {track}, " +
                   $"direction: {direction}}}";
        }
    }
}