# UnityEvasionExample
This is a demo scene that demonstrates the behaviours ability to calculate an evasion path against multiple pursuers and obstacles.  It is intended to be used as the basis for an evasion action in a behaviour tree but could be applied independently.  When multiple pursuers are engaged against the player, the evasion route will be a weighted average of the evasion routes from each enemy.  Enemies closer to the player will have a greater influence on the evasion route.

## How to view
Load the project and run it.  Switch to scene view.

The sphere in the center represents the player object.  The grey sphere around it is the players awareness radius.  Anything outside this radius will be not be considered a threat and will not trigger evasion behaviour.

The EvasionBehaviour component on the sphere maintains a list of enemies (the blue cubes).  If an object isn't in this list it won't ever be evaulated as a threat.

The enemies have two lines protruding from them to signify their field of view.  If the player is not in the field of view of an enemy, even if the enemy inside the awareness radius, the enemy will not be evaluated as a threat.

Try moving one of the enemies inside the awareness radius and rotate it so that the player comes into its field of view.
The player will now draw a magenta line which represents the optimum evasion route away from the greatest threat.  The player will also draw 5 rays.  These are centered around the optimum evasion route and will check for obstacles.

The red/brown sphere represents the point that the player has selected as its evasion vector and would be where the player would move to if it were under the control of a NavMeshAgent.

Now try moving and rotating a second enemy so that the player treats it as a threat.  The evasion route will be adjusted to optimally avoid both pursuers.  Move one pursuer close to the player and you'll notice the evasion route updates to move away from this closer enemy.

Add more enemies and see how it reacts.  Eventually you'll notice that the red/brown sphere is no longer on the magenta line.  Despite being the optimum route for evasion from the greatest threats, it's possible this route might point towards a distant (and less concerning) threat.  Therefore the behaviour picks the ray cast that results in the greatest distance from the closest enemy (closest in relation to the potential evasion vectors).

Move the green obstacle cubes into the scene.  These are on the StaticEnvironment layer so can be hit by the raycasts.  Try moving one into the path of a ray.

Try blocking all the rays.  If all routes are blocked then the ray which results in the greatest distance from the closest enemy will be selected AS LONG AS the distance to that evasion vector is greater than the minimum escape distance.  If none of the potential escape vectors are further than the minimum distance the behaviour will invoke panic mode. 

## Panic Mode
Panic mode will cause the player to cast 15 (configurable number) rays through a 360 arc.  The best vector will be selected based on its distance from the closest enemy. Panic mode will then be turned off.

If you block all the rays emanating from the player, you will see it flip between panic mode and normal mode.  In a non-demo situation the player would move to the 'panic' chosen destination before re-evaluating so this flipping wouldn't happen.
