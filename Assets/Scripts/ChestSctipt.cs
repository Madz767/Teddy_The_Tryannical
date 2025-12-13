using UnityEngine;

//==========================================
//           Chest Script
//==========================================
//what this handles: chest interaction, item drops, state persistence
//why this is separate: keeps chest logic modular and reusable
//what this interacts with: GameManager for state persistence



public class ChestSctipt : MonoBehaviour
{


    [Header("Interaction")]
    public float interactionRadius = 1.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("State")]
    public string chestID;
    public bool isOpen = false;

    [Header("Drops")]
    public GameObject[] dropPrefabs;
    public Transform dropPoint;

    [Header("SpriteControl")]
    //for the open visual
    public GameObject openChestVisual;
    //for the closed visual
    public GameObject closedChestVisual; 

    private Transform player;
    private bool hasDropped = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;



        //FROM - TODD :)
        //NOTE 1: Make sure each chest has a unique chestID set in the inspector not in code

        //NOTE 2: This requires a GameManager script with a collectedChests HashSet<string>
        //NOTE 2.25: See the GameManager script for an example implementation
        //NOTE 2.5: So don't forget to add the game manager to your scene

        //NOTE 3: This does not save like a dark souls game, it only persists during the current play session
        //NOTE 3.5: so don't treat this like it will always be there, it won't be, just like my sleep schedule


        //this is the chest checking if it has been collected before
        //allowing for persistence across scenes
        if (GameManager.Instance.collectedChests.Contains(chestID))
        {
            isOpen = true;
            hasDropped = true;
        }

        UpdateVisual();
    }
    private void Update()
    {
        if (isOpen || player == null)
            return;

        if (Input.GetKeyDown(interactKey) && PlayerInRange())
        {
            OpenChest();
        }
    }

    private bool PlayerInRange()
    {
        return Vector2.Distance(transform.position, player.position) <= interactionRadius;
    }

    private void OpenChest()
    {
        isOpen = true;

        if (!hasDropped)
        {
            DropItems();
            hasDropped = true;
        }

        GameManager.Instance.collectedChests.Add(chestID);
        UpdateVisual();
    }

    private void DropItems()
    {
        if (dropPoint == null)
            dropPoint = transform;

        foreach (GameObject item in dropPrefabs)
        {
            Instantiate(item, dropPoint.position, Quaternion.identity);
        }
    }

    private void UpdateVisual()
    {
        if (openChestVisual != null)
            openChestVisual.SetActive(isOpen);

        if (closedChestVisual != null)
            closedChestVisual.SetActive(!isOpen);
    }


    private void OnDrawGizmosSelected()
    {
        //I really like this, it helps a lot
        //this is really cool and i love whoever made this
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }


}
