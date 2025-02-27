using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class fishGeneration : MonoBehaviour
{

    //public int fishIndex = 0;
    public GameObject fishprefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //using for testing purposes
        bool spaceKeyPressed = Keyboard.current.spaceKey.wasPressedThisFrame;

        if(spaceKeyPressed) {
            GameObject current = new GameObject();
            //current.name = "npcFISH" + fishIndex.toString();

            //prefab to gen, position to gen, rotation to gen
            Instantiate(fishprefab, new Vector3(0, 0, 0), Quaternion.identity);
            //current.AddComponent<BoxCollider>();
            //fishIndex += 1;
        }
    }
}
