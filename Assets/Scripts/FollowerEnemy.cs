using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowerEnemy : Enemy
{

    float followDistance = 10f;
    float nearFollowDistance = 10f;
    float farFollowDistance = 30f;
    bool near = true;

    float timeBetweenSwaps = 10;
    float timeOfNextSwap = 10;

    protected override void StateMachineSetup()
    {

        // States
        // What states are different?
        // Search for a target point
        // move to target point
        // find targets to shoot at

        // Maybe it should have a number of distinct positions
        // When it's close, shoot small arms fire
        // when far away, shoot mortar fire
        // Or support the other units

        // Can I reuse any of the state classes I already have?
        // Should I be able to?
        // If I can't reuse some, does that mean I should rework them so that I can
        // Or make new ones?

        if(trainEngine == null)
        {
            Debug.Log("Follower StateMachineSetup. No Train");
        }

        _stateMachine = new StateMachine();

        // States
        //var search = new SearchForTrainIntercept(this);
        var follow = new FollowTrain(this);
        var reposition = new Reposition(this);
        var searchOnTrain = new SearchForTargetsOnTrain(this);
        var attack = new AttackFromGround(this);
        // reposition
        //      Has 2 distinct modes. Far away and close
        //      While far away, either support or fire AoE
        //      While close, try to find players and shoot them
        // find player
        // shoot AoE

        // is follow a state?
        // it would have you move towards what your current orbit position is
        // maybe you just always do that and don't need a state?
        // should I update moveTarget every frame?

        // maybe I should use intercept in time
        // but then just move the point 10 to the right or whatever
        // and just keep updating that point when I get close to it
        // Then the horse wouldn't lose distance by moving towards a moving point
        // This might make you lose ground. If you're near the train, the intercept will be close
        // if you doctor the point too much, you'll travel at too much of an angle to the far point
        // and the train will pull away as you're walking sideways
        // recalculate based on time rather than position? still won't help. You start off at the wrong angle
        // new intercept method? maybe. But you're not trying to intercept. You don't need to be at a 
        // certain pos at a certain time
        // instead of being position based, be speed based?
        // try to get to the right position just by changing speed? How?
        // instead of a position, maybe a position delta? And lerp towards it over time. 
        // I want to gain 5 on the x and lose 5 on the z
        // so you move that way frame one, and end up moving 1 on the x, but 2 on the z (bc the train moved)
        // so the lerp compensated by slowing your z movement
        // doesn't really solve the moving straight right problem I don't think
        // +z would have to be interpreted as an increase in speed
        // Maybe the new position has the train's speed added to it
        // then you solve the hypotenuse with your max speed
        // but you don't want to get there before the train
        // you still want to stay allongside the train
        // boids system?
        // have a vector towards your preffered point
        // a vector keeping you from going ahead of the train
        // maybe I can just have the target move smoothly to the new location instead of all at once
        // then the horse can keep up at the right 'height'
        // still has the same problem
        // how does attacking interact?
        // find a target, need to get closer to hit them
        // what distance do you stay from the train?
        // maybe they don't have normal attacks?
        // they have the AoE mortar from range
        // from up close, they just spray a shotgun blast near the player like a bullet hell?

        // Attacking
        // doesn't really work with current AttackHostile
        // it forces you to stop sometimes
        // it tries to find a path to your target (you're not on the nav mesh) 
        // where do I need to be to attack the target? Wherever I can see them?

        // Transitions
        At(follow, reposition, TimeUp());
        At(reposition, follow, True());
        At(follow, searchOnTrain, IsNear());
        At(searchOnTrain, attack, HasHostileTarget());
        At(searchOnTrain, reposition, NoHostileTarget());
        At(attack, reposition, CantSeeTarget());
        // At(search, moveToTrain, HasTrainRZPoint());
        // At(moveToTrain, findPlayer, CanSeePlayer() && closeMode());
        // At(moveToTrain, repositon, !CanSeePlayer() && closeMode()); // how hard should they try to find the player before giving up?
        // At(reposition, shootAoe, farMode()); // when you get to the far orbit
        // At(shootAoe, reposition, farMode() && (mortalOnCD() || outOfMortarAmmo())); // swap back if you've been mortarring for too long or you're out of ammo. Maybe not immediately if you see the player

        // maybe can reuse searchForTargetsOnTrain and AttackHostile
        // need to move HoldPos to OnExit of Board
        // in search:
        // need to make stockpiles optional/ only work while on the train
        // could move the friendly set in train to train manager

        _stateMachine.SetState(follow);


        void At(IState from, IState to, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);

        // Func
        Func<bool> TimeUp() => () =>
        {
            return Time.time > timeOfNextSwap;
        };
        Func<bool> True() => () => true;
        Func<bool> IsNear() => () => near;
        Func<bool> CanSeePlayer() => () =>
        {
            return canSeeHostileTarget;
        };
        Func<bool> CanAttackPlayer() => () => IsNear()() && CanSeePlayer()();
        Func<bool> HasHostileTarget() => () => hostileTarget != null;
        Func<bool> NoHostileTarget() => () => !HasHostileTarget()();
        Func<bool> CantSeeTarget() => () => !canSeeHostileTarget;

    }

    protected override void FixedUpdate()
    {
        if (followingTrain)
        {
            Vector3 trainPos = trainEngine.transform.position;

            float difference = transform.position.x - trainPos.x;
            // if this is positive, you're on the right side

            //TargetMarker.position = trainPos + new Vector3(followDistance * Mathf.Sign(difference), 0, 0);
            Vector3 target = trainPos + new Vector3(followDistance * Mathf.Sign(difference), 0, 0);
            TargetMarker.position = Vector3.MoveTowards(TargetMarker.position, target, moveSpeed);
        }
        base.FixedUpdate();
    }

    internal override void FollowTrain()
    {
        base.FollowTrain();
        followingTrain = true;
    }

    internal override void SwapOrbits()
    {
        base.SwapOrbits();
        near = !near;

        followDistance = near ? nearFollowDistance : farFollowDistance;

        timeOfNextSwap = Time.time + timeBetweenSwaps;
    }
}
