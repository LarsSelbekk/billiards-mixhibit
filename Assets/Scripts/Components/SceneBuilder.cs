using UnityEngine;

public class SceneBuilder : MonoBehaviour
{
    public GameObject TablePrefab;

    private static SceneBuilder Instance { get; set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        GameManager.OnReset += Reset;
    }

    private void Start()
    {
        SpawnTable();
    }

    public void Reset()
    {
        ResetTable();
    }

    private void ResetTable()
    {
        foreach (var table in GameObject.FindGameObjectsWithTag("Table"))
        {
            Destroy(table);
        }

        SpawnTable();
    }

    private void SpawnTable()
    {
        var table = Instantiate(TablePrefab);
        table.tag = "Table";
    }
}