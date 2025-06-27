using UnityEngine;

/// <summary>
/// This script creates a trail at the location of a gameobject with a particular width and color.
/// </summary>

public class CreateTrail : MonoBehaviour
{
    public GameObject trailPrefab = null;
    public SpawnFromList spawnFromList; 
    public GameObject currentObject;

    private float width = 0.05f;
    private Color color = Color.white;

    public GameObject currentTrail = null;

    void Start()
    {
        currentObject = spawnFromList.currentObject;
    }

    public void SetCurrentObject()
    {
        currentObject = spawnFromList.currentObject;
    }

    public void StartTrail()
    {
            currentTrail = Instantiate(trailPrefab, transform.position, transform.rotation, transform);
            ApplySettings(currentTrail);
    }

    private void ApplySettings(GameObject trailObject)
    {
        TrailRenderer trailRenderer = trailObject.GetComponent<TrailRenderer>();
        trailRenderer.widthMultiplier = width;
        trailRenderer.startColor = color;
        trailRenderer.endColor = color;
    }

    public void EndTrail()
    {
        if (currentTrail)
        { 
            currentTrail.transform.parent = currentObject.transform;
            currentTrail = null;
        }
    }

    public void SetWidth(float value)
    {
        width = value;
    }

    public void SetColor(Color value)
    {
        color = value;
    }
}
