using UnityEngine;

namespace V2X
{
    /// <summary>
    /// Example script showing how to set up a V2X intersection with stop zones.
    /// This is for reference only - you would set this up manually in the Unity editor.
    /// </summary>
    public class IntersectionSetupExample : MonoBehaviour
    {
        [Header("Intersection Setup")]
        [Tooltip("The RSU that manages this intersection")]
        public StopRSU rsu;
        
        [Tooltip("Stop zones for each approach that needs to stop")]
        public StopZone[] stopZones;
        
        [Tooltip("Intersection zone that tracks vehicles inside the intersection")]
        public BoxCollider intersectionZone;

        void Start()
        {
            // Ensure intersection zone is set to trigger
            if (intersectionZone != null)
            {
                intersectionZone.isTrigger = true;
            }
            
            // Connect stop zones to RSU
            foreach (var stopZone in stopZones)
            {
                if (stopZone != null)
                {
                    stopZone.rsu = rsu;
                }
            }
        }

        // Visual debugging
        void OnDrawGizmosSelected()
        {
            if (rsu != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(rsu.transform.position, 2f);
            }
            
            if (intersectionZone != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(intersectionZone.transform.position, intersectionZone.size);
            }
        }
    }
} 