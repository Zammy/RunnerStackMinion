using UnityEngine;

public class SimpleLevelGenerator : MonoBehaviour
{
    public GameObject Segment;
    public int SegmentsToCreate = 20;
    public float SegmentSize = 20f;

    void Start()
    {
        for (int i = 0; i < SegmentsToCreate; i++)
        {
            var pos = transform.position;
            pos.z += i * SegmentSize;
            Instantiate(Segment, pos, Quaternion.identity, transform);
        }
    }
}
