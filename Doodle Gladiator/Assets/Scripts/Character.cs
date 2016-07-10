using UnityEngine;
using System.Collections;

public delegate void AttackDelegate(GeneralAttack attack);
public class Character : MonoBehaviour {

	public int HitPoints=100;
	public string Type;
	public float Speed=10;
	public int Aggression=100;
	private BoxCollider2D _Colider;
	private Transform _transform;
	private GeneralAI _AIController;

	// Use this for initialization
	void Start () 
	{
		Debug.Log("Caracter->Start()");
		_Colider = GetComponent<BoxCollider2D>();
		_transform = GetComponent<Transform>();
		_AIController = GetComponent<GeneralAI>();
	}
	public  event  AttackDelegate Attack;
	public BoxCollider2D GetColider()
	{
		return _Colider;
	}
	public float Size
	{
		get
		{
			return _Colider.size.x/2;
		}
	}
	public Vector3 Position
	{
		get
		{
			return _transform.position;
		}
	}
	public void SendAttack(GeneralAttack attack)
	{
		if (Attack != null) {
			Attack(attack);
		}
	}

}
