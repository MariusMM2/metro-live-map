public static class StationMeta
{
    public static readonly string[] MetroNames = {"Metro M1", "Metro M2"};

    public static readonly MetroLine[] M1 =
    {
        new MetroLine(Direction.Vanløse, Direction.Vestamager, "M1"),
        new MetroLine(Direction.Vestamager, Direction.Vanløse, "M1")
    };

    public static readonly MetroLine[] M2 =
    {
        new MetroLine(Direction.Vanløse, Direction.Lufthavnen, "M2"),
        new MetroLine(Direction.Lufthavnen, Direction.Vanløse, "M2")
    };

    public static class Direction
    {
        public const string Vanløse = "Vanløse";
        public const string Vestamager = "Vestamager";
        public const string Lufthavnen = "Lufthavnen";
    }

    public class MetroLine
    {
        public string To { get; }
        public string From { get; }
        public string Name { get; }

        public MetroLine(string to, string from, string name)
        {
            To = to;
            From = from;
            Name = name;
        }
    }
}