using UnityEngine;
using UnityEngine.SceneManagement;

namespace Advertisement
{
    public class LoadBannerOnSceneLoaded : MonoBehaviour
    {
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AdManager.Instance.LoadBanner();
            AdManager.Instance.ShowBanner();
        }
    }
}
