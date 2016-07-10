using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float speed;
	public float x;
	private Rigidbody2D rb;

	
	void Start ()
	{
		Debug.Log("PlayerController->Start()");
		rb = GetComponent<Rigidbody2D>();
	}
	// Update is called once per frame
	void FixedUpdate () {
		x = Input.GetAxis ("Horizontal");
		Vector2 movementVector = new Vector2(x,0);
		rb.AddForce(movementVector * speed);
	}
	public void Move(Vector2 direction, float speed)
	{
		direction.x = direction.x / Mathf.Abs(direction.x);
		//rb.AddForce (direction * speed);
		transform.Translate (direction * speed * Time.deltaTime);
	}
	public void Move(Vector3 direction, float speed)
	{
		Move(new Vector2(direction.x,0),speed);
	}
}
