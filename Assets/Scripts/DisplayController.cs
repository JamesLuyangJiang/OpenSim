using EVP;
using UnityEngine;
using UnityEngine.UI;    // If using the default Text UI
// using TMPro;         // If using TextMeshPro

public class DisplayController : MonoBehaviour
{
    public Rigidbody carRigidbody;
    public VehicleStandardInput vehicleInput;
    public Text speedText;
    public Text driveGearText;
    public Text reverseGearText;

    // Conversion from m/s to mph:
    // 1 m/s ~= 2.23694 mph
    private float conversionRate = 2.23694f;

    void Update()
    {
        // ------ Speed Display ------
        // Get the speed in m/s
        float speedInMetersPerSecond = carRigidbody.velocity.magnitude;

        // Convert to mph
        float speedInMph = speedInMetersPerSecond * conversionRate;

        // Update text (round or format as you like)
        speedText.text = Mathf.RoundToInt(speedInMph).ToString() + " mph";

        // ------ Gear Display ------
        if (vehicleInput.reverse) {
            Color driveGearColor = driveGearText.color;
            Color reverseGearColor = reverseGearText.color;
            reverseGearColor.a = 1f; // Set alpha to 100% for reverse gear
            driveGearColor.a = 0.25f; // Set alpha to 50% for drive gear
            reverseGearText.color = reverseGearColor;
            driveGearText.color = driveGearColor;
        }
        else
        {
            Color driveGearColor = driveGearText.color;
            Color reverseGearColor = reverseGearText.color;
            driveGearColor.a = 1f; // Set alpha to 100% for drive gear
            reverseGearColor.a = 0.25f; // Set alpha to 50% for reverse gear
            driveGearText.color = driveGearColor;
            reverseGearText.color = reverseGearColor;
        }
    }
}
