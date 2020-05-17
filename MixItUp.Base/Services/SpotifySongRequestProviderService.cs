﻿using MixItUp.Base.Model.SongRequests;
using MixItUp.Base.Model.Spotify;
using MixItUp.Base.Util;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public class SpotifySongRequestProviderService : ISongRequestProviderService
    {
        public const string SpotifyDefaultAlbumArt = "https://developer.spotify.com/assets/branding-guidelines/icon1@2x.png";

        private const string SpotifyLinkPrefix = "https://open.spotify.com/track/";
        private const string SpotifyTrackPrefix = "spotify:track:";

        private const string SpotifyUserPlaylistLinkRegex = @"https://open.spotify.com/user/[\w._-]+/playlist/\w+";
        private const string SpotifyPlaylistLinkRegex = @"https://open.spotify.com/playlist/\w+";
        private const string SpotifyPlaylistRegex = @"spotify:playlist:";
        private const string SpotifyPlaylistUriFormat = "spotify:playlist:{0}";
        private const string SpotifyUserPlaylistRegex = @"spotify:user:\w+:playlist:";

        public SongRequestServiceTypeEnum Type { get { return SongRequestServiceTypeEnum.Spotify; } }

        public async Task<bool> Initialize()
        {
            if (ChannelSession.Services.Spotify == null || (await ChannelSession.Services.Spotify.GetCurrentlyPlaying()) == null)
            {
                return false;
            }
            return true;
        }

        public async Task<IEnumerable<SongRequestModel>> GetPlaylist(string identifier)
        {
            List<SongRequestModel> results = new List<SongRequestModel>();
            try
            {
                if (Regex.IsMatch(identifier, SpotifyPlaylistRegex, RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(identifier, SpotifyUserPlaylistRegex, RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(identifier, SpotifyUserPlaylistLinkRegex, RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(identifier, SpotifyPlaylistLinkRegex, RegexOptions.IgnoreCase))
                {
                    if (ChannelSession.Services.Spotify != null)
                    {
                        if (Regex.IsMatch(ChannelSession.Settings.DefaultPlaylist, SpotifyUserPlaylistLinkRegex, RegexOptions.IgnoreCase) ||
                            Regex.IsMatch(ChannelSession.Settings.DefaultPlaylist, SpotifyPlaylistLinkRegex, RegexOptions.IgnoreCase))
                        {
                            string playlistID = ChannelSession.Settings.DefaultPlaylist.Split(new char[] { '/' }).Last();
                            if (!string.IsNullOrEmpty(playlistID))
                            {
                                if (playlistID.Contains("?"))
                                {
                                    playlistID = playlistID.Substring(0, playlistID.IndexOf("?"));
                                }
                                identifier = string.Format(SpotifyPlaylistUriFormat, playlistID);
                            }
                        }

                        string id = identifier.Split(new string[] { ":playlist:" }, StringSplitOptions.RemoveEmptyEntries).Last();
                        if (!string.IsNullOrEmpty(id))
                        {
                            IEnumerable<SpotifySongModel> songs = await ChannelSession.Services.Spotify.GetPlaylistSongs(new SpotifyPlaylistModel { ID = id });
                            if (songs != null)
                            {
                                foreach (SpotifySongModel song in songs)
                                {
                                    results.Add(new SongRequestModel()
                                    {
                                        Type = SongRequestServiceTypeEnum.Spotify,
                                        ID = song.ID,
                                        URI = song.Uri,
                                        Name = song.ToString(),
                                        AlbumImage = (!string.IsNullOrEmpty(song.Album?.ImageLink)) ? song.Album?.ImageLink : SpotifyDefaultAlbumArt,
                                        Length = song.Duration
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return results;
        }

        public async Task<IEnumerable<SongRequestModel>> Search(string identifier)
        {
            List<SongRequestModel> results = new List<SongRequestModel>();
            try
            {
                if (identifier.StartsWith(SpotifyTrackPrefix) || identifier.StartsWith(SpotifyLinkPrefix))
                {
                    identifier = identifier.Replace(SpotifyTrackPrefix, "");
                    identifier = identifier.Replace(SpotifyLinkPrefix, "");
                    if (identifier.Contains('?'))
                    {
                        identifier = identifier.Substring(0, identifier.IndexOf('?'));
                    }

                    SpotifySongModel song = await ChannelSession.Services.Spotify.GetSong(identifier);
                    if (song != null)
                    {
                        if (!song.Explicit || ChannelSession.Settings.SpotifyAllowExplicit)
                        {
                            results.Add(new SongRequestModel()
                            {
                                ID = song.ID,
                                URI = song.Uri,
                                Name = song.ToString(),
                                AlbumImage = (!string.IsNullOrEmpty(song.Album?.ImageLink)) ? song.Album?.ImageLink : SpotifyDefaultAlbumArt,
                                Type = SongRequestServiceTypeEnum.Spotify,
                                Length = song.Duration
                            });
                        }
                    }
                }
                else
                {
                    foreach (SpotifySongModel song in await ChannelSession.Services.Spotify.SearchSongs(identifier))
                    {
                        if (!song.Explicit || ChannelSession.Settings.SpotifyAllowExplicit)
                        {
                            results.Add(new SongRequestModel()
                            {
                                ID = song.ID,
                                URI = song.Uri,
                                Name = song.ToString(),
                                AlbumImage = (!string.IsNullOrEmpty(song.Album?.ImageLink)) ? song.Album?.ImageLink : SpotifyDefaultAlbumArt,
                                Type = SongRequestServiceTypeEnum.Spotify,
                                Length = song.Duration
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return results;
        }

        public async Task<SongRequestCurrentlyPlayingModel> GetStatus()
        {
            try
            {
                SpotifyCurrentlyPlayingModel currentlyPlaying = await ChannelSession.Services.Spotify.GetCurrentlyPlaying();
                if (currentlyPlaying != null && currentlyPlaying.ID != null)
                {
                    SongRequestCurrentlyPlayingModel result = new SongRequestCurrentlyPlayingModel()
                    {
                        ID = currentlyPlaying.ID,
                        URI = currentlyPlaying.Uri,
                        Name = currentlyPlaying.ToString(),
                        AlbumImage = (!string.IsNullOrEmpty(currentlyPlaying.Album?.ImageLink)) ? currentlyPlaying.Album?.ImageLink : SpotifyDefaultAlbumArt,
                        Type = SongRequestServiceTypeEnum.Spotify,
                        Progress = currentlyPlaying.CurrentProgress,
                        Length = currentlyPlaying.Duration,
                        Volume = currentlyPlaying.Volume,
                    };

                    if (currentlyPlaying.IsPlaying)
                    {
                        result.State = SongRequestStateEnum.Playing;
                    }
                    else if (currentlyPlaying.CurrentProgress > 0)
                    {
                        result.State = SongRequestStateEnum.Paused;
                    }
                    else
                    {
                        result.State = SongRequestStateEnum.Ended;
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public async Task<SongRequestCurrentlyPlayingModel> Play(SongRequestModel song)
        {
            try
            {
                if (await ChannelSession.Services.Spotify.PlaySong(song.URI))
                {
                    return new SongRequestCurrentlyPlayingModel()
                    {
                        ID = song.ID,
                        URI = song.URI,
                        Name = song.Name,
                        AlbumImage = song.AlbumImage,
                        Length = song.Length,
                        Type = song.Type,
                        IsFromBackupPlaylist = song.IsFromBackupPlaylist,
                        User = song.User,
                        State = SongRequestStateEnum.Playing,
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public async Task Pause()
        {
            try
            {
                await ChannelSession.Services.Spotify.PauseCurrentlyPlaying();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public async Task Resume()
        {
            try
            {
                await ChannelSession.Services.Spotify.PlayCurrentlyPlaying();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public async Task Stop() { await this.Pause(); }

        public async Task SetVolume(int volume)
        {
            try
            {
                await ChannelSession.Services.Spotify.SetVolume(volume);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}
