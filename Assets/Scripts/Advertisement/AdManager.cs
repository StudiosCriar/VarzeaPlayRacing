using System.Collections;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.UI;

namespace Advertisement
{
    public class AdManager : MonoBehaviour
    {
        [SerializeField] private Image interstitialOverlay;
        
        [Header("Reload settings")]
        [SerializeField, Min(0)] private int bannerMaxLoadAttempts = 3;
        [SerializeField, Min(0)] private int interstitialMaxLoadAttempts = 3;
        [SerializeField, Min(0)] private int rewardedMaxLoadAttempts = 3;
        [SerializeField, Min(10)] private int reloadAttemptDelay = 30;
        
        [Header("Android Ad Units")]
        [SerializeField] private string androidBannerAdUnitId;
        [SerializeField] private string androidInterstitialAdUnitId;
        [SerializeField] private string androidRewardedAdUnitId;
        
        [Header("iOS Ad Units")]
        [SerializeField] private string iOSBannerAdUnitId;
        [SerializeField] private string iOSInterstitialAdUnitId;
        [SerializeField] private string iOSRewardedAdUnitId;

        public static AdManager Instance { get; private set; }

        private string _bannerAdUnitId;
        private string _interstitialAdUnitId;
        private string _rewardedAdUnitId;

        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
        
        private int _bannerLoadAttempts;
        private int _interstitialLoadAttempts;
        private int _rewardedLoadAttempts;

        private bool _shouldDisplayBanner = true;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
#if UNITY_ANDROID
            _bannerAdUnitId = androidBannerAdUnitId;
            _interstitialAdUnitId = androidInterstitialAdUnitId;
            _rewardedAdUnitId = androidRewardedAdUnitId;
#elif UNITY_IOS
            _bannerAdUnitId = iOSBannerAdUnitId;
            _interstitialAdUnitId = iOSInterstitialAdUnitId;
            _rewardedAdUnitId = iOSRewardedAdUnitId;
#endif
            
            Initialize();
        }

        private void Initialize()
        {
            MobileAds.Initialize(initStatus =>
            {
                foreach (var adapter in initStatus.getAdapterStatusMap())
                {
                    Debug.Log($"Adapter {adapter.Key}: {adapter.Value.InitializationState}");
                }
                
                if (!string.IsNullOrWhiteSpace(_bannerAdUnitId)) LoadBanner();
                if (!string.IsNullOrWhiteSpace(_interstitialAdUnitId)) LoadInterstitial();
                if (!string.IsNullOrWhiteSpace(_rewardedAdUnitId)) LoadRewarded();
            });
        }

        #region Banner
        
        public void LoadBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.OnBannerAdLoaded -= OnBannerLoaded;
                _bannerView.OnBannerAdLoadFailed -= OnBannerLoadFailed;
                
