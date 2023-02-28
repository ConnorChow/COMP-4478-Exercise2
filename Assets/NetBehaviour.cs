using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetBehaviour : MonoBehaviour {

    //Sound Effects
    public AudioClip scoreClip;
    public AudioClip loseClip;
    private AudioSource FeedbackPlayer;

    //holds all droppable Fish/Bomb GameObjects
    public GameObject[] DroppingObjects = new GameObject[5];

    //random generator to drop things
    System.Random random = new System.Random();

    TMP_Text scoreText;
    int score = 0;

    //Display High Score
    TMP_Text highScoreText;
    [SerializeField] private ScoreData scoreData;
    int highScore = 0;

    TMP_Text GameOverText;

    //Hashsets keep track of which GameObjects can be dropped and which can't
    HashSet<int> Droppable = new HashSet<int>();
    HashSet<int> Dropping = new HashSet<int>();
    Dictionary<GameObject, int> objectIndexDictionary = new Dictionary<GameObject, int>();

    //Timer for dropping Fish frequency
    public float dropInterval = 2.0f;
    float timer = 0.0f;

    public float speed = 10.0f;

    //For Starting game
    bool GameStarted = false;
    TMP_Text StartText;

    //For Ending Game
    bool GameOver = false;
    public float GameOverDelay = 2;

    // Start is called before the first frame update
    void Start() {
        //fill the Hashset of droppable
        for (int i = 0; i < DroppingObjects.Length; i++) {
            Droppable.Add(i);
            objectIndexDictionary.Add(DroppingObjects[i], i);
        }

        scoreData = new ScoreData();

        highScore = LoadScore();

        scoreText = GameObject.Find("Score").GetComponent<TMP_Text>();
        highScoreText = GameObject.Find("HighScore").GetComponent<TMP_Text>();

        GameOverText = GameObject.Find("GameOver").GetComponent<TMP_Text>();
        StartText = GameObject.Find("StartMenu").GetComponent<TMP_Text>();

        FeedbackPlayer = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update() {
        float axis = Input.GetAxis("Horizontal");
        if (transform.position.x > 7.5f) {
            if (axis > 0) {
                axis = 0;
            }
        } else if (transform.position.x < -7.5f) {
            if (axis < 0) {
                axis = 0;
            }
        }
        float direction = axis * speed * Time.deltaTime;
        if (!GameOver) {
            transform.position = new Vector3(transform.position.x + direction, transform.position.y, transform.position.z);
        }

        scoreText.text = "Score: " + score;
        highScoreText.text = "High Score: " + highScore;

        timer -= Time.deltaTime;

        if (timer < 0.0f && Droppable.Count > 0 && !GameOver && GameStarted) {
            timer = dropInterval;
            DropNew();
        }

        //Terminate Objects that have fallen past a certain point
        float height;
        int[] RemoveQueue = new int[10];
        int RemovableEntities = 0;
        if (Dropping.Count > 0 && !GameOver && GameStarted) {
            foreach (int i in Dropping) {
                height = DroppingObjects[i].transform.position.y;
                if (height < -7.0f) {
                    RemoveQueue[RemovableEntities] = i; RemovableEntities++;
                }
            }
        }
        if (RemovableEntities > 0 && !GameOver) {
            int removeIndex = 0;
            for (int i = 0; i < RemovableEntities; i++) {
                removeIndex = RemoveQueue[i];
                Recycle(removeIndex);
            }
        }

        //Input Check that starts GamePlay
        if (Input.GetKeyDown("1") && !GameStarted) {
            StartText.enabled = false;
            GameStarted = true;
            dropInterval = 1;
        } else if (Input.GetKeyDown("2") && !GameStarted) {
            StartText.enabled = false;
            GameStarted = true;
            dropInterval = 0.5f;
        } else if (Input.GetKeyDown("3") && !GameStarted) {
            StartText.enabled = false;
            GameStarted = true;
            dropInterval = 0.25f;
        }

        //Timer that resets scene
        if (GameOver) {
            GameOverDelay -= Time.deltaTime;
            if (GameOverDelay <= 0 || Input.GetKeyDown("space")) {
                SaveScore(score);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        //For player to quite game
        if (Input.GetKeyDown("escape")) {
            Application.Quit();
        }
    }

    private void DropNew() {
        int i = Droppable.ElementAt(random.Next(Droppable.Count));

        DroppingObjects[i].transform.position = new Vector3(UnityEngine.Random.Range(-7.0f, 7.0f), 8.0f, DroppingObjects[i].transform.position.z);

        DroppingObjects[i].GetComponent<Rigidbody2D>().WakeUp();
        DroppingObjects[i].GetComponent<Rigidbody2D>().simulated = true;

        DroppingObjects[i].GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        DroppingObjects[i].GetComponent<Rigidbody2D>().angularVelocity = 0;
        //DroppingObjects[i].GetComponent<Rigidbody2D>().inertia = 0;
        DroppingObjects[i].transform.rotation = new Quaternion(0, 0, 0, 0);

        Droppable.Remove(i);
        Dropping.Add(i);
    }

    private void Recycle(int i) {
        DroppingObjects[i].transform.position = new Vector3(0.0f, 15.0f, DroppingObjects[i].transform.position.z);

        DroppingObjects[i].GetComponent<Rigidbody2D>().velocity = Vector3.zero;
        DroppingObjects[i].GetComponent<Rigidbody2D>().simulated = false;

        Droppable.Add(i);
        Dropping.Remove(i);

    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Fish") {
            int i = 0;
            GameObject obj = other.gameObject;

            if (objectIndexDictionary.TryGetValue(obj, out i) && !GameOver) {
                score++;
                FeedbackPlayer.clip = scoreClip; FeedbackPlayer.Play();
                Recycle(i);
            }

        } else if (other.tag == "Bomb") {
            FeedbackPlayer.clip = loseClip;
            if (!GameOver)
                FeedbackPlayer.Play();
            GameOver = true;
            GameOverText.enabled = true;
        }
    }

    //If player gets a new high score, it gets saved to the JSON file
    public void SaveScore(int score) {
        if (score > scoreData.highScore) {
            scoreData.highScore = score;

            string data = JsonUtility.ToJson(scoreData);

            File.WriteAllText(Application.persistentDataPath + "/scoreSheet.json", data);
        }
    }
    //Load recent High score from JSON file
    public int LoadScore() {
        if (File.Exists(Application.persistentDataPath + "/scoreSheet.json") && File.ReadAllText(Application.persistentDataPath + "/scoreSheet.json") != "{}") {
            scoreData = JsonUtility.FromJson<ScoreData>(File.ReadAllText(Application.persistentDataPath + "/scoreSheet.json"));
            return scoreData.highScore;
        }
        return 0;
    }
}

//Allows local game to save current High score
[System.Serializable] public class ScoreData {
    public int highScore;
}