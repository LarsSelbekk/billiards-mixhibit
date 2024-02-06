using UnityEngine;

namespace Components
{
    public class TableHolder : MonoBehaviour
    {
        // used to keep track of the objects making up the table experience
        // since grabbed objects are moved to root, checking actual children is not sufficient
        public GameObject[] heldObjects;
    }
}
