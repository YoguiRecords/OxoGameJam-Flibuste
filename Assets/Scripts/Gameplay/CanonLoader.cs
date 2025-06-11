using UnityEngine;

public class CanonLoader : MonoBehaviour, ILoadable
{
    [field: SerializeField]
    public bool alreadyLoad { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyLoad == false && other.GetComponent<ILoadable>() != null)
        {
            other.GetComponent<ILoadable>().Load(true);
            Load(true);
        }
    }

    public void Load(bool isLoaded)
    {
        alreadyLoad = isLoaded;
    }
}
