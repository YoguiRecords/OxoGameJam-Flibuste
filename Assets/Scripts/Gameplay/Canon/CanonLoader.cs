using UnityEngine;

public class CanonLoader : MonoBehaviour, ILoadable
{
    [field: SerializeField]
    public bool alreadyLoad { get; set; }

    [SerializeField]
    private int m_maxBallstock;

    [field: SerializeField]
    public int currentBallStock { get; set; }

    private void OnTriggerEnter(Collider other)
    {
        if (currentBallStock < m_maxBallstock && other.GetComponent<ILoadable>() != null)
        {
            if(currentBallStock < m_maxBallstock)
            {
                currentBallStock++;
            }
            other.GetComponent<ILoadable>().Load(true);
            Load(true);
            currentBallStock = 0;
        }
    }

    public void Load(bool isLoaded)
    {
        alreadyLoad = isLoaded;
    }
}
