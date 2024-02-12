using UnityEngine;
using UnityEngine.UI;

namespace Exact.Example
{
    [RequireComponent(typeof(Text))]
    public class NumberText : MonoBehaviour
    {
        Text text;

        private void Awake()
        {
            text = GetComponent<Text>();
            text.text = "";
        }

        public void SetNumber(int number)
        {
            if (number < 0) 
            { 
                text.text = ""; 
            }
            else 
            { 
                text.text = number.ToString(); 
            }
        }
    }
}
