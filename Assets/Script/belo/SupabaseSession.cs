using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>One in-memory refresh lock shared by cloud sync and both admin editors.</summary>
public static class SupabaseSession
{
    [Serializable] private class RefreshPayload { public string refresh_token; }
    [Serializable] private class User { public string id; }
    [Serializable] private class Tokens
    {
        public string access_token;
        public string refresh_token;
        public User user;
    }
    [Serializable] private class Claims { public long exp; }
    private static bool refreshing;
    private static string rejectedToken;
    private static int sessionGeneration;
    private static UnityWebRequest activeRefreshRequest;
    public static string LastError { get; private set; }

    public static void CancelPendingRefresh()
    {
        sessionGeneration++;
        activeRefreshRequest?.Abort();
        activeRefreshRequest = null;
        refreshing = false;
        rejectedToken = null;
        LastError = "";
    }

    public static DateTime ExpiryUtc(string token)
    {
        try
        {
            string[] parts = token.Split('.');
            if (parts.Length != 3) return DateTime.MinValue;
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
            var claims = JsonUtility.FromJson<Claims>(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return claims != null && claims.exp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(claims.exp).UtcDateTime : DateTime.MinValue;
        }
        catch (Exception) { return DateTime.MinValue; }
    }

    public static IEnumerator EnsureAccessToken(Action<bool> completed)
    {
        while (refreshing) yield return null;
        string token = MainMenuSaveSlots.CurrentAccessToken;
        string owner = MainMenuSaveSlots.CurrentUserId;
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(owner))
        {
            LastError = "Sign in to sync your progress.";
            completed?.Invoke(false);
            yield break;
        }
        if (token != rejectedToken && ExpiryUtc(token) > DateTime.UtcNow.AddSeconds(90))
        {
            LastError = "";
            completed?.Invoke(true);
            yield break;
        }
        string refresh = MainMenuSaveSlots.CurrentRefreshToken;
        if (string.IsNullOrWhiteSpace(refresh))
        {
            LastError = "Your session expired. Progress is saved on this device; sign in again to sync.";
            completed?.Invoke(false);
            yield break;
        }
        int generation = sessionGeneration;
        refreshing = true;
        try
        {
            string url = MainMenuSaveSlots.CurrentSupabaseProjectUrl.TrimEnd('/') + "/auth/v1/token?grant_type=refresh_token";
            using (var request = new UnityWebRequest(url, "POST"))
            {
                activeRefreshRequest = request;
                request.timeout = 20;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(
                    JsonUtility.ToJson(new RefreshPayload { refresh_token = refresh })));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("apikey", MainMenuSaveSlots.CurrentSupabasePublishableKey);
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                Tokens response = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try { response = JsonUtility.FromJson<Tokens>(request.downloadHandler.text); }
                    catch (ArgumentException) { }
                }
                // A response from a session that logged out/switched accounts must never sign it back in.
                bool accepted = generation == sessionGeneration && response?.user?.id == owner &&
                    !string.IsNullOrWhiteSpace(response.access_token) &&
                    MainMenuSaveSlots.AcceptRefreshedSession(owner, token, response.access_token, response.refresh_token);
                if (generation == sessionGeneration)
                    LastError = accepted ? "" : request.responseCode == 400 || request.responseCode == 401
                        ? "Your session expired. Progress is saved on this device; sign in again to sync."
                        : "Cannot refresh your session right now. Progress is saved locally and will retry.";
                completed?.Invoke(accepted);
            }
        }
        finally
        {
            if (generation == sessionGeneration)
            {
                refreshing = false;
                activeRefreshRequest = null;
            }
        }
    }

    public static IEnumerator Send(UnityWebRequest request)
    {
        string owner = MainMenuSaveSlots.CurrentUserId;
        bool ready = false;
        yield return EnsureAccessToken(value => ready = value);
        if (!ready || owner != MainMenuSaveSlots.CurrentUserId) yield break;
        string token = MainMenuSaveSlots.CurrentAccessToken;
        request.timeout = 20;
        request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();
        if (request.responseCode == 401) rejectedToken = token;
    }
}
