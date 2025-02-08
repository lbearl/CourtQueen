namespace CourtQueen.Models;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Court { get; set; } // Court assignment (0-7)

    public int Position { get; set; }
    public int Wins { get; set; } = 0;
}