                _bannerView.Destroy();
                _bannerView = null;
            }

            Debug.Log("Loading banner ad");
            
            var adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            
            _bannerView = new BannerView(_bannerAdUnitId, adaptiveSize, AdPosition.Bottom);
            _bannerView.LoadAd(new AdRequest());
            
            _bannerView.OnBannerAdLoaded += OnBannerLoaded;
            _bannerView.OnBannerAdLoadFailed += OnBannerLoadFailed;

            if (!_shouldDisplayBanner) _bannerView.Hide();
        }

        private void OnBannerLoaded()
        {
            _interstitialLoadAttempts = 0;
        }
        
        private void OnBannerLoadFailed(AdError adError)
        {
            Debug.LogError($"Banner ad failed to load with error: {adError}");
            ReloadAdOnFailure("banner", ref _bannerLoadAttempts, bannerMaxLoadAttempts, LoadBanner);
        }

        public void ShowBanner()
        {
            _shouldDisplayBanner = true;
            _bannerView?.Show();
        }
        
        public void HideBanner()
        {
            _shouldDisplayBanner = false;
            _bannerView?.Hide();
        }
        
        #endregion

        #region Interstitial
        
        public void LoadInterstitial()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
            
            Debug.Log("Loading interstitial ad");

            InterstitialAd.Load(_interstitialAdUnitId, new AdRequest(),
                (ad, error) =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"Interstitial ad failed to load with error: {error}");
                        ReloadAdOnFailure("interstitial", ref _interstitialLoadAttempts, interstitialMaxLoadAttempts, LoadInterstitial);
                        return;
                    }

                    Debug.Log($"Interstitial ad loaded with response: {ad.GetResponseInfo()}");
                    
                    _interstitialAd = ad;
                    _interstitialLoadAttempts = 0;
                });
        }
        
        public void ShowInterstitial(System.Action onInterstitialClosed = null)
        {
            if (_interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                Debug.LogError("Interstitial is not ready");
                onInterstitialClosed?.Invoke();
                return;
            }
            
            _interstitialAd.OnAdFullScreenContentClosed += OnClosed;
            _interstitialAd.OnAdFullScreenContentFailed += OnError;
            
#if !UNITY_EDITOR
            _interstitialAd.Show();
            DisplayOverlay();
#else
            DisplayOverlay(OnClosed);
#endif

            return;

            void OnClosed()
            {
                Debug.Log("Interstitial ad closed");
                _interstitialAd.OnAdFullScreenContentClosed -= OnClosed;
                onInterstitialClosed?.Invoke();
                LoadInterstitial();
            }

            void OnError(AdError adError)
            {
                Debug.LogError($"Interstitial ad failed to show with error: {adError}");
                _interstitialAd.OnAdFullScreenContentFailed -= OnError;
                onInterstitialClosed?.Invoke();
                LoadInterstitial();
            }
        }
        
        #endregion
        
        #region Rewarded

        private void LoadRewarded()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.OnAdFullScreenContentClosed -= LoadRewarded;
                _rewardedAd.OnAdFullScreenContentFailed -= OnRewardedFullScreenContentFailed;
                
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
            
            Debug.Log("Loading rewarded ad");
            
            RewardedAd.Load(_rewardedAdUnitId, new AdRequest(),
                (ad, error) =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"Rewarded ad failed to load with error: {error}");
                        ReloadAdOnFailure("rewarded", ref _rewardedLoadAttempts, rewardedMaxLoadAttempts, LoadRewarded);
                        return;
                    }

                    Debug.Log($"Rewarded ad loaded with response: {ad.GetResponseInfo()}");
                    
                    _rewardedAd = ad;
                    
                    _rewardedAd.OnAdFullScreenContentClosed += LoadRewarded;
                    _rewardedAd.OnAdFullScreenContentFailed += OnRewardedFullScreenContentFailed;

                    _rewardedLoadAttempts = 0;
                });
        }
        
        private void OnRewardedFullScreenContentFailed(AdError error)
        {
            Debug.LogError($"Rewarded ad failed to open full screen content with error: {error}");
            LoadRewarded();
        }

        public void ShowRewarded(System.Action onRewardEarned)
        {
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                Debug.LogError("Rewarded is not ready");
                return;
            }
            
            _rewardedAd.Show(reward =>
            {
                Debug.Log($"Rewarded ad rewarded the user. Type: {reward.Type}, amount: {reward.Amount}");
                onRewardEarned?.Invoke();
            });
        }
        
        #endregion
        
        private void DisplayOverlay(System.Action onOverlayClosed = null)
        {
            interstitialOverlay.enabled = true;
            // await Task.Delay(3000);
            StartCoroutine(InvokeAfter(() =>
            {
                interstitialOverlay.enabled = false;
                onOverlayClosed?.Invoke();
            }, 3));
        }

        private void ReloadAdOnFailure(string adDescription, ref int loadAttempts, int maxLoadAttempts, System.Action reloadAction)
        {
            if (loadAttempts >= maxLoadAttempts)
            {
                Debug.Log($"Number of attempts to load {adDescription} exceeded ({maxLoadAttempts})");
                return;
            }
            
            loadAttempts++;
            
            Debug.Log($"Trying to reload {adDescription} ({loadAttempts}) in {reloadAttemptDelay} seconds");
            StartCoroutine(InvokeAfter(reloadAction, reloadAttemptDelay));
            // Invoke(nameof(reloadAction), reloadAttemptDelay);
        }

        // private static async void InvokeAfter(System.Action action, int delay)
        private static IEnumerator InvokeAfter(System.Action action, int delay)
        {
            // await Task.Delay(delay * 1000);
            yield return new WaitForSecondsRealtime(delay);
            Debug.Log($"Trying to reload ad");
            action.Invoke();
        }
    }
}