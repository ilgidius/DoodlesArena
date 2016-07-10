using UnityEngine;
using System.Collections;

public interface ICharacterAnimator
{
	void PlayAttack (GeneralAttack attack);
	void PlayBlock();
	float Speed{ set; get; }
	bool isWalk{ set; get;}
	void FUpdate (ProcessState state) ;

}
public class CharacterAnimator : ICharacterAnimator {

	private Animator _Animator;
	// Update is called once per frame
	public void FUpdate(ProcessState state) 
	{
		_Animator.SetBool ("Walk", isWalk);
		_Animator.SetInteger("AIState", (int)state);

	}
	public CharacterAnimator(Animator animator)
	{
		_Animator = animator;
	}
	public void PlayAttack(GeneralAttack attack)
	{
		_Animator.SetFloat ("AttackType", attack.AttackId);
		_Animator.SetTrigger ("Attack");
	}
	public void PlayBlock()
	{
		_Animator.SetTrigger ("Block");
	}
	public float Speed{ set; get; }

	public bool isWalk{ set; get; }


}
