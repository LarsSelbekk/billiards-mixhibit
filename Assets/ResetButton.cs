using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        // TODO: game seems to space out instead of properly restarting...
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}