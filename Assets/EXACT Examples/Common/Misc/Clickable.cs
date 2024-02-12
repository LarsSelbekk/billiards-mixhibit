using UnityEngine;
using UnityEngine.Events;

namespace Exact.Example
{
    public class Clickable : MonoBehaviour
    {
        public UnityEvent MouseDown;
        public UnityEvent MouseUp;

        private void OnMouseDown()
        {
            MouseDown.Invoke();
        }

        private void OnMouseUpAsButton()
        {
            MouseUp.Invoke();
        }
    }
}
