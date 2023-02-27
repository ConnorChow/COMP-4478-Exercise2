using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DroppingObjectBehaviour : MonoBehaviour {

    [SerializeField] private float repeatDelay = 0.25f;
    float time;
    private AudioSource soundPlayer;

    // Start is called before the first frame update
    void Start() {
        soundPlayer = GetComponent<AudioSource>();
        time = repeatDelay;
    }

    // Update is called once per frame
    void Update() {
        time -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (time <= 0 && collision.gameObject.tag != "Fish" && collision.gameObject.tag != "Bomb") {
            soundPlayer.Play();
            time = repeatDelay;
        }
    }
}
