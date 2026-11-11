using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class HighScoreData
{
    public List<int> scores = new List<int>();
}

/// class to save and load high scores from a JSON file.
public static class ScoreManager
{
    public const int MaxScores = 4;
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "highscores.json");

    /// Loads the top 4 scores from JSON in descending order.

    public static List<int> LoadScores()
    {
        List<int> result = new List<int>();

        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                HighScoreData data = JsonUtility.FromJson<HighScoreData>(json);

                if (data != null && data.scores != null)
                {
                    result.AddRange(data.scores);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScoreManager] Failed to load scores from JSON: {ex.Message}");
        }

        // Sort descending
        result.Sort((a, b) => b.CompareTo(a));

        while (result.Count < MaxScores)
        {
            result.Add(0);
        }

        if (result.Count > MaxScores)
        {
            result.RemoveRange(MaxScores, result.Count - MaxScores);
        }

        return result;
    }

    /// Saves the top 4 scores to the  JSON file.
    public static void SaveScores(List<int> scores)
    {
        try
        {
            HighScoreData data = new HighScoreData();

            if (scores != null)
            {
                data.scores = new List<int>(scores);
                data.scores.Sort((a, b) => b.CompareTo(a));

                // Keep only top 4
                if (data.scores.Count > MaxScores)
                {
                    data.scores.RemoveRange(MaxScores, data.scores.Count - MaxScores);
                }

                while (data.scores.Count < MaxScores)
                {
                    data.scores.Add(0);
                }
            }
            else
            {
                data.scores = new List<int> { 0, 0, 0, 0 };
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[ScoreManager] High scores saved to JSON at: {FilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ScoreManager] Failed to save scores to JSON: {ex.Message}");
        }
    }

    /// Checks if newScore qualifies for the top 4 scores.
    /// If it does, inserts it at the correct index (descending order) and saves to JSON.
    /// Returns true if the score was added to the top 4.
    public static bool TryAddScore(int newScore, out int rankIndex)
    {
        rankIndex = -1;

        if (newScore <= 0)
            return false;

        List<int> scores = LoadScores();

        // Find the correct index to insert (descending order)
        int insertIndex = -1;
        for (int i = 0; i < scores.Count; i++)
        {
            if (newScore > scores[i])
            {
                insertIndex = i;
                break;
            }
        }

        // If it qualifies to be in the top 4
        if (insertIndex != -1)
        {
            scores.Insert(insertIndex, newScore);
            rankIndex = insertIndex;

            // Keep only top 4
            if (scores.Count > MaxScores)
            {
                scores.RemoveRange(MaxScores, scores.Count - MaxScores);
            }

            SaveScores(scores);
            return true;
        }

        return false;
    }

    /// Overload without out parameter.
    public static bool TryAddScore(int newScore)
    {
        return TryAddScore(newScore, out _);
    }

    /// Resets all high scores back to 0.
    public static void ClearScores()
    {
        SaveScores(new List<int> { 0, 0, 0, 0 });
    }
}

