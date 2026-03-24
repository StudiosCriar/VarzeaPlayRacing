using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Network
{
    [RequireComponent(typeof(NetworkManager))]
    public class TextureDownloader : MonoBehaviour
    {
        [SerializeField] [Tooltip("Texture directory URL")]
        private string baseUrl;

        [SerializeField] [Tooltip("This key must be unique. No other texture downloader should have the same key")]
        private string key;

        [SerializeField] private bool loadOnStart = true;

        private string TextureCacheDirectoryPath => Path.Combine(Application.persistentDataPath, "Cache", key);
        private string AssetsFilePath => Path.Combine(TextureCacheDirectoryPath, $"{key} Assets.json");

        public event Action<string> OnLog;
        public event Action<Error> OnError;
        public event Action OnTexturesLoaded;

        public enum ErrorType
        {
            ListingFailed,
            CacheFileError,
            DownloadFailed,
            NetworkUnavailable,
            ReadFromCacheFailed,
            CachedTextureNotFound
        }

        public struct Error
        {
            public string Message;
            public ErrorType ErrorType;
            public Exception Exception;
        }

        public List<Texture2D> Textures => new(_textures.Values);

        private Dictionary<Uri, Texture2D> _textures;
        private Dictionary<Uri, CachedTexture> _textureCache;

        private NetworkManager _networkManager;

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();

#if !UNITY_WEBGL
            Directory.CreateDirectory(TextureCacheDirectoryPath);

            try
            {
                var content = File.ReadAllText(AssetsFilePath);
                _textureCache = JsonConvert.DeserializeObject<Dictionary<Uri, CachedTexture>>(content);
            }
            catch (FileNotFoundException)
            {
                OnLog?.Invoke("Texture cache file not found");
                _textureCache = new Dictionary<Uri, CachedTexture>();
            }
            catch (Exception e)
            {
                OnError?.Invoke(new Error
                {
                    Message = $"An error occurred trying to read the texture cache file: {AssetsFilePath}.",
                    ErrorType = ErrorType.CacheFileError,
                    Exception = e
                });

                _textureCache = new Dictionary<Uri, CachedTexture>();
                return;
            }
#else // WEBGL
            _textureCache = new Dictionary<Uri, CachedTexture>();
#endif

            if (loadOnStart) Load();
        }

        [ContextMenu("Load")]
        public async void Load(bool useCacheIfOffline = true)
        {
#if UNITY_WEBGL
            // WEBGL: Sempre carrega online, ignora cache local
            LoadTexturesOnline();
#else
            OnLog?.Invoke("Checking internet connection...");

            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                LoadTexturesOnline();
                return;
            }

            OnError?.Invoke(new Error
            {
                Message = "No internet connection.",
                ErrorType = ErrorType.NetworkUnavailable,
                Exception = null
            });

            if (useCacheIfOffline) LoadTexturesOffline();
#endif
        }

        private async Task<List<Uri>> ListDirectory(Uri url, string[] extensions = null)
        {
            var request = await _networkManager.Get(url, null);

            if (request.error != null) throw new Exception(request.error);

            return NetworkManager.ParseApacheDirectoryIndex(url, request.downloadHandler.text, extensions);
        }

        private async Task<Texture2D> DownloadTexture(Uri uri, bool useCache = true)
        {
            var headers = new Dictionary<string, string>();

            if (useCache && _textureCache.TryGetValue(uri, out var asset))
            {
                headers["If-None-Match"] = asset.etag;
                // Debug.Log($"Using cache. ETag: {asset.etag}");
            }

            var request = await _networkManager.GetTexture(uri, headers);

            // Debug.Log($"Response Code: {request.responseCode}");

            // Cache hit
            if (request.responseCode == 304) return null;

            if (request.error != null)
            {
                throw new Exception($"(Request) {request.error}\n" +
                                    $"(DownloadHandler){request.downloadHandler.error}");
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            var etag = request.GetResponseHeader("etag");

            SaveTextureToCache(uri, etag);

            return texture;
        }

        private void SaveTextureToCache(Uri uri, string etag)
        {
            var path = GetTexturePath(uri);

            _textureCache[uri] = new CachedTexture
            {
                Uri = uri,
                path = path,
                name = Path.GetFileNameWithoutExtension(uri.LocalPath),
                etag = etag
            };

            // Debug.Log($"Caching asset: {_textureCache[uri].path}");
        }

        private async Task<Texture2D> LoadTextureFromCache(Uri uri)
        {
            if (!_textureCache.TryGetValue(uri, out var cachedTexture))
            {
                OnError?.Invoke(new Error
                {
                    Message = $"Could not find {uri} in cache",
                    ErrorType = ErrorType.CachedTextureNotFound,
                    Exception = null
                });

                return null;
            }

            var data = await File.ReadAllBytesAsync(cachedTexture.path);

            var localTexture = new Texture2D(1, 1)
            {
                name = Path.GetFileNameWithoutExtension(uri.LocalPath)
            };

            if (localTexture.LoadImage(data, false)) return localTexture;

            OnError?.Invoke(new Error
            {
                Message = $"Could not load {uri}",
                ErrorType = ErrorType.ReadFromCacheFailed,
                Exception = null
            });

            return null;
        }

        private void ClearCache(IList<Uri> fileList)
        {
            var deleteList = _textureCache.Where(f => !fileList.Contains(f.Key)).ToList();

            foreach (var (uri, cachedTexture) in deleteList)
            {
                try
                {
                    Debug.Log($"Deleting {cachedTexture.name} ({uri})");
                    File.Delete(cachedTexture.path);
                    _textureCache.Remove(uri);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public async void LoadTexturesOffline()
        {
            _textures = new Dictionary<Uri, Texture2D>();

            foreach (var uri in _textureCache.Keys)
            {
                var textureName = Path.GetFileNameWithoutExtension(uri.LocalPath);
                OnLog?.Invoke($"Loading texture from cache: {textureName}");
                var texture = await LoadTextureFromCache(uri);
                if (texture) _textures[uri] = texture;
            }

            OnTexturesLoaded?.Invoke();
        }

        public async void LoadTexturesOnline()
        {
            _textures = new Dictionary<Uri, Texture2D>();

            List<Uri> fileList;

            try
            {
                fileList = await ListDirectory(new Uri(baseUrl));
            }
            catch (Exception e)
            {
                OnError?.Invoke(new Error
                {
                    Message = "Failed to retrieve list of textures online.",
                    ErrorType = ErrorType.ListingFailed,
                    Exception = e
                });

                return;
            }

#if !UNITY_WEBGL
            ClearCache(fileList);
#endif

            foreach (var uri in fileList)
            {
                var textureName = Path.GetFileNameWithoutExtension(uri.LocalPath);
                OnLog?.Invoke($"Downloading texture: {textureName}");

                Texture2D remoteTexture;

                try
                {
                    remoteTexture = await DownloadTexture(uri);
                }
                catch (Exception e)
                {
                    OnError?.Invoke(new Error
                    {
                        Message = $"Failed to download texture: {uri}.",
                        ErrorType = ErrorType.DownloadFailed,
                        Exception = e
                    });

                    continue;
                }

                if (remoteTexture == null)
                {
#if !UNITY_WEBGL
                    _textures[uri] = await LoadTextureFromCache(uri);
#endif
                    continue;
                }

                _textures[uri] = remoteTexture;
                remoteTexture.name = textureName;

#if !UNITY_WEBGL
                var path = GetTexturePath(uri);
                await File.WriteAllBytesAsync(path, remoteTexture.EncodeToPNG());
#endif
            }

            OnTexturesLoaded?.Invoke();

#if !UNITY_WEBGL
            await File.WriteAllTextAsync(AssetsFilePath,
                JsonConvert.SerializeObject(_textureCache, Formatting.Indented));
#endif
        }

        private string GetTexturePath(Uri uri)
        {
            var path = Path.Combine(TextureCacheDirectoryPath, Path.GetFileName(uri.LocalPath));
            return Path.ChangeExtension(path, "png");
        }
    }
}