//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using Advertisement;
using UnityEngine;
using UnityEngine.UI;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class Pause_Menu : MonoBehaviour
    {
        public GameObject pauseMenu;
        public Text Loading;

        public string GarageScene = "Garage";

        [HideInInspector] public bool raceIsStarted = false;

        public void Pause()
        {
            if (raceIsStarted)
            {
                AudioListener.volume = 0;
                Time.timeScale = 0;
                pauseMenu.SetActive(true);
                AdManager.Instance.HideBanner();
            }
        }

        public void Resume()
        {
            AudioListener.volume = 1f;
            Time.timeScale = FindObjectOfType<Race_Manager>().timeScale;
            pauseMenu.SetActive(false);
            AdManager.Instance.ShowBanner();
        }

        public void Restart()
        {
            AudioListener.volume = 1f;
            Time.timeScale = FindObjectOfType<Race_Manager>().timeScale;
            
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            if (operation == null)
            {
                Debug.LogError("SceneManager.LoadSceneAsync operation is null");
                return;
            }
            
            operation.allowSceneActivation = false;
            
            AdManager.Instance.ShowInterstitial(() =>
            {
                Loading.text = "Loading...";
                operation.allowSceneActivation = true;
            });
        }
        
        public void Exit()
        {
            AudioListener.volume = 1f;
            Time.timeScale = FindObjectOfType<Race_Manager>().timeScale;

            if (!raceIsStarted)
            {
                Loading.text = "Loading...";
                UnityEngine.SceneManagement.SceneManager.LoadScene(GarageScene);
                return;
            }
            
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(GarageScene);
            
            if (operation == null)
            {
                Debug.LogError("SceneManager.LoadSceneAsync operation is null");
                return;
            }
            
            operation.allowSceneActivation = false;
            
            AdManager.Instance.ShowInterstitial(() =>
            {
                Loading.text = "Loading...";
                operation.allowSceneActivation = true;
            });
        }
    }
}