
using System.Collections.Generic;
using UnityEngine;


public class Movement
{
    //Movement Class is for store information about previous player movements
    public Movement(int newID, bool[] newInputs, Transform newTransform)
    {
        id = newID;
        inputs = newInputs;
        transform = newTransform;
    }

    public int id;
    public bool[] inputs;
    public Transform transform;
}


public class Controller : MonoBehaviour
{
    //Inspector Components
    public CharacterController controller;
    [SerializeField] Transform CameraDirection;

    //Initilize Member Variables
    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public int id = 0;
    private float missileCoolDown = 5f;
    private float timer = 5f;
    private bool[] playerMovements = new bool[5];



    public Dictionary<int, Movement> movements = new Dictionary<int, Movement>();
    int movementID = 0;

    public void Start()
    {
        //Movement will take place in fixed timestep so mutiply by fixedDeltatime to keep these values consistant with normal update
        moveSpeed *= Time.fixedDeltaTime;
    }

    public void Init(int playerID)
    {
        id = playerID;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Mouse0)) // shoot bullet
        {
            Client.PlayerShoot(CameraDirection.forward);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && timer > missileCoolDown) // only allow player to shoot the player after cooldown has ended
        {
            timer = 0; // handle cool downs client side to reduce the amount of messages need to send
            Client.PlayerMissileShoot(CameraDirection.forward);
        }

    }

    //Send Inputs to Server at a fixed rate as to not throttle the socket
    private void FixedUpdate()
    {
        SendInputToServer();
    }

    private void SendInputToServer()
    {
        playerMovements = new bool[] // Store the key presses in a bool as its the most efficient way to send the data
        {
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
            Input.GetKey(KeyCode.Space)
        };

        AddMovement(playerMovements); // Store the actions the player took during this fixed update
        Client.PlayerMovement(id, movementID, playerMovements); //Send the player movement to the server
        movementID++; // increase the id to give each movement its own id
    }

    private Vector3 Move(bool[] inputs, Transform transform)
    {
        //Convert from bool form into a vector2 we can use
        Vector2 inputDirection = Vector2.zero;
        if (inputs[0])
        {
            inputDirection.y += 1;
        }
        if (inputs[1])
        {
            inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            inputDirection.x += 1;
        }

        //move wasd in refernce to where the player is looking
        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        moveDirection *= moveSpeed;

        return moveDirection;
    }

    public void AddMovement(bool[] inputs)
    {
        movements[movementID] = new Movement(movementID, inputs, transform);
    }

    public void CheckMovement(int id, Vector3 newPosition, Quaternion newRotation)
    {
        List<int> toBeRemoved= new List<int>(); // Store all movements that to be removed since we cant remove them as we are going

        if(movements.ContainsKey(id)) // check to see if the movement has already been processed and deleted e.g if the packets were out of order
        {
            foreach (var movement in movements) 
            {
                if(movement.Key <= id) 
                {
                    toBeRemoved.Add(movement.Key); // Delete all old movement data that has alread been process by the server
                }
            }

            foreach (int removement in toBeRemoved)
            {
                movements.Remove(removement); // now remove the movements after iterating
            }


            controller.enabled = false; //disable controller to allows us to manually move the player
            transform.position = newPosition; // update position
            transform.rotation = newRotation; // update rotation
            controller.enabled = true;

            //Once we have set the position we cyle through all the movements left and apply them to the player again
            //We do this so we can move immediately even with high latency
            //Then as the server is catching up and validating out movements it should eventually reach out predicted positon
            //however if the server movement deviates from the client movement then we may end up in an unexpected place

            foreach (var movement in movements)
            {
                controller.Move(Move(movement.Value.inputs, movement.Value.transform));
            }

        }
    }

}




