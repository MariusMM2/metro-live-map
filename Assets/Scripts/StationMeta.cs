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

    public struct MetroLine
    {
        public string To,
            From,
            Name;
    }
}