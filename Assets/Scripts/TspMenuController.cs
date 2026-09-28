using UnityEngine;
public class TspMenuController : MonoBehaviour
{
    void Start() { gameObject.AddComponent<TriviaIQApp>().Initialize(false); }
}
