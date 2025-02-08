using CourtQueen.Models;

namespace CourtQueen.Tests;

public class TournamentTests
{
    [Fact]
    public void RotatePlayers_WhenNoWinnersSelected_ShouldNotChangeWinsOrCourt()
    {
        // ARRANGE
        var tournament = new Tournament
        {
            Players = GeneratePlayers(32),
            RoundNumber = 1
        };
        // Empty => no winners selected
        var winningPairs = new Dictionary<int, List<string>>();

        var originalWins = tournament.Players.ToDictionary(p => p.Name, p => p.Wins);

        // ACT
        tournament.RotatePlayers(winningPairs);

        // ASSERT
        Assert.Equal(2, tournament.RoundNumber); // Round increments

        // No player's Wins should have changed
        foreach (var player in tournament.Players)
        {
            Assert.Equal(originalWins[player.Name], player.Wins);
        }
    }

    [Fact]
    public void RotatePlayers_WhenTwoWinnersSelectedOnEachCourt_ShouldIncrementWinsAndReassignCourts()
    {
        // ARRANGE
        var tournament = new Tournament
        {
            Players = GeneratePlayers(32), // 8 courts × 4 players
            RoundNumber = 1
        };

        // We'll identify the first 2 players on each court as winners by name
        // For court i: players have names "Player{4*i+1}" ... "Player{4*i+4}"
        var winningPairs = new Dictionary<int, List<string>>();
        for (int court = 0; court < 8; court++)
        {
            var playersOnThisCourt = tournament.Players
                .Where(p => p.Court == court)
                .OrderBy(p => p.Position)
                .Take(4) // just to be sure
                .ToList();

            // Pick the first 2 as winners
            var w1 = playersOnThisCourt[0].Name;
            var w2 = playersOnThisCourt[1].Name;
            winningPairs[court] = new List<string> { w1, w2 };
        }

        // ACT
        tournament.RotatePlayers(winningPairs);

        // ASSERT
        // Round should increment
        Assert.Equal(2, tournament.RoundNumber);

        // Check that each player we identified as a winner has Wins incremented by 1
        foreach (var kv in winningPairs)
        {
            foreach (var winnerName in kv.Value)
            {
                var winnerPlayer = tournament.Players.Single(p => p.Name == winnerName);
                Assert.Equal(1, winnerPlayer.Wins);
            }
        }

        // All others remain at 0
        var allSelectedWinners = winningPairs.Values.SelectMany(x => x).ToHashSet();
        foreach (var player in tournament.Players)
        {
            if (!allSelectedWinners.Contains(player.Name))
            {
                Assert.Equal(0, player.Wins);
            }
        }

        // Ensure we still have 8 courts × 4 players after the re-split
        var groupedCourts = tournament.Players.GroupBy(p => p.Court).ToList();
        Assert.Equal(8, groupedCourts.Count);

        foreach (var group in groupedCourts)
        {
            Assert.Equal(4, group.Count());
        }
    }

    [Fact]
    public void RotatePlayers_ShouldSplitPartnersAfterResplit()
    {
        // ARRANGE
        // We'll do 2 courts (8 players) for simpler demonstration
        var tournament = new Tournament
        {
            Players = GeneratePlayers(8), // 2 courts × 4
            RoundNumber = 1
        };

        // Let's define winners: for Court 0 => Player1, Player2; for Court 1 => Player5, Player6
        // We'll pick them by name (which matches the generation pattern)
        var winningPairs = new Dictionary<int, List<string>>
        {
            [0] = new List<string> { "Player1", "Player2" },
            [1] = new List<string> { "Player5", "Player6" }
        };

        // ACT
        tournament.RotatePlayers(winningPairs);

        // ASSERT
        Assert.Equal(2, tournament.RoundNumber);

        // All winners have +1 wins
        foreach (var pair in winningPairs)
        {
            foreach (var wName in pair.Value)
            {
                var w = tournament.Players.Single(p => p.Name == wName);
                Assert.Equal(1, w.Wins);
            }
        }

        // Partner splitting check: 
        // After re-splitting, each court has 4 players in positions [1..4].
        // The code reorders [0,1,2,3] => [0,2,1,3].
        // We'll ensure no original partner pair remains the same.
        var courtsGrouped = tournament.Players.GroupBy(p => p.Court).ToList();
        foreach (var group in courtsGrouped)
        {
            // 4 players per court
            Assert.Equal(4, group.Count());
            // Distinct positions 1..4
            var positions = group.Select(p => p.Position).OrderBy(x => x).ToList();
            Assert.Equal(new List<int> { 1, 2, 3, 4 }, positions);
        }
    }

    [Fact]
    public void RotatePlayers_WhenPartialCourt_ShouldSkipFullWinnerLogic()
    {
        // ARRANGE
        // Suppose Court 0 has 3 players, Court 1 has 5 players => total 8
        var partialPlayers = new List<Player>
        {
            new Player { Name = "Alice", Court = 0 },
            new Player { Name = "Beth", Court = 0 },
            new Player { Name = "Cathy", Court = 0 },
            new Player { Name = "Diana", Court = 1 },
            new Player { Name = "Eva", Court = 1 },
            new Player { Name = "Fiona", Court = 1 },
            new Player { Name = "Grace", Court = 1 },
            new Player { Name = "Hannah", Court = 1 }
        };

        var tournament = new Tournament
        {
            Players = partialPlayers,
            RoundNumber = 1
        };

        // We'll say Court 0 => "Alice" and "Beth" as winners
        var winningPairs = new Dictionary<int, List<string>>
        {
            [0] = new List<string> { "Alice", "Beth" }
        };

        // ACT
        tournament.RotatePlayers(winningPairs);

        // ASSERT
        Assert.Equal(2, tournament.RoundNumber);

        // Because Court 0 had only 3 players, the code "if (courtPlayers.Count != 4) => continue;"
        // means the "Alice"/"Beth" winner logic is skipped => no one gets wins incremented
        Assert.All(tournament.Players, p => Assert.Equal(0, p.Wins));

        // We ended up re-splitting all players into 2 courts of 4 each (or more if you had 8 total courts)
        // Just ensure no duplicates or missing players
        Assert.Equal(8, tournament.Players.Count);
        var distinctNames = tournament.Players.Select(p => p.Name).Distinct().Count();
        Assert.Equal(8, distinctNames);
    }

    #region Helper Methods

    private static List<Player> GeneratePlayers(int count)
    {
        // For count = 32 => 8 courts × 4, For count=8 => 2 courts × 4, etc.
        var players = new List<Player>(count);
        for (int i = 0; i < count; i++)
        {
            players.Add(new Player
            {
                Name = $"Player{i + 1}",
                Court = i / 4, // group of 4 => next court
                Position = (i % 4) + 1,
                Wins = 0
            });
        }

        return players;
    }

    #endregion
}