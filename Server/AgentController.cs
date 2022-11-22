using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RobotData
{
    public string id;
    public float x, y, z;
    public bool tieneCaja;

    public RobotData(string id, float x, float y, float z, bool tieneCaja)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
        this.tieneCaja = tieneCaja;
    }
}

[Serializable]
public class CajaData
{
    public string id;
    public float x, y, z;
    public string tipo;

    public CajaData(string id, float x, float y, float z, string tipo)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
        this.tipo = tipo;
    }
}

[Serializable]
public class AlmacenData
{
    public string id;
    public float x, y, z;
    public int numCajas;

    public AlmacenData(string id, float x, float y, float z, int numCajas)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
        this.numCajas = numCajas;
    }
}
[Serializable]

public class RobotsData
{
    public List<RobotData> positions;

    public RobotsData() => this.positions = new List<RobotData>();
}

[Serializable]

public class CajasData
{
    public List<CajaData> positions;

    public CajasData() => this.positions = new List<CajaData>();
}

[Serializable]

public class AlmacenesData
{
    public List<AlmacenData> positions;

    public AlmacenesData() => this.positions = new List<AlmacenData>();
}


public class AgentController : MonoBehaviour
{
    string url = "http://localhost:8585";
    string getRobotsEndpoint = "/getAgents";
    string getObstaclesEndpoint = "/getObstacles";
    string getCajasEndpoint = "/getCajas";
    string getAlmacenessEndpoint = "/getPilas";
    string sendConfigEndpoint = "/init";
    string updateEndpoint = "/update";
    RobotsData agentsData, obstacleData;
    CajasData cajasData;
    AlmacenesData almacenesData;
    Dictionary<string, GameObject> agents, poralamacenar, gridcaja, idalmacen;
    Dictionary<string, Vector3> prevPositions, currPositions;
    Dictionary<string, int> cantcajas;

    bool updated = false, started = false;

    public GameObject agentPrefab, obstaclePrefab, floor, cajaprefab, almacenprefab, llevandocaja;
    public int NAgents, width, height, cajas, pasos;
    public float velocidad = 5.0f;
    private float tiempo, dt;

    void Start()
    {
        agentsData = new RobotsData();
        obstacleData = new RobotsData();
        cajasData = new CajasData();
        almacenesData = new AlmacenesData();
        prevPositions = new Dictionary<string, Vector3>();
        currPositions = new Dictionary<string, Vector3>();

        agents = new Dictionary<string, GameObject>();
        poralamacenar = new Dictionary<string, GameObject>();
        gridcaja = new Dictionary<string, GameObject>();
        idalmacen = new Dictionary<string, GameObject>();
        cantcajas = new Dictionary<string, int>();

        floor.transform.localScale = new Vector3((float)width / 10, 1, (float)height / 10);
        floor.transform.localPosition = new Vector3((float)width / 2 - 0.5f, 0, (float)height / 2 - 0.5f);

        tiempo = velocidad;

        StartCoroutine(SendConfiguration());
    }

    private void Update()
    {
        if (tiempo < 0)
        {
            tiempo = velocidad;
            updated = false;
            StartCoroutine(UpdateSimulation());
        }

        if (updated)
        {
            tiempo -= Time.deltaTime;
            dt = 1.0f - (tiempo / velocidad);

            foreach (var agent in currPositions)
            {
                Vector3 currentPosition = agent.Value;
                Vector3 previousPosition = prevPositions[agent.Key];

                Vector3 interpolated = Vector3.Lerp(previousPosition, currentPosition, dt);
                Vector3 direction = currentPosition - interpolated;

                agents[agent.Key].transform.localPosition = interpolated;
                if (direction != Vector3.zero) agents[agent.Key].transform.rotation = Quaternion.LookRotation(direction);
                poralamacenar[agent.Key].transform.localPosition = interpolated;
                if (direction != Vector3.zero) poralamacenar[agent.Key].transform.rotation = Quaternion.LookRotation(direction);

            }
        }
    }

