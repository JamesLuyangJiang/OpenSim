using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using EVP;
using DG.Tweening;

public class ScenarioSelector : MonoBehaviour
{
    public List<GameObject> carModels; // Assign the car prefab in the inspector
    private GameObject carObj; // The car object to be spawned
    public RectTransform spawnPoint1; // Assign spawn point for option 1
    public RectTransform spawnPoint2; // Assign spawn point for option 2
    public Canvas uiCanvas; // Assign your Canvas here
    private Camera carCamera; // Assign the car camera dynamically or find it in code

    public GameObject scenarioUI; // Drag the scenarioUI object here
    public GameObject vehicleUI; // Drag the vehicleUI object here
    public GameObject inGameUI; // Drag the InGameUI object here

    public NpcController spawner;

    public GameObject arrowGroup1; // The parent object holding all arrows for scenario 1
    public GameObject arrowGroup2; // The parent object holding all arrows for scenario 2
    private GameObject[] arrows;
    public float fadeDistance = 10f; // Distance at which arrows start fading

    private int scenarioIndex = 0; // Index to track the current scenario

    // -------Initialization-------
    void Start()
    {
        carObj = carModels[0]; // Default to the first car model
        DeactivateVehicleAudio();
    }

    public void SetScenarioIndex(int index)
    {
        scenarioIndex = index;

        if (vehicleUI != null)
        {
            vehicleUI.SetActive(true);
        }
        if (scenarioUI != null)
        {
            scenarioUI.SetActive(false);
        }
    }

    public void SetCar(int index)
    {
        // Safety check to ensure 'index' is within the list range
        if (index < 0 || index >= carModels.Count)
        {
            Debug.LogWarning("Index is out of range!");
            return;
        }

        for (int i = 0; i < carModels.Count; i++)
        {
            // Activate the object if its index matches; deactivate otherwise
            carModels[i].SetActive(i == index);
        }

        // Assign the car prefab to the carObj variable
        carObj = carModels[index];

        ActivateVehicleAudio();

        if (scenarioIndex == 1)
        {
            SpawnCar(spawnPoint1);
            SpawnArrows(arrowGroup1);
        }
        else if (scenarioIndex == 2)
        {
            SpawnCar(spawnPoint2);
            SpawnArrows(arrowGroup2);
        }
    }

    // -------Car and Arrow Spawning-------
    private void SpawnCar(Transform spawnPoint)
    {
        SpawnNPC();
        // Spawn the car at the spawn point
        RectTransform carRectTransform = carObj.GetComponent<RectTransform>();
        carRectTransform.position = spawnPoint.position;
        carRectTransform.rotation = spawnPoint.rotation;

        // Find the camera in the car
        Transform cameraTransform = carObj.transform.Find("Cameras_Mirrors/EgoCarMainCamera");
        if (cameraTransform != null)
        {
            carCamera = cameraTransform.GetComponent<Camera>();
            if (carCamera != null)
            {
                SwitchToCarCamera(carCamera);
            }
            else
            {
                Debug.LogWarning("EgoCarMainCamera does not have a Camera component!");
            }
        }
        else
        {
            Debug.LogWarning("EgoCarMainCamera not found under Car/Cameras_Mirrors!");
        }
    }

