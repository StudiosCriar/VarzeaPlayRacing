using System;
using Advertisement;
using UnityEngine;

[RequireComponent(typeof(Network.TextureDownloader))]
public class TextureDownloaderUIHandler : MonoBehaviour
{
    private Network.TextureDownloader _textureDownloader;

    private void Awake()
    {
        _textureDownloader = GetComponent<Network.TextureDownloader>();
    }
    
    private void Start()
    {
        AdManager.Instance.HideBanner();
    }
        
    private void OnEnable()
    {
        _textureDownloader.OnLog += OnLog;
        _textureDownloader.OnError += OnError;
        _textureDownloader.OnTexturesLoaded += OnFinish;
    }

    private void OnDisable()
    {
        _textureDownloader.OnLog -= OnLog;
        _textureDownloader.OnError -= OnError;
        _textureDownloader.OnTexturesLoaded -= OnFinish;
    }

    private static void OnLog(string message)
    {
        OverlayCanvasManager.Instance.Open(message, false);
    }
        
    private void OnError(Network.TextureDownloader.Error error)
    {
        switch (error.ErrorType)
        {
            case Network.TextureDownloader.ErrorType.NetworkUnavailable:
                OverlayCanvasManager.Instance.Open("No internet connection available. Using cache.".ToUpper(), () =>
                {
                    _textureDownloader.LoadTexturesOffline();
                    AdManager.Instance.ShowBanner();
                });
                break;
            case Network.TextureDownloader.ErrorType.ListingFailed:
                OverlayCanvasManager.Instance.Open(error.Message.ToUpper(), () =>
                {
                    _textureDownloader.LoadTexturesOffline();
                    AdManager.Instance.ShowBanner();
                });
                break;
            case Network.TextureDownloader.ErrorType.CacheFileError:
                OverlayCanvasManager.Instance.Open(error.Message.ToUpper(), () =>
                {
                    _textureDownloader.Load();
                    AdManager.Instance.ShowBanner();
                });
                break;
            case Network.TextureDownloader.ErrorType.ReadFromCacheFailed:
            case Network.TextureDownloader.ErrorType.CachedTextureNotFound:
            case Network.TextureDownloader.ErrorType.DownloadFailed:
                OverlayCanvasManager.Instance.Open(error.Message.ToUpper(), AdManager.Instance.ShowBanner);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
        
    private static void OnFinish()
    {
        OverlayCanvasManager.Instance.Close();
        AdManager.Instance.ShowBanner();
    }
}