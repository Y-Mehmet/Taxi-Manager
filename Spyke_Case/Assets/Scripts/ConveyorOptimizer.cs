using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optimizes conveyor belt performance by limiting visible passengers
/// and culling off-screen objects
/// </summary>
public class ConveyorOptimizer : MonoBehaviour
{
    [Header("Performance Settings")]
    [Tooltip("Maximum number of active passengers on conveyor at once")]
    public int maxActivePassengers = 12;
    
    [Tooltip("Check for off-screen passengers every N seconds")]
    public float cullCheckInterval = 0.5f;
    
    [Header("Culling Settings")]
    [Tooltip("Distance from camera to disable passengers")]
    public float cullDistance = 15f;
    
    private Camera mainCamera;
    private float lastCullCheck = 0f;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        // Periodic culling check
        if (Time.time - lastCullCheck > cullCheckInterval)
        {
            lastCullCheck = Time.time;
            CullOffScreenPassengers();
        }
    }
    
    /// <summary>
    /// Disable passengers that are too far from camera or off-screen
    /// </summary>
    private void CullOffScreenPassengers()
    {
        if (ConveyorBelt.Instance == null || mainCamera == null) return;
        
        var passengers = ConveyorBelt.Instance.GetComponentsInChildren<PassengerGroup>(true);
        
        foreach (var passenger in passengers)
        {
            if (passenger == null) continue;
            
            // Calculate distance from camera
            float distance = Vector3.Distance(passenger.transform.position, mainCamera.transform.position);
            
            // Check if in camera view
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(passenger.transform.position);
            bool isInView = viewportPoint.z > 0 && 
                           viewportPoint.x > -0.1f && viewportPoint.x < 1.1f && 
                           viewportPoint.y > -0.1f && viewportPoint.y < 1.1f;
            
            // Disable if too far or off-screen
            bool shouldBeActive = distance < cullDistance && isInView;
            
            if (passenger.gameObject.activeSelf != shouldBeActive)
            {
                passenger.gameObject.SetActive(shouldBeActive);
            }
        }
    }
}
