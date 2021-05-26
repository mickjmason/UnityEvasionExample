using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EvasionBehaviour : MonoBehaviour
{
    [Tooltip("List of GameObjects that will be treated as enemies")]
    [SerializeField] private List<GameObject> _enemies = new List<GameObject>();
    [Tooltip("Radius of circle in which enemies are considered a threat")]
    [SerializeField] private float _awarenessRadius = 10f;
    [Tooltip("How far forward we will look when calculating evasion points")]
    [SerializeField] private float _hitDistanceThreshold = 5f;
    [Tooltip("The field of view for enemies and us")]
    [SerializeField] private float _fov = 120f;
    [Tooltip("How many rays to cast when checking potential escape routes")]
    [SerializeField] private int _numberOfRays = 5;
    [Tooltip("The minimum distance from the player for an escape vector to be considered good")]
    [SerializeField] private float _minimumEscapeDistance = 2f;
    [Tooltip("Maxiumum number of rays to cast when an escape cannot be found using '_numberOfRays' rays")]
    [SerializeField] private int _panicRays = 15;
    [Tooltip("The radius of the Player object. This would typically be taken from a NavMesh agent")]
    [SerializeField] private float _agentRadius = 1f;

    private bool IsPanicMode = false;
    private List<NavHelper> _sortedEnemiesOfConcern = new List<NavHelper>();
    private Vector3 _escapeVector = new Vector3();
    // Start is called before the first frame update
    void Start()
    {
        // If user has specified even number of rays, add an extra one.
        // This ensures there will always be a ray firing directly ahead
        if(_numberOfRays %2 == 0)
        {
            _numberOfRays++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // NavHelper is a class that allows us to the distance to a target
        // without having to calculate it each time
        List<NavHelper> enemiesOfConcern = new List<NavHelper>();

        // Check which enemies we need to be bothered about
        foreach(var enemy in _enemies)
        {
            // Get the distance to the enemy
            var distance = (transform.position - enemy.transform.position).magnitude;

            //Check if enemy is within our awareness circle
            if (distance < _awarenessRadius)
            {
                // Check if we are in the enemies line of sight (assumed to have the same FOV as us)
                if (SeenByEnemy(enemy))
                {
                    // Add enemy to a list of enemies to consider during evasion
                    enemiesOfConcern.Add(new NavHelper() { Target = enemy, Distance = distance });
                }
            }
        }

        
      
        // if we have any enemies to worry about
        if (enemiesOfConcern.Any())
        {
            // Sort the enemies by their relative distance to us.  Closest first
            _sortedEnemiesOfConcern = enemiesOfConcern.OrderBy(x => x.Distance).ToList();
            // Create a new Vector3 to hold our eventual escapeDirection
            Vector3 escapeDirection = new Vector3();

            // If there is more than one enemy, calculate an escape route based on the level of threat
            // each enemy poses, as determined by their distance from us
            if (_sortedEnemiesOfConcern.Count > 1)
            {
                // Iterate through each enemy and create a weighted escape vector
                // The weighting is determined by taking the distance from us to the enemy
                // as a percentage of the Awareness Radius, and inverting it.
                // So an enemy that was 10% of the awareness radius away from us would have a weighting of 90% applied.
                // An enemy that was 70% of the awareness radius away from us (I.E further), would have a weighting of 30% applied.
                foreach (var closeEnemy in _sortedEnemiesOfConcern)
                {
                    // Get the direction from the enemy to us (that is the best direction for us to move in to escape this single enemy)
                    var enemyEscapeDirection = (transform.position - closeEnemy.Target.transform.position).normalized;
                    // Drawing a ray for visualisation purposes
                    Debug.DrawRay(closeEnemy.Target.transform.position, enemyEscapeDirection * closeEnemy.Distance, Color.red, 1f);
                    // Calculate the weighting.  By subtracting the enemy distance from the radius, we get
                    // a value that is the inverse of the distance from us to the enemy
                    // Divide that value by the awareness radius to give us a decimal representation of the weighting percentage
                    var weighting = (_awarenessRadius - closeEnemy.Distance) / _awarenessRadius;

                    // Multiply the default escape vector (directly away) with the weighting
                    var weightedEscapeVector = enemyEscapeDirection * weighting;

                    // Created a blended vector that is influenced by all enemies
                    escapeDirection += weightedEscapeVector;
                }
            } else
            {
                // If there's only one enemy, then run directly away from it.
                escapeDirection = (transform.position - _sortedEnemiesOfConcern[0].Target.transform.position).normalized;
            }

            // Draw another ray, because they are pretty
            Debug.DrawRay(transform.position, escapeDirection.normalized * 10, Color.magenta, 0.1f);

            // Now we've got a direction to run, we should make sure we aren't going to crash into anything
            _escapeVector = CalculateWaypoint(escapeDirection.normalized, _numberOfRays,_fov);
        }
    }

    private Vector3 CalculateWaypoint(Vector3 preferredDirection, int numRays, float fov)
    {
        // Create a vector to hold our escape destination
        var escapeVector = new Vector3();

            // Create a list of VectorHelpers.  These just contain the direction used in the following raycast and a bool
            // to indicate if the ray hit anything.  We can't just use hit.transform as we still need to work out where the ray 'ends'
            // even if it doesn't hit anything
            List<VectorHelper> hits = new List<VectorHelper>();
            // Subtract 1 from the (odd) number of rays to make sure one is always pointing forward ( 5 rays will give four segments)
            // I.E 120 degree FOV covered by 5 rays, will create 4 segments each 30 degrees in angle
            var angleIncrements = fov / (numRays - 1);
            // Get the starting angle which will be negative of half the field of view (120/2 * -1) = -60 starting angle
            var currentAngle = (fov / 2) * -1;

            // Cast the desired number of Rays
            for(int i = 0; i < numRays; i++)
            {
                // Create a new direction that is currentAngle degrees away from the escape direction we previously calculated
                var modifiedDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * preferredDirection;
                // Cast a ray to see if it hits anything on our obstacle layer (1 << 6)
                Physics.Raycast(transform.position, modifiedDirection, out RaycastHit hit, _hitDistanceThreshold, 1 << 6);
                // I love rays
                Debug.DrawRay(transform.position, modifiedDirection * 5, Color.blue, 0.5f);
                // Create a new VectorHelper to store some pertinent information
                VectorHelper vhelper = new VectorHelper();
                // Set the direction of our ray
                vhelper.Direction = modifiedDirection;
                // And whether it hit anything
                vhelper.IsHit = hit.transform == null ? false : true;
                vhelper.Distance = hit.distance;
                // Add this VectorHelper to our list for future processing
                hits.Add(vhelper);
                
                currentAngle += angleIncrements;
            }

            // Our favourite escape routes should be ones that don't hit anything
            //Get any destinations that didn't hit an obstacle
            var clearVectors = hits.Where(x => x.IsHit == false).ToList();
        if (clearVectors.Any())
        {
            // Calculate the best escape route from the list of routes that don't collide with obstacles
            escapeVector = GetBestEscapeVector(preferredDirection, clearVectors);
        }
        else
        {
            // If we are here, it means all our routes collide with something, so let's just use them all
            escapeVector = GetBestEscapeVector(preferredDirection, hits);
        }

        return escapeVector;
    }

    private Vector3 GetBestEscapeVector(Vector3 preferredDirection,List<VectorHelper> vHelpers)
    {
        // Create some zero valued placeholders that will be used to store the details of the current
        // best escape route
        var currentDistance = 0f;
        Vector3 currentEscapeVector = new Vector3();
        // Iterate through the VectorHelpers and select the one that is furthest from an enemy
        foreach (var vHelper in vHelpers)
        {
            // Get the proposed escape destination
            Vector3 pos;
            if (vHelper.IsHit)
            {
                pos = transform.position + (vHelper.Direction * (vHelper.Distance - _agentRadius));
            } else
            {
                pos = transform.position + (vHelper.Direction * _hitDistanceThreshold);
            }

            // Get the closest enemy by using Linq to sort the enemies by distance from our
            // proposed escape destination (pos - x.Target.transform.position).magnitude
            // use .First() to pick the first one in that ascendingly sorted list (I.E the closest)
            var closestEnemy = _sortedEnemiesOfConcern.OrderBy(x => (pos - x.Target.transform.position).magnitude).First();
            // Reclaculate the distance (there's probably a more efficient way to do this)
            var distanceToEnemy = (pos - closestEnemy.Target.transform.position).magnitude;

            // If the distance to the enemy is more than the currently greatest distance, then let's store
            // this distance and escape vector instead
            if (distanceToEnemy > currentDistance)
            {
                currentDistance = distanceToEnemy;
                currentEscapeVector = pos;
            }
        }


        // If our current escape route is close than our desired minimum distance
        if ((currentEscapeVector - transform.position).magnitude < _minimumEscapeDistance)
        {   //  if we aren't in panic mode, invoke it and recast for a destination with more rays
            // and a wider field of view
            if (!IsPanicMode)
            {
                IsPanicMode = true;
                currentEscapeVector = CalculateWaypoint(preferredDirection, _panicRays, 360);
            } else
            // If we are already in PanicMode, turn it off and just use the escape vector that we've
            // generated as it means we still couldn't find a good route even when using _panicRays 
            // number of rays
            {
                IsPanicMode = false;
            }

        }


        // By this point we have found the escape destination that will take us away from our
        // current biggest threat, without crashing into something whilst still maintaining as much distance from
        // all enemies as possible
        return currentEscapeVector;
    }

    private bool SeenByEnemy(GameObject enemy)
    {
        var inView = false;
        var directionToPlayer = transform.position - enemy.transform.position;
        var angleToPlayer = Vector3.Angle(directionToPlayer, enemy.transform.forward);

        var fovExtent = _fov / 2;
        if(angleToPlayer >= -fovExtent && angleToPlayer <= fovExtent)
        {
            inView = true;
        }

        return inView;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 0.1f);
        Gizmos.DrawSphere(transform.position, _awarenessRadius);
        Gizmos.color = new Color(1f, .5f, .5f, 1f);
        Gizmos.DrawSphere(_escapeVector, 1);

    }
}
