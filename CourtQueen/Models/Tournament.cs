namespace CourtQueen.Models;

public class Tournament
{
    public List<Player> Players { get; set; } = new();
    public int RoundNumber { get; set; } = 1;
    public string? QueenName { get; set; }

    /// <summary>
    /// Sorts the entire list of players by total wins (descending).
    /// Useful for a "leaderboard."
    /// </summary>
    public IEnumerable<Player> GetLeaderboard()
    {
        return Players
            .OrderByDescending(p => p.Wins)
            .ThenBy(p => p.Name); // Tie-break by name, optional
    }

    public void CrownQueen()
    {
        // The queen is the person who has the most wins who played the last round on the Queens Court
        // also need to account for the possibility of a tie
        var potentialQueens = Players.Where(p => p.Court == 0 && p.Wins == Players.Max(p => p.Wins));
        var enumerable = potentialQueens as Player[] ?? potentialQueens.ToArray();
        QueenName = enumerable.Length > 1 ? "Tie" : enumerable.First().Name;
    }

    /// <summary>
    /// Rotates players based on manually selected winners.
    /// </summary>
    /// <param name="winningPairs">Dictionary where key = court number, value = pair of player names that won</param>
    public void RotatePlayers(Dictionary<int, List<string>> winningPairs)
    {
        const int TOTAL_COURTS = 8;
        const int PLAYERS_PER_COURT = 4;

        // 1. Group players by their Court property
        //    We'll identify winners & losers in one pass, updating "Court" in place.
        var groupedByCourt = Players
            .GroupBy(p => p.Court)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 2. For each court (0–7), find up to 4 players
        //    and assign them as winners/losers if we have a valid winning pair.
        for (int court = 0; court < TOTAL_COURTS; court++)
        {
            if (!groupedByCourt.ContainsKey(court))
                continue;

            var courtPlayers = groupedByCourt[court];
            if (courtPlayers.Count != PLAYERS_PER_COURT)
                continue; // If not exactly 4, skip "true" winners logic (handle partial below)

            // Check if we have exactly 2 winners selected for this court
            if (!winningPairs.TryGetValue(court, out var winnerNames) || winnerNames.Count != 2)
                continue; // No valid winners chosen => skip logic

            var winnerSet = new HashSet<string>(winnerNames, StringComparer.OrdinalIgnoreCase);
            var winners = new List<Player>(2);
            var losers = new List<Player>(2);

            // Separate winners vs losers
            foreach (var player in courtPlayers)
            {
                if (winnerSet.Contains(player.Name))
                {
                    winners.Add(player);
                }
                else
                {
                    losers.Add(player);
                }
            }

            // If we do have 2 winners and 2 losers, update them in place
            if (winners.Count == 2 && losers.Count == 2)
            {
                // Increment wins for each winner
                foreach (var w in winners)
                {
                    w.Wins++;
                }

                // Winners move up (unless they're on Court 0)
                int winnerCourt = (court > 0) ? court - 1 : 0;
                winners.ForEach(w => w.Court = winnerCourt);

                // Losers move down (unless they're on Court 7)
                int loserCourt = (court < TOTAL_COURTS - 1) ? court + 1 : court;
                losers.ForEach(l => l.Court = loserCourt);
            }
        }

        // 3. Now we "flatten" the entire list again and re‑split it
        //    so that each court ends with exactly 4 players.
        //    This prevents partial courts from persisting.

        // 3a. Grab a snapshot of players (with updated courts)
        var allPlayers = Players.ToList(); // Defensive copy

        // 3b. We'll simply *sort* them by their new "Court" (optional)
        //     This is mostly for consistency, so that we deal them out in a stable order.
        //     If you'd prefer random distribution after the up/down, remove or adjust this step.
        allPlayers = allPlayers.OrderBy(p => p.Court).ToList();

        // 3c. Clear each player's Court so we can deal them fresh
        //     or you can leave them if you want to keep partial logic
        allPlayers.ForEach(p => p.Court = -1);

        // 4. Re‑split: distribute the 32 players (or however many) into 8 courts of 4
        //    (If you have partial/more players, adjust accordingly.)
        for (int i = 0; i < TOTAL_COURTS; i++)
        {
            // Indices: from i*4 to i*4+3
            var slice = allPlayers.Skip(i * PLAYERS_PER_COURT).Take(PLAYERS_PER_COURT).ToList();
            slice.ForEach(p => p.Court = i);
        }

        // 5. Force partner splitting in each court
        //    [0,1,2,3] => [0,2,1,3]
        for (int i = 0; i < TOTAL_COURTS; i++)
        {
            var playersOnCourt = allPlayers.Where(p => p.Court == i).ToList();
            if (playersOnCourt.Count == PLAYERS_PER_COURT)
            {
                var reorder = new List<Player>
                {
                    playersOnCourt[0],
                    playersOnCourt[2],
                    playersOnCourt[1],
                    playersOnCourt[3]
                };

                // Assign new positions
                reorder[0].Position = 1;
                reorder[1].Position = 2;
                reorder[2].Position = 3;
                reorder[3].Position = 4;

                // We update them back in the master list
                // (You could also directly set them in `Players`.)
                for (int idx = 0; idx < 4; idx++)
                {
                    reorder[idx].Court = i;
                }
            }
        }

        // 6. Update the master Players reference. Now each player's Court is correct.
        Players = allPlayers;

        // 7. Advance round
        RoundNumber++;

        // Debug output
        Console.WriteLine($"=== End of Round {RoundNumber - 1} ===");
        foreach (var p in Players)
        {
            Console.WriteLine($"Player: {p.Name}, Court: {p.Court}, Wins: {p.Wins}, Pos: {p.Position}");
        }
    }
}