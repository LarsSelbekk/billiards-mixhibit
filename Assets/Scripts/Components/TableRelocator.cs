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

#if UNITY_EDITOR
    private void Awake()
    {
        // don't use SSAs in editor
        Destroy(this);
    }
#endif

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
        if (table == null) return;
        if (_tableAnchor == null)
        {
            _tableAnchor = GameObject.FindGameObjectWithTag("TableAnchor");
            if (!_tableAnchor) return;
            _tableAnchorPlane = _tableAnchor.GetComponent<OVRScenePlane>();
        }

        if (_tableAnchor == null || _tableAnchorPlane == null) return;
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