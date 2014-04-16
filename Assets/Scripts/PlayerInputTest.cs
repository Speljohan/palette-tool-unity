using UnityEngine;
using System.Collections;

public class PlayerInputTest : MonoBehaviour {

	float moveSpeed = 10f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		float x = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
		float y = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

		GetComponent<CharacterController>().Move(new Vector3(x, y, 0));
		Camera.main.transform.position = new Vector3(x, y, 0);
	}
}
