using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Snacken/NommianDatabase")]
public class NommianDatabase : ScriptableObject
{
    [System.Serializable]
    public class Nommian
    {
        public GameObject nommianPrefab;

        [Tooltip("Difficulty level of enemy from 1-3 (generally)")]
        public int difficulty;
    }

    public List<Nommian> nommians;
}
