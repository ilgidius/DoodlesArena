using UnityEngine;
using System.Collections;

public class GeneralAttack
{

	public int Damage=10;
	public float Range=100;
	public int InitiativeMin = 80;
	public int InitiativeMax = 120;
	private int _AttackId=0;
	public virtual int GetInitiative()
	{
		return Random.Range(InitiativeMin,InitiativeMax);
	}
	public GeneralAttack(int attackId){
		_AttackId = attackId;
	}
	public GeneralAttack(){}
	public int AttackId
	{
		get{
			return _AttackId;
		}
	}
}