    private void SpawnArrows(GameObject group)
    {
        // Clear the references in the arrows array
        if (arrows != null)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                arrows[i] = null;  // Remove the reference to each GameObject
            }
        }

        // Get all arrows under the arrowGroup
        arrows = new GameObject[group.transform.childCount];
        for (int i = 0; i < group.transform.childCount; i++)
        {
            arrows[i] = group.transform.GetChild(i).gameObject;
        }

        foreach (GameObject arrow in arrows)
        {
            arrow.SetActive(true);
        }
    }

    void Update()
    {
        if (arrows == null)
        {
            return;
        }
        foreach (GameObject arrow in arrows)
        {
            // Get the distance to the player
            float distance = Vector3.Distance(carObj.transform.position, arrow.transform.position);

            // If the car is close enough, start fading the arrow
            if (distance < fadeDistance)
            {
                ArrowFade fadeScript = arrow.GetComponent<ArrowFade>();
                fadeScript.StartFading();
            }

            if (distance < 1f)
            {
                ArrowFade fadeScript = arrow.GetComponent<ArrowFade>();
                fadeScript.SetArrowTransparency(0f);
            }
        }
    }

    public void ResetArrows(bool deactivate)
    {
        foreach (GameObject arrow in arrows)
        {
            // Reset transparency and disable all arrows
            ArrowFade fadeScript = arrow.GetComponent<ArrowFade>();
            fadeScript.ResetFade();
            arrow.SetActive(!deactivate); // Do not deactivate arrows during in-game reset
        }
    }

    private void SpawnNPC()
    {
        spawner.StartAllCars();
    }

    private void ResetNPC()
    {
        spawner.ResetAllCars();
    }

    // -------Button Event Listeners-------
    // Event listener for the in game Menu button
    public void OnMenuButtonClicked(bool finish = false)
    {
        // ResetNPC();
        if (!finish) ResetArrows(true);
        DeactivateVehicleAudio();

        // Reset the ego vehicle's position and rotation
        VehicleController vehicleController = carObj.GetComponent<VehicleController>();
        vehicleController.ResetVehicle();

        Transform cameraTransform = carObj.transform.Find("Cameras_Mirrors/EgoCarMainCamera");
        CameraSwitch cameraSwitch = cameraTransform.GetComponent<CameraSwitch>();
        if (cameraSwitch != null)
        {
            cameraSwitch.ResetCamera();
        }

        // Show the initial menu UI and hide the in-game UI
        if (scenarioUI != null)
        {
            scenarioUI.SetActive(true);
        }

        if (inGameUI != null)
        {
            inGameUI.SetActive(false);
        }
    }

    // Event listener for the Back button
    public void OnBackButtonClicked()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnBackScenarioButtonClicked()
    {
        // Show the initial menu UI and hide the in-game UI
        if (scenarioUI != null)
        {
            scenarioUI.SetActive(true);
        }

        if (vehicleUI != null)
        {
            vehicleUI.SetActive(false);
        }
    }

    public void OnResetButtonClicked()
    {
        ResetArrows(false);
        // Reset the ego vehicle's position and rotation
        VehicleController vehicleController = carObj.GetComponent<VehicleController>();
        vehicleController.ResetVehicle();

        // Spawn the car at the spawn point
        RectTransform carRectTransform = carObj.GetComponent<RectTransform>();
        RectTransform spawnPoint = scenarioIndex == 1 ? spawnPoint1 : spawnPoint2;
        carRectTransform.position = spawnPoint.position;
        carRectTransform.rotation = spawnPoint.rotation;

        Transform cameraTransform = carObj.transform.Find("Cameras_Mirrors/EgoCarMainCamera");
        CameraSwitch cameraSwitch = cameraTransform.GetComponent<CameraSwitch>();
        if (cameraSwitch != null)
        {
            cameraSwitch.ResetCamera();
        }
    }

    // -------Helpers-------
    // Activate the Vehicle Audio of the ego vehicle
    public void ActivateVehicleAudio()
    {
        VehicleAudio egoVehicleAudio = carObj.GetComponent<VehicleAudio>();
        if (egoVehicleAudio != null)
        {
            egoVehicleAudio.enabled = true;
        }
    }

    // Deactivate the Vehicle Audio of the ego vehicle
    public void DeactivateVehicleAudio()
    {
        VehicleAudio egoVehicleAudio = carObj.GetComponent<VehicleAudio>();
        if (egoVehicleAudio != null)
        {
            egoVehicleAudio.enabled = false;
        }
    }
    
    private void SwitchToCarCamera(Camera carCamera)
    {
        // Enable the car's camera
        carCamera.enabled = true;
        carCamera.targetDisplay = 0;
        carCamera.gameObject.SetActive(true); // Ensure the camera GameObject is active

        // Update the canvas to use the new camera
        if (uiCanvas != null)
        {
            uiCanvas.worldCamera = carCamera;
        }

        // Hide the initial menu UI and show the in-game UI
        if (vehicleUI != null)
        {
            vehicleUI.SetActive(false);
        }

        if (inGameUI != null)
        {
            inGameUI.SetActive(true);
        }
    }
}
