﻿using System.Net.Http.Headers;
using AniListNet.Helpers;
using AniListNet.Models;
using AniListNet.Objects;

namespace AniListNet;

public partial class AniClient
{

    public bool IsAuthenticated { get; private set; }

    public async Task<bool> TryAuthenticateAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            _ = await GetAuthenticatedUserAsync();
            IsAuthenticated = true;
        }
        catch
        {
            _client.DefaultRequestHeaders.Authorization = null;
            IsAuthenticated = false;
        }
        return IsAuthenticated;
    }

    public async Task<User> GetAuthenticatedUserAsync()
    {
        var selections = new GqlSelection("Viewer", GqlParser.ParseType(typeof(User)));
        var response = await PostRequestAsync(selections);
        return response["Viewer"].ToObject<User>();
    }

    public async Task<MediaEntry> SaveMediaEntryAsync(int id, MediaEntryMutation mutation)
    {
        var parameters = new List<GqlParameter> { new("mediaId", id) };
        if (mutation.Status.HasValue)
            parameters.Add(new GqlParameter("status", mutation.Status.Value));
        if (mutation.Score.HasValue)
            parameters.Add(new GqlParameter("score", mutation.Score.Value));
        if (mutation.Progress.HasValue)
            parameters.Add(new GqlParameter("progress", mutation.Progress.Value));
        if (mutation.VolumeProgress.HasValue)
            parameters.Add(new GqlParameter("progressVolumes", mutation.VolumeProgress.Value));
        if (mutation.StartDate.HasValue)
            parameters.Add(new GqlParameter("startedAt", new GqlParameter[]
            {
                new("year", mutation.StartDate.Value.Year),
                new("month", mutation.StartDate.Value.Month),
                new("day", mutation.StartDate.Value.Day)
            }));
        if (mutation.CompleteDate.HasValue)
            parameters.Add(new GqlParameter("completedAt", new List<GqlParameter>
            {
                new("year", mutation.CompleteDate.Value.Year),
                new("month", mutation.CompleteDate.Value.Month),
                new("day", mutation.CompleteDate.Value.Day)
            }));
        var selections = new GqlSelection("SaveMediaListEntry", typeof(MediaEntry).ToSelections(), parameters.ToArray());
        var response = await PostRequestAsync(selections, true);
        return response["SaveMediaListEntry"].ToObject<MediaEntry>();
    }

    public async Task<bool> DeleteMediaEntryAsync(int id)
    {
        var selections = new GqlSelection("DeleteMediaListEntry", new GqlSelection[]
        {
            new("deleted")
        }, new GqlParameter[]
        {
            new("id", id)
        });
        var response = await PostRequestAsync(selections, true);
        return response["DeleteMediaListEntry"]["deleted"].ToObject<bool>();
    }

    public async Task<bool> ToggleMediaFavoriteAsync(int id, MediaType type)
    {
        await ToggleFavoriteAsync(type switch
        {
            MediaType.Anime => "animeId",
            MediaType.Manga => "mangaId"
        }, id);
        return (await GetMediaAsync(id)).IsFavorite; // TODO: can be updated to improve performance
    }

    public async Task<bool> ToggleCharacterFavoriteAsync(int id)
    {
        await ToggleFavoriteAsync("characterId", id);
        return (await GetCharacterAsync(id)).IsFavorite; // TODO: can be updated to improve performance
    }

    public async Task<bool> ToggleStaffFavoriteAsync(int id)
    {
        await ToggleFavoriteAsync("staffId", id);
        return (await GetStaffAsync(id)).IsFavorite; // TODO: can be updated to improve performance
    }

    public async Task<bool> ToggleStudioFavoriteAsync(int id)
    {
        await ToggleFavoriteAsync("studioId", id);
        return (await GetStudioAsync(id)).IsFavorite; // TODO: can be updated to improve performance
    }

    /* below is methods that is privately used */

    private async Task ToggleFavoriteAsync(string field, int id)
    {
        var selections = new GqlSelection("ToggleFavourite", new GqlSelection[]
        {
            new("anime", new GqlSelection[]
            {
                new("pageInfo", new GqlSelection[]
                {
                    new("total")
                })
            })
        }, new GqlParameter[]
        {
            new(field, id)
        });
        _ = await PostRequestAsync(selections, true);
    }

}