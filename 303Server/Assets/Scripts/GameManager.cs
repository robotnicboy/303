
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Instance implementation so that the class can be acessed from anywhere but also only have one version of itself
    public static GameManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    //Prefabs
    public GameObject playerPrefab;
    public GameObject bulletPrefab;
    public GameObject missilePrefab;
    public GameObject area;

    //References
    public GameObject ball;

    private void FixedUpdate()
    {
        try
        {   
            //Update Ball Position on all clients
            Server.SpherePosition(ball); // TODO test without the catch
        }
        catch
        {

        }
       
    }

    //Instantiaters
    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Player>();
    }

    public Bullet InstantiateBullet(Vector3 position, Quaternion rotation)
    {
        return Instantiate(bulletPrefab, position, rotation).GetComponent<Bullet>();
    }

    public Missile InstantiateMissile()
    {
        return Instantiate(missilePrefab,new Vector3(0,0,0), Quaternion.identity).GetComponent<Missile>();
    }

    public GameObject InstantiateArea(Vector3 position)
    {
        return Instantiate(area, position, Quaternion.identity);
    }

}
