using UnityEngine;
using UnityEngine.Events;

using System.Collections.Generic;

namespace Exact.Example
{
    class DiceDetector : MonoBehaviour
    {
        [SerializeField]
        List<string> tags = new List<string>(6);

        int _number = -1;
        public int Number { get => _number; private set { _number = value; OnDiceUpdate.Invoke(_number); } }

        public UnityEvent<int> OnDiceUpdate;

        string activeTag;
        List<string> inactiveTags = new List<string>();

        public void OnDisconnect()
        {
            Number = -1;
            activeTag = null;
            inactiveTags.Clear();
        }

        public void OnRFIDEnter(string tag)
        {
            if (!tags.Contains(tag)) { return; }

            if (activeTag == null) 
            { 
                activeTag = tag;
                Number = tags.IndexOf(activeTag) + 1;
            }
            else if (!inactiveTags.Contains(tag)) 
            { 
                inactiveTags.Add(tag); 
            }
        }

        public void OnRFIDExit(string tag)
        {
            if(tag == activeTag)
            {
                if(inactiveTags.Count == 0)
                {
                    activeTag = null;
                    Number = -1;
                }
                else
                {
                    activeTag = inactiveTags[0];
                    Number = tags.IndexOf(activeTag) + 1;
                    inactiveTags.RemoveAt(0);
                }
            }
            else if(inactiveTags.Contains(tag))
            {
                inactiveTags.Remove(tag);
            }
        }
    }
}
