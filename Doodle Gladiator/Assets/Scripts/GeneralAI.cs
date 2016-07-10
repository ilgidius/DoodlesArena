using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;


public enum ProcessState
{
	Idle,//0
	Any,//1
	Attacking,//2
	Walking,//3
	Running,//4
	Blocking,//5
	Harming,//6
	Dodging,//7
	Action,//8
	Dead,//9
	Waiting//10
}

public enum Command
{
	Stop,
	Move,
	Attack,
	Block,
	Harm,
	Stay,
	Die
}
#region AI state machine
public class AIStateMachine
{
	class StateTransition
	{
		readonly ProcessState CurrentState;
		readonly Command Command;
		public Action Logic=(()=>{});
		
		public StateTransition (ProcessState currentState, Command command)
		{
			CurrentState = currentState;
			Command = command;
		}
		public void Invoke ()
		{
			if (Logic != null) {
				Logic.Invoke ();
			}
		}
		
		public override int GetHashCode ()
		{
			return 17 + 23 * CurrentState.GetHashCode() + 31 * Command.GetHashCode();
		}
		
		public override bool Equals (object obj)
		{
			StateTransition other = obj as StateTransition;
			return other != null && this.CurrentState == other.CurrentState && this.Command == other.Command;
		}
	}
	public readonly string Id = "AIStateMachine:"+Guid.NewGuid().ToString();
	private ProcessState _CurrentState;
	Dictionary<StateTransition, ProcessState> transitions;

	public ProcessState CurrentState {
		get	{
			return _CurrentState;
			} private set{
			_CurrentState=value;

		}}
	
	public AIStateMachine ()
	{
		CurrentState = ProcessState.Idle;
		transitions = new Dictionary<StateTransition, ProcessState>
		{
			{ new StateTransition(ProcessState.Idle, Command.Stop), ProcessState.Waiting },
			{ new StateTransition(ProcessState.Idle, Command.Move), ProcessState.Walking },
			{ new StateTransition(ProcessState.Idle, Command.Attack), ProcessState.Attacking},
			{ new StateTransition(ProcessState.Idle, Command.Block), ProcessState.Blocking},
			{ new StateTransition(ProcessState.Idle, Command.Harm), ProcessState.Harming},
			{ new StateTransition(ProcessState.Idle, Command.Stay), ProcessState.Idle},
			{ new StateTransition(ProcessState.Idle, Command.Die), ProcessState.Dead},
			{ new StateTransition(ProcessState.Attacking, Command.Stay), ProcessState.Idle},
			{ new StateTransition(ProcessState.Blocking, Command.Stay), ProcessState.Idle},
			{ new StateTransition(ProcessState.Walking, Command.Stay), ProcessState.Idle},
			{ new StateTransition(ProcessState.Harming, Command.Stay), ProcessState.Idle}
		};
	}
	public AIStateMachine(Action func):base(){
		foreach (var item in transitions) 
		{
			item.Key.Logic=func;
		}
	}
	public ProcessState GetNext (Command command)
	{
		StateTransition transition; 
		ProcessState nextState=CurrentState;
		int temp1 = (int)command;
		try {
			//Command stop always switches to "waiting" state
			if (command == Command.Stop) {
				transition = new StateTransition (ProcessState.Idle, command);
			} 
			//From "waiting" we can go to any other state. Similar to "idle"
			else if(CurrentState.Equals(ProcessState.Waiting)){
				transition = new StateTransition (ProcessState.Idle, command);
			}
			else {
				transition = new StateTransition (CurrentState, command);
			}
			KeyValuePair<StateTransition,ProcessState> kvp = (from item in transitions
			        where item.Key.Equals(transition)
					select item).FirstOrDefault ();
			if (kvp.Key!=null) {
				kvp.Key.Invoke ();
				nextState = kvp.Value;
				//Debug.Log (string.Format ("Found Key:{0}",kvp.Key.GetHashCode().ToString()));
							}
			else{
				Debug.LogError ("Invalid transition: State: " + CurrentState + " -> Command: " + command);
			}
		} catch (Exception ex) {
			Debug.LogError ("Invalid transition: " + CurrentState + " -> " + command);
		}
		int temp2 = (int)nextState;
		return nextState;
	}
	
	public ProcessState MoveNext (Command command)
	{
		Debug.Log (string.Format ("{0}->MoveNext:State {1},Command {2}",Id,CurrentState.ToString(),command.ToString()));
		CurrentState = GetNext (command);
		return CurrentState;
	}
}
#endregion 

public class GeneralAI : MonoBehaviour
{

