using UnityEngine;

namespace Exact
{
    [CreateAssetMenu(menuName = "EXACT/Settings", fileName = "EXACTSettings")]
    public class Settings : ScriptableObject
    {
        public string host = "192.168.4.1";
        public int port = 1883;
    }
}
