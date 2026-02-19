using Microsoft.Data.Sqlite;
using Avalonia;
using TyperPro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace TyperPro.Services;

public static class DatabaseService
{
    private const string ConnectionString = "Data Source=typerpro.db";

    public static void Initialize()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Players (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            CREATE TABLE IF NOT EXISTS Rounds (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PlayerId INTEGER,
                RoundName TEXT,
                AttemptsUsed INTEGER,
                Wpm REAL,
                Accuracy REAL,
                RawWpm REAL,
                CorrectChars INTEGER,
                IncorrectChars INTEGER,
                MissedChars INTEGER,
                ExtraChars INTEGER,
                Consistency REAL,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY(PlayerId) REFERENCES Players(Id)
            );
            CREATE TABLE IF NOT EXISTS RoundPoints (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RoundId INTEGER,
                PointType TEXT,
                X REAL,
                Y REAL,
                FOREIGN KEY(RoundId) REFERENCES Rounds(Id)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public static async Task<int> SavePlayer(string name)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Players (Name) VALUES ($name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public static async Task<int> SaveRound(string playerName, RoundResult result)
    {
        int playerId = await GetPlayerId(playerName);
        if (playerId == 0)
            playerId = await SavePlayer(playerName);

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Rounds (PlayerId, RoundName, AttemptsUsed, Wpm, Accuracy, RawWpm,
                                CorrectChars, IncorrectChars, MissedChars, ExtraChars, Consistency)
            VALUES ($pid, $round, $attempts, $wpm, $acc, $raw, $corr, $inc, $miss, $extra, $cons);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$pid", playerId);
        cmd.Parameters.AddWithValue("$round", result.RoundName);
        cmd.Parameters.AddWithValue("$attempts", result.AttemptsUsed);
        cmd.Parameters.AddWithValue("$wpm", result.Wpm);
        cmd.Parameters.AddWithValue("$acc", result.Accuracy);
        cmd.Parameters.AddWithValue("$raw", result.RawWpm);
        cmd.Parameters.AddWithValue("$corr", result.CorrectChars);
        cmd.Parameters.AddWithValue("$inc", result.IncorrectChars);
        cmd.Parameters.AddWithValue("$miss", result.MissedChars);
        cmd.Parameters.AddWithValue("$extra", result.ExtraChars);
        cmd.Parameters.AddWithValue("$cons", result.Consistency);

        int roundId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        await SavePoints(roundId, "wpm", result.WpmPoints, connection);
        await SavePoints(roundId, "raw", result.RawWpmPoints, connection);
        await SavePoints(roundId, "error", result.ErrorPoints, connection);

        return roundId;
    }

    private static async Task<int> GetPlayerId(string name)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id FROM Players WHERE Name = $name LIMIT 1";
        cmd.Parameters.AddWithValue("$name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    private static async Task SavePoints(int roundId, string type, List<Point> points, SqliteConnection connection)
    {
        foreach (var p in points)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO RoundPoints (RoundId, PointType, X, Y) VALUES ($rid, $type, $x, $y)";
            cmd.Parameters.AddWithValue("$rid", roundId);
            cmd.Parameters.AddWithValue("$type", type);
            cmd.Parameters.AddWithValue("$x", p.X);
            cmd.Parameters.AddWithValue("$y", p.Y);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}