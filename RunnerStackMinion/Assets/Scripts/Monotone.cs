using UnityEngine;

public class Monotone<T> : MonoBehaviour
{
    public static T I { get; private set; }
    protected virtual void Awake()
    {
        I = GetComponent<T>();
    }
}