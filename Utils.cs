#nullable enable
using Dalamud.Interface.Textures.TextureWraps;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Dalamud.Interface.Textures;


namespace EasyZoomReborn;

public static class Utils
{
    
    internal static ConcurrentDictionary<string, ImageLoadingResult> CachedTextures = new();
    internal static ConcurrentDictionary<(uint ID, bool HQ), ImageLoadingResult> CachedIcons = new();

    private static readonly List<Func<byte[], byte[]>> _conversionsToBitmap = new() { b => b, };

    static volatile bool ThreadRunning = false;
    internal static HttpClient httpClient = new HttpClient();

    public static bool TryGetTextureWrap(string url, out IDalamudTextureWrap? textureWrap)
    {
        if (!CachedTextures.TryGetValue(url, out var result))
        {
            result = new();
            CachedTextures[url] = result;
            BeginThreadIfNotRunning();
        }
        textureWrap = result.Texture;
        return result.Texture != null;
    }

    internal static void BeginThreadIfNotRunning()
    {
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        if (ThreadRunning) return;
        EasyZoomRebornPlugin.PluginLog.Verbose("Starting ThreadLoadImageHandler");
        ThreadRunning = true;
        new Thread(() =>
        {
            int idleTicks = 0;
            Safe(delegate
            {
                while (idleTicks < 100)
                {
                    Safe(delegate
                    {
                        {
                            if (CachedTextures.TryGetFirst(x => x.Value.IsCompleted == false, out var keyValuePair))
                            {
                                idleTicks = 0;
                                keyValuePair.Value.IsCompleted = true;
                                EasyZoomRebornPlugin.PluginLog.Verbose("Loading image " + keyValuePair.Key);
                                if (keyValuePair.Key.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || keyValuePair.Key.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                                {
                                    var result = httpClient.GetAsync(keyValuePair.Key).Result;
                                    result.EnsureSuccessStatusCode();
                                    var content = result.Content.ReadAsByteArrayAsync().Result;

                                    IDalamudTextureWrap? texture = null;
                                    foreach (var conversion in _conversionsToBitmap)
                                    {
                                        if (conversion == null) continue;

                                        try
                                        {
                                            texture = EasyZoomRebornPlugin.TextureProvider.CreateFromImageAsync(conversion(content)).Result;
                                            if (texture != null) break;
                                        }
                                        catch (Exception ex)
                                        {
                                            EasyZoomRebornPlugin.PluginLog.Fatal("Exception in utils try get texture: " + ex.Message);
                                        }
                                    }
                                    keyValuePair.Value.TextureWrap = texture;
                                }
                                else
                                {
                                    if (File.Exists(keyValuePair.Key))
                                    {
                                        keyValuePair.Value.ImmediateTexture = EasyZoomRebornPlugin.TextureProvider.GetFromFile(keyValuePair.Key);
                                    }
                                    else
                                    {
                                        keyValuePair.Value.ImmediateTexture = EasyZoomRebornPlugin.TextureProvider.GetFromGame(keyValuePair.Key);
                                    }
                                }
                            }
                        }
                        {
                            if (CachedIcons.TryGetFirst(x => x.Value.IsCompleted == false, out var keyValuePair))
                            {
                                idleTicks = 0;
                                keyValuePair.Value.IsCompleted = true;
                                EasyZoomRebornPlugin.PluginLog.Verbose($"Loading icon {keyValuePair.Key.ID}, hq={keyValuePair.Key.HQ}");
                                keyValuePair.Value.ImmediateTexture = EasyZoomRebornPlugin.TextureProvider.GetFromGameIcon(new(keyValuePair.Key.ID, hiRes:keyValuePair.Key.HQ));
                            }
                        }
                    });
                    idleTicks++;
                    if(!CachedTextures.Any(x => x.Value.IsCompleted) && !CachedIcons.Any(x => x.Value.IsCompleted)) Thread.Sleep(100);
                }
            });
            EasyZoomRebornPlugin.PluginLog.Verbose($"Stopping ThreadLoadImageHandler, ticks={idleTicks}");
            ThreadRunning = false;
        }).Start();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Safe(System.Action a, bool suppressErrors = false)
    {
        try
        {
            a();
        }
        catch (Exception e)
        {
            if (!suppressErrors) EasyZoomRebornPlugin.PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
        }
    }
    
    public static bool TryGetFirst<K, V>(this IDictionary<K, V> dictionary, Func<KeyValuePair<K, V>, bool> predicate, out KeyValuePair<K, V> keyValuePair)
    {
        try
        {
            keyValuePair = dictionary.First(predicate);
            return true;
        }
        catch(Exception)
        {
            keyValuePair = default;
            return false;
        }
    }
}

internal class ImageLoadingResult
{
    internal ISharedImmediateTexture? ImmediateTexture;
    internal IDalamudTextureWrap? TextureWrap;
    internal IDalamudTextureWrap? Texture => ImmediateTexture?.GetWrapOrDefault() ?? TextureWrap;
    internal bool IsCompleted = false;

    public ImageLoadingResult(ISharedImmediateTexture? immediateTexture)
    {
        ImmediateTexture = immediateTexture;
    }

    public ImageLoadingResult(IDalamudTextureWrap? textureWrap)
    {
        TextureWrap = textureWrap;
    }

    public ImageLoadingResult()
    {
    }
}