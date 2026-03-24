//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ALIyerEdon;

namespace ALIyerEdon
{
	public class CarSelect : MonoBehaviour
	{

		// SpawnPoint
		public Transform spawnPoint;

		// Cars prefabs array
		public GameObject[] cars;

		// Each Car value
		public int[] carPrices;

		// Lock Icon,Shop window,Buy button
		public GameObject lockIcon, buyButton, Price, noEnoughScores;
		public GameObject purchaseUI;
		// public GameObject colorPicker;

		// Display total scores
		public Text TotalScores;

		// Selected car ID
		[HideInInspector] public int ID;

		//TotalScore text, car value text
		public Text carPriceText;

		public LevelSelect levelSelect;

		// SetActive(true) loading window before start loading level
		public GameObject Loading;


		void Start()
		{

			// Read lastest car selected ID before    
			ID = PlayerPrefs.GetInt("CarID");

			// Update total scores display
			TotalScores.text = "Total Score: " + PlayerPrefs.GetInt("TotalScores").ToString();

			// Distroy all players car before instantiate the new one
			Destroy(GameObject.FindGameObjectWithTag("Player"));

			// Instantiate last selected car by saved ID
			Instantiate(cars[ID], spawnPoint.position, spawnPoint.rotation);

			// if (FindObjectOfType<Car_Color>())
			// 	colorPicker.SetActive(true);
			// else
			// 	colorPicker.SetActive(false);

			// Enable or disable buy , lock and car price displays
			if (PlayerPrefs.GetInt("Car" + ID.ToString()) == 3)
			{
				lockIcon.SetActive(false);
				buyButton.SetActive(false);
				Price.SetActive(false);
			}
			else
			{
				lockIcon.SetActive(true);
				buyButton.SetActive(true);
				Price.SetActive(true);
			}

			// Update current car value text
			carPriceText.text = carPrices[ID].ToString() + " $";

			// Update current car is locked or not
			if (PlayerPrefs.GetInt("Car" + ID.ToString()) == 3)
			{
				lockIcon.SetActive(false);
				buyButton.SetActive(false);
			}
			else
			{
				lockIcon.SetActive(true);
				buyButton.SetActive(true);
			}
		}

		// Public function for NextCar select button in menu
		public void NextCar()
		{
			if (ID < cars.Length - 1)
				ID++;

			PlayerPrefs.SetInt("CarID", ID);

			// Distroy all players car before instantiate the new one
			Destroy(GameObject.FindGameObjectWithTag("Player"));

			ID = PlayerPrefs.GetInt("CarID");

			// Instantiate last selected car by saved ID
			Instantiate(cars[ID], spawnPoint.position, spawnPoint.rotation);

			if (PlayerPrefs.GetInt("Car" + ID.ToString()) == 3)
			{
				lockIcon.SetActive(false);
				buyButton.SetActive(false);
				Price.SetActive(false);
			}
			else
			{
				lockIcon.SetActive(true);
				buyButton.SetActive(true);
				Price.SetActive(true);
			}

			carPriceText.text = carPrices[ID].ToString() + " $";

			// if (FindObjectOfType<Car_Color>())
			// 	colorPicker.SetActive(true);
			// else
			// 	colorPicker.SetActive(false);
		}
		// Public function for PrevCar select button in menu
		public void PrevCar()
		{

			if (ID > 0)
				ID--;

			PlayerPrefs.SetInt("CarID", ID);

			Destroy(GameObject.FindGameObjectWithTag("Player"));


			ID = PlayerPrefs.GetInt("CarID");

			// Instantiate last selected car by saved ID
			Instantiate(cars[ID], spawnPoint.position, spawnPoint.rotation);

			if (PlayerPrefs.GetInt("Car" + ID.ToString()) == 3)
			{
				lockIcon.SetActive(false);
				buyButton.SetActive(false);
				Price.SetActive(false);
			}
			else
			{
				lockIcon.SetActive(true);
				buyButton.SetActive(true);
				Price.SetActive(true);
			}

			carPriceText.text = carPrices[ID].ToString() + " $";

			// if (FindObjectOfType<Car_Color>())
			// 	colorPicker.SetActive(true);
			// else
			// 	colorPicker.SetActive(false);
		}

		public void Buy_CurrentCar()
		{
			purchaseUI.SetActive(true);
		}

		// Buy current selected car
		public void BuyCar()
		{

			// Check player have enough money
			if (carPrices[ID] <= PlayerPrefs.GetInt("TotalScores"))
			{

				PlayerPrefs.SetInt("Car" + ID.ToString(), 3);

				// Reduce current car price from the total scores
				PlayerPrefs.SetInt("TotalScores",
					PlayerPrefs.GetInt("TotalScores") - carPrices[ID]);


				// Disable lock icon for current car
				lockIcon.SetActive(false);

				// Disable Buy button for current car
				buyButton.SetActive(false);

				// Disable car price text
				Price.SetActive(false);

				purchaseUI.SetActive(false);

				// Update total scores display
				TotalScores.text = "Total Score: " + PlayerPrefs.GetInt("TotalScores").ToString();
			}
			else
			{
				// Show the shop offer window
				noEnoughScores.SetActive(true);
			}
		}

		// Select current car
		public void SelectCar()
		{
			if (PlayerPrefs.GetInt("Car" + ID.ToString()) == 3)
			{
				// Set current selected car ID for instantiate in main level    
				PlayerPrefs.SetInt("CarID", ID);

				Loading.SetActive(true);

				SceneManager.LoadSceneAsync(levelSelect.levelNames[PlayerPrefs.GetInt("LevelID")]);
			}
		}
	}
}