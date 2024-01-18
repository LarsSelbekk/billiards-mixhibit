using System;
using UnityEngine;
using Utils;

public class TableRelocator : MonoBehaviour
{
    public GameObject table;
    public GameObject tableBoundsParent;

    private Vector3 _tableOriginalScale;
    private Bounds _tableBounds;
    private GameObject _tableAnchor;
    private OVRScenePlane _tableAnchorPlane;

    // Start is called before the first frame update
    private void Start()
    {
        if (!table) return;
        _tableOriginalScale = table.transform.localScale;
        _tableBounds = BoundUtils.GetObjectAndChildrenComponentBounds<Renderer>(
            tableBoundsParent != null ? tableBoundsParent : table
        );
    }


    // Update is called once per frame
    private void Update()
    {
        if (!table) return;
        if (!_tableAnchor)
        {
            _tableAnchor = GameObject.FindGameObjectWithTag("TableAnchor");
            try
            {
                _tableAnchorPlane = _tableAnchor.GetComponent<OVRScenePlane>();
            }
            catch (NullReferenceException)
            {

            }
        }
        if (!_tableAnchor || !_tableAnchorPlane) return;
        PlaceOnPhysicalTable();
    }

    private void PlaceOnPhysicalTable()
    {
        table.transform.position = _tableAnchor.transform.position;
        var anchorRotation = Quaternion.LookRotation(-_tableAnchor.transform.up);
        float requiredScaling;
        var shouldRotate90 = _tableAnchorPlane.Height > _tableAnchorPlane.Width;
        if (shouldRotate90)
        {
            table.transform.rotation = anchorRotation * Quaternion.Euler(0, 90, 0);
            requiredScaling =
                Math.Min(
                    _tableAnchorPlane.Height / _tableBounds.size.x,
                    _tableAnchorPlane.Width / _tableBounds.size.z
                );
        }
        else
        {
            table.transform.rotation = anchorRotation;
            requiredScaling =
                Math.Min(
                    _tableAnchorPlane.Width / _tableBounds.size.x,
                    _tableAnchorPlane.Height / _tableBounds.size.z
                );
        }
        table.transform.localScale = _tableOriginalScale * requiredScaling;
    }
}
