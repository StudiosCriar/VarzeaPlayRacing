using UnityEngine;

namespace Advertisement
{
    public class BannerController : MonoBehaviour
    {
        public void Show()
        {
            AdManager.Instance.ShowBanner();
        }

        public void Hide()
        {
            AdManager.Instance.HideBanner();
        }
    }
}