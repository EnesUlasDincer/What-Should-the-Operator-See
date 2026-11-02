using UnityEngine;

public class AnchorBinder : MonoBehaviour
{
    [Header("Scene Objects to Anchor")]
    public GameObject object1;
    public GameObject object2;
    public GameObject object3;

    void Start()
    {
        // Place each at world-locked anchors
        CreateAnchorFor(object1);
        CreateAnchorFor(object2);
        CreateAnchorFor(object3);
    }

    void CreateAnchorFor(GameObject target)
    {
        if (target == null) return;

        // Create an empty GameObject at target's current position
        GameObject anchorGO = new GameObject(target.name + "_Anchor");
        anchorGO.transform.position = target.transform.position;
        anchorGO.transform.rotation = target.transform.rotation;

        // Add anchor component and save it
        var anchor = anchorGO.AddComponent<OVRSpatialAnchor>();
        anchor.Save();

        // Reparent your object to the anchor
        target.transform.SetParent(anchorGO.transform, worldPositionStays: true);
    }
}
