using UnityEngine;
using UnityEngine.Events;

namespace Exact.Example
{
    class ScoreKeeper : MonoBehaviour
    {
        int _score = 0;
        public UnityEvent<int> OnScoreUpdate;
        public int Score 
        { 
            get => _score; 
            set 
            { 
                _score = value;
                OnScoreUpdate.Invoke(_score);

                if(Score > HighScore)
                {
                    HighScore = Score;
                }
            }
        }
        

        int _highscore = 0;
        public UnityEvent<int> OnHighScoreUpdate;
        public int HighScore 
        { 
            get => _highscore; 
            set 
            { 
                _highscore = value; OnHighScoreUpdate.Invoke(_highscore); 
            } 
        }

        private void Start()
        {
            Reset();
        }

        public void Reset()
        {
            Score = 0;
            HighScore = 0;
        }
    }
}
