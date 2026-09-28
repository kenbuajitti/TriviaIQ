using UnityEngine;
public class TspGameController : MonoBehaviour
{
    void Start() { gameObject.AddComponent<TriviaIQApp>().Initialize(true); }
}
