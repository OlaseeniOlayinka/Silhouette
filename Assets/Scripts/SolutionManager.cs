using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Analytics;

public class SolutionManager : MonoBehaviour
{
    public GameObject[] wallSolutions;
    private Dictionary<CubeScript, int> solutionDict = new Dictionary<CubeScript, int>();
    private Dictionary<CubeScript, int> nonSolutionDict = new Dictionary<CubeScript, int>();

    private LevelManager levelManager;

    // the target solution
    private int targetSolution = 0;
    private bool foundSolution;
    protected float Timer = 0f;

    public void CubeTriggerEnter(bool isSolutionCube, CubeScript cube)
    {
        if (isSolutionCube)
        {
            if (solutionDict.ContainsKey(cube) == false)
            {
                solutionDict.Add(cube, 0);
            }
            solutionDict[cube]++;
        }
        else
        {
            if (nonSolutionDict.ContainsKey(cube) == false)
            {
                nonSolutionDict.Add(cube, 0);
            }
            nonSolutionDict[cube]++;
        }

        CheckSolution();
    }
    public void CubeTriggerExit(bool isSolutionCube, CubeScript cube)
    {
        if (isSolutionCube)
        {
            solutionDict[cube]--;
            if (solutionDict[cube] == 0)
            {
                solutionDict.Remove(cube);
            }
        }
        else
        {
            nonSolutionDict[cube]--;
            if (nonSolutionDict[cube] == 0)
            {
                nonSolutionDict.Remove(cube);
            }
        }

        CheckSolution();
    }

    private void CheckSolution()
    {
        if (foundSolution == false && targetSolution == solutionDict.Keys.Count && nonSolutionDict.Keys.Count == 0)
        {
            Debug.Log("You Win!");
            foundSolution = true;
            levelManager.LoadVictoryScene();
        }
    }

    void Awake()
    {
        foundSolution = false;
        levelManager = GameObject.Find("GameManager").GetComponent<LevelManager>();

        PlayerData.NumberOfSeconds = 0;
        PlayerData.LevelsStarted.Add(PlayerData.CurrentLevel);

        Debug.Log("The current level: " + PlayerData.CurrentLevel);

        AnalyticsSender.SendLevelReachedEvent(PlayerData.CurrentLevel);
        
        foreach (GameObject wallSolution in wallSolutions)
        {
            for (int i = 0; i < wallSolution.transform.childCount; ++i)
            {
                GameObject cube = wallSolution.transform.GetChild(i).gameObject;
                BoxCollider collider = cube.GetComponent<BoxCollider>();

                cube.AddComponent<CubeScript>();
                cube.GetComponent<CubeScript>().manager = this;

                if (cube.CompareTag(CubeScript.SOLUTION_CUBE_TAG))
                {
                    // DEBUG
                    cube.GetComponent<MeshRenderer>().enabled = true;
                    targetSolution++;
                }
                else
                {
                    cube.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Timer += Time.deltaTime;
        if(Timer > 1)
        {
            Timer = 0f;
            PlayerData.NumberOfSeconds += 1;
        }
    }
}
