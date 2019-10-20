using UnityEngine;

public class FishInstantiator : MonoBehaviour
{
    public GameObject prefab;

    [Range(0, 300)]
    public int number;

    private void Start()
    {
        for (int i = 0; i < number; i++)
        {
            Instantiate(prefab, new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5)), Quaternion.identity, this.transform);
        }
    }
}