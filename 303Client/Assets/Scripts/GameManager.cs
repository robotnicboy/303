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
    public GameObject enemyPrefab;
    public GameObject bulletPrefab;
    public GameObject missilePrefab;

    //Refences
    public GameObject ball;

    public static float GameTime = 0;

    private void Update()
    {
       ball.GetComponent<Object>().LinearPrediction(); // Run ball prediciton in the game manager since its not owned by any single client
       GameTime += Time.deltaTime;
       Menu.instance.UpdateGameTime(GameTime);
    }

    //Instantiaters
    public GameObject InstantiatePlayer(Vector3 position) 
    {
        return Instantiate(playerPrefab, position, Quaternion.identity);
    }

    public GameObject InstantiateEnemy(Vector3 position) 
    {
        return Instantiate(enemyPrefab, position, Quaternion.identity);
    }

    public GameObject InstantiateBullet(Vector3 position, Quaternion rotation) 
    {
        return Instantiate(bulletPrefab, position, rotation);
    }

    public GameObject InstantiateMissile(Vector3 position, Quaternion rotation) 
    {
        return Instantiate(missilePrefab, position, rotation);
    }

    //Server Recieve Functions
    public void BallPosition(Packet packet)
    {
        float time = packet.ReadFloat();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        ball.GetComponent<Object>().AddMessage(position, rotation, time);

    }

}
