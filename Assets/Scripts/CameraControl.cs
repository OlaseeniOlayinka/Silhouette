using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class CameraControl : MonoBehaviour
{
    // The main board that the camera rotates around
    public GameObject board;
    public GameObject gameManager;
    public GameObject arrowKeys;
    public float ROTATE_SPEED_DRAG = 50.0f;
    public float angleMax = 43.0f;

    private int ignoreEdgeLayer;
    private int defaultLayer;
    private int invisibleLayer;
    private int invisibleSelectedLayer;

    private const float ROTATE_SPEED = 100.0f;
    private const float MAX_RAYCAST_DIST = 1000f;
    private const string BOX_TAG = "Box";
    private const string POLY_TAG = "Poly";
    private bool keyPressed = false;
    private bool mouseDragging = false;
    private bool isVictorySceneLoaded = false;

    public static event Action ClickEvent;
    public static event Action DeselectEvent;

    private Vector3 dragOrigin;
    private Vector3 initialVector = Vector3.forward;

    public static bool isTutorial = false;

    private void DeselectAllPolys()
    {
        if (gameManager.GetComponent<PlayerMovement>().SelectedPolyIndex >= 0)
        {
            foreach (Transform child in gameManager.GetComponent<PlayerMovement>().SelectedPoly.transform)
            {
                child.gameObject.layer = (child.gameObject.layer == invisibleSelectedLayer || child.gameObject.layer == invisibleLayer) ? invisibleLayer : ignoreEdgeLayer;
                
            }
            DeselectEvent?.Invoke();
        }

        gameManager.GetComponent<PlayerMovement>().SelectedPolyIndex = -1;
    }

    private void SelectPoly(GameObject poly)
    {
        DeselectAllPolys();
        var polys = gameManager.GetComponent<PlayerMovement>().allPolygons;
        var index = System.Array.IndexOf(polys, poly);
        gameManager.GetComponent<PlayerMovement>().SelectedPolyIndex = index;

        foreach (Transform child in poly.transform)
        {
            child.gameObject.layer = (child.gameObject.layer == invisibleSelectedLayer || child.gameObject.layer == invisibleLayer) ? invisibleSelectedLayer : defaultLayer;
            
        }
        ClickEvent?.Invoke();
    }

    private void SelectPoly(int step)
    {
        var polys = gameManager.GetComponent<PlayerMovement>().allPolygons;
        var currentIndex = gameManager.GetComponent<PlayerMovement>().SelectedPolyIndex;
        int count = currentIndex + step;
        if (count < 0)
        {
            count = polys.Length - 1;
        }
        else if (count >= polys.Length)
        {
            count = 0;
        }

        SelectPoly(polys[count], count);
    }

    private void SelectPoly(GameObject poly, int index)
    {
        DeselectAllPolys();
        gameManager.GetComponent<PlayerMovement>().SelectedPolyIndex = index;

        foreach (Transform child in poly.transform)
        {
            child.gameObject.layer = (child.gameObject.layer == invisibleSelectedLayer || child.gameObject.layer == invisibleLayer) ? invisibleSelectedLayer : defaultLayer;
        }

    }

    void Awake()
    {
        ignoreEdgeLayer = LayerMask.NameToLayer("Ignore Edge Detection");
        defaultLayer = LayerMask.NameToLayer("Default");
        invisibleLayer = LayerMask.NameToLayer("Invisible");
        invisibleSelectedLayer = LayerMask.NameToLayer("InvisibleSelected");
    }

    // Start is called before the first frame update
    void Start()
    {
        if (board.transform != null)
        {
            initialVector = transform.position - board.transform.position;
            initialVector.y = 0;
        }
        isTutorial = false;
        StartCoroutine(FixEdges());
    }

    IEnumerator FixEdges()
    {
        yield return new WaitForFixedUpdate();
        GetComponent<EdgeDetect>().normalsSensitivity = 1f;
    }

    public static void EnableTutorial() {
        isTutorial = true;
    }

    // Update is called once per frame
    void Update()
    {
        var isMoving = gameManager.GetComponent<PlayerMovement>().IsMoving;

        if (!isMoving && !isVictorySceneLoaded)
        {

            // Pressing mouse 1 AND not pressing the buttons
            if (Input.GetMouseButtonDown(0) && UnityEngine.EventSystems.EventSystem.current != null &&
            !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                RaycastHit[] hits;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                hits = Physics.RaycastAll(ray, MAX_RAYCAST_DIST).Where(hit => hit.transform.gameObject.CompareTag(BOX_TAG)).ToArray();
                if (hits.Length > 0) // Hit a "Box" -> Select it
                {
                    System.Array.Sort(hits, (hit1, hit2) => hit1.distance < hit2.distance ? -1 : 1);
                    SelectPoly(hits[0].transform.parent.gameObject);
                }
                else if(!isTutorial)// Clicked on nothing -> Deselect selected box
                {
                    DeselectAllPolys();
                }
                dragOrigin = Input.mousePosition;
                mouseDragging = true;
                return;
            }
        
            if (Input.GetKeyDown(KeyCode.Tab) && !isTutorial && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                SelectPoly(1);
            }

            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.Tab) && !isTutorial)
            {
                SelectPoly(-1);
            }
        }


        /* CAMERA STUFF */
        float rotateDegrees = 0f;

        // Pressing LeftArrow -> Rotate the camera left
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotateDegrees += ROTATE_SPEED * Time.deltaTime;
            keyPressed = true;
        }

        // Pressing RightArrow -> Rotate the camera right
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            rotateDegrees -= ROTATE_SPEED * Time.deltaTime;
            keyPressed = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            mouseDragging = false;
        }

        // Rotate the camera if key is pressed or mouse0 is down
        if (board.transform != null && (keyPressed || mouseDragging))
        {

            if (mouseDragging)
            {
                Vector3 dragVector = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
                rotateDegrees = ROTATE_SPEED_DRAG * dragVector.x;
                dragOrigin = Input.mousePosition;
            }

            // rotates the Camera & UI buttons
            Vector3 currentVector = transform.position - board.transform.position;
            currentVector.y = 0;
            float angleBetween = Vector3.Angle(initialVector, currentVector) * (Vector3.Cross(initialVector, currentVector).y > 0 ? 1 : -1);
            float newAngle = Mathf.Clamp(angleBetween + rotateDegrees, -angleMax, angleMax);
            rotateDegrees = newAngle - angleBetween;
            PlayerData.DegreesCameraRotated += Mathf.Abs(rotateDegrees);
            this.transform.RotateAround(board.transform.position, Vector3.up, rotateDegrees);
            arrowKeys.transform.RotateAround(arrowKeys.transform.position, Vector3.forward, rotateDegrees);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene.name == LevelManager.VICTORY_SCENE_NAME || scene.name == LevelManager.TUTORIAL_COMPLETE_SCENE_NAME)
        {
            DeselectAllPolys();
            isVictorySceneLoaded = true;
        }

    }

    void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == LevelManager.VICTORY_SCENE_NAME || scene.name == LevelManager.TUTORIAL_COMPLETE_SCENE_NAME)
        {
            isVictorySceneLoaded = false;
        }

    }

    void OnEnable()
    {
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}
