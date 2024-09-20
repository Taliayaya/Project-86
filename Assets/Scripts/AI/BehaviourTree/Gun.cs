using UnityEngine;


public class Gun : MonoBehaviour
{
    public GameObject shotPrefab;
    public Transform[] gunPoints;
    public float fireRate;

    bool firing;
    float fireTimer;

    int gunPointIndex;

    void Update()
    {
        if (firing)
        {
            while (fireTimer >= 1 / fireRate)
            {
                SpawnShot();
                fireTimer -= 1 / fireRate;
            }

            fireTimer += Time.deltaTime;
            firing = false;
        }
        else
        {
            if (fireTimer < 1 / fireRate)
            {
                fireTimer += Time.deltaTime;
            }
            else
            {
                fireTimer = 1 / fireRate;
            }
        }
    }

    void SpawnShot()
    {
        var gunPoint = gunPoints[gunPointIndex++];
        Instantiate(shotPrefab, gunPoint.position, gunPoint.rotation);
        gunPointIndex %= gunPoints.Length;
    }

    public void Fire()
    {
        firing = true;
    }
}
