using System.Linq;
using UnityEngine;
using Utils;

public class ResetButtonRelocator : MonoBehaviour
{
    public GameObject resetButton;
    public GameObject resetButtonBoundsParent;

    private GameObject _wallAnchor;
    private OVRScenePlane _wallAnchorPlane;

    private float _outFromWallOffset;

    // Start is called before the first frame update
    private void Start()
    {
        if (!resetButton) return;
        var buttonBounds = BoundUtils.GetObjectAndChildrenComponentBounds<Renderer>(
            resetButtonBoundsParent != null ? resetButtonBoundsParent : resetButton
        );
        _outFromWallOffset = buttonBounds.size.y;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!resetButton) return;
        if (!_wallAnchor)
        {
            PickWallAnchor();
        }

        if (!_wallAnchor || !_wallAnchorPlane) return;
        PlaceButtonOnWall();
    }

    private void PickWallAnchor()
    {
        var wallAnchors = GameObject.FindGameObjectsWithTag("WallAnchor");
        var wallAnchorPlanes = wallAnchors
            .Select(a => a.GetComponent<OVRScenePlane>())
            .Where(p => p != null).ToArray();
        if (!wallAnchorPlanes.Any()) return;
        // choose the widest wall
        _wallAnchorPlane = wallAnchorPlanes.Aggregate((agg, next) => next.Width > agg.Width ? next : agg);
        _wallAnchor = _wallAnchorPlane.gameObject;
    }

    private void PlaceButtonOnWall()
    {
        resetButton.transform.rotation =
            Quaternion.LookRotation(-_wallAnchor.transform.up, _wallAnchor.transform.forward);
        var wallPosition = _wallAnchor.transform.position;
        resetButton.transform.position = new Vector3(
            wallPosition.x,
            1.5f,
            wallPosition.z
        ) + _outFromWallOffset * resetButton.transform.up;
    }
}