	const float DISTANCE_EPSELON = 100;
	private GameObject _GameObject;
	private Character _CharacterInfo;
	private PlayerController _PlayerController;
	private List<Character> _Enemies = new List<Character> ();
	private ICharacterAnimator _Animator;
	private AIStateMachine _StateMachine;
	private Character _CurrentTarget;
	public bool isPlayer =false;
	private bool _isAIEnable=true;
	public bool _isAnimationFinished=true;
	//private State _State = State.Idle;
	#region Animation Conditions variables
	private int _AttackType;
	private int _BlockType;
	#endregion
	// Use this for initialization
	void Start ()
	{
		Debug.Log("GeneralAI->Start()");
		_StateMachine = new AIStateMachine();
		_GameObject = GetComponent<GameObject> ();
		_CharacterInfo = transform.root.GetComponent<Character> ();
		_PlayerController = transform.root.GetComponent<PlayerController> ();
		_Animator = new CharacterAnimator (GetComponent<Animator> ());
		StartCoroutine("GeneralFlow"); 
	}
	void Update ()
	{
	_Animator.FUpdate (_StateMachine.CurrentState);
	}
	// Update is called once per frame
	void FixedUpdate ()
	{
	}
	protected virtual bool FindEnemy ()
	{
		bool result = false;
		GameObject[] enemiesObjects;
		if (isPlayer) {
			enemiesObjects = GameObject.FindGameObjectsWithTag ("Enemy");
		} else {
			enemiesObjects = GameObject.FindGameObjectsWithTag ("Player");
		}
		if (enemiesObjects.Length > 0) {

			foreach (var item in enemiesObjects) {
				Character enemyObject = item.transform.root.GetComponent<Character> ();
				if(!_Enemies.Contains(enemyObject)&&enemyObject.HitPoints>=0){
				enemyObject.Attack+=HandleIncomingAttack;
					_Enemies.Add (enemyObject);
				}
			}
			_Enemies.RemoveAll((x)=>{return x.HitPoints<=0;});
			result = true;
			_CurrentTarget= _Enemies.FirstOrDefault();
		} 
		return result;
	}
	private float GetDistanceToEnemy (Character enemy)
	{
		float distance = Mathf.Infinity;
		distance = Vector3.Distance (enemy.Position, transform.position) - enemy.Size;
		return distance;
	}
	protected void MoveToEnemy (Character enemy, float distanceBeforeEnemy)
	{
		Vector3 diff = enemy.Position - transform.position;
		if (distanceBeforeEnemy < DISTANCE_EPSELON) {
			distanceBeforeEnemy = DISTANCE_EPSELON;
		}
		if (GetDistanceToEnemy (enemy) > distanceBeforeEnemy) {
			_StateMachine.MoveNext(Command.Move);
			_PlayerController.Move (diff, _CharacterInfo.Speed);
		} else {
			_StateMachine.MoveNext(Command.Stop);
		}
	}
	protected  virtual IEnumerator GeneralFlow ()
	{
		yield return new WaitForSeconds(1);
		while (_isAIEnable) {
			if (_isAnimationFinished&&
			    (_StateMachine.CurrentState == ProcessState.Idle ||
			 _StateMachine.CurrentState == ProcessState.Walking)) {
				FindEnemy ();
				if (_Enemies.Any ()) {

					float distance = GetDistanceToEnemy (_CurrentTarget);
					GeneralAttack attack = SelectAttackType ();

					if (distance > attack.Range) {
						MoveToEnemy (_CurrentTarget, DISTANCE_EPSELON);
						yield return new WaitForSeconds(.1f);
						continue;
					} else {
						_StateMachine.MoveNext (Command.Stop);
						if (GeneralFormulas.isAgrression (_CharacterInfo.Aggression)) {	
							Attack (_CurrentTarget, attack);
						} else {
							yield return new WaitForSeconds(1);
							_StateMachine.MoveNext (Command.Stay);
						}
					}
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
		yield return null;
	}
	public void onAnimationFinished()
	{
		Debug.Log (string.Format ("Animation Finished: SM id",_StateMachine.Id));
		_StateMachine.MoveNext (Command.Stay);
		_isAnimationFinished = true;
	}
	public void onAnimationFinished2(string animationName)
	{
		Debug.Log (string.Format ("Animation {0} Finished: SM id{1}",animationName,_StateMachine.Id));
		_StateMachine.MoveNext (Command.Stay);
		_isAnimationFinished = true;
	}
	public void onAnimationStarted()
	{
		Debug.Log (string.Format ("Animation Stated: SM id {0}",_StateMachine.Id));
		_isAnimationFinished = false;
	}
	public void onAnimationStarted2(string animationName)
	{
		Debug.Log (string.Format ("Animation {0} Stated: SM id{1}",animationName,_StateMachine.Id));
		_isAnimationFinished = false;
	}
	protected void Attack (Character target, GeneralAttack attack)
	{
		_CharacterInfo.SendAttack (attack);
			_StateMachine.MoveNext(Command.Attack);
			_Animator.PlayAttack(attack);
	}
	protected void HandleIncomingAttack(GeneralAttack attack)
	{
		_StateMachine.MoveNext(Command.Stop);
		int attInitiative = attack.GetInitiative ();
		if (_CharacterInfo.Aggression < attInitiative) 
		{
			Defense (attack);
		}
	}
	private void HandleIncomingDamage(GeneralAttack attack)
	{

		_CharacterInfo.HitPoints -= attack.Damage;
		if (_CharacterInfo.HitPoints <= 0) {
			_StateMachine.MoveNext (Command.Die);
			_isAIEnable = false;
			//destroy object
		} else {
			_StateMachine.MoveNext(Command.Harm);
		}
	}

	private void Defense (GeneralAttack attack)
	{
		if (UnityEngine.Random.Range (0, 100) > 50) {
			_StateMachine.MoveNext(Command.Block);
			_Animator.PlayBlock();

		} else {
			HandleIncomingDamage(attack);

		}
	}
	private GeneralAttack SelectAttackType()
	{
		GeneralAttack attackResult;
		attackResult = new GeneralAttack ();
		return attackResult;
	}

}