    IEnumerator UpdateSimulation()
    {
        UnityWebRequest www = UnityWebRequest.Get(url + updateEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else
        {
            StartCoroutine(GetAgentsData());
            StartCoroutine(GetAlmacenesData());
            StartCoroutine(GetCajasData());
        }
    }

    IEnumerator SendConfiguration()
    {
        WWWForm form = new WWWForm();

        form.AddField("NAgents", NAgents.ToString());
        form.AddField("width", width.ToString());
        form.AddField("height", height.ToString());
        form.AddField("cajas", cajas.ToString());
        form.AddField("pasos", pasos.ToString());

        UnityWebRequest www = UnityWebRequest.Post(url + sendConfigEndpoint, form);
        www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Configuration upload complete!");
            Debug.Log("Getting Agents positions");
            StartCoroutine(GetAgentsData());
            StartCoroutine(GetAlmacenesData());
            StartCoroutine(GetObstacleData());
            StartCoroutine(GetCajasData());
        }
    }

    IEnumerator GetAgentsData()
    {
        UnityWebRequest www = UnityWebRequest.Get(url + getRobotsEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else
        {
            agentsData = JsonUtility.FromJson<RobotsData>(www.downloadHandler.text);

            Debug.Log(www.downloadHandler.text);

            foreach (RobotData agent in agentsData.positions)
            {
                Vector3 newAgentPosition = new Vector3(agent.x, (float)0, agent.z);

                if (!started)
                {
                    prevPositions[agent.id] = newAgentPosition;
                    agents[agent.id] = Instantiate(agentPrefab, newAgentPosition, Quaternion.identity);
                    poralamacenar[agent.id] = Instantiate(llevandocaja, newAgentPosition, Quaternion.identity);
                    poralamacenar[agent.id].SetActive(false);
                }
                else
                {
                    Vector3 currentPosition = new Vector3();
                    if (currPositions.TryGetValue(agent.id, out currentPosition))
                        prevPositions[agent.id] = currentPosition;
                    currPositions[agent.id] = newAgentPosition;
                    if (agent.tieneCaja == true)
                    {
                        agents[agent.id].SetActive(false);
                        poralamacenar[agent.id].SetActive(true);
                    }
                    else
                    {
                        agents[agent.id].SetActive(true);
                        poralamacenar[agent.id].SetActive(false);
                    }


                }
            }
        }
    }

    IEnumerator GetObstacleData()
    {
        UnityWebRequest www = UnityWebRequest.Get(url + getObstaclesEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else
        {
            obstacleData = JsonUtility.FromJson<RobotsData>(www.downloadHandler.text);

            Debug.Log(obstacleData.positions);

            foreach (RobotData obstacle in obstacleData.positions)
            {
                Instantiate(obstaclePrefab, new Vector3(obstacle.x, (float)0.5, obstacle.z), Quaternion.identity);
            }
        }
    }

    IEnumerator GetCajasData()
    {
        UnityWebRequest www = UnityWebRequest.Get(url + getCajasEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else
        {
            cajasData = JsonUtility.FromJson<CajasData>(www.downloadHandler.text);

            Debug.Log(cajasData.positions);

            foreach (CajaData caja in cajasData.positions)
            {
                if (!started)
                {
                    gridcaja[caja.id] = Instantiate(cajaprefab, new Vector3(caja.x, (float)0.0, caja.z), Quaternion.identity);
                    Debug.Log(gridcaja[caja.id]);
                }
                else
                {
                    if (caja.tipo == "vacio")
                    {
                        gridcaja[caja.id].SetActive(false);
                    }
                }
            }
            updated = true;
            if (!started) started = true;
        }
    }

    IEnumerator GetAlmacenesData()
    {
        UnityWebRequest www = UnityWebRequest.Get(url + getAlmacenessEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else
        {
            almacenesData = JsonUtility.FromJson<AlmacenesData>(www.downloadHandler.text);

            Debug.Log(almacenesData.positions);

            foreach (AlmacenData pila in almacenesData.positions)
            {
                if (!started)
                {
                    idalmacen[pila.id] = Instantiate(almacenprefab, new Vector3(pila.x, (float)0.0, pila.z), Quaternion.identity);
                    cantcajas[pila.id] = pila.numCajas;
                }
                else
                {
                    if (pila.numCajas != cantcajas[pila.id])
                    {
                        cantcajas[pila.id] = cantcajas[pila.id] + 1;
                        Instantiate(cajaprefab, new Vector3(pila.x, ((cantcajas[pila.id] * (float)0.94)), pila.z), Quaternion.identity);
                    }
                }
            }
        }
    }